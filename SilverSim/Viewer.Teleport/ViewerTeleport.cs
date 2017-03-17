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
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Main.Common;
using System.ComponentModel;

namespace SilverSim.Viewer.Teleport
{
    [Description("Viewer Teleport Handler")]
    public class ViewerTeleport : IPlugin, IPacketHandlerExtender
    {
        public ViewerTeleport()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        [PacketHandler(MessageType.TeleportRequest)]
        [PacketHandler(MessageType.TeleportLocationRequest)]
        [PacketHandler(MessageType.TeleportLandmarkRequest)]
        [PacketHandler(MessageType.StartLure)]
        [PacketHandler(MessageType.TeleportLureRequest)]
        [PacketHandler(MessageType.TeleportCancel)]
        public void HandleMessage(Message m)
        {

        }
    }

    [PluginName("ViewerTeleport")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerTeleport();
        }
    }
}
