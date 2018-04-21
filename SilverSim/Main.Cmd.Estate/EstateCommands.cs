// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Estate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace SilverSim.Main.Cmd.Estate
{
    [Description("Estate Console Commands")]
    [PluginName("Commands")]
    public class EstateCommands : IPlugin
    {
        private readonly string m_RegionStorageName;
        private readonly string m_EstateServiceName;
        private GridServiceInterface m_RegionStorage;
        private EstateServiceInterface m_EstateService;
        private AggregatingAvatarNameService m_AvatarNameService;
        private SceneList m_Scenes;

        public EstateCommands(IConfig ownSection)
        {
            m_RegionStorageName = ownSection.GetString("RegionStorage", "RegionStorage");
            m_EstateServiceName = ownSection.GetString("EstateService", "EstateService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            m_RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            loader.CommandRegistry.AddShowCommand("estates", ShowEstatesCmd);
            loader.CommandRegistry.AddChangeCommand("estate", ChangeEstateCmd);
            loader.CommandRegistry.AddCreateCommand("estate", CreateEstateCmd);
            loader.CommandRegistry.AddDeleteCommand("estate", DeleteEstateCmd);
            loader.CommandRegistry.AddAlertCommand("estate", AlertEstateCmd);

            var avatarNameServicesList = new RwLockedList<AvatarNameServiceInterface>();
            IConfig sceneConfig = loader.Config.Configs["DefaultSceneImplementation"];
            if(sceneConfig != null)
            {
                string avatarNameServices = sceneConfig.GetString("AvatarNameServices", string.Empty);
                if (!string.IsNullOrEmpty(avatarNameServices))
                {
                    foreach (string p in avatarNameServices.Split(','))
                    {
                        avatarNameServicesList.Add(loader.GetService<AvatarNameServiceInterface>(p.Trim()));
                    }
                }
            }
            m_AvatarNameService = new AggregatingAvatarNameService(avatarNameServicesList);
        }

        private UGUIWithName ResolveName(UGUI uui)
        {
            UGUIWithName resultUui;
            if (m_AvatarNameService.TryGetValue(uui, out resultUui))
            {
                return resultUui;
            }
            return (UGUIWithName)uui;
        }

        public void ShowEstatesCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            List<EstateInfo> estates;

            if (args[0] == "help")
            {
                io.Write("show estates");
                return;
            }

            estates = m_EstateService.All;

            var output = new StringBuilder("Estate List:\n----------------------------------------------");
            foreach (EstateInfo estateInfo in estates)
            {
                output.AppendFormat("\nEstate {0} [{1}]:\n  Owner={2}\n", estateInfo.Name, estateInfo.ID, ResolveName(estateInfo.Owner).FullName);
            }
            io.Write(output.ToString());
        }

        public void ChangeEstateCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            EstateInfo estateInfo;
            uint estateID;
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("create region not allowed from restricted console");
            }
            else if (args[0] == "help" || args.Count < 3)
            {
                io.Write("change estate <estateid> parameters...\n\n" +
                    "Parameters:\n" +
                    "name <name>\n" +
                    "parentestateid <id>\n" +
                    "owner <uui>|<uuid>|<firstname>.<lastname>\n" +
                    "pricepermeter <value>\n" +
                    "billablefactor <factor>\n" +
                    "abuseemail <email>");
            }
            else if(!uint.TryParse(args[2], out estateID))
            {
                io.WriteFormatted("{0} is not a valid number.", args[2]);
            }
            else if (!m_EstateService.TryGetValue(estateID, out estateInfo))
            {
                io.WriteFormatted("Estate with id {0} does not exist.", estateID);
            }
            else
            {
                int argi;
                bool changeEstateData = false;
                for (argi = 3; argi < args.Count; argi += 2)
                {
                    switch (args[argi].ToLower())
                    {
                        case "name":
                            estateInfo.Name = args[argi + 1];
                            changeEstateData = true;
                            break;

                        case "abuseemail":
                            estateInfo.AbuseEmail = args[argi + 1];
                            changeEstateData = true;
                            break;

                        case "parentestateid":
                            if (!uint.TryParse(args[argi + 1], out estateInfo.ParentEstateID))
                            {
                                io.WriteFormatted("{0} is not a number", args[argi + 1]);
                                return;
                            }
                            changeEstateData = true;
                            break;

                        case "owner":
                            if(!m_AvatarNameService.TranslateToUUI(args[argi + 1], out estateInfo.Owner))
                            {
                                io.WriteFormatted("{0} is not a valid owner.", args[argi + 1]);
                                return;
                            }
                            changeEstateData = true;
                            break;

                        case "billablefactor":
                            if(!double.TryParse(args[argi + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out estateInfo.BillableFactor))
                            {
                                io.WriteFormatted("{0} is not a valid float number.", args[argi + 1]);
                                return;
                            }
                            changeEstateData = true;
                            break;

                        case "pricepermeter":
                            if (!int.TryParse(args[argi + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out estateInfo.PricePerMeter))
                            {
                                io.WriteFormatted("{0} is not a valid integer number.", args[argi + 1]);
                                return;
                            }
                            changeEstateData = true;
                            break;

                        default:
                            io.WriteFormatted("Parameter {0} is not valid.", args[argi]);
                            return;
                    }
                }

                if (changeEstateData)
                {
                    try
                    {
                        m_EstateService.Update(estateInfo);
                    }
                    catch (Exception e)
                    {
                        io.WriteFormatted("Could not change estate parameters: {0}", e.Message);
                    }

                    /* trigger estate data update */
                    foreach (UUID regionid in m_EstateService.RegionMap[estateInfo.ID])
                    {
                        SceneInterface scene;
                        if (m_Scenes.TryGetValue(regionid, out scene))
                        {
                            scene.TriggerEstateUpdate();
                        }
                    }
                }
            }
        }

        public void CreateEstateCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            EstateInfo estateInfo;
            uint estateID;
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("create estate not allowed from restricted console");
            }
            else if (args[0] == "help" || args.Count < 4)
            {
                io.Write("create estate <estatename> <estateid> parameters...\n\n" +
                    "Parameters:\n" +
                    "owner <uui>|<uuid>|<firstname>.<lastname>\n" +
                    "parentestateid <parentestateid>\n" +
                    "pricepermeter <value>\n" +
                    "billablefactor <factor>\n" +
                    "abuseemail <email>");
            }
            else if(!uint.TryParse(args[3], out estateID))
            {
                io.Write("Estate ID is not a number.");
            }
            else if(m_EstateService.ContainsKey(estateID))
            {
                io.WriteFormatted("Estate with id {0} already exists.", estateID);
            }
            else
            {
                estateInfo = new EstateInfo
                {
                    ID = estateID,
                    Name = args[2],
                    PricePerMeter = 1,
                    BillableFactor = 1
                };
                for (int argi = 4; argi + 1 < args.Count; argi += 2)
                {
                    switch (args[argi].ToLower())
                    {
                        case "abuseemail":
                            estateInfo.AbuseEmail = args[argi + 1];
                            break;

                        case "parentestateid":
                            if (!uint.TryParse(args[argi + 1], out estateInfo.ParentEstateID))
                            {
                                io.WriteFormatted("{0} is not a number", args[argi + 1]);
                                return;
                            }
                            break;

                        case "owner":
                            if (!m_AvatarNameService.TranslateToUUI(args[argi + 1], out estateInfo.Owner))
                            {
                                io.WriteFormatted("{0} is not a valid owner.", args[argi + 1]);
                                return;
                            }
                            break;

                        case "billablefactor":
                            if (!double.TryParse(args[argi + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out estateInfo.BillableFactor))
                            {
                                io.WriteFormatted("{0} is not a valid float number.", args[argi + 1]);
                                return;
                            }
                            break;

                        case "pricepermeter":
                            if (!int.TryParse(args[argi + 1], out estateInfo.PricePerMeter))
                            {
                                io.WriteFormatted("{0} is not a valid integer number.", args[argi + 1]);
                                return;
                            }
                            break;

                        default:
                            io.WriteFormatted("Parameter {0} is not valid.", args[argi]);
                            return;
                    }
                }

                try
                {
                    m_EstateService.Update(estateInfo);
                }
                catch (Exception e)
                {
                    io.WriteFormatted("Could not create estate: {0}", e.Message);
                }
            }
        }

        public void DeleteEstateCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            uint estateID;
            EstateInfo estateInfo;
            List<UUID> regions;
            if (limitedToScene != UUID.Zero)
            {
                io.WriteFormatted("delete estate not allowed from restricted console");
            }
            else if (args[0] == "help" || args.Count < 3)
            {
                io.Write("delete estate <estateid>");
            }
            else if (!uint.TryParse(args[2], out estateID))
            {
                io.WriteFormatted("{0} is not a valid estate id.", estateID);
            }
            else if (!m_EstateService.TryGetValue(estateID, out estateInfo))
            {
                io.WriteFormatted("Estate with id {0} does not exist.", estateID);
            }
            else
            {
                regions = m_EstateService.RegionMap[estateID];
                if (m_EstateService.RegionMap[estateID].Count != 0)
                {
                    var output = new StringBuilder("Please unlink regions from estate first.\n\nLinked Scene List:\n----------------------------------------------");
                    foreach (UUID rID in regions)
                    {
                        Types.Grid.RegionInfo rInfo;
                        if (m_RegionStorage.TryGetValue(UUID.Zero, rID, out rInfo))
                        {
                            Vector3 gridcoord = rInfo.Location;
                            output.AppendFormat("\nRegion {0} [{1}]:\n  Location={2} (grid coordinate {5})\n  Size={3}\n  Owner={4}\n", rInfo.Name, rInfo.ID, gridcoord.ToString(), rInfo.Size.ToString(), ResolveName(rInfo.Owner).FullName, gridcoord.X_String + "," + gridcoord.Y_String);
                        }
                    }
                    io.Write(output.ToString());
                }
                else
                {
                    try
                    {
                        if(!m_EstateService.Remove(estateID))
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    catch (Exception e)
                    {
                        io.WriteFormatted("Could not delete estate {0}: {1}", estateID, e.Message);
                    }
                }
            }
        }

        public void AlertEstateCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            uint estateID;
            if (limitedToScene != UUID.Zero)
            {
                io.Write("alert estate is not allowed from restricted console");
                return;
            }
            else if (args[0] == "help" || args.Count < 4)
            {
                io.Write("alert estate <id> <message>");
                return;
            }
            else if (!uint.TryParse(args[2], out estateID))
            {
                io.Write("Invalid estate id.");
                return;
            }

            string msg = string.Join(" ", args.GetRange(3, args.Count - 3));
            foreach (UUID regionID in m_EstateService.RegionMap[estateID])
            {
                SceneInterface scene;
                if (m_Scenes.TryGetValue(regionID, out scene))
                {
                    foreach (IAgent agent in scene.RootAgents)
                    {
                        agent.SendAlertMessage(msg, scene.ID);
                    }
                }
            }
        }
    }
}
