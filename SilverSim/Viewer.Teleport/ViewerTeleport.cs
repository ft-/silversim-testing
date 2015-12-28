// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
