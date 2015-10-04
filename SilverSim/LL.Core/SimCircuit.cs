// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.IM;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Core
{
    public class SimCircuit : Circuit
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL SIM CIRCUIT");
        private static readonly UDPPacketDecoder m_PacketDecoder = new UDPPacketDecoder(true);

        public UUID RemoteSceneID { get; protected set; }
        public UUID SessionID { get; protected set; }
        public GridVector RemoteLocation { get; protected set; }
        public Vector3 RemoteOffset { get; protected set; }
        SceneInterface m_Scene = null;
        private object m_SceneSetLock = new object();
        private ChatServiceInterface m_ChatService;
        private ChatServiceInterface.Listener m_ChatListener;

        public SimCircuit(
            LLUDPServer server,
            UInt32 circuitcode,
            UUID remoteSceneID,
            UUID sessionID,
            GridVector remoteLocation,
            Vector3 remoteOffset)
            : base(server, circuitcode)
        {
            RemoteSceneID = remoteSceneID;
            SessionID = sessionID;
            RemoteLocation = remoteLocation;
            RemoteOffset = remoteOffset;
        }

        public override void Dispose()
        {
            Scene = null;
        }

        protected override void CheckForeNewDataToSend()
        {
        }

        protected override void LogMsgLogoutReply()
        {
            
        }

        protected override void LogMsgOnLogoutCompletion()
        {
            
        }

        protected override void LogMsgOnTimeout()
        {
            
        }

        protected override void OnCircuitSpecificPacketReceived(MessageType mType, UDPPacket pck)
        {
            /* we know the message type now, so we have to decode it when possible */
            switch (mType)
            {
                case MessageType.ScriptDialogReply:
                    /* nothing to do */
                    break;

                case MessageType.ChatFromViewer:
                    /* nothing to do */
                    break;

                case MessageType.TransferRequest:
                    /* nothing to do */
                    break;

                case MessageType.ChatPass:
                    {
                        ListenEvent ev = new ListenEvent();
                        ev.Channel = pck.ReadInt32();
                        ev.GlobalPosition = pck.ReadVector3f();
                        ev.ID = pck.ReadUUID();
                        ev.OwnerID = pck.ReadUUID();
                        ev.Name = pck.ReadStringLen8();
                        ev.SourceType = (ListenEvent.ChatSourceType)pck.ReadUInt8();
                        ev.Type = (ListenEvent.ChatType)pck.ReadUInt8();
                        double radius = pck.ReadFloat();
                        byte simAccess = pck.ReadUInt8();
                        ev.Message = pck.ReadStringLen16();
                        SceneInterface scene = m_Scene;
                        if (scene != null)
                        {
                            ev.OriginSceneID = scene.ID;
                            Server.RouteChat(ev);
                        }
                    }
                    break;

                default:
                    UDPPacketDecoder.PacketDecoderDelegate del;
                    if (m_PacketDecoder.PacketTypes.TryGetValue(mType, out del))
                    {
                        Message m = del(pck);
                        /* we got a decoder, so we can make use of it */
                        m.ReceivedOnCircuitCode = CircuitCode;
                        m.CircuitAgentID = new UUID(RemoteSceneID);
                        try
                        {
                            m.CircuitAgentOwner = UUI.Unknown;
                            m.CircuitSessionID = SessionID;
                            m.CircuitSceneID = new UUID(RemoteSceneID);
                        }
                        catch
                        {
                            /* this is a specific error that happens only during logout */
                            return;
                        }

                        /* we keep the circuit relatively dumb so that we have no other logic than how to send and receive messages to the remote sim.
                            * It merely collects delegates to other objects as well to call specific functions.
                            */
                        Action<Message> mdel;
                        if (m_MessageRouting.TryGetValue(m.Number, out mdel))
                        {
                            mdel(m);
                        }
                        else if (m.Number == MessageType.ImprovedInstantMessage)
                        {
                            ImprovedInstantMessage im = (ImprovedInstantMessage)m;
                            if (im.CircuitAgentID != im.AgentID ||
                                im.CircuitSessionID != im.SessionID)
                            {
                                break;
                            }
                            if (m_IMMessageRouting.TryGetValue(im.Dialog, out mdel))
                            {
                                mdel(m);
                            }
                            else
                            {
                                m_Log.DebugFormat("Unhandled im message {0} received", im.Dialog.ToString());
                            }
                        }
                        else if (m.Number == MessageType.GenericMessage)
                        {
                            SilverSim.Viewer.Messages.Generic.GenericMessage genMsg = (SilverSim.Viewer.Messages.Generic.GenericMessage)m;
                            if (m_GenericMessageRouting.TryGetValue(genMsg.Method, out mdel))
                            {
                                mdel(m);
                            }
                            else
                            {
                                m_Log.DebugFormat("Unhandled generic message {0} received", m.Number.ToString());
                            }
                        }
                        else
                        {
                            m_Log.DebugFormat("Unhandled message type {0} received", m.Number.ToString());
                        }
                    }
                    else
                    {
                        /* Ignore we have no decoder for that */
                    }
                    break;
            }
        }

        protected override void SendViaEventQueueGet(Message m)
        {
            /* no EQG on sim circuits */
        }

        protected override void StartSpecificThreads()
        {
            /* no additional threads required */
        }

        protected override void StopSpecificThreads()
        {
            /* no additional threads required */
        }

        protected override void SendSimStats(int dt)
        {
            /* no sim stats */
        }

        public SceneInterface Scene
        {
            get
            {
                return m_Scene;
            }

            set
            {
                lock (m_SceneSetLock) /* scene change serialization */
                {
                    if (null != m_Scene)
                    {
                        if (m_ChatListener != null)
                        {
                            m_ChatListener.Remove();
                            m_ChatListener = null;
                        }
                        m_ChatService = null;
                    }
                    m_Scene = value;
                    if (null != m_Scene)
                    {
                        m_ChatService = m_Scene.GetService<ChatServiceInterface>();
                        if (null != m_ChatService)
                        {
                            try
                            {
                                m_ChatListener = m_ChatService.AddChatPassListener(ChatListenerAction);
                            }
                            catch
                            {
                                m_ChatService = null;
                            }
                        }
                    }
                }
            }
        }

        #region Chat Listener
        const int PUBLIC_CHANNEL = 0;
        const int DEBUG_CHANNEL = 0x7FFFFFFF;

        private void ChatListenerAction(ListenEvent le)
        {
            if(ListenEvent.ChatSourceType.Agent == le.SourceType ||
                DEBUG_CHANNEL == le.Channel)
            {
                /* do not route agent communication or debug messages.
                 * Agents have childs. 
                 */
                return;
            }
            SceneInterface scene = m_Scene;
            if(null == scene)
            {
                return;
            }
            else if(le.OriginSceneID != UUID.Zero)
            {
                /* do not route routed messages */
                return;
            }
            Messages.Chat.ChatPass cp = new Messages.Chat.ChatPass();
            cp.ChatType = (Messages.Chat.ChatType)(byte)le.Type;
            cp.Name = le.Name;
            cp.Message = le.Message;
            cp.Position = le.GlobalPosition + RemoteOffset;
            cp.ID = le.ID;
            cp.SourceType = (Messages.Chat.ChatSourceType)(byte)le.SourceType;
            cp.OwnerID = le.OwnerID;
            cp.Channel = le.Channel;
            SendMessage(cp);
        }
        #endregion
    }
}
