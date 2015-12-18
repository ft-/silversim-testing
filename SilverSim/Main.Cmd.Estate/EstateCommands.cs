// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Estate;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SilverSim.Main.Cmd.Estate
{
    public class EstateCommands : IPlugin
    {
        readonly string m_RegionStorageName;
        readonly string m_EstateServiceName;
        GridServiceInterface m_RegionStorage;
        EstateServiceInterface m_EstateService;
        readonly List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();
        private static readonly ILog m_Log = LogManager.GetLogger("ESTATE COMMANDS");

        public EstateCommands(string regionStorageName, string estateServiceName)
        {
            m_RegionStorageName = regionStorageName;
            m_EstateServiceName = estateServiceName;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            m_RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            Common.CmdIO.CommandRegistry.ShowCommands.Add("estates", ShowEstatesCmd);
            Common.CmdIO.CommandRegistry.ChangeCommands.Add("estate", ChangeEstateCmd);
            Common.CmdIO.CommandRegistry.CreateCommands.Add("estate", CreateEstateCmd);
            Common.CmdIO.CommandRegistry.DeleteCommands.Add("estate", DeleteEstateCmd);

            IConfig sceneConfig = loader.Config.Configs["DefaultSceneImplementation"];
            if(null != sceneConfig)
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

        bool TranslateToUUI(string arg, out UUI uui)
        {
            uui = UUI.Unknown;
            if (arg.Contains(","))
            {
                bool found = false;
                string[] names = arg.Split(new char[] { ',' }, 2);
                if(names.Length == 1)
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

        public void ShowEstatesCmd(List<string> args, Common.CmdIO.TTY io, UUID limitedToScene)
        {
            List<EstateInfo> estates;

            if (args[0] == "help")
            {
                io.Write("show estates");
                return;
            }

            estates = m_EstateService.All;


            string output = "Estate List:\n----------------------------------------------";
            foreach (EstateInfo estateInfo in estates)
            {
                output += string.Format("\nEstate {0} [{1}]:\n  Owner={2}\n", estateInfo.Name, estateInfo.ID, estateInfo.Owner.FullName);
            }
            io.Write(output);
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
                    "owner <uui>|<uuid>|<firstname>,<lastname>\n" +
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
                            if(!TranslateToUUI(args[argi + 1], out estateInfo.Owner))
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
                        m_EstateService[estateInfo.ID] = estateInfo;
                    }
                    catch (Exception e)
                    {
                        io.WriteFormatted("Could not change estate parameters: {0}", e.Message);
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
                    "owner <uui>|<uuid>|<firstname>,<lastname>\n" +
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
                estateInfo = new EstateInfo();
                estateInfo.ID = estateID;
                estateInfo.Name = args[2];
                estateInfo.PricePerMeter = 1;
                estateInfo.BillableFactor = 1;

                for (int argi = 4; argi + 1 < args.Count; argi += 2)
                {
                    switch (args[argi].ToLower())
                    {
                        case "name":
                            estateInfo.Name = args[argi + 1];
                            break;

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
                            if (!TranslateToUUI(args[argi + 1], out estateInfo.Owner))
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
                            if (!int.TryParse(args[argi + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out estateInfo.PricePerMeter))
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
                    m_EstateService[estateInfo.ID] = estateInfo;
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
                    string output = "Please unlink regions from estate first.\n\nLinked Scene List:\n----------------------------------------------";
                    foreach (UUID rID in regions)
                    {
                        Types.Grid.RegionInfo rInfo;
                        if (m_RegionStorage.TryGetValue(UUID.Zero, rID, out rInfo))
                        {
                            Vector3 gridcoord = rInfo.Location;
                            output += string.Format("\nRegion {0} [{1}]:\n  Location={2} (grid coordinate {5})\n  Size={3}\n  Owner={4}\n", rInfo.Name, rInfo.ID, gridcoord.ToString(), rInfo.Size.ToString(), rInfo.Owner.FullName, gridcoord.X_String + "," + gridcoord.Y_String);
                        }
                    }
                    io.Write(output);
                }
                else
                {
                    try
                    {
                        m_EstateService[estateID] = null;
                    }
                    catch (Exception e)
                    {
                        io.WriteFormatted("Could not delete estate {0}: {1}", estateID, e.Message);
                    }
                }
            }
        }

    }

    #region Factory
    [PluginName("Commands")]
    public class EstateCommandsFactory : IPluginFactory
    {
        public EstateCommandsFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            List<string> avatarNameServiceNames = new List<string>();
            string avatarNameServices = ownSection.GetString("AvatarNameServices", string.Empty);
            if (!string.IsNullOrEmpty(avatarNameServices))
            {
                foreach (string p in avatarNameServices.Split(','))
                {
                    avatarNameServiceNames.Add(p.Trim());
                }
            }

            return new EstateCommands(ownSection.GetString("RegionStorage", "RegionStorage"), ownSection.GetString("EstateService", "EstateService"));
        }
    }
    #endregion
}
