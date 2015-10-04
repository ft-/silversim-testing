// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Main.Common;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Viewer.Friends
{
    public class ViewerFriendsServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        [PacketHandler(MessageType.AcceptFriendship)]
        [PacketHandler(MessageType.DeclineFriendship)]
        [PacketHandler(MessageType.TerminateFriendship)]
        [PacketHandler(MessageType.GrantUserRights)]
        BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        bool m_ShutdownFriends = false;

        public ViewerFriendsServer()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Friends Handler Thread";

            while (!m_ShutdownFriends)
            {
                KeyValuePair<AgentCircuit, Message> req;
                try
                {
                    req = RequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;
            }
        }

        public ShutdownOrder ShutdownOrder
        {
            get 
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_ShutdownFriends = true;
        }
    }

    [PluginName("ViewerFriendsServer")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerFriendsServer();
        }
    }
}
