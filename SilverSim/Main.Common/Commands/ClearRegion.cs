// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Main.Common.Commands
{
    public static class ClearRegion
    {
        public static void CmdHandler(List<string> args, CmdIO.TTY io, UUID limitedToScene)
        {
            SceneInterface scene;
            if (args[0] == "help")
            {
                io.Write("clears region entirely");
                return;
            }
            else if(UUID.Zero != limitedToScene)
            {
                scene = SceneManager.Scenes[limitedToScene];
            }
            else if(UUID.Zero == io.SelectedScene)
            {
                scene = SceneManager.Scenes[io.SelectedScene];
            }
            else
            {
                io.Write("no region selected");
                return;
            }
            scene.ClearObjects();
            scene.ClearParcels();
            io.Write("Region cleared.");
        }
    }
}
