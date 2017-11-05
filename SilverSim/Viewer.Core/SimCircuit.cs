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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

using log4net;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.IM;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.IM;
using System;
using System.Collections.Generic;

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

        private readonly Dictionary<MessageType, Action<Message>> m_MessageRouting = new Dictionary<MessageType, Action<Message>>();
        private readonly Dictionary<string, Action<Message>> m_GenericMessageRouting = new Dictionary<string, Action<Message>>();
        private readonly Dictionary<string, Action<Message>> m_GodlikeMessageRouting = new Dictionary<string, Action<Message>>();
        private readonly Dictionary<GridInstantMessageDialog, Action<Message>> m_IMMessageRouting = new Dictionary<GridInstantMessageDialog, Action<Message>>();

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
            /* intentionally left empty */
        }

        protected override void LogMsgLogoutReply()
        {
            /* intentionally left empty */
        }

        protected override void LogMsgOnLogoutCompletion()
        {
            /* intentionally left empty */
        }

        protected override void LogMsgOnTimeout()
        {
            /* intentionally left empty */
        }

        protected override void OnCircuitSpecificPacketReceived(MessageType mType, UDPPacket p)
        {
            /* we know the message type now, so we have to decode it when possible */
            switch (mType)
            {
                case MessageType.ScriptDialogReply:
                    /* nothing to do */
                case MessageType.ChatFromViewer:
                    /* nothing to do */
                case MessageType.TransferRequest:
                    /* nothing to do */
                    break;

                case MessageType.ChatPass:
                    {
                        var ev = new ListenEvent
                        {
                            Channel = p.ReadInt32(),
                            GlobalPosition = p.ReadVector3f(),
                            ID = p.ReadUUID(),
                            OwnerID = p.ReadUUID(),
                            Name = p.ReadStringLen8(),
                            SourceType = (ListenEvent.ChatSourceType)p.ReadUInt8(),
                            Type = (ListenEvent.ChatType)p.ReadUInt8()
                        };
                        /* radius */
                        p.ReadFloat();
                        /* simaccess */ p.ReadUInt8();
                        ev.Message = p.ReadStringLen16();
                        var scene = Scene;
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
                        var m = del(p);
                        /* we got a decoder, so we can make use of it */
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
                            var im = (ImprovedInstantMessage)m;
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
                            var genMsg = (GenericMessage)m;
                            if (m_GenericMessageRouting.TryGetValue(genMsg.Method, out mdel))
                            {
                                mdel(m);
                            }
                            else
                            {
                                m_Log.DebugFormat("Unhandled generic message {0} received", genMsg.Method);
                            }
                        }
                        else if (m.Number == MessageType.GodlikeMessage)
                        {
                            var genMsg = (GodlikeMessage)m;
                            if (m_GodlikeMessageRouting.TryGetValue(genMsg.Method, out mdel))
                            {
                                mdel(m);
                            }
                            else
                            {
                                m_Log.DebugFormat("Unhandled godlike message {0} received", genMsg.Method);
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

        protected override void SendSimStats(long dt)
        {
            /* no sim stats */
        }

        public SceneInterface Scene { get; set; }

        public class ChildAgentUpdater : IAgentChildUpdateServiceInterface
        {
            private readonly SimCircuit m_Circuit;
            private readonly uint m_ViewerCircuitCode;

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
