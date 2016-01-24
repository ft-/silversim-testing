// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;

namespace SilverSim.Main.Cmd.Region
{
    #region Service Implementation
    [Description("Region Console Commands")]
    public class RegionCommands : IPlugin
    {
        readonly string m_RegionStorageName;
        readonly string m_EstateServiceName;
        readonly string m_SimulationStorageName;
        GridServiceInterface m_RegionStorage;
        SceneFactoryInterface m_SceneFactory;
        EstateServiceInterface m_EstateService;
        SimulationDataStorageInterface m_SimulationData;
        private static readonly ILog m_Log = LogManager.GetLogger("REGION COMMANDS");
        private string m_ExternalHostName = string.Empty;
        private uint m_HttpPort;
        private string m_Scheme = Uri.UriSchemeHttp;
        readonly List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();

        public RegionCommands(string regionStorageName, string estateServiceName, string simulationStorageName)
        {
            m_RegionStorageName = regionStorageName;
            m_EstateServiceName = estateServiceName;
            m_SimulationStorageName = simulationStorageName;
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
            m_SimulationData = loader.GetService<SimulationDataStorageInterface>(m_SimulationStorageName);
            Common.CmdIO.CommandRegistry.CreateCommands.Add("region", CreateRegionCmd);
            Common.CmdIO.CommandRegistry.CreateCommands.Add("regions", CreateRegionsCmd);
            Common.CmdIO.CommandRegistry.DeleteCommands.Add("region", DeleteRegionCmd);
            Common.CmdIO.CommandRegistry.ShowCommands.Add("regionstats", ShowRegionStatsCmd);
            Common.CmdIO.CommandRegistry.ShowCommands.Add("regions", ShowRegionsCmd);
            Common.CmdIO.CommandRegistry.EnableCommands.Add("region", EnableRegionCmd);
            Common.CmdIO.CommandRegistry.DisableCommands.Add("region", DisableRegionCmd);
            Common.CmdIO.CommandRegistry.StartCommands.Add("region", StartRegionCmd);
            Common.CmdIO.CommandRegistry.StopCommands.Add("region", StopRegionCmd);
            Common.CmdIO.CommandRegistry.ChangeCommands.Add("region", ChangeRegionCmd);
            Common.CmdIO.CommandRegistry.AlertCommands.Add("region", AlertRegionCmd);
            Common.CmdIO.CommandRegistry.RestartCommands.Add("region", RestartRegionCmd);
            Common.CmdIO.CommandRegistry.AlertCommands.Add("regions", AlertRegionsCmd);
            Common.CmdIO.CommandRegistry.AlertCommands.Add("agent", AlertAgentCmd);
            Common.CmdIO.CommandRegistry.KickCommands.Add("agent", KickAgentCmd);
            Common.CmdIO.CommandRegistry.ShowCommands.Add("agents", ShowAgentsCmd);
            Common.CmdIO.CommandRegistry.EnableCommands.Add("logins", EnableDisableLoginsCmd);
            Common.CmdIO.CommandRegistry.DisableCommands.Add("logins", EnableDisableLoginsCmd);
            Common.CmdIO.CommandRegistry.ShowCommands.Add("neighbors", ShowNeighborsCmd);
            Common.CmdIO.CommandRegistry.ClearCommands.Add("objects", ClearObjectsCmd);
            Common.CmdIO.CommandRegistry.ClearCommands.Add("parcels", ClearParcelsCmd);
            Common.CmdIO.CommandRegistry.ClearCommands.Add("region", ClearRegionCmd);
            Common.CmdIO.CommandRegistry.SelectCommands.Add("region", SelectRegionCmd);
            Common.CmdIO.CommandRegistry.ShowCommands.Add("parcels", ShowParcelsCmd);
            Common.CmdIO.CommandRegistry.GetCommands.Add("sunparam", GetSunParamCmd);
            Common.CmdIO.CommandRegistry.SetCommands.Add("sunparam", SetSunParamCmd);
            Common.CmdIO.CommandRegistry.ResetCommands.Add("sunparam", ResetSunParamCmd);
            Common.CmdIO.CommandRegistry.GetCommands.Add("moonparam", GetMoonParamCmd);
            Common.CmdIO.CommandRegistry.SetCommands.Add("moonparam", SetMoonParamCmd);
            Common.CmdIO.CommandRegistry.ResetCommands.Add("moonparam", ResetMoonParamCmd);
            Common.CmdIO.CommandRegistry.EnableCommands.Add("tidal", EnableTidalParamCmd);
            Common.CmdIO.CommandRegistry.DisableCommands.Add("tidal", DisableTidalParamCmd);
            Common.CmdIO.CommandRegistry.GetCommands.Add("tidalparam", GetTidalParamCmd);
            Common.CmdIO.CommandRegistry.SetCommands.Add("tidalparam", SetTidalParamCmd);
            Common.CmdIO.CommandRegistry.ResetCommands.Add("tidalparam", ResetTidalParamCmd);
            Common.CmdIO.CommandRegistry.ResetCommands.Add("windparam", ResetWindParamCmd);
            Common.CmdIO.CommandRegistry.GetCommands.Add("waterheight", GetWaterheightCmd);
            Common.CmdIO.CommandRegistry.SetCommands.Add("waterheight", SetWaterheightCmd);

            IConfig sceneConfig = loader.Config.Configs["DefaultSceneImplementation"];
            if (null != sceneConfig)
            {
                string avatarNameServices = sceneConfig.GetString("AvatarNameServices", string.Empty);
                if (!string.IsNullOrEmpty(avatarNameServices))
                {
                    foreach (string p in avatarNameServices.Split(','))
                    {
                        m_AvatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(p.Trim()));
                    }
                }
            }
        }

        UUI ResolveName(UUI uui)
        {
            UUI resultUui;
            foreach(AvatarNameServiceInterface service in m_AvatarNameServices)
            {
                if(service.TryGetValue(uui, out resultUui))
                {
                    return resultUui;
                }
            }
            return uui;
        }

        bool TranslateToUUI(string arg, out UUI uui)
        {
            uui = UUI.Unknown;
            if (arg.Contains(","))
            {
                bool found = false;
                string[] names = arg.Split(new char[] { ',' }, 2);
                if (names.Length == 1)
                {
                    names = new string[] { names[0], string.Empty };
                }
                foreach (AvatarNameServiceInterface service in m_AvatarNameServices)
                {
                    UUI founduui;
                    if (service.TryGetValue(names[0], names[1], out founduui))
                    {
                        uui = founduui;
                        found = true;
                        break;
                    }
                }
                return found;
            }
            else if (UUID.TryParse(arg, out uui.ID))
            {
                bool found = false;
                foreach (AvatarNameServiceInterface service in m_AvatarNameServices)
                {
                    UUI founduui;
                    if (service.TryGetValue(uui.ID, out founduui))
                    {
                        uui = founduui;
                        found = true;
                        break;
                    }
                }
                return found;
            }
            else if (!UUI.TryParse(arg, out uui))
            {
                return false;
            }
            return true;
        }

        #region Region control commands
        void ChangeRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            RegionInfo rInfo;
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("create region not allowed from restricted console");
            }
            else if (args[0] == "help" || args.Count < 3)
            {
                io.Write("change region <regionname> parameters...\n\n" +
                    "Parameters:\n" +
                    "name <name>\n" +
                    "port <port>\n" +
                    "scopeid <uuid>\n" +
                    "productname <regiontype>\n" +
                    "owner <uui>|<uuid>|<firstname>,<lastname>\n" +
                    "estate <name>\n" +
                    "externalhostname <hostname>\n" +
                    "access pg|mature|adult\n" +
                    "staticmaptile <uuid>\n");
            }
            else if (!m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                io.WriteFormatted("Region with name {0} does not exist.", args[2]);
            }
            else
            {
                int argi;
                bool changeRegionData = false;
                EstateInfo selectedEstate = null;
                for (argi = 3; argi < args.Count; argi += 2)
                {
                    switch (args[argi].ToLower())
                    {
                        case "name":
                            rInfo.Name = args[argi + 1];
                            changeRegionData = true;
                            break;

                        case "port":
                            if (!uint.TryParse(args[argi + 1], out rInfo.ServerPort))
                            {
                                io.WriteFormatted("Port {0} is not valid", args[argi + 1]);
                                return;
                            }
                            if (rInfo.ServerPort < 1 || rInfo.ServerPort > 65535)
                            {
                                io.WriteFormatted("Port {0} is not valid", args[argi + 1]);
                                return;
                            }
                            changeRegionData = true;
                            break;

                        case "scopeid":
                            if (!UUID.TryParse(args[argi + 1], out rInfo.ScopeID))
                            {
                                io.WriteFormatted("{0} is not a valid UUID.", args[argi + 1]);
                                return;
                            }
                            changeRegionData = true;
                            break;

                        case "productname":
                            rInfo.ProductName = args[argi + 1];
                            changeRegionData = true;
                            break;

                        case "owner":
                            if (!TranslateToUUI(args[argi + 1], out rInfo.Owner))
                            {
                                io.WriteFormatted("{0} is not a valid owner.", args[argi + 1]);
                                return;
                            }
                            changeRegionData = true;
                            break;

                        case "estate":
                            if (!m_EstateService.TryGetValue(args[argi + 1], out selectedEstate))
                            {
                                io.WriteFormatted("{0} is not known as an estate", args[argi + 1]);
                                return;
                            }
                            break;

                        case "externalhostname":
                            rInfo.ServerIP = args[argi + 1];
                            changeRegionData = true;
                            break;

                        case "access":
                            switch (args[argi + 1])
                            {
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
                            changeRegionData = true;
                            break;

                        case "staticmaptile":
                            if (!UUID.TryParse(args[argi + 1], out rInfo.RegionMapTexture))
                            {
                                io.WriteFormatted("{0} is not a valid UUID.", args[argi + 1]);
                                return;
                            }
                            changeRegionData = true;
                            break;

                        default:
                            io.WriteFormatted("Parameter {0} is not valid.", args[argi]);
                            return;
                    }
                }

                SceneInterface si;
                if (SceneManager.Scenes.TryGetValue(rInfo.ID, out si))
                {
                    io.WriteFormatted("Please stop region first.");
                    return;
                }
                if (changeRegionData)
                {
                    try
                    {
                        m_RegionStorage.RegisterRegion(rInfo);
                    }
                    catch (Exception e)
                    {
                        io.WriteFormatted("Could not change region parameters: {0}", e.Message);
                    }
                }
                if (null != selectedEstate)
                {
                    m_EstateService.RegionMap[rInfo.ID] = selectedEstate.ID;
                }
            }
        }

        void CreateRegionsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("create region not allowed from restricted console");
            }
            else if (args.Count < 5)
            {
                io.WriteFormatted("create regions from ini <regions.ini file>");
                return;
            }
            else if(m_EstateService.All.Count == 0)
            {
                io.Write("please create an estate first");
            }
            else if (args[2] == "from" && args[3] == "ini")
            {
                IConfigSource cfg;
                try
                {
                    if (Uri.IsWellFormedUriString(args[4], UriKind.Absolute))
                    {
                        using (Stream s = Http.Client.HttpRequestHandler.DoStreamGetRequest(args[4], null, 20000))
                        {
                            using (XmlReader r = XmlReader.Create(s))
                            {
                                cfg = new XmlConfigSource(r);
                            }
                        }
                    }
                    else
                    {
                        cfg = new IniConfigSource(args[4]);
                    }
                }
                catch (Exception e)
                {
                    io.WriteFormatted("Could not open {0}: {1}", args[4], e.Message);
                    return;
                }

                string msg = string.Empty;

                foreach (IConfig regionEntry in cfg.Configs)
                {
                    RegionInfo r = new RegionInfo();
                    r.Name = regionEntry.Name;
                    r.ID = regionEntry.GetString("RegionUUID");
                    r.Location = new GridVector(regionEntry.GetString("Location"), 256);
                    r.ServerPort = (uint)regionEntry.GetInt("InternalPort");
                    r.ServerURI = string.Format("{0}://{1}:{2}/", m_Scheme, m_ExternalHostName, m_HttpPort);
                    r.Size.X = ((uint)regionEntry.GetInt("SizeX", 256) + 255) & (~(uint)255);
                    r.Size.Y = ((uint)regionEntry.GetInt("SizeY", 256) + 255) & (~(uint)255);
                    r.Flags = RegionFlags.RegionOnline;
                    r.ProductName = regionEntry.GetString("RegionType", "Mainland");
                    r.Owner = new UUI(regionEntry.GetString("Owner"));
                    r.ScopeID = regionEntry.GetString("ScopeID", "00000000-0000-0000-0000-000000000000");
                    r.ServerHttpPort = m_HttpPort;
                    r.RegionMapTexture = regionEntry.GetString("MaptileStaticUUID", "00000000-0000-0000-0000-000000000000");
                    switch (regionEntry.GetString("Access", "mature").ToLower())
                    {
                        case "pg":
                            r.Access = RegionAccess.PG;
                            break;

                        case "mature":
                        default:
                            r.Access = RegionAccess.Mature;
                            break;

                        case "adult":
                            r.Access = RegionAccess.Adult;
                            break;
                    }

                    r.ServerIP = regionEntry.GetString("ExternalHostName", m_ExternalHostName);
                    RegionInfo rInfoCheck;
                    if (m_RegionStorage.TryGetValue(UUID.Zero, r.Name, out rInfoCheck))
                    {
                        if (msg.Length != 0)
                        {
                            msg += "\n";
                        }
                        msg += string.Format("Region {0} is already used by region id {1}. Skipping.", rInfoCheck.Name, rInfoCheck.ID);
                    }
                    else
                    {
                        m_RegionStorage.RegisterRegion(r);
                        List<EstateInfo> allEstates = m_EstateService.All;
                        List<EstateInfo> ownerEstates = new List<EstateInfo>(from estate in allEstates where estate.Owner.EqualsGrid(r.Owner) select estate);
                        if (ownerEstates.Count != 0)
                        {
                            m_EstateService.RegionMap[r.ID] = ownerEstates[0].ID;
                            io.WriteFormatted("Assigning new region {0} to estate {1} owned by {2}", r.Name, allEstates[0].Name, allEstates[0].Owner.FullName);
                        }
                        else if (allEstates.Count != 0)
                        {
                            m_EstateService.RegionMap[r.ID] = allEstates[0].ID;
                            io.WriteFormatted("Assigning new region {0} to estate {1} owned by {2}", r.Name, allEstates[0].Name, allEstates[0].Owner.FullName);
                        }
                    }
                }
            }
            else
            {
                io.Write("wrong command line for create regions");
                return;
            }
        }

        void CreateRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            RegionInfo rInfo;
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
                    "productname <regiontype>\n" +
                    "owner <uui>|<uuid>|<firstname>,<lastname>\n" +
                    "estate <name> - sets region owner to estate owner\n" +
                    "externalhostname <hostname>\n" +
                    "access pg|mature|adult\n" +
                    "staticmaptile <uuid>\n" +
                    "status online|offline");
            }
            else if (m_EstateService.All.Count == 0)
            {
                io.Write("please create an estate first");
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                io.WriteFormatted("Region with name {0} already exists.", args[2]);
            }
            else
            {
                rInfo = new RegionInfo();
                EstateInfo selectedEstate = null;
                rInfo.Name = args[2];
                rInfo.ID = UUID.Random;
                rInfo.Access = RegionAccess.Mature;
                rInfo.ServerHttpPort = m_HttpPort;
                rInfo.ScopeID = UUID.Zero;
                rInfo.ServerIP = m_ExternalHostName;
                rInfo.Size = new GridVector(256, 256);
                rInfo.ProductName = "Mainland";

                if (!uint.TryParse(args[3], out rInfo.ServerPort))
                {
                    io.WriteFormatted("Port {0} is not valid", args[3]);
                    return;
                }
                if (rInfo.ServerPort < 1 || rInfo.ServerPort > 65535)
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
                    switch (args[argi].ToLower())
                    {
                        case "externalhostname":
                            rInfo.ServerIP = args[argi + 1];
                            break;

                        case "regionid":
                            if (!UUID.TryParse(args[argi + 1], out rInfo.ID))
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

                        case "productname":
                            rInfo.ProductName = args[argi + 1];
                            break;

                        case "estate":
                            if (!m_EstateService.TryGetValue(args[argi + 1], out selectedEstate))
                            {
                                io.WriteFormatted("{0} is not known as an estate", args[argi + 1]);
                                return;
                            }
                            rInfo.Owner = selectedEstate.Owner;
                            break;

                        case "owner":
                            if (!TranslateToUUI(args[argi + 1], out rInfo.Owner))
                            {
                                io.WriteFormatted("{0} is not a valid owner.", args[argi + 1]);
                                return;
                            }
                            break;

                        case "status":
                            switch (args[argi + 1].ToLower())
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
                            switch (args[argi + 1])
                            {
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
                        m_EstateService.RegionMap[rInfo.ID] = allEstates[0].ID;
                        io.WriteFormatted("Assigning new region {0} to estate {1} owned by {2}", rInfo.Name, allEstates[0].Name, allEstates[0].Owner.FullName);
                    }
                }
            }
        }

        void DeleteRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("delete region not allowed from restricted console");
            }
            else if (args[0] == "help" || args.Count < 3)
            {
                io.Write("delete region <regionname>");
            }
            else
            {
                RegionInfo rInfo;
                if(!m_RegionStorage.TryGetValue(args[2], out rInfo))
                {
                    io.WriteFormatted("Region '{0}' not found", args[2]);
                }
                else if(SceneManager.Scenes.ContainsKey(rInfo.ID))
                {
                    io.WriteFormatted("Region '{0}' is running.", args[2]);
                }
                else
                {
                    m_SimulationData.RemoveRegion(rInfo.ID);
                    m_EstateService.RegionMap.Remove(rInfo.ID);
                    m_RegionStorage.DeleteRegion(UUID.Zero, rInfo.ID);
                    io.WriteFormatted("Region '{0}' deleted.", args[2]);
                }
            }
        }

        void RestartRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            RegionInfo rInfo;

            if (args[0] == "help" || args.Count < 4)
            {
                io.Write("restart region <regionname> seconds\nrestart region <regionname> abort");
            }
            else if (!m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                io.Write("No region selected");
            }
            else if(limitedToScene != UUID.Zero && rInfo.ID != limitedToScene)
            {
                io.Write("Not possible to restart another region than the current one");
            }
            else
            {
                SceneInterface scene;
                int timeToRestart;
                if(!SceneManager.Scenes.TryGetValue(rInfo.ID, out scene))
                {
                    io.Write("region not started");
                }
                else if(args[3].ToLower() == "abort")
                {
                    io.Write("Region restart abort requested");
                    scene.AbortRegionRestart();
                }
                else if(int.TryParse(args[3], out timeToRestart))
                {
                    io.Write("Region restart requested");
                    scene.RequestRegionRestart(timeToRestart);
                }
                else
                {
                    io.Write("Invalid seconds specified: " + args[3]);
                }
            }
        }

        void EnableRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if (args.Count < 3)
            {
                io.Write("missing region name");
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                m_RegionStorage.AddRegionFlags(rInfo.ID, RegionFlags.RegionOnline);
            }
            else
            {
                io.Write("No region found");
            }
        }

        void DisableRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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

        void StartRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
                    try
                    {
                        si = m_SceneFactory.Instantiate(rInfo);
                    }
                    catch (Exception e)
                    {
                        io.WriteFormatted("Failed to start region: {0}", e.Message);
                        return;
                    }
                    SceneManager.Scenes.Add(si);
                    si.LoadSceneAsync();
                }
            }
        }

        void StopRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
        #endregion

        #region Region and Simulator notice
        void AlertRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help")
            {
                io.Write("alert region <message>");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("alert needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            if (args.Count >= 3)
            {
                string msg = string.Join(" ", args.GetRange(2, args.Count - 2));
                foreach (IAgent agent in scene.RootAgents)
                {
                    agent.SendAlertMessage(msg, scene.ID);
                }
            }
        }

        void AlertRegionsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if(limitedToScene != UUID.Zero)
            {
                io.Write("alert regions is not allowed from restricted console.");
                return;
            }
            if (args[0] == "help" || args.Count < 3)
            {
                io.Write("alert regions <message>");
                return;
            }

            string msg = string.Join(" ", args.GetRange(2, args.Count - 2));
            foreach (SceneInterface scene in SceneManager.Scenes.Values)
            {
                foreach (IAgent agent in scene.RootAgents)
                {
                    agent.SendAlertMessage(msg, scene.ID);
                }
            }
        }
        #endregion

        #region Agent control (Login/Messages/Kick)
        void AlertAgentCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help" && args.Count < 5)
            {
                io.Write("alert agent <firstname> <lastname> <message>");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("alert agent needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            bool agentFound = false;
            string msg = string.Join(" ", args.GetRange(4, args.Count - 4));
            foreach (IAgent agent in scene.RootAgents)
            {
                UUI agentid = agent.Owner;
                if (agentid.FullName.ToLower() == (args[2] + " " + args[3]).ToLower())
                {
                    agent.SendAlertMessage(msg, scene.ID);
                    agentFound = true;
                }
            }

            if (!agentFound)
            {
                io.WriteFormatted("Agent {0} {1} not found.", args[2], args[3]);
            }
        }

        void KickAgentCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help" || args.Count < 4)
            {
                io.Write("kick agent <firstname> <lastname> <message>");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("kick agent needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            string msg = args.Count >= 4 ?
                string.Join(" ", args.GetRange(4, args.Count - 4)) :
                string.Empty;

            bool agentFound = false;
            foreach (IAgent agent in scene.RootAgents)
            {
                UUI agentid = agent.Owner;
                if(args.Count < 4)
                {
                    msg = this.GetLanguageString(agent.CurrentCulture, "YouHaveBeenKicked", "You have been kicked.");
                }
                if (agentid.FullName.ToLower() == (args[2] + " " + args[3]).ToLower())
                {
                    agent.KickUser(msg);
                    agentFound = true;
                }
            }

            if(!agentFound)
            {
                io.WriteFormatted("Agent {0} {1} not found.", args[2], args[3]);
            }
        }

        void EnableDisableLoginsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help")
            {
                io.Write("enable logins\ndisable logins");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.WriteFormatted("{0} logins needs a selected region before.", args[0]);
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }
            else if(args[0] == "enable")
            {
                scene.LoginControl.Ready(SceneInterface.ReadyFlags.LoginsEnable);
            }
            else if(args[0] == "disable")
            {
                scene.LoginControl.NotReady(SceneInterface.ReadyFlags.LoginsEnable);
            }
        }
        #endregion

        #region Show commands
        void ShowRegionStatsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help")
            {
                io.Write("show regionstats - Shows region statistics");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("show regionstats needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            string output = string.Empty;
            output += string.Format("Environment FPS: {0}\n", scene.Environment.EnvironmentFps);
            output += string.Format("Physics FPS: {0}\n", scene.PhysicsScene.PhysicsFPS);
            output += string.Format("Root Agents: {0}", scene.RootAgents.Count);
            io.Write(output);
        }

        void ShowRegionsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            IEnumerable<RegionInfo> regions;

            if (args[0] == "help")
            {
                io.Write("show regions ([enabled|disabled|online|offline])");
                return;
            }
            else if (args.Count < 3)
            {
                regions = m_RegionStorage.GetAllRegions(UUID.Zero);
            }
            else if (args[2] == "enabled")
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
                foreach (SceneInterface scene in SceneManager.Scenes.Values)
                {
                    regionList.Add(scene.GetRegionInfo());
                }
                regions = regionList;
            }
            else if (args[2] == "offline")
            {
                List<UUID> onlineRegions = new List<UUID>();

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
                    output += string.Format("\nRegion {0} [{1}]: (Port {6})\n  Location={2} (grid coordinate {5})\n  Size={3}\n  Owner={4}\n  GatekeeperURI={7}\n",
                        rInfo.Name, rInfo.ID,
                        gridcoord.ToString(),
                        rInfo.Size.ToString(),
                        ResolveName(rInfo.Owner).FullName,
                        gridcoord.X_String + "," + gridcoord.Y_String,
                        rInfo.ServerPort,
                        rInfo.GridURI);
                }
            }
            io.Write(output);
        }

        void ShowNeighborsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help")
            {
                io.Write("show neighbors - Shows neighbors");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("show neighbors needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            string output = "Neighbor List:\n----------------------------------------------";
            foreach (SceneInterface.NeighborEntry neighborInfo in scene.Neighbors.Values)
            {
                Vector3 gridcoord = neighborInfo.RemoteRegionData.Location;
                output += string.Format("\nRegion {0} [{1}]:\n  Location={2} (grid coordinate {5})\n  Size={3}\n  Owner={4}\n",
                    neighborInfo.RemoteRegionData.Name,
                    neighborInfo.RemoteRegionData.ID, gridcoord.ToString(),
                    neighborInfo.RemoteRegionData.Size.ToString(),
                    ResolveName(neighborInfo.RemoteRegionData.Owner).FullName,
                    gridcoord.X_String + "," + gridcoord.Y_String);
            }
            io.Write(output);

        }

        void ShowAgentsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help")
            {
                io.Write("show agents\nshow agents full");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("show agents needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            IEnumerable<IAgent> agents;
            string output;
            if (args.Count == 3 && args[2] == "full")
            {
                agents = scene.Agents;
                output = "All Agents: -----------------\n";
            }
            else
            {
                agents = scene.RootAgents;
                output = "Root Agents: -----------------\n";
            }

            foreach(IAgent agent in agents)
            {
                output += string.Format("\n{0}\n    id={1}  Type={2}\n", agent.Owner.FullName, agent.Owner.ID.ToString(), agent.IsInScene(scene) ? "Root" : "Child");
            }
            io.Write(output);
        }

        void ShowParcelsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID sceneID = UUID.Zero != limitedToScene ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (!SceneManager.Scenes.TryGetValue(sceneID, out scene))
            {
                io.Write("No region selected.");
            }
            else
            {
                string output = "Parcel List:\n--------------------------------------------------------------------------------";
                foreach (ParcelInfo parcel in scene.Parcels)
                {
                    output += string.Format("\nParcel {0} ({1}):\n  Owner={2}\n",
                        parcel.Name,
                        parcel.ID,
                        ResolveName(parcel.Owner).FullName);
                }
                io.Write(output);
            }
        }
        #endregion

        #region Clear commands
        void ClearObjectsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("clear all region objects");
                return;
            }
            else if (UUID.Zero != limitedToScene)
            {
                scene = SceneManager.Scenes[limitedToScene];
            }
            else if (UUID.Zero != io.SelectedScene)
            {
                scene = SceneManager.Scenes[io.SelectedScene];
            }
            else
            {
                io.Write("no region selected");
                return;
            }
            scene.ClearObjects();
            io.Write("All objects deleted.");
        }

        void ClearParcelsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("clears region parcels entirely");
                return;
            }
            else if (UUID.Zero != limitedToScene)
            {
                scene = SceneManager.Scenes[limitedToScene];
            }
            else if (UUID.Zero != io.SelectedScene)
            {
                scene = SceneManager.Scenes[io.SelectedScene];
            }
            else
            {
                io.Write("no region selected");
                return;
            }
            scene.ResetParcels();
            io.Write("Region parcels cleared");
        }

        void ClearRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("clears region entirely");
                return;
            }
            else if (UUID.Zero != limitedToScene)
            {
                scene = SceneManager.Scenes[limitedToScene];
            }
            else if (UUID.Zero != io.SelectedScene)
            {
                scene = SceneManager.Scenes[io.SelectedScene];
            }
            else
            {
                io.Write("no region selected");
                return;
            }
            scene.ClearObjects();
            scene.ResetParcels();
            io.Write("Region cleared.");
        }
        #endregion

        #region Select region command
        void SelectRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                io.Write("select region <region name>\nselect region to root");
            }
            else if (limitedToScene != UUID.Zero)
            {
                io.Write("select region is not possible with limited console");
            }
            else if (args.Count == 4)
            {
                if (args[2] != "to" || args[3] != "root")
                {
                    io.Write("invalid parameters for select region");
                }
                else
                {
                    io.SelectedScene = UUID.Zero;
                }
            }
            else if (args.Count == 3)
            {
                SceneInterface scene;
                if (SceneManager.Scenes.TryGetValue(args[2], out scene))
                {
                    io.SelectedScene = scene.ID;
                    io.WriteFormatted("region {0} selected", args[2]);
                }
                else
                {
                    io.WriteFormatted("region {0} does not exist or is not online", args[2]);
                }
            }
            else
            {
                io.Write("invalid parameters for select region");
            }
        }
        #endregion

        #region Sun Params
        void ResetSunParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("reset sunparam - reset sunparam to defaults");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment.ResetSunToDefaults();
            }
        }

        void SetSunParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 4 ||
                (args[2] == "durations" && args.Count < 5))
            {
                io.Write("set sunparam durations <secsperday> <daysperyear>\n" +
                    "set sunparam averagetilt <value>\n" +
                    "set sunparam seasonaltilt <value>\n" +
                    "set sunparam normalizedoffset <value>\n" +
                    "set sunparam updateeverymsecs <value>\n" +
                    "set sunparam sendsimtimeeverynthupdate <value>");
            }
            else if(selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                double value;
                int ivalue;
                uint uvalue;
                switch (args[2])
                {
                    case "updateeverymsecs":
                        if(int.TryParse(args[3], out ivalue))
                        {
                            scene.Environment.SunUpdateEveryMsecs = ivalue;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    case "sendsimtimeeverynthupdate":
                        if (uint.TryParse(args[3], out uvalue))
                        {
                            scene.Environment.SendSimTimeEveryNthSunUpdate = uvalue;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    case "durations":
                        uint secperday;
                        uint daysperyear;
                        if(uint.TryParse(args[3], out secperday) &&
                            uint.TryParse(args[4], out daysperyear))
                        {
                            scene.Environment.SetSunDurationParams(secperday, daysperyear);
                        }
                        else
                        {
                            io.Write("Invalid values");
                        }
                        break;

                    case "averagetilt":
                        if(double.TryParse(args[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                        {
                            scene.Environment.AverageSunTilt = value;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    case "seasonaltilt":
                        if (double.TryParse(args[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                        {
                            scene.Environment.SeasonalSunTilt = value;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    case "normalizedoffset":
                        if (double.TryParse(args[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                        {
                            scene.Environment.SunNormalizedOffset = value;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    default:
                        io.WriteFormatted("Invalid parameter {0}", args[2]);
                        break;
                }
            }
        }


        void GetSunParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 3)
            {
                io.Write("get sunparam durations\n" +
                    "get sunparam averagetilt\n" +
                    "get sunparam seasonaltilt\n" +
                    "get sunparam normalizedoffset\n" +
                    "get sunparam updateveryms\n" +
                    "get sunparam sendsimtimeeverynthupdate");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                switch (args[2])
                {
                    case "updateeverymsecs":
                        io.WriteFormatted("Update sun every {0} ms", scene.Environment.SunUpdateEveryMsecs);
                        break;

                    case "sendsimtimeeverynthupdate":
                        io.WriteFormatted("Send sim time every {0} sun update", scene.Environment.SendSimTimeEveryNthSunUpdate);
                        break;

                    case "durations":
                        uint secperday;
                        uint daysperyear;
                        scene.Environment.GetSunDurationParams(out secperday, out daysperyear);
                        io.WriteFormatted("Seconds per day {0}\nDays per year {1}", secperday, daysperyear);
                        break;

                    case "averagetilt":
                        io.WriteFormatted("Average Tilt {0} rad", scene.Environment.AverageSunTilt);
                        break;

                    case "seasonaltilt":
                        io.WriteFormatted("Seasonal Tilt {0} rad", scene.Environment.SeasonalSunTilt);
                        break;

                    case "normalizedoffset":
                        io.WriteFormatted("Normalized Offset {0}", scene.Environment.SunNormalizedOffset);
                        break;

                    default:
                        io.WriteFormatted("Invalid parameter {0}", args[2]);
                        break;
                }
            }
        }
        #endregion

        #region Moon Params
        void ResetMoonParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("reset moonparam - reset moon parameters to defaults");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment.ResetMoonToDefaults();
            }
        }

        void SetMoonParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 4)
            {
                io.Write("set moonparam period <seconds>\n" +
                    "set moonparam phaseoffset <offset>");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                double value;
                switch (args[2])
                {
                    case "period":
                        if (double.TryParse(args[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                        {
                            scene.Environment.MoonPeriodLengthInSecs = value;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    case "phaseoffset":
                        if (double.TryParse(args[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                        {
                            scene.Environment.MoonPhaseOffset = value;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    default:
                        io.WriteFormatted("Invalid parameter {0}", args[2]);
                        break;
                }
            }
        }


        void GetMoonParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 3)
            {
                io.Write("get moonparam period\n" +
                    "get moonparam phaseoffset");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                switch (args[2])
                {
                    case "period":
                        io.WriteFormatted("Period {0}", scene.Environment.MoonPeriodLengthInSecs);
                        break;

                    case "phaseoffset":
                        io.WriteFormatted("Phase Offset {0} rad", scene.Environment.MoonPhaseOffset);
                        break;

                    default:
                        io.WriteFormatted("Invalid parameter {0}", args[2]);
                        break;
                }
            }
        }
        #endregion

        #region Tidal Params
        void ResetTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("reset tidalparam - reset tidal parameters to defaults");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment.ResetTidalToDefaults();
            }
        }


        void EnableTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("enable tidal");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment[EnvironmentController.BooleanWaterParams.EnableTideControl] = true;
            }
        }

        void DisableTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("disable tidal");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment[EnvironmentController.BooleanWaterParams.EnableTideControl] = false;
            }
        }

        void SetTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 3 ||
                (args.Count < 4))
            {
                io.Write("set tidalparam baseheight <baseheight>\n" + 
                    "set tidalparam moonamplitude <amplitude>\n" +
                    "set tidalparam sunamplitude <amplitude>\n" + 
                    "set tidalparam updateeverymsecs <millseconds>\n");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                double value;
                int ivalue;
                switch (args[2])
                {
                    case "updateeverymsecs":
                        if(int.TryParse(args[3], out ivalue))
                        {
                            scene.Environment.UpdateTidalModelEveryMsecs = ivalue;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    case "baseheight":
                        if (double.TryParse(args[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                        {
                            scene.Environment[EnvironmentController.FloatWaterParams.TidalBaseHeight] = value;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    case "moonamplitude":
                        if (double.TryParse(args[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                        {
                            scene.Environment[EnvironmentController.FloatWaterParams.TidalMoonAmplitude] = value;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    case "sunamplitude":
                        if (double.TryParse(args[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                        {
                            scene.Environment[EnvironmentController.FloatWaterParams.TidalSunAmplitude] = value;
                        }
                        else
                        {
                            io.Write("Invalid value");
                        }
                        break;

                    default:
                        io.WriteFormatted("Invalid parameter {0}", args[2]);
                        break;
                }
            }
        }


        void GetTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 3)
            {
                io.Write("get tidalparam baseheight\n" +
                    "get tidalparam sunamplitude\n" +
                    "get tidalparam moonamplitude\n" +
                    "get tidalparam enabled\n" + 
                    "get tidalparam updateeverymsecs");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                switch (args[2])
                {
                    case "updateeverymsecs":
                        io.WriteFormatted("Update tidal model every {0} msecs", scene.Environment.UpdateTidalModelEveryMsecs);
                        break;

                    case "baseheight":
                        io.WriteFormatted("Base height {0}", scene.Environment[EnvironmentController.FloatWaterParams.TidalBaseHeight]);
                        break;

                    case "sunamplitude":
                        io.WriteFormatted("Sun amplitude {0}", scene.Environment[EnvironmentController.FloatWaterParams.TidalSunAmplitude]);
                        break;

                    case "moonamplitude":
                        io.WriteFormatted("Moon amplitude {0}", scene.Environment[EnvironmentController.FloatWaterParams.TidalMoonAmplitude]);
                        break;

                    case "enabled":
                        io.WriteFormatted("Enabled {0}", scene.Environment[EnvironmentController.BooleanWaterParams.EnableTideControl].ToString());
                        break;

                    default:
                        io.WriteFormatted("Invalid parameter {0}", args[2]);
                        break;
                }
            }
        }
        #endregion

        #region Wind Params
        void ResetWindParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("reset windparam - reset wind parameters to defaults");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment.ResetWindToDefaults();
            }
        }

        #endregion

        #region Waterheight
        void SetWaterheightCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 3)
            {
                io.Write("set waterheight <waterheight>");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                double value;
                if (double.TryParse(args[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                {
                    scene.RegionSettings.WaterHeight = value;
                    scene.TriggerRegionSettingsChanged();
                }
                else
                {
                    io.Write("Invalid value");
                }
            }
        }

        void GetWaterheightCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("get waterheight");
            }
            else if (selectedScene == UUID.Zero || !SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                io.WriteFormatted("Water Height {0}", scene.RegionSettings.WaterHeight);
            }
        }
        #endregion
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
            return new RegionCommands(ownSection.GetString("RegionStorage", "RegionStorage"),
                ownSection.GetString("EstateService", "EstateService"),
                ownSection.GetString("SimulationDataStorage", "SimulationDataStorage"));
        }
    }
    #endregion
}
