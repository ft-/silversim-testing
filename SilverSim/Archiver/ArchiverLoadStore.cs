/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Main.Common.HttpClient;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.Archiver
{
    class ArchiverLoadStore : IPlugin
    {
        public ArchiverLoadStore()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            CommandRegistry.LoadCommands.Add("oar", LoadOarCommand);
            CommandRegistry.SaveCommands.Add("oar", SaveOarCommand);
        }

        public void SaveOarCommand(List<string> args, TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                string outp = "Available commands:\n";
                outp += "save oar [--publish] [--noassets] <filename>\n";
                io.Write(outp);
                return;
            }

            UUID selectedScene = io.SelectedScene;
            SceneInterface scene;
            if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
                scene = SceneManager.Scenes[selectedScene];
            }
            else
            {
                io.Write("Multi-region OARs currently not supported");
                return;
            }

            string filename = null;
            SilverSim.Archiver.OAR.OAR.SaveOptions options = OAR.OAR.SaveOptions.None;

            for (int argi = 2; argi < args.Count; ++argi)
            {
                string arg = args[argi];
                if (arg == "--noassets")
                {
                    options |= OAR.OAR.SaveOptions.NoAssets;
                }
                else if (arg == "--publish")
                {
                    options |= OAR.OAR.SaveOptions.Publish;
                }
                else
                {
                    filename = arg;
                }
            }

            Stream s;
            try
            {
                s = new FileStream(filename, FileMode.Create);
            }
            catch(Exception e)
            {
                io.Write(e.Message);
                return;
            }
            try
            {
                OAR.OAR.Save(scene, options, s);
                io.Write("OAR saved successfully.");
                s.Close();
            }
            catch (Exception e)
            {
                io.Write(e.Message);
                s.Close();
            }
        }

        public void LoadOarCommand(List<string> args, TTY io, UUID limitedToScene)
        {
            if (args[0] == "help")
            {
                string outp = "Available commands:\n";
                outp += "load oar [--skip-assets] [--merge] <filename>\n";
                outp += "load oar [--skip-assets] [--merge] <url>\n";
                io.Write(outp);
                return;
            }

            UUID selectedScene = io.SelectedScene;
            SceneInterface scene;
            if (limitedToScene != UUID.Zero)
            {
                selectedScene = limitedToScene;
                scene = SceneManager.Scenes[selectedScene];
            }
            else
            {
                scene = null;
            }

            string filename = null;
            SilverSim.Archiver.OAR.OAR.LoadOptions options = OAR.OAR.LoadOptions.None;

            for (int argi = 2; argi < args.Count; ++argi)
            {
                string arg = args[argi];
                if (arg == "--skip-assets")
                {
                    options |= OAR.OAR.LoadOptions.NoAssets;
                }
                else if (arg == "--merge")
                {
                    options |= OAR.OAR.LoadOptions.Merge;
                }
                else
                {
                    filename = arg;
                }
            }

            if (string.IsNullOrEmpty(filename))
            {
                io.Write("No filename or url specified.\n");
                return;
            }

            Stream s;
            if (Uri.IsWellFormedUriString(filename, UriKind.Absolute))
            {
                try
                {
                    s = HttpRequestHandler.DoStreamGetRequest(filename, null, 20000);
                }
                catch(Exception e)
                {
                    io.Write(e.Message);
                    return;
                }
            }
            else
            {
                try
                {
                    s = new FileStream(filename, FileMode.Open);
                }
                catch(Exception e)
                {
                    io.Write(e.Message);
                    return;
                }
            }
            try
            {
                OAR.OAR.Load(scene, options, s);
                io.Write("OAR loaded successfully.");
            }
            catch (OAR.OAR.MultiRegionOARLoadingTriedOnRegion)
            {
                io.Write("Multi-Region OAR cannot be loaded with a pre-selected region");
            }
            catch (OAR.OAR.OARFormatException)
            {
                io.Write("OAR file is corrupt");
            }
            catch (Exception e)
            {
                io.Write(e.Message);
            }
            try
            {
                s.Close();
            }
            catch
            {

            }
        }
    }
}
