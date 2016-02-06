// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Threading;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Friend;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Viewer.Friends
{
    [Description("Viewer Friends Handler")]
    public class ViewerFriendsServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        /* even though some one may look out here for loading friends list,
         * it is not here since it is part of the login protocol.
         */
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

                switch(m.Number)
                {
                    case MessageType.AcceptFriendship:
                        HandleAcceptFriendship(m);
                        break;

                    case MessageType.DeclineFriendship:
                        HandleDeclineFriendship(m);
                        break;

                    case MessageType.TerminateFriendship:
                        HandleTerminateFriendship(m);
                        break;

                    case MessageType.GrantUserRights:
                        HandleGrantUserRights(m);
                        break;

                    default:
                        break;
                }
            }
        }

        void HandleAcceptFriendship(Message m)
        {
            AcceptFriendship req = (AcceptFriendship)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleDeclineFriendship(Message m)
        {
            AcceptFriendship req = (AcceptFriendship)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleTerminateFriendship(Message m)
        {
            TerminateFriendship req = (TerminateFriendship)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGrantUserRights(Message m)
        {
            GrantUserRights req = (GrantUserRights)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
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
