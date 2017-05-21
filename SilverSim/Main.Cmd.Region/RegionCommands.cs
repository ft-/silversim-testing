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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SilverSim.Main.Cmd.Region
{
    #region Service Implementation
    [Description("Region Console Commands")]
    public class RegionCommands : IPlugin
    {
        private readonly string m_RegionStorageName;
        private readonly string m_EstateServiceName;
        private readonly string m_SimulationStorageName;
        private GridServiceInterface m_RegionStorage;
        private SceneFactoryInterface m_SceneFactory;
        private EstateServiceInterface m_EstateService;
        private SimulationDataStorageInterface m_SimulationData;
        private static readonly ILog m_Log = LogManager.GetLogger("REGION COMMANDS");
        private ExternalHostNameServiceInterface m_ExternalHostNameService;
        private ConfigurationLoader m_Loader;
        private BaseHttpServer m_HttpServer;
        private SceneList m_Scenes;
        private AvatarNameServiceInterface m_AvatarNameService;

        public RegionCommands(string regionStorageName, string estateServiceName, string simulationStorageName)
        {
            m_RegionStorageName = regionStorageName;
            m_EstateServiceName = estateServiceName;
            m_SimulationStorageName = simulationStorageName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_HttpServer = loader.HttpServer;
            m_Scenes = loader.Scenes;
            m_Loader = loader;
            m_ExternalHostNameService = loader.ExternalHostNameService;

            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            m_RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");
            m_SimulationData = loader.GetService<SimulationDataStorageInterface>(m_SimulationStorageName);
            loader.CommandRegistry.AddCreateCommand("region", CreateRegionCmd);
            loader.CommandRegistry.AddCreateCommand("regions", CreateRegionsCmd);
            loader.CommandRegistry.AddDeleteCommand("region", DeleteRegionCmd);
            loader.CommandRegistry.AddShowCommand("regionstats", ShowRegionStatsCmd);
            loader.CommandRegistry.AddShowCommand("regions", ShowRegionsCmd);
            loader.CommandRegistry.AddEnableCommand("region", EnableRegionCmd);
            loader.CommandRegistry.AddDisableCommand("region", DisableRegionCmd);
            loader.CommandRegistry.AddStartCommand("region", StartRegionCmd);
            loader.CommandRegistry.AddStopCommand("region", StopRegionCmd);
            loader.CommandRegistry.AddChangeCommand("region", ChangeRegionCmd);
            loader.CommandRegistry.AddAlertCommand("region", AlertRegionCmd);
            loader.CommandRegistry.AddRestartCommand("region", RestartRegionCmd);
            loader.CommandRegistry.AddAlertCommand("regions", AlertRegionsCmd);
            loader.CommandRegistry.AddAlertCommand("agent", AlertAgentCmd);
            loader.CommandRegistry.AddKickCommand("agent", KickAgentCmd);
            loader.CommandRegistry.AddShowCommand("agents", ShowAgentsCmd);
            loader.CommandRegistry.AddEnableCommand("logins", EnableDisableLoginsCmd);
            loader.CommandRegistry.AddDisableCommand("logins", EnableDisableLoginsCmd);
            loader.CommandRegistry.AddShowCommand("neighbors", ShowNeighborsCmd);
            loader.CommandRegistry.AddClearCommand("objects", ClearObjectsCmd);
            loader.CommandRegistry.AddClearCommand("parcels", ClearParcelsCmd);
            loader.CommandRegistry.AddClearCommand("region", ClearRegionCmd);
            loader.CommandRegistry.AddSelectCommand("region", SelectRegionCmd);
            loader.CommandRegistry.AddShowCommand("parcels", ShowParcelsCmd);
            loader.CommandRegistry.AddGetCommand("windvelocity", GetWindVelocityCmd);
            loader.CommandRegistry.AddSetCommand("windvelocity", SetWindVelocityCmd);
            loader.CommandRegistry.AddGetCommand("windpresetvelocity", GetWindPresetVelocityCmd);
            loader.CommandRegistry.AddSetCommand("windpresetvelocity", SetWindPresetVelocityCmd);
            loader.CommandRegistry.AddGetCommand("sunparam", GetSunParamCmd);
            loader.CommandRegistry.AddSetCommand("sunparam", SetSunParamCmd);
            loader.CommandRegistry.AddResetCommand("sunparam", ResetSunParamCmd);
            loader.CommandRegistry.AddGetCommand("moonparam", GetMoonParamCmd);
            loader.CommandRegistry.AddSetCommand("moonparam", SetMoonParamCmd);
            loader.CommandRegistry.AddResetCommand("moonparam", ResetMoonParamCmd);
            loader.CommandRegistry.AddEnableCommand("tidal", EnableTidalParamCmd);
            loader.CommandRegistry.AddDisableCommand("tidal", DisableTidalParamCmd);
            loader.CommandRegistry.AddGetCommand("tidalparam", GetTidalParamCmd);
            loader.CommandRegistry.AddSetCommand("tidalparam", SetTidalParamCmd);
            loader.CommandRegistry.AddResetCommand("tidalparam", ResetTidalParamCmd);
            loader.CommandRegistry.AddResetCommand("windparam", ResetWindParamCmd);
            loader.CommandRegistry.AddGetCommand("waterheight", GetWaterheightCmd);
            loader.CommandRegistry.AddSetCommand("waterheight", SetWaterheightCmd);
            loader.CommandRegistry.Commands.Add("rebake", RebakeCmd);
            loader.CommandRegistry.AddEnableCommand("script", EnableScriptCmd);
            loader.CommandRegistry.AddDisableCommand("script", DisableScriptCmd);
            loader.CommandRegistry.AddEnableCommand("scripts", EnableScriptsCmd);
            loader.CommandRegistry.AddShowCommand("scripts", ShowScriptsCmd);
            loader.CommandRegistry.AddClearCommand("hacdcache", ClearHacdCacheCmd);

            IConfig sceneConfig = loader.Config.Configs["DefaultSceneImplementation"];
            var avatarNameServicesList = new RwLockedList<AvatarNameServiceInterface>();
            if (sceneConfig != null)
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

        private UUI ResolveName(UUI uui)
        {
            UUI resultUui;
            if(m_AvatarNameService.TryGetValue(uui, out resultUui))
            {
                return resultUui;
            }
            return uui;
        }

        [Description("Clear HACD cache")]
        private void ClearHacdCacheCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            if(args[0] == "help")
            {
                io.Write("clear hacdcache\nOnly use this command in case of physics having wrong shapes loaded. \nAll regions have to be restarted.");
            }
            else if(limitedToScene != UUID.Zero)
            {
                io.Write("clearing HACD cache cannot be done on limited console.");
            }
            else
            {
                var beforePsm = new List<IPhysicsHacdCleanCache>();
                var withPsm = new List<IPhysicsHacdCleanCache>();
                var afterPsm = new List<IPhysicsHacdCleanCache>();
                foreach(IPhysicsHacdCleanCache service in m_Loader.GetServicesByValue<IPhysicsHacdCleanCache>())
                {
                    switch(service.CleanOrder)
                    {
                        case HacdCleanCacheOrder.BeforePhysicsShapeManager:
                            beforePsm.Add(service);
                            break;

                        case HacdCleanCacheOrder.PhysicsShapeManager:
                            withPsm.Add(service);
                            break;

                        case HacdCleanCacheOrder.AfterPhysicsShapeManager:
                            afterPsm.Add(service);
                            break;

                        default:
                            break;
                    }
                }

                try
                {
                    foreach (IPhysicsHacdCleanCache service in beforePsm)
                    {
                        service.CleanCache();
                    }
                    foreach (IPhysicsHacdCleanCache service in withPsm)
                    {
                        service.CleanCache();
                    }
                    foreach (IPhysicsHacdCleanCache service in afterPsm)
                    {
                        service.CleanCache();
                    }
                    io.Write("Restart all regions now for rebuilding HACD data.");
                }
                catch(Exception e)
                {
                    io.WriteFormatted("Could not clean HACD cache: {0}", e.Message);
                }
            }
        }

        private void ShowScriptsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help")
            {
                io.Write("show scripts [functional|not functional] [running|not running]");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("show scripts needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            bool excludeRunning = false;
            bool excludeNotRunning = false;
            bool excludeFunctional = false;
            bool excludeNonFunctional = false;
            bool negate = false;

            for(int i = 2; i < args.Count; ++i)
            {
                switch(args[i])
                {
                    case "not":
                        negate = true;
                        break;

                    case "running":
                        excludeNotRunning = !negate;
                        excludeRunning = negate;
                        negate = false;
                        break;

                    case "functional":
                        excludeNonFunctional = !negate;
                        excludeFunctional = negate;
                        negate = false;
                        break;

                    default:
                        break;
                }
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
            }
            else
            {
                foreach(ObjectPart part in scene.Primitives)
                {
                    ObjectGroup group = part.ObjectGroup;
                    if(group == null)
                    {
                        continue;
                    }
                    foreach (ObjectPartInventoryItem item in part.Inventory.Values)
                    {
                        ScriptInstance instance = item.ScriptInstance;
                        if(item.InventoryType == Types.Inventory.InventoryType.LSLText)
                        {
                            if(excludeNonFunctional && instance == null)
                            {
                                continue;
                            }
                            if (instance != null)
                            {
                                if (excludeFunctional)
                                {
                                    continue;
                                }
                                if (excludeRunning && instance.IsRunning)
                                {
                                    continue;
                                }
                                if (excludeNotRunning && !instance.IsRunning)
                                {
                                    continue;
                                }
                            }
                            io.WriteFormatted("Script {0} ({1})\n- Primitive: {2} ({3})\n- Object: {4} ({5}):\n- Functional: {6}\n- Running: {7}",
                                item.Name, item.AssetID,
                                part.Name, part.ID,
                                group.Name, group.ID,
                                instance != null ? "yes" : "no",
                                instance?.IsRunning == true ? "running" : "not running");
                        }
                    }
                }
            }
        }

        private void EnableScriptCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            UUID selectedPart;
            if (args[0] == "help" || args.Count < 4)
            {
                io.Write("enable script <prim-id> <script-name>");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("enable script needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            ObjectPart part;
            ObjectPartInventoryItem item;
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
            }
            else if (!UUID.TryParse(args[2], out selectedPart))
            {
                io.Write("invalid uuid");
            }
            else if (!scene.Primitives.TryGetValue(selectedPart, out part))
            {
                io.Write("primitive not found");
            }
            else if(!part.Inventory.TryGetValue(args[3], out item))
            {
                io.Write("item not found");
            }
            else
            {
                ScriptInstance instance = item.ScriptInstance;
                if (instance == null)
                {
                    io.Write("item is not a valid script to be controlled");
                }
                else
                {
                    instance.IsRunning = true;
                    io.Write("script is set running");
                }
            }
        }

        private void EnableScriptsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help")
            {
                io.Write("enable scripts");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("enable scripts needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
            }
            else
            {
                int count = 0;
                foreach(ObjectPart part in scene.Primitives)
                {
                    foreach(ObjectPartInventoryItem item in part.Inventory.Values)
                    {
                        ScriptInstance instance = item.ScriptInstance;
                        if(instance?.IsRunning == false)
                        {
                            instance.IsRunning = true;
                            ++count;
                        }
                    }
                }
                io.WriteFormatted("Scripts enabled: {0}", count);
            }
        }

        private void DisableScriptCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            UUID selectedPart;
            if (args[0] == "help" || args.Count < 4)
            {
                io.Write("disable script <prim-id> <script-name>");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("disable script needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            ObjectPart part;
            ObjectPartInventoryItem item;
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
            }
            else if (!UUID.TryParse(args[2], out selectedPart))
            {
                io.Write("invalid uuid");
            }
            else if (!scene.Primitives.TryGetValue(selectedPart, out part))
            {
                io.Write("primitive not found");
            }
            else if (!part.Inventory.TryGetValue(args[3], out item))
            {
                io.Write("item not found");
            }
            else
            {
                ScriptInstance instance = item.ScriptInstance;
                if (instance == null)
                {
                    io.Write("item is not a valid script to be controlled");
                }
                else
                {
                    instance.IsRunning = true;
                    io.Write("script is set running");
                }
            }
        }

        #region Region control commands
        private void ChangeRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
                    "owner <uui>|<uuid>|<firstname>.<lastname>\n" +
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
                            if (Uri.IsWellFormedUriString(rInfo.Name, UriKind.Absolute))
                            {
                                io.WriteFormatted("Naming an region based on URI structure is not allowed. See {0}", rInfo.Name);
                                return;
                            }
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
                            if(string.IsNullOrEmpty(rInfo.ProductName))
                            {
                                rInfo.ProductName = "Mainland";
                            }
                            changeRegionData = true;
                            break;

                        case "owner":
                            if (!m_AvatarNameService.TranslateToUUI(args[argi + 1], out rInfo.Owner))
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
                if (m_Scenes.TryGetValue(rInfo.ID, out si))
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
                if (selectedEstate != null)
                {
                    m_EstateService.RegionMap[rInfo.ID] = selectedEstate.ID;
                }
            }
        }

        private void CreateRegionsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
                        using (var s = Http.Client.HttpClient.DoStreamGetRequest(args[4], null, 20000))
                        {
                            using (var r = XmlReader.Create(s))
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

                var msg = new StringBuilder();

                foreach(IConfig regionEntry in cfg.Configs)
                {
                    if(Uri.IsWellFormedUriString(regionEntry.Name, UriKind.Absolute))
                    {
                        io.WriteFormatted("Naming an region based on URI structure is not allowed. See {0}", regionEntry.Name);
                        return;
                    }
                }

                foreach (IConfig regionEntry in cfg.Configs)
                {
                    var r = new RegionInfo()
                    {
                        Name = regionEntry.Name,
                        ID = regionEntry.GetString("RegionUUID"),
                        Location = new GridVector(regionEntry.GetString("Location"), 256),
                        ServerPort = (uint)regionEntry.GetInt("InternalPort"),
                        ServerURI = string.Empty,
                        Size = new GridVector
                        {
                            X = ((uint)regionEntry.GetInt("SizeX", 256) + 255) & (~(uint)255),
                            Y = ((uint)regionEntry.GetInt("SizeY", 256) + 255) & (~(uint)255)
                        },
                        Flags = RegionFlags.RegionOnline,
                        ProductName = regionEntry.GetString("RegionType", "Mainland"),
                        Owner = new UUI(regionEntry.GetString("Owner")),
                        ScopeID = regionEntry.GetString("ScopeID", "00000000-0000-0000-0000-000000000000"),
                        ServerHttpPort = m_HttpServer.Port,
                        RegionMapTexture = regionEntry.GetString("MaptileStaticUUID", "00000000-0000-0000-0000-000000000000")
                    };
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

                    r.ServerIP = string.Empty;
                    RegionInfo rInfoCheck;
                    if (m_RegionStorage.TryGetValue(UUID.Zero, r.Name, out rInfoCheck))
                    {
                        if (msg.Length != 0)
                        {
                            msg.Append("\n");
                        }
                        msg.AppendFormat("Region {0} is already used by region id {1}. Skipping.", rInfoCheck.Name, rInfoCheck.ID);
                    }
                    else
                    {
                        m_RegionStorage.RegisterRegion(r);
                        List<EstateInfo> allEstates = m_EstateService.All;
                        var ownerEstates = new List<EstateInfo>(from estate in allEstates where estate.Owner.EqualsGrid(r.Owner) select estate);
                        if (ownerEstates.Count != 0)
                        {
                            m_EstateService.RegionMap[r.ID] = ownerEstates[0].ID;
                            msg.AppendFormat("Assigning new region {0} to estate {1} owned by {2}", r.Name, allEstates[0].Name, ResolveName(allEstates[0].Owner).FullName);
                        }
                        else if (allEstates.Count != 0)
                        {
                            m_EstateService.RegionMap[r.ID] = allEstates[0].ID;
                            msg.AppendFormat("Assigning new region {0} to estate {1} owned by {2}", r.Name, allEstates[0].Name, ResolveName(allEstates[0].Owner).FullName);
                        }
                    }
                }

                if(msg.Length != 0)
                {
                    msg.Append("\n");
                    io.Write(msg.ToString());
                }
            }
            else
            {
                io.Write("wrong command line for create regions");
                return;
            }
        }

        private void CreateRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
                    "owner <uui>|<uuid>|<firstname>.<lastname>\n" +
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
            else if (Uri.IsWellFormedUriString(args[2], UriKind.Absolute))
            {
                io.WriteFormatted("Naming an region based on URI structure is not allowed. See {0}", args[2]);
                return;
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                io.WriteFormatted("Region with name {0} already exists.", args[2]);
            }
            else
            {
                EstateInfo selectedEstate = null;
                rInfo = new RegionInfo()
                {
                    Name = args[2],
                    ID = UUID.Random,
                    Access = RegionAccess.Mature,
                    ServerHttpPort = m_HttpServer.Port,
                    ScopeID = UUID.Zero,
                    ServerIP = string.Empty,
                    Size = new GridVector(256, 256),
                    ProductName = "Mainland"
                };
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
                if (rInfo.Size.X % 256 != 0 || rInfo.Size.Y % 256 != 0)
                {
                    io.Write("Invalid region size " + rInfo.ToString());
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
                            if(string.IsNullOrEmpty(rInfo.ProductName))
                            {
                                rInfo.ProductName = "Mainland";
                            }
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
                            if (!m_AvatarNameService.TranslateToUUI(args[argi + 1], out rInfo.Owner))
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
                rInfo.ServerURI = string.Empty;
                m_RegionStorage.RegisterRegion(rInfo);

                if (selectedEstate != null)
                {
                    m_EstateService.RegionMap[rInfo.ID] = selectedEstate.ID;
                    io.WriteFormatted("Assigning new region {0} to estate {1} owned by {2}", rInfo.Name, selectedEstate.Name, selectedEstate.Owner.FullName);
                }
                else
                {
                    List<EstateInfo> allEstates = m_EstateService.All;
                    var ownerEstates = new List<EstateInfo>(from estate in allEstates where estate.Owner.EqualsGrid(rInfo.Owner) select estate);
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

        private void DeleteRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
                if(!m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
                {
                    io.WriteFormatted("Region '{0}' not found", args[2]);
                }
                else if(m_Scenes.ContainsKey(rInfo.ID))
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

        private void RestartRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
                if(!m_Scenes.TryGetValue(rInfo.ID, out scene))
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

        private void EnableRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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

        private void DisableRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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

        private void StartRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if (args.Count < 3)
            {
                io.Write("missing region name");
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                SceneInterface si;
                if (m_Scenes.TryGetValue(rInfo.ID, out si))
                {
                    io.Write(string.Format("Region '{0}' ({1}) is already started", rInfo.Name, rInfo.ID.ToString()));
                }
                else
                {
                    rInfo.GridURI = m_Loader.GatekeeperURI;
                    if (string.IsNullOrEmpty(rInfo.ServerIP))
                    {
                        rInfo.ServerIP = m_ExternalHostNameService.ExternalHostName;
                    }

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
                    m_Scenes.Add(si);
                    si.LoadScene();
                }
            }
        }

        private void StopRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if (args.Count < 3)
            {
                io.Write("missing region name");
            }
            else if (m_RegionStorage.TryGetValue(UUID.Zero, args[2], out rInfo))
            {
                SceneInterface si;
                if (!m_Scenes.TryGetValue(rInfo.ID, out si))
                {
                    io.Write(string.Format("Region '{0}' ({1}) is not started", rInfo.Name, rInfo.ID.ToString()));
                }
                else
                {
                    m_Scenes.Remove(si);
                }
            }
        }
        #endregion

        #region Region and Simulator notice
        private void AlertRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
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

        private void AlertRegionsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            foreach (SceneInterface scene in m_Scenes.Values)
            {
                foreach (IAgent agent in scene.RootAgents)
                {
                    agent.SendAlertMessage(msg, scene.ID);
                }
            }
        }
        #endregion

        #region Agent control (Login/Messages/Kick)
        private void AlertAgentCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
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

        private void RebakeCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help" || args.Count < 3)
            {
                io.Write("rebake <firstname> <lastname>");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("rebake needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            bool agentFound = false;
            foreach (IAgent agent in scene.RootAgents)
            {
                UUI agentid = agent.Owner;
                if (agentid.FullName.ToLower() == (args[1] + " " + args[2]).ToLower())
                {
                    agent.RebakeAppearance(io.Write);
                    agentFound = true;
                }
            }

            if (!agentFound)
            {
                io.WriteFormatted("Agent {0} {1} not found.", args[1], args[2]);
            }
        }

        private void KickAgentCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
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

        private void EnableDisableLoginsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
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
        private void ShowRegionStatsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else
            {
                selectedScene = io.SelectedScene;
            }

            var formattedList = new FormattedListBuilder()
                .AddColumn("Region", 30)
                .AddColumn("Env FPS", 10)
                .AddColumn("Phys FPS", 10)
                .AddColumn("Phys Engine", 20)
                .AddHeader()
                .AddSeparator();
            if (selectedScene == UUID.Zero)
            {
                foreach (SceneInterface scene in m_Scenes.Values)
                {
                    formattedList.AddData(
                        scene.Name,
                        scene.Environment.EnvironmentFps.ToString("N2"),
                        scene.PhysicsScene.PhysicsFPS.ToString("N2"),
                        scene.PhysicsScene.PhysicsEngineName);
                }
            }
            else
            {
                SceneInterface scene;
                if (!m_Scenes.TryGetValue(selectedScene, out scene))
                {
                    io.Write("no scene selected");
                    return;
                }
                formattedList.AddData(
                    scene.Name,
                    scene.Environment.EnvironmentFps.ToString("N2"),
                    scene.PhysicsScene.PhysicsFPS.ToString("N2"),
                    scene.PhysicsScene.PhysicsEngineName);
            }
            io.Write(formattedList.ToString());
        }

        private void ShowRegionsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
                var regionList = new List<RegionInfo>();
                foreach (SceneInterface scene in m_Scenes.Values)
                {
                    regionList.Add(scene.GetRegionInfo());
                }
                regions = regionList;
            }
            else if (args[2] == "offline")
            {
                List<UUID> onlineRegions = new List<UUID>();

                foreach (SceneInterface scene in m_Scenes.Values)
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

            var output = new StringBuilder("Scene List:\n----------------------------------------------");
            foreach (RegionInfo rInfo in regions)
            {
                if (limitedToScene == UUID.Zero || rInfo.ID == limitedToScene)
                {
                    Vector3 gridcoord = rInfo.Location;
                    output.AppendFormat("\nRegion {0} [{1}]: (Port {6})\n  Location={2} (grid coordinate {5})\n  Size={3}\n  Owner={4}\n  GatekeeperURI={7}\n",
                        rInfo.Name, rInfo.ID,
                        gridcoord.ToString(),
                        rInfo.Size.ToString(),
                        ResolveName(rInfo.Owner).FullName,
                        gridcoord.X_String + "," + gridcoord.Y_String,
                        rInfo.ServerPort,
                        m_Loader.GatekeeperURI);
                }
            }
            io.Write(output.ToString());
        }

        private void ShowNeighborsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            var output = new StringBuilder("Neighbor List:\n----------------------------------------------");
            foreach (SceneInterface.NeighborEntry neighborInfo in scene.Neighbors.Values)
            {
                Vector3 gridcoord = neighborInfo.RemoteRegionData.Location;
                output.AppendFormat("\nRegion {0} [{1}]:\n  Location={2} (grid coordinate {5})\n  Size={3}\n  Owner={4}\n",
                    neighborInfo.RemoteRegionData.Name,
                    neighborInfo.RemoteRegionData.ID, gridcoord.ToString(),
                    neighborInfo.RemoteRegionData.Size.ToString(),
                    ResolveName(neighborInfo.RemoteRegionData.Owner).FullName,
                    gridcoord.X_String + "," + gridcoord.Y_String);
            }
            io.Write(output.ToString());
        }

        private void SetWindPresetVelocityCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help" || args.Count != 4)
            {
                io.Write("set windpresetvelocity <x,y,z> <velx,vely,velz> - Set wind preset at position x,y,z to <velx,vely,velz>");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("set windpresetvelocity needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            Vector3 pos;
            Vector3 vel;
            if (!Vector3.TryParse("<" + args[2] + ">", out pos))
            {
                io.Write("invalid position specified");
                return;
            }
            if (!Vector3.TryParse("<" + args[3] + ">", out vel))
            {
                io.Write("invalid velocity specified");
                return;
            }

            scene.Environment.Wind.PresetWind[pos] = vel;
            io.WriteFormatted("Set wind preset velocity at {0}: {1}", pos.ToString(), vel.ToString());
        }

        private void GetWindPresetVelocityCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help" || args.Count != 3)
            {
                io.Write("get windpresetvelocity <x,y,z> - get wind present velocity at position x,y,z");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("get windpresetvelocity needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            Vector3 pos;
            if (!Vector3.TryParse("<" + args[2] + ">", out pos))
            {
                io.Write("invalid position specified");
                return;
            }

            io.WriteFormatted("Wind preset velocity at {0}: {1}", pos.ToString(), scene.Environment.Wind.PresetWind[pos].ToString());
        }

        private void SetWindVelocityCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help" || args.Count != 4)
            {
                io.Write("set windvelocity <x,y,z> <velx,vely,velz> - Set wind at position x,y,z to <velx,vely,velz>");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("set windvelocity needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            Vector3 pos;
            Vector3 vel;
            if (!Vector3.TryParse("<" + args[2] + ">", out pos))
            {
                io.Write("invalid position specified");
                return;
            }
            if (!Vector3.TryParse("<" + args[3] + ">", out vel))
            {
                io.Write("invalid velocity specified");
                return;
            }

            scene.Environment.Wind[pos] = vel;
            io.WriteFormatted("Set wind velocity at {0}: {1}", pos.ToString(), vel.ToString());
        }

        private void GetWindVelocityCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene;
            if (args[0] == "help" || args.Count > 3)
            {
                io.Write("get windvelocity - Get prevailing wind velocity\nget windvelocity <x,y,z> - get wind velocity at position x,y,z");
                return;
            }
            else if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }
            else if (io.SelectedScene == UUID.Zero)
            {
                io.Write("get windvelocity needs a selected region before.");
                return;
            }
            else
            {
                selectedScene = io.SelectedScene;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            if (args.Count == 3)
            {
                Vector3 pos;
                if(!Vector3.TryParse("<" + args[2] + ">", out pos))
                {
                    io.Write("invalid position specified");
                    return;
                }

                io.WriteFormatted("Wind velocity at {0}: {1}", pos.ToString(), scene.Environment.Wind[pos].ToString());
            }
            else
            {
                io.WriteFormatted("Prevailing wind velocity: {0}", scene.Environment.Wind.PrevailingWind.ToString());
            }
        }

        private void ShowAgentsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            if (!m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("no scene selected");
                return;
            }

            IEnumerable<IAgent> agents;
            var output = new StringBuilder();
            if (args.Count == 3 && args[2] == "full")
            {
                agents = scene.Agents;
                output.Append("All Agents: -----------------\n");
            }
            else
            {
                agents = scene.RootAgents;
                output.Append("Root Agents: -----------------\n");
            }

            foreach(IAgent agent in agents)
            {
                output.AppendFormat("\n{0}\n    id={1}  Type={2}\n", agent.Owner.FullName, agent.Owner.ID.ToString(), agent.IsInScene(scene) ? "Root" : "Child");
            }
            io.Write(output.ToString());
        }

        private void ShowParcelsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID sceneID = UUID.Zero != limitedToScene ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (!m_Scenes.TryGetValue(sceneID, out scene))
            {
                io.Write("No region selected.");
            }
            else
            {
                StringBuilder output = new StringBuilder("Parcel List:\n--------------------------------------------------------------------------------");
                foreach (ParcelInfo parcel in scene.Parcels)
                {
                    output.AppendFormat("\nParcel {0} ({1}):\n  Owner={2}\n",
                        parcel.Name,
                        parcel.ID,
                        ResolveName(parcel.Owner).FullName);
                }
                io.Write(output.ToString());
            }
        }
        #endregion

        #region Clear commands
        private void ClearObjectsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("clear all region objects");
                return;
            }
            else if (UUID.Zero != limitedToScene)
            {
                scene = m_Scenes[limitedToScene];
            }
            else if (UUID.Zero != io.SelectedScene)
            {
                scene = m_Scenes[io.SelectedScene];
            }
            else
            {
                io.Write("no region selected");
                return;
            }
            scene.ClearObjects();
            io.Write("All objects deleted.");
        }

        private void ClearParcelsCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("clears region parcels entirely");
                return;
            }
            else if (UUID.Zero != limitedToScene)
            {
                scene = m_Scenes[limitedToScene];
            }
            else if (UUID.Zero != io.SelectedScene)
            {
                scene = m_Scenes[io.SelectedScene];
            }
            else
            {
                io.Write("no region selected");
                return;
            }
            scene.ResetParcels();
            io.Write("Region parcels cleared");
        }

        private void ClearRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("clears region entirely");
                return;
            }
            else if (UUID.Zero != limitedToScene)
            {
                scene = m_Scenes[limitedToScene];
            }
            else if (UUID.Zero != io.SelectedScene)
            {
                scene = m_Scenes[io.SelectedScene];
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
        private void SelectRegionCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
                if (m_Scenes.TryGetValue(args[2], out scene))
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
        private void ResetSunParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("reset sunparam - reset sunparam to defaults");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment.ResetSunToDefaults();
            }
        }

        private void SetSunParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if(selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
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

        private void GetSunParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
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
        private void ResetMoonParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("reset moonparam - reset moon parameters to defaults");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment.ResetMoonToDefaults();
            }
        }

        private void SetMoonParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 4)
            {
                io.Write("set moonparam period <seconds>\n" +
                    "set moonparam phaseoffset <offset>");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
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

        private void GetMoonParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 3)
            {
                io.Write("get moonparam period\n" +
                    "get moonparam phaseoffset");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
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
        private void ResetTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("reset tidalparam - reset tidal parameters to defaults");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment.ResetTidalToDefaults();
            }
        }

        private void EnableTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("enable tidal");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment[EnvironmentController.BooleanWaterParams.EnableTideControl] = true;
            }
        }

        private void DisableTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("disable tidal");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("No region selected");
            }
            else
            {
                scene.Environment[EnvironmentController.BooleanWaterParams.EnableTideControl] = false;
            }
        }

        private void SetTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
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

        private void GetTidalParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
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
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
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
        private void ResetWindParamCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("reset windparam - reset wind parameters to defaults");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
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
        private void SetWaterheightCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help" || args.Count < 3)
            {
                io.Write("set waterheight <waterheight>");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
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

        private void GetWaterheightCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            UUID selectedScene = limitedToScene != UUID.Zero ? limitedToScene : io.SelectedScene;
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("get waterheight");
            }
            else if (selectedScene == UUID.Zero || !m_Scenes.TryGetValue(selectedScene, out scene))
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
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new RegionCommands(ownSection.GetString("RegionStorage", "RegionStorage"),
                ownSection.GetString("EstateService", "EstateService"),
                ownSection.GetString("SimulationDataStorage", "SimulationDataStorage"));
    }
    #endregion
}
