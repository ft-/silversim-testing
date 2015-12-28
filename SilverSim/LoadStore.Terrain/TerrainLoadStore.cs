// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;

namespace SilverSim.LoadStore.Terrain
{
    [Description("Terrain Load/Store Support")]
    public class TerrainLoadStore : IPlugin
    {
        private readonly Dictionary<string, ITerrainFileStorage> m_TerrainFileStorages = new Dictionary<string, ITerrainFileStorage>();

        public TerrainLoadStore(ConfigurationLoader loader)
        {
            foreach (Type t in GetType().Assembly.GetTypes())
            {
                if (t.GetCustomAttributes(typeof(TerrainStorageTypeAttribute), false).Length > 0 &&
                    t.GetInterfaces().Contains(typeof(IPlugin)) &&
                    t.GetInterfaces().Contains(typeof(ITerrainFileStorage)))
                {
                    object o = Activator.CreateInstance(t);
                    loader.AddPlugin(((ITerrainFileStorage)o).Name, (IPlugin)o);
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            CommandRegistry.LoadCommands.Add("terrain", LoadTerrainCommand);
            CommandRegistry.SaveCommands.Add("terrain", SaveTerrainCommand);

            foreach(ITerrainFileStorage iface in loader.GetServicesByValue<ITerrainFileStorage>())
            {
                m_TerrainFileStorages.Add(iface.Name, iface);
            }
        }

        void ParseLocation(string inp, out uint x, out uint y)
        {
            string[] parts = inp.Split(',');
            if(parts.Length != 2)
            {
                throw new ArgumentException("inp");
            }
            if(!uint.TryParse(parts[0], out x) ||
                !uint.TryParse(parts[1], out y))
            {
                throw new ArgumentException("inp");
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void LoadTerrainCommand(List<string> args, TTY io, UUID limitedToScene)
        {
            UUID selectedScene = io.SelectedScene;
            SceneInterface scene;
            if(limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }

            ITerrainFileStorage loader;
            if(args[0] == "help")
            {
                string outp = "Available commands:\n";
                outp += "load terrain <format> <filename>\n";
                outp += "load terrain <format> <filename> x,y w,h\n";
                outp += "\nAvailable Formats:\n";
                foreach (KeyValuePair<string, ITerrainFileStorage> kvp in m_TerrainFileStorages)
                {
                    if (kvp.Value.SupportsLoading)
                    {
                        outp += string.Format("{0}\n", kvp.Key);
                    }
                }
                io.Write(outp);
            }
            else if(args.Count < 4 || args.Count > 6)
            {
                io.Write("invalid parameters for load terrain\n");
            }
            else if(!m_TerrainFileStorages.TryGetValue(args[1], out loader) || !loader.SupportsLoading)
            {
                io.WriteFormatted("unknown terrain file format {0}\n", args[1]);
            }
            else if(selectedScene == UUID.Zero)
            {
                io.Write("No region selected.\n");
            }
            else if(!SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("Selected region does not exist anymore.\n");
            }
            else
            {
                uint x = 0;
                uint y = 0;
                uint w = scene.RegionData.Size.X;
                uint h = scene.RegionData.Size.Y;

                if (args.Count >= 5)
                {
                    try
                    {
                        ParseLocation(args[4], out x, out y);
                    }
                    catch
                    {
                        io.WriteFormatted("Invalid start location {0} given", args[4]);
                        return;
                    }
                    if(w <= x || h <= y || (x % 16) != 0 || (y % 16) != 0)
                    {
                        io.WriteFormatted("Invalid start location {0} given", args[4]);
                        return;
                    }
                    w -= x;
                    h -= y;
                }

                if(args.Count == 6)
                {
                    uint nw, nh;
                    try
                    {
                        ParseLocation(args[5], out nw, out nh);
                    }
                    catch
                    {
                        io.WriteFormatted("Invalid size {0} given", args[5]);
                        return;
                    }
                    if (w < x + nw || h < y + nh || w < nw || h < nh || (nw % 16) != 0 || (nh % 16) != 0)
                    {
                        io.WriteFormatted("Invalid size {0} given", args[5]);
                        return;
                    }
                    w = nw;
                    h = nh;
                }

                List<LayerPatch> patches;

                try
                {
                    patches = loader.LoadFile(args[3], (int)w, (int)h);
                }
                catch(Exception e)
                {
                    io.WriteFormatted("Could no load file {0}: {1}", args[3], e.Message);
                    return;
                }

                foreach(LayerPatch patch in patches)
                {
                    if(patch.X >= w / 16 || patch.Y >= h / 16)
                    {
                        io.Write("Terrain data from file exceeds given size\n");
                        return;
                    }
                    patch.X += (x / 16);
                    patch.Y += (y / 16);

                }

                scene.Terrain.AllPatches = patches;
                io.WriteFormatted("Terrain data loaded from file {0}.\n", args[3]);
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void SaveTerrainCommand(List<string> args, TTY io, UUID limitedToScene)
        {
            UUID selectedScene = io.SelectedScene;
            SceneInterface scene;
            if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
            }

            ITerrainFileStorage storer;
            if (args[0] == "help")
            {
                string outp = "Available commands:\n";
                outp += "save terrain <format> <filename>\n";
                outp += "save terrain <format> <filename> x,y w,h\n";
                outp += "\nAvailable formats:\n";
                foreach (KeyValuePair<string, ITerrainFileStorage> kvp in m_TerrainFileStorages)
                {
                    if (kvp.Value.SupportsSaving)
                    {
                        outp += string.Format("{0}\n", kvp.Key);
                    }
                }
                io.Write(outp);
            }
            else if (args.Count != 4 && args.Count != 6)
            {
                io.Write("invalid parameters for save terrain\n");
            }
            else if (!m_TerrainFileStorages.TryGetValue(args[1], out storer) || !storer.SupportsSaving)
            {
                io.WriteFormatted("unknown terrain file format {0}\n", args[1]);
            }
            else if (selectedScene == UUID.Zero)
            {
                io.Write("No region selected.\n");
            }
            else if (!SceneManager.Scenes.TryGetValue(selectedScene, out scene))
            {
                io.Write("Selected region does not exist anymore.\n");
            }
            else
            {
                uint x = 0;
                uint y = 0;
                uint w = scene.RegionData.Size.X;
                uint h = scene.RegionData.Size.Y;

                if (args.Count >= 5)
                {
                    try
                    {
                        ParseLocation(args[4], out x, out y);
                    }
                    catch
                    {
                        io.WriteFormatted("Invalid start location {0} given", args[4]);
                        return;
                    }
                    if (w <= x || h <= y || (x % 16) != 0 || (y % 16) != 0)
                    {
                        io.WriteFormatted("Invalid start location {0} given", args[4]);
                        return;
                    }
                    w -= x;
                    h -= y;
                }

                if (args.Count == 6)
                {
                    uint nw, nh;
                    try
                    {
                        ParseLocation(args[5], out nw, out nh);
                    }
                    catch
                    {
                        io.WriteFormatted("Invalid size {0} given", args[5]);
                        return;
                    }
                    if (w < x + nw || h < y + nh || w < nw || h < nh || (nw % 16) != 0 || (nh % 16) != 0)
                    {
                        io.WriteFormatted("Invalid size {0} given", args[5]);
                        return;
                    }
                    w = nw;
                    h = nh;
                }

                List<LayerPatch> patches = new List<LayerPatch>();
                foreach(LayerPatch patch in scene.Terrain.AllPatches)
                {
                    if(patch.X >= x && patch.Y >= y && patch.X < x + w && patch.Y < y + h)
                    {
                        patches.Add(patch);
                    }
                }

                try
                {
                    storer.SaveFile(args[3], patches);
                }
                catch (Exception e)
                {
                    io.WriteFormatted("Could no save terrain file {0}: {1}", args[3], e.Message);
                    return;
                }
                io.WriteFormatted("Saved terrain file {0}", args[3]);
            }
        }
    }
}
