// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.IM;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Core
{
    public class SimCircuit : Circuit
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL SIM CIRCUIT");
        private static readonly UDPPacketDecoder m_PacketDecoder = new UDPPacketDecoder(true);

        public UUID RemoteSceneID { get; protected set; }
        public UUID SessionID { get; protected set; }
        public GridVector RemoteLocation { get; protected set; }

        public SimCircuit(
            LLUDPServer server,
            UInt32 circuitcode,
            UUID remoteSceneID,
            UUID sessionID,
            GridVector remoteLocation)
            : base(server, circuitcode)
        {
            RemoteSceneID = remoteSceneID;
            SessionID = sessionID;
            RemoteLocation = remoteLocation;
        }

        public override void Dispose()
        {
            
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
                        Server.RouteChat(ev);
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
                            SilverSim.LL.Messages.Generic.GenericMessage genMsg = (SilverSim.LL.Messages.Generic.GenericMessage)m;
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
    }
}
