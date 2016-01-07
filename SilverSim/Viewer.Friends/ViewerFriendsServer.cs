// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Threading;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Viewer.Friends
{
    [Description("Viewer Friends Handler")]
    public class ViewerFriendsServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        [PacketHandler(MessageType.AcceptFriendship)]
        [PacketHandler(MessageType.DeclineFriendship)]
        [PacketHandler(MessageType.TerminateFriendship)]
        [PacketHandler(MessageType.GrantUserRights)]
        readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        bool m_ShutdownFriends;

        public ViewerFriendsServer()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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
