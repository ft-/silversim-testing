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
using SilverSim.Main.Common.CmdIO;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SilverSim.Main.Cmd.MapServer
{
    [Description("Map Server Console Commands")]
    public class MapServerCommands : IPlugin
    {
        readonly string m_GridServiceName;
        readonly string m_RegionDefaultFlagsServiceName;
        GridServiceInterface m_GridService;
        RegionDefaultFlagsServiceInterface m_RegionDefaultFlagsService;

        public MapServerCommands(IConfig ownSection)
        {
            m_GridServiceName = ownSection.GetString("GridService", "GridService");
            m_RegionDefaultFlagsServiceName = ownSection.GetString("RegionDefaultFlagsService", "RegionDefaultFlagsService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            m_RegionDefaultFlagsService = loader.GetService<RegionDefaultFlagsServiceInterface>(m_RegionDefaultFlagsServiceName);
            loader.CommandRegistry.AddChangeCommand("regionflags", ChangeRegionFlagDefaultsCmd);
            loader.CommandRegistry.AddClearCommand("regionflags", ClearRegionFlagDefaultsCmd);
            loader.CommandRegistry.AddShowCommand("defaultregionflags", ShowRegionFlagDefaultsCmd);
            loader.CommandRegistry.AddUnregisterCommand("gridregion", UnregisterRegionCmd);
            loader.CommandRegistry.AddShowCommand("gridregions", ShowRegionsCmd);
        }

        void ShowRegionsCmd(List<string> args, TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Command not allowed on limited console");
            }
            else if (args[0] == "help" || args.Count != 3)
            {
                io.Write("show gridregions <searchkey>");
            }
            else
            {
                var sb = new StringBuilder("Regions:\n--------------------------------------------------\n");
                foreach (RegionInfo ri in m_GridService.SearchRegionsByName(UUID.Zero, args[2]))
                {
                    sb.AppendFormat("Region {0}\n- ID: ({1})\n- Flags: {2}\n", ri.Name, ri.ID, ri.Flags.ToString());
                }
                io.Write(sb.ToString());
            }
        }

        void UnregisterRegionCmd(List<string> args, TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Command not allowed on limited console");
            }
            else if (args[0] == "help" || args.Count != 4)
            {
                io.Write("unregister region id <regionid>\nunregister region name <name>");
            }
            else
            {
                UUID id;
                RegionInfo ri;
                switch(args[2])
                {
                    case "id":
                        if(!UUID.TryParse(args[3], out id))
                        {
                            io.Write("regionid is not valid");
                            return;
                        }
                        break;

                    case "name":
                        if (!m_GridService.TryGetValue(UUID.Zero, args[3], out ri))
                        {
                            io.WriteFormatted("region \"{0}\" is not known", args[3]);
                            return;
                        }
                        id = ri.ID;
                        break;

                    default:
                        io.WriteFormatted("Unknown region identifier type {0}", args[2]);
                        return;
                }

                try
                {
                    m_GridService.UnregisterRegion(UUID.Zero, id);
                }
                catch
                {
                    io.Write("Failed to remove region");
                }
            }
        }

        void ShowRegionFlagDefaultsCmd(List<string> args, TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Command not allowed on limited console");
            }
            else if (args[0] == "help")
            {
                io.Write("show defaultregionflags");
            }
            else
            {
                var sb = new StringBuilder("Default RegionFlags:\n----------------------------------------------------------------------\n");
                foreach(KeyValuePair<UUID, RegionFlags> kvp in m_RegionDefaultFlagsService.GetAllRegionDefaultFlags())
                {
                    RegionInfo ri;
                    if(m_GridService.TryGetValue(kvp.Key, out ri))
                    {
                        sb.AppendFormat("Region {0} ({1}):\n- {2}", ri.Name, kvp.Key, kvp.Value.ToString());
                    }
                    else
                    {
                        sb.AppendFormat("Region ? ({0}):\n- {1}", kvp.Key, kvp.Value.ToString());
                    }
                }
                io.Write(sb.ToString());
            }
        }

        void ChangeRegionFlagDefaultsCmd(List<string> args, TTY io, UUID limitedToScene)
        {
            if(limitedToScene != UUID.Zero)
            {
                io.Write("Command not allowed on limited console");
            }
            else if(args[0] == "help" || args.Count < 4 || args.Count % 2 != 0)
            {
                io.Write("change regionflags id <uuid> flags..\n" +
                        "change regionflags name <name> flags..\n\nFlags:\n" +
                        "fallback true|false\ndefault true|false\ndefaulthg true|false\npersistent true|false");
            }
            else
            {
                UUID id;
                if(args[2] == "id")
                {
                    if(!UUID.TryParse(args[3], out id))
                    {
                        io.Write("uuid is not valid");
                        return;
                    }
                }
                else if(args[2] == "name")
                {
                    RegionInfo ri;
                    if(m_GridService.TryGetValue(UUID.Zero, args[3], out ri))
                    {
                        id = ri.ID;
                    }
                    else
                    {
                        io.WriteFormatted("unknown region {0}", args[3]);
                        return;
                    }
                }
                else
                {
                    io.Write("Invalid parameters");
                    return;
                }

                var setFlags = RegionFlags.None;
                var removeFlags = RegionFlags.None;

                bool val;
                for(int argi = 4; argi < args.Count; argi += 2)
                {
                    switch(args[argi])
                    {
                        case "fallback":
                            if(!bool.TryParse(args[argi + 1], out val))
                            {
                                io.WriteFormatted("{0} is not a valid boolean", args[argi + 1]);
                                return;
                            }
                            if(val)
                            {
                                setFlags |= RegionFlags.FallbackRegion;
                                removeFlags &= (~RegionFlags.FallbackRegion);
                            }
                            else
                            {
                                setFlags &= (~RegionFlags.FallbackRegion);
                                removeFlags |= RegionFlags.FallbackRegion;
                            }
                            break;
                        case "default":
                            if (!bool.TryParse(args[argi + 1], out val))
                            {
                                io.WriteFormatted("{0} is not a valid boolean", args[argi + 1]);
                                return;
                            }
                            if (val)
                            {
                                setFlags |= RegionFlags.DefaultRegion;
                                removeFlags &= (~RegionFlags.DefaultRegion);
                            }
                            else
                            {
                                setFlags &= (~RegionFlags.DefaultRegion);
                                removeFlags |= RegionFlags.DefaultRegion;
                            }
                            break;
                        case "defaulthg":
                            if (!bool.TryParse(args[argi + 1], out val))
                            {
                                io.WriteFormatted("{0} is not a valid boolean", args[argi + 1]);
                                return;
                            }
                            if (val)
                            {
                                setFlags |= RegionFlags.DefaultHGRegion;
                                removeFlags &= (~RegionFlags.DefaultHGRegion);
                            }
                            else
                            {
                                setFlags &= (~RegionFlags.DefaultHGRegion);
                                removeFlags |= RegionFlags.DefaultHGRegion;
                            }
                            break;
                        case "persistent":
                            if (!bool.TryParse(args[argi + 1], out val))
                            {
                                io.WriteFormatted("{0} is not a valid boolean", args[argi + 1]);
                                return;
                            }
                            if (val)
                            {
                                setFlags |= RegionFlags.Persistent;
                                removeFlags &= (~RegionFlags.Persistent);
                            }
                            else
                            {
                                setFlags &= (~RegionFlags.Persistent);
                                removeFlags |= RegionFlags.Persistent;
                            }
                            break;
                        default:
                            io.WriteFormatted("{0} is not a known flag", args[argi]);
                            return;
                    }
                }

                try
                {
                    m_GridService.AddRegionFlags(id, setFlags);
                    m_GridService.RemoveRegionFlags(id, removeFlags);
                    m_RegionDefaultFlagsService.ChangeRegionDefaultFlags(id, setFlags, removeFlags);
                }
                catch
                {
                    io.Write("Failed to set new region flag defaults");
                }
            }
        }

        void ClearRegionFlagDefaultsCmd(List<string> args, TTY io, UUID limitedToScene)
        {
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Command not allowed on limited console");
            }
            else if (args[0] == "help" || args.Count != 4)
            {
                io.Write("clear regionflags id <uuid>\n" +
                        "clear regionflags name <name>");
            }
            else
            {
                UUID id;
                if (args[2] == "id")
                {
                    if (!UUID.TryParse(args[3], out id))
                    {
                        io.Write("uuid is not valid");
                        return;
                    }
                }
                else if (args[2] == "name")
                {
                    RegionInfo ri;
                    if (m_GridService.TryGetValue(UUID.Zero, args[3], out ri))
                    {
                        id = ri.ID;
                    }
                    else
                    {
                        io.WriteFormatted("unknown region {0}", args[3]);
                        return;
                    }
                }
                else
                {
                    io.Write("Invalid parameters");
                    return;
                }

                try
                {
                    m_GridService.RemoveRegionFlags(id, RegionFlags.FallbackRegion | RegionFlags.DefaultRegion | RegionFlags.DefaultHGRegion | RegionFlags.Persistent);
                    m_RegionDefaultFlagsService.ChangeRegionDefaultFlags(id, RegionFlags.None, ~RegionFlags.None);
                }
                catch
                {
                    io.Write("Failed to set clear region flag defaults");
                }
            }
        }
    }

    [PluginName("MapServerCommands")]
    public class MapServerCommandsFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MapServerCommands(ownSection);
        }
    }
}
