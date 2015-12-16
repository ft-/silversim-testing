// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;
using SilverSim.Types;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types.Grid;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.Management.Scene;
using log4net;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types.Estate;

namespace SilverSim.Main.Cmd.Region
{
    #region Service Implementation
    public class RegionCommands : IPlugin
    {
        readonly string m_RegionStorageName;
        readonly string m_EstateServiceName;
        GridServiceInterface m_RegionStorage;
        SceneFactoryInterface m_SceneFactory;
        EstateServiceInterface m_EstateService;
        private static readonly ILog m_Log = LogManager.GetLogger("REGION COMMANDS");
        private string m_ExternalHostName = string.Empty;
        private uint m_HttpPort;
        private string m_Scheme = Uri.UriSchemeHttp;

        public RegionCommands(string regionStorageName, string estateServiceName)
        {
            m_RegionStorageName = regionStorageName;
            m_EstateServiceName = estateServiceName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs["Network"];
            if (config != null)
            {
                m_ExternalHostName = config.GetString("ExternalHostName", "SYSTEMIP");
                m_HttpPort = (uint)config.GetInt("HttpListenerPort", 9000);

                if (config.Contains("ServerCertificate"))
                {
                    m_Scheme = Uri.UriSchemeHttps;
                }
            }

            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            m_RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");
            Common.CmdIO.CommandRegistry.CreateCommands.Add("region", CreateRegionCmd);
            Common.CmdIO.CommandRegistry.ShowCommands.Add("regions", ShowRegionsCmd);
            Common.CmdIO.CommandRegistry.EnableCommands.Add("region", EnableRegionCmd);
            Common.CmdIO.CommandRegistry.DisableCommands.Add("region", DisableRegionCmd);
            Common.CmdIO.CommandRegistry.StartCommands.Add("region", StartRegionCmd);
            Common.CmdIO.CommandRegistry.StopCommands.Add("region", StopRegionCmd);
        }

        public void ShowRegionsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            IEnumerable<RegionInfo> regions;

            if (args[0] == "help")
            {
                io.Write("show regions ([enabled|disabled|online|offline])");
                return;
            }
            else if(args.Count < 3)
            {
                regions = m_RegionStorage.GetAllRegions(UUID.Zero);
            }
            else if(args[2] == "enabled")
            {
                regions = m_RegionStorage.GetOnlineRegions();
            }
            else if (args[2] == "disabled")
            {
                regions = from rInfo in m_RegionStorage.GetAllRegions(UUID.Zero) where (rInfo.Flags & RegionFlags.RegionOnline) == 0 select rInfo;
            }
            else if (args[2] == "online")
            {
                List<RegionInfo> regionList = new List<RegionInfo>();
                foreach(SceneInterface scene in Scene.Management.Scene.SceneManager.Scenes.Values)
                {
                    regionList.Add(scene.RegionData);
                }
                regions = regionList;
            }
            else if (args[2] == "offline")
            {
                List<UUID> onlineRegions = new List<UUID>();
                
                List<RegionInfo> regionList = new List<RegionInfo>();
                foreach (SceneInterface scene in SceneManager.Scenes.Values)
                {
                    onlineRegions.Add(scene.ID);
                }
                regions = from rInfo in m_RegionStorage.GetAllRegions(UUID.Zero) where !onlineRegions.Contains(rInfo.ID) select rInfo;
            }
            else
            {
                io.WriteFormatted(string.Format("{0} is not a known token", args[2]));
                return;
            }

            string output = "Scene List:\n----------------------------------------------";
            foreach (RegionInfo rInfo in regions)
            {
                if (limitedToScene == UUID.Zero || rInfo.ID == limitedToScene)
                {
                    Vector3 gridcoord = rInfo.Location;
                    output += string.Format("\nRegion {0} [{1}]:\n  Location={2} (grid coordinate {5})\n  Size={3}\n  Owner={4}\n", rInfo.Name, rInfo.ID, gridcoord.ToString(), rInfo.Size.ToString(), rInfo.Owner.FullName, gridcoord.X_String + "," + gridcoord.Y_String);
                }
            }
            io.Write(output);
        }

        public void CreateRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("create region not allowed from restricted console");
            }
            else if (args[0] == "help" || args.Count < 5)
            {
                io.Write("create region <regionname> <port> <location> parameters...\n\n" +
                    "Parameters:\n" +
                    "regionid <uuid> - if not specified, a random uuid will be generated\n" +
                    "scopeid <uuid>\n" +
                    "size <x>,<y> - region size\n" +
                    "regiontype <regiontype>\n" +
                    "owner <uui>\n" +
                    "estate <name> - sets region owner to estate owner\n" +
                    "externalhostname <hostname>\n" +
                    "access trial|pg|mature|adult\n" +
                    "staticmaptile <uuid>\n" +
                    "status online|offline");
            }
            else
            {
                RegionInfo rInfo = new RegionInfo();
                EstateInfo selectedEstate = null;
                rInfo.Name = args[2];
                rInfo.ID = UUID.Random;
                rInfo.Access = RegionAccess.Mature;
                rInfo.ServerHttpPort = m_HttpPort;
                rInfo.ScopeID = UUID.Zero;
                rInfo.ServerIP = m_ExternalHostName;
                rInfo.Size = new GridVector(256, 256);

                if (!uint.TryParse(args[3], out rInfo.ServerPort))
                {
                    io.WriteFormatted("Port {0} is not valid", args[3]);
                    return;
                }

                try
                {
                    rInfo.Location = new GridVector(args[4], 256);
                }
                catch (Exception e)
                {
                    io.Write(e.ToString());
                    return;
                }

                for (int argi = 5; argi + 1 < args.Count; argi += 2)
                {
                    switch(args[argi].ToLower())
                    {
                        case "externalhostname":
                            rInfo.ServerIP = args[argi + 1];
                            break;

                        case "regionid":
                            if(!UUID.TryParse(args[argi + 1], out rInfo.ID))
                            {
                                io.WriteFormatted("{0} is not a valid UUID.", args[argi + 1]);
                                return;
                            }
                            break;

                        case "scopeid":
                            if (!UUID.TryParse(args[argi + 1], out rInfo.ScopeID))
                            {
                                io.WriteFormatted("{0} is not a valid UUID.", args[argi + 1]);
                                return;
                            }
                            break;

                        case "staticmaptile":
                            if (!UUID.TryParse(args[argi + 1], out rInfo.RegionMapTexture))
                            {
                                io.WriteFormatted("{0} is not a valid UUID.", args[argi + 1]);
                                return;
                            }
                            break;

                        case "size":
                            try
                            {
                                rInfo.Size = new GridVector(args[argi + 1], 256);
                            }
                            catch (Exception e)
                            {
                                io.WriteFormatted("{0} is not valid: {1}", args[argi + 1], e.ToString());
                                return;
                            }
                            break;

                        case "regiontype":
                            break;

                        case "estate":
                            if(m_EstateService.TryGetValue(args[argi + 1], out selectedEstate))
                            {
                                io.WriteFormatted("{0} is not known as an estate", args[argi + 1]);
                                return;
                            }
                            rInfo.Owner = selectedEstate.Owner;
                            break;

                        case "owner":
                            if(!UUI.TryParse(args[argi + 1], out rInfo.Owner))
                            {
                                io.WriteFormatted("{0} is not a valid UUI.", args[argi + 1]);
                                return;
                            }
                            break;

                        case "status":
                            switch(args[argi + 1].ToLower())
                            {
                                case "online":
                                    rInfo.Flags = RegionFlags.RegionOnline;
                                    break;

                                case "offline":
                                    rInfo.Flags = RegionFlags.None;
                                    break;

                                default:
                                    io.WriteFormatted("{0} is not a valid status.", args[argi + 1]);
                                    return;
                            }
                            break;

                        case "access":
                            switch(args[argi + 1])
                            {
                                case "trial":
                                    rInfo.Access = RegionAccess.Trial;
                                    break;

                                case "pg":
                                    rInfo.Access = RegionAccess.PG;
                                    break;

                                case "mature":
                                    rInfo.Access = RegionAccess.Mature;
                                    break;

                                case "adult":
                                    rInfo.Access = RegionAccess.Adult;
                                    break;

                                default:
                                    io.WriteFormatted("{0} is not a valid access", args[argi + 1]);
                                    return;
                            }
                            break;

                        default:
                            io.WriteFormatted("{0} is not a valid parameter", args[argi]);
                            return;
                    }
                }
                rInfo.ServerURI = string.Format("{0}://{1}:{2}/", m_Scheme, m_ExternalHostName, m_HttpPort);
                m_RegionStorage.RegisterRegion(rInfo);

                if (selectedEstate != null)
                {
                    m_EstateService.RegionMap[rInfo.ID] = selectedEstate.ID;
                    io.WriteFormatted("Assigning new region {0} to estate {1} owned by {2}", rInfo.Name, selectedEstate.Name, selectedEstate.Owner.FullName);
                }
                else
                {
                    List<EstateInfo> allEstates = m_EstateService.All;
                    List<EstateInfo> ownerEstates = new List<EstateInfo>(from estate in allEstates where estate.Owner.EqualsGrid(rInfo.Owner) select estate);
                    if (ownerEstates.Count != 0)
                    {
                        m_EstateService.RegionMap[rInfo.ID] = ownerEstates[0].ID;
                        io.WriteFormatted("Assigning new region {0} to estate {1} owned by {2}", rInfo.Name, allEstates[0].Name, allEstates[0].Owner.FullName);
                    }
                    else if (allEstates.Count != 0)
                    {
                        rInfo.Owner = allEstates[0].Owner;
                        m_EstateService.RegionMap[rInfo.ID] = allEstates[0].ID;
                        io.WriteFormatted("Assigning new region {0} to estate {1} owned by {2}", rInfo.Name, allEstates[0].Name, allEstates[0].Owner.FullName);
                    }
                }
            }
        }

        public void DeleteRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("delete region not allowed from restricted console");
            }
            else if (args[0] == "help")
            {
                io.Write("delete region <regionname>");
            }
            else
            {
            }
        }

        public void EnableRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            RegionInfo rInfo;
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("enable region not allowed from restricted console");
            }
            else if (args[0] == "help")
            {
                io.Write("enable region <regionname>");
            }
            else if(args.Count < 3)
            {
                io.Write("missing region name");
            }
            else if(m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                m_RegionStorage.AddRegionFlags(rInfo.ID, RegionFlags.RegionOnline);
            }
            else
            {
                io.Write("No region found");
            }
        }

        public void DisableRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            RegionInfo rInfo;
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("disable region not allowed from restricted console");
            }
            else if (args[0] == "help")
            {
                io.Write("disable region <regionname>");
            }
            else if (args.Count < 3)
            {
                io.Write("missing region name");
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                m_RegionStorage.RemoveRegionFlags(rInfo.ID, RegionFlags.RegionOnline);
            }
            else
            {
                io.Write("No region found");
            }
        }

        public void StartRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            RegionInfo rInfo;
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("start region not allowed from restricted console");
            }
            else if (args[0] == "help")
            {
                io.Write("start region <regionname>");
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                SceneInterface si;
                if (SceneManager.Scenes.TryGetValue(rInfo.ID, out si))
                {
                    io.Write(string.Format("Region '{0}' ({1}) is already started", rInfo.Name, rInfo.ID.ToString()));
                }
                else
                {
                    io.Write(string.Format("Starting region {0} ({1})", rInfo.Name, rInfo.ID.ToString()));
                    m_Log.InfoFormat("Starting Region {0} ({1})", rInfo.Name, rInfo.ID.ToString());
                    si = m_SceneFactory.Instantiate(rInfo);
                    SceneManager.Scenes.Add(si);
                    si.LoadSceneAsync();
                }
            }
        }

        public void StopRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            RegionInfo rInfo;
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("stop region not allowed from restricted console");
            }
            else if (args[0] == "help")
            {
                io.Write("stop region <regionname>");
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                SceneInterface si;
                if (!SceneManager.Scenes.TryGetValue(rInfo.ID, out si))
                {
                    io.Write(string.Format("Region '{0}' ({1}) is not started", rInfo.Name, rInfo.ID.ToString()));
                }
                else
                {
                    SceneManager.Scenes.Remove(si);
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("Commands")]
    public class RegionCommandsFactory : IPluginFactory
    {
        public RegionCommandsFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new RegionCommands(ownSection.GetString("RegionStorage", "RegionStorage"), ownSection.GetString("EstateService", "EstateService"));
        }
    }
    #endregion
}
