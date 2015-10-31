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
using SilverSim.Scene.Types.Agent;
using SilverSim.Viewer.Messages.Agent;

namespace SilverSim.Viewer.Core
{
    public class SimCircuit : Circuit
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SIM CIRCUIT");
        private static readonly UDPPacketDecoder m_PacketDecoder = new UDPPacketDecoder(true);

        public UUID RemoteSceneID { get; protected set; }
        public UUID SessionID { get; protected set; }
        public GridVector RemoteLocation { get; protected set; }
        /* <summary>RemoteOffset = RemoteGlobalPosition - LocalGlobalPosition</summary> */
        public Vector3 RemoteOffset { get; protected set; }
        SceneInterface m_Scene;

        public SimCircuit(
            UDPCircuitsManager server,
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

        protected override void CheckForNewDataToSend()
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

        protected override void OnCircuitSpecificPacketReceived(MessageType mType, UDPPacket p)
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
                        ev.Channel = p.ReadInt32();
                        ev.GlobalPosition = p.ReadVector3f();
                        ev.ID = p.ReadUUID();
                        ev.OwnerID = p.ReadUUID();
                        ev.Name = p.ReadStringLen8();
                        ev.SourceType = (ListenEvent.ChatSourceType)p.ReadUInt8();
                        ev.Type = (ListenEvent.ChatType)p.ReadUInt8();
                        /* radius */ p.ReadFloat();
                        /* simaccess */ p.ReadUInt8();
                        ev.Message = p.ReadStringLen16();
                        SceneInterface scene = m_Scene;
                        if (scene != null)
                        {
                            ev.OriginSceneID = scene.ID;
                            scene.ChatPassInbound(RemoteSceneID, ev);
                        }
                    }
                    break;

                default:
                    Func<UDPPacket, Message> del;
                    if (m_PacketDecoder.PacketTypes.TryGetValue(mType, out del))
                    {
                        Message m = del(p);
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
                m_Scene = value;
            }
        }

        public class ChildAgentUpdater : IAgentChildUpdateServiceInterface
        {
            SimCircuit m_Circuit;
            uint m_ViewerCircuitCode;

            public ChildAgentUpdater(SimCircuit circuit, uint viewercircuitcode)
            {
                m_Circuit = circuit;
                m_ViewerCircuitCode = viewercircuitcode;
            }

            public void SendMessage(ChildAgentPositionUpdate m)
            {
                m.ViewerCircuitCode = m_ViewerCircuitCode;
                m.SessionID = m_Circuit.SessionID;
                m.AgentID = m_Circuit.RemoteSceneID;
                m_Circuit.SendMessage(m);
            }

            public void SendMessage(ChildAgentUpdate m)
            {
                m.ViewerCircuitCode = m_ViewerCircuitCode;
                m.SessionID = m_Circuit.SessionID;
                m.AgentID = m_Circuit.RemoteSceneID;
                m_Circuit.SendMessage(m);
            }

            public void Disconnect()
            {
                /* not needed */
            }
        }
    }
}
