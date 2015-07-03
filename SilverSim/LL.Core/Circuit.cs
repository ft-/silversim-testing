/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Economy;
using SilverSim.Main.Common.Caps;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Economy;
using SilverSim.Types.IM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    public partial class Circuit : IDisposable
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL CIRCUIT");
        private static Encoding UTF8NoBOM = new System.Text.UTF8Encoding(false);
        private static readonly UDPPacketDecoder m_PacketDecoder = new UDPPacketDecoder();
        public UInt32 CircuitCode { get; private set; }
        public UUID SessionID = UUID.Zero;
        public UUID AgentID = UUID.Zero;
        public LLAgent Agent = null;
        private SceneInterface m_Scene;
        private BlockingQueue<Message> m_TxQueue = new BlockingQueue<Message>();
        private bool m_TxRunning = false;
        private Thread m_TxThread = null;
        private int __SequenceNumber = 0;
        private NonblockingQueue<UInt32> m_AckList = new NonblockingQueue<UInt32>();
        private LLUDPServer m_Server;
        public EndPoint RemoteEndPoint;
        private RwLockedDictionary<byte, int> m_PingSendTicks = new RwLockedDictionary<byte, int>();
        private RwLockedDictionary<string, UUID> m_RegisteredCapabilities = new RwLockedDictionary<string, UUID>();
        private CapsHttpRedirector m_CapsRedirector;
        private object m_SceneSetLock = new object();
        public int LastMeasuredLatencyTickCount { get; private set; }
        private uint m_LogoutReplySeqNo = 0;
        private bool m_LogoutReplySent = false;
        private object m_LogoutReplyLock = new object(); /* this is only for guarding access sequence to m_LogoutReply* variables */
        private int m_LogoutReplySentAtTime;
        private int m_LastReceivedPacketAtTime;
        private ChatServiceInterface m_ChatService;
        private ChatServiceInterface.Listener m_ChatListener;
        private ChatServiceInterface.Listener m_DebugChannelListener;

        private Thread m_TextureDownloadThread;
        private bool m_TextureDownloadThreadRunning = false;
        private BlockingQueue<Message> m_TextureDownloadQueue = new BlockingQueue<Message>();
        private Dictionary<MessageType, Action<Message>> m_MessageRouting = new Dictionary<MessageType, Action<Message>>();

        private Thread m_InventoryThread;
        private bool m_InventoryThreadRunning = false;
        private BlockingQueue<Message> m_InventoryRequestQueue = new BlockingQueue<Message>();
        public string GatekeeperURI { get; protected set; }

        private Thread m_ObjectUpdateThread;
        private bool m_ObjectUpdateThreadRunning = false;

        int m_PacketsReceived = 0;
        int m_PacketsSent = 0;
        int m_AgentUpdatesReceived = 0;
        int m_UnackedBytes = 0;
        object m_UnackedBytesLock = new object();

        private uint NextSequenceNumber
        {
            get
            {
                return (uint)Interlocked.Increment(ref __SequenceNumber);
            }
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
                        if(m_ChatListener != null)
                        {
                            m_ChatListener.Remove();
                            m_ChatListener = null;
                        }
                        if(m_DebugChannelListener != null)
                        {
                            m_DebugChannelListener.Remove();
                            m_DebugChannelListener = null;
                        }
                        m_ChatService = null;
                        
                        foreach (KeyValuePair<string, object> kvp in m_Scene.SceneCapabilities)
                        {
                            if (kvp.Value is ICapabilityInterface)
                            {
                                ICapabilityInterface iface = (ICapabilityInterface)(kvp.Value);
                                RemoveCapability(iface.CapabilityName);
                            }
                        }
                    }
                    m_Scene = value;
                    if (null != m_Scene)
                    {
                        UUID sceneCapID = UUID.Random;
                        foreach (KeyValuePair<string, object> kvp in m_Scene.SceneCapabilities)
                        {
                            if (kvp.Value is ICapabilityInterface)
                            {
                                ICapabilityInterface iface = (ICapabilityInterface)(kvp.Value);
                                AddCapability(iface.CapabilityName, sceneCapID, iface.HttpRequestHandler);
                            }
                        }

                        m_ChatService = m_Scene.GetService<ChatServiceInterface>();
                        if(null != m_ChatService)
                        {
                            try
                            {
                                m_ChatListener = m_ChatService.AddAgentListen(PUBLIC_CHANNEL, "", UUID.Zero, "", ChatGetAgentUUID, ChatGetAgentPosition, ChatListenerAction);
                            }
                            catch
                            {
                                m_ChatService = null;
                            }
                            try
                            {
                                m_DebugChannelListener = m_ChatService.AddAgentListen(DEBUG_CHANNEL, "", UUID.Zero, "", ChatGetAgentUUID, ChatGetAgentPosition, ChatListenerAction);
                            }
                            catch
                            {
                                m_DebugChannelListener = null;
                            }
                        }
                    }
                }
            }
        }

        #region Chat Listener
        private Vector3 ChatGetAgentPosition()
        {
            GridVector thisRegionPos = Scene.GridPosition;
            GridVector rootAgentRegionPos = Agent.GetRootAgentGridPosition(thisRegionPos);
            GridVector diff = rootAgentRegionPos - thisRegionPos;
            Vector3 agentPos = Agent.GlobalPosition;
            agentPos.X += diff.X;
            agentPos.Y += diff.Y;

            return agentPos;
        }

        private UUID ChatGetAgentUUID()
        {
            return AgentID;
        }

        const int PUBLIC_CHANNEL = 0;
        const int DEBUG_CHANNEL = 0x7FFFFFFF;

        private void ChatListenerAction(ListenEvent le)
        {
            Messages.Chat.ChatFromSimulator cfs = new Messages.Chat.ChatFromSimulator();
            cfs.Audible = Messages.Chat.ChatAudibleLevel.Fully;
            cfs.ChatType = (Messages.Chat.ChatType)(byte)le.Type;
            cfs.FromName = le.Name;
            cfs.Message = le.Message;
            cfs.Position = le.GlobalPosition;
            cfs.SourceID = le.ID;
            cfs.SourceType = (Messages.Chat.ChatSourceType)(byte)le.SourceType;
            cfs.OwnerID = le.OwnerID;
            if (le.Channel == DEBUG_CHANNEL)
            {
                /* limit debug channel to root agents */
                if(Agent.IsInScene(Scene))
                {
                    SendMessage(cfs);
                }
            }
            else
            {
                SendMessage(cfs);
            }
        }
        #endregion

        public C5.TreeDictionary<uint, UDPPacket> m_UnackedPacketsHash = new C5.TreeDictionary<uint,UDPPacket>();

        void AddMessageRouting(object o)
        {
            foreach(MethodInfo mi in o.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                PacketHandler[] pas = (PacketHandler[])Attribute.GetCustomAttributes(mi, typeof(PacketHandler));
                foreach (PacketHandler pa in pas)
                {
                    if (m_MessageRouting.ContainsKey(pa.Number))
                    {
                        m_Log.FatalFormat("Method {0} registered duplicate {1}", mi.Name, pa.Number.ToString());
                    }
                    else if (mi.ReturnType != typeof(void))
                    {
                        m_Log.FatalFormat("Method {0} return type is not void", mi.Name);
                    }
                    else if (mi.GetParameters().Length != 1)
                    {
                        m_Log.FatalFormat("Method {0} parameter count does not match", mi.Name);
                    }
                    else if (mi.GetParameters()[0].ParameterType != typeof(Message))
                    {
                        m_Log.FatalFormat("Method {0} parameter types do not match", mi.Name);
                    }
                    else
                    {
#if DEBUG
                        m_Log.InfoFormat("Method {0} of {1} registered for {2}", mi.Name, o.GetType().Name, pa.Number.ToString());
#endif
                        m_MessageRouting.Add(pa.Number, (Action<Message>)Delegate.CreateDelegate(typeof(Action<Message>), o, mi));
                    }
                }
#if DEBUG
                if(pas.Length == 0)
                {
                    if (mi.ReturnType != typeof(void))
                    {
                    }
                    else if (mi.GetParameters().Length != 1)
                    {
                    }
                    else if (mi.GetParameters()[0].ParameterType != typeof(Message))
                    {
                    }
                    else
                    {
                        m_Log.InfoFormat("Candidate method {0} of {1} is not registered", mi.Name, o.GetType().Name);
                    }
                }
#endif
            }
        }

        public Circuit(LLAgent agent, LLUDPServer server, UInt32 circuitcode, CapsHttpRedirector capsredirector, UUID regionSeedID, Dictionary<string, string> serviceURLs, string gatekeeperURI)
        {
            InitializeTransmitQueueing();
            InitSimStats();

            Agent = agent;
            m_Server = server;
            CircuitCode = circuitcode;
            m_CapsRedirector = capsredirector;
            GatekeeperURI = gatekeeperURI;
            
            /* the following two capabilities are mandatory */
            AddCapability("SEED", regionSeedID, RegionSeedHandler);
            AddCapability("EventQueueGet", regionSeedID, Cap_EventQueueGet);

            SetupDefaultCapabilities(regionSeedID, server.Scene.CapabilitiesConfig, serviceURLs);
            Scene = server.Scene;

            m_MessageRouting.Add(MessageType.CopyInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.ChangeInventoryItemFlags, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.CreateInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.CreateInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.FetchInventory, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.FetchInventoryDescendents, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.MoveInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.PurgeInventoryDescendents, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.RemoveInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.RemoveInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.UpdateInventoryFolder, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.UpdateInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.LinkInventoryItem, m_InventoryRequestQueue.Enqueue);
            m_MessageRouting.Add(MessageType.RemoveInventoryObjects, m_InventoryRequestQueue.Enqueue);

            m_MessageRouting.Add(MessageType.RequestImage, m_TextureDownloadQueue.Enqueue);

            AddMessageRouting(this);
            AddMessageRouting(Agent);
            AddMessageRouting(Scene);

            m_LastReceivedPacketAtTime = Environment.TickCount;
            uint pcks;
            for(pcks = 0; pcks < 200; ++pcks)
            {
                m_TxObjectPool.Enqueue(new UDPPacket());
            }
        }

        ~Circuit()
        {
            m_UploadCapabilities.Clear();
            Agent = null;
            Scene = null;
            Dispose();
        }

        static Circuit()
        {
            InitializeTransmitQueueRouting();
        }

        #region Receive Logic
        public void PacketReceived(EndPoint ep, UDPPacket pck, List<UInt32> acknumbers)
        {
            /* no need for returning packets here since we are obliqued never to pass them around.
             * We just decode them here to actual messages
             */
            MessageType mType = pck.ReadMessageType();

            m_LastReceivedPacketAtTime = Environment.TickCount;
            
            Interlocked.Increment(ref m_PacketsReceived);

            /* do we have some acks from the packet's end? */
            if(null != acknumbers)
            {
                int unackedReleasedCount = 0;
                bool ackedObjects = false;
                bool ackedSomethingElse = false;
                foreach(UInt32 ackno in acknumbers)
                {
                    UDPPacket p_acked = null;
                    lock (m_UnackedPacketsHash)
                    {
                        if (m_UnackedPacketsHash.Contains(ackno))
                        {
                            p_acked = (UDPPacket)m_UnackedPacketsHash[ackno];
                            m_UnackedPacketsHash.Remove(ackno);
                        }
                    }

                    if (null != p_acked)
                    {
                        unackedReleasedCount += p_acked.DataLength;
                        Interlocked.Decrement(ref m_AckThrottlingCount[(int)p_acked.OutQueue]);
                        if(p_acked.OutQueue == Message.QueueOutType.Object)
                        {
                            m_TxObjectPool.Enqueue(p_acked);
                            ackedObjects = true;
                        }
                        else
                        {
                            ackedSomethingElse = true;
                        }
                    }

                    if(ackedSomethingElse)
                    {
                        m_TxQueue.Enqueue(new AcksReceived());
                    }
                    if(ackedObjects)
                    {
                        m_TxObjectQueue.Enqueue(null);
                    }

                    lock (m_LogoutReplyLock)
                    {
                        if (ackno == m_LogoutReplySeqNo && m_LogoutReplySent)
                        {
                            m_Log.InfoFormat("Logout of agent {0} completed", Agent.ID);
                            TerminateCircuit();
                            return;
                        }
                    }
                }
                lock (m_UnackedBytesLock)
                {
                    m_UnackedBytes -= unackedReleasedCount;
                }
            }

            if(pck.IsReliable)
            {
                /* we have to ack */
                switch(mType)
                {
                    case MessageType.CompleteAgentMovement:
                        /* Immediate ack */
                        m_Server.SendPacketTo(UDPPacket.PacketAckImmediate(pck.SequenceNumber), RemoteEndPoint);
                        break;

                    default:
                        m_AckList.Enqueue(pck.SequenceNumber);
                        break;
                }
            }

            /* we know the message type now, so we have to decode it when possible */
            switch(mType)
            { 
                case MessageType.PacketAck:
                    /* we decode it here, no need to pass it anywhere else */
                    int unackedReleasedCount = 0;
                    bool ackedObjects = false;
                    bool ackedSomethingElse = false;
                    uint cnt = pck.ReadUInt8();
                    for(uint i = 0; i < cnt; ++i)
                    {
                        uint ackno = pck.ReadUInt32();
                        UDPPacket p_acked = null;
                        lock (m_UnackedPacketsHash)
                        {
                            if(m_UnackedPacketsHash.Contains(ackno))
                            {
                                p_acked = (UDPPacket)m_UnackedPacketsHash[ackno];
                                m_UnackedPacketsHash.Remove(ackno);
                            }
                        }
                        if (null != p_acked)
                        {
                            unackedReleasedCount += p_acked.DataLength;
                            Interlocked.Decrement(ref m_AckThrottlingCount[(int)p_acked.OutQueue]);
                            if (p_acked.OutQueue == Message.QueueOutType.Object)
                            {
                                m_TxObjectPool.Enqueue(p_acked);
                                ackedObjects = true;
                            }
                            else
                            {
                                ackedSomethingElse = true;
                            }
                        }

                        lock (m_LogoutReplyLock)
                        {
                            if (ackno == m_LogoutReplySeqNo && m_LogoutReplySent)
                            {
                                m_Log.InfoFormat("Logout of agent {0} completed", Agent.ID);
                                TerminateCircuit();
                                return;
                            }
                        }
                    }

                    if(ackedSomethingElse)
                    {
                        m_TxQueue.Enqueue(new AcksReceived());
                    }
                    if(ackedObjects)
                    {
                        m_TxObjectQueue.Enqueue(null);
                    }

                    lock (m_UnackedBytesLock)
                    {
                        m_UnackedBytes -= unackedReleasedCount;
                    }
                    break;

                case MessageType.StartPingCheck:
                    byte pingID = pck.ReadUInt8();
                    UInt32 oldestUnacked = pck.ReadUInt32();

                    UDPPacket newpck = new UDPPacket();
                    newpck.WriteMessageType(MessageType.CompletePingCheck);
                    newpck.WriteUInt8(pingID);
                    newpck.SequenceNumber = NextSequenceNumber;
                    m_Server.SendPacketTo(newpck, ep);
                    /* check for unacks */
                    try
                    {
                        foreach (uint keyval in m_UnackedPacketsHash.Keys)
                        {
                            UDPPacket Value;
                            lock (m_UnackedPacketsHash)
                            {
                                if (!m_UnackedPacketsHash.Contains(keyval))
                                {
                                    continue;
                                }
                                Value = (UDPPacket)m_UnackedPacketsHash[keyval];
                            }
                            if (Environment.TickCount - Value.TransferredAtTime > 1000)
                            {
                                if (Value.ResentCount++ < 5)
                                {
                                    Value.TransferredAtTime = Environment.TickCount;
                                    m_Server.SendPacketTo(Value, RemoteEndPoint);
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                    break;

                case MessageType.CompletePingCheck:
                    byte ackPingID = pck.ReadUInt8();
                    int timesent;
                    if(m_PingSendTicks.Remove(ackPingID, out timesent))
                    {
                        LastMeasuredLatencyTickCount = (timesent - Environment.TickCount) / 2;
                    }
                    break;

                case MessageType.AgentThrottle:
                    HandleThrottlePacket(Messages.Agent.AgentThrottle.Decode(pck));
                    break;

                case MessageType.ScriptDialogReply:
                    /* script dialog uses a different internal format, so we decode it specifically */
                    if(!pck.ReadUUID().Equals(AgentID))
                    {

                    }
                    else if (!pck.ReadUUID().Equals(SessionID))
                    {

                    }
                    else
                    {
                        /* specific decoder for ListenEvent */
                        ListenEvent ev = new ListenEvent();
                        ev.TargetID = pck.ReadUUID();
                        ev.Channel = pck.ReadInt32();
                        ev.ButtonIndex = pck.ReadInt32();
                        ev.Message = pck.ReadStringLen8();
                        ev.ID = AgentID;
                        ev.Type = ListenEvent.ChatType.Say;
                        m_Server.RouteChat(ev);
                    }
                    break;

                case MessageType.ChatFromViewer:
                    /* chat uses a different internal format, so we decode it specifically */
                    if(!pck.ReadUUID().Equals(AgentID))
                    {

                    }
                    else if (!pck.ReadUUID().Equals(SessionID))
                    {

                    }
                    else
                    {
                        ListenEvent ev = new ListenEvent();
                        ev.ID = AgentID;
                        ev.Message = pck.ReadStringLen16();
                        byte type = pck.ReadUInt8();

                        ev.Type = (ListenEvent.ChatType)type;
                        ev.Channel = pck.ReadInt32();
                        ev.GlobalPosition = Agent.GlobalPosition;
                        ev.Name = Agent.Name;
                        ev.TargetID = UUID.Zero;
                        ev.SourceType = ListenEvent.ChatSourceType.Agent;
                        ev.OwnerID = AgentID;
                        m_Server.RouteChat(ev);
                    }
                    break;

                case MessageType.ImprovedInstantMessage:
                    /* IM uses a different internal format, so decode it and pass it on */
                    if(!pck.ReadUUID().Equals(AgentID))
                    {

                    }
                    else if (!pck.ReadUUID().Equals(SessionID))
                    {

                    }
                    else
                    {
                        GridInstantMessage im = new GridInstantMessage();
                        im.FromAgent.ID = AgentID;
                        pck.ReadBoolean();
                        im.IsFromGroup = false;
                        im.ToAgent.ID = pck.ReadUUID();
                        im.ParentEstateID = pck.ReadUInt32();
                        im.RegionID = pck.ReadUUID();
                        im.Position.X = pck.ReadFloat();
                        im.Position.Y = pck.ReadFloat();
                        im.Position.Z = pck.ReadFloat();
                        im.IsOffline = pck.ReadUInt8() != 0;
                        im.Dialog = (GridInstantMessageDialog) pck.ReadUInt8();
                        im.IMSessionID = pck.ReadUUID();
                        im.Timestamp = Date.UnixTimeToDateTime(pck.ReadUInt32());
                        im.FromAgent.FullName = pck.ReadStringLen8();
                        im.Message = pck.ReadStringLen8();
                        im.BinaryBucket = pck.ReadBytes(pck.ReadUInt16());
                        /* TODO: pass on to IMService, add onresult to the im */
                        m_Server.RouteIM(im);
                    }
                    
                    break;

                case MessageType.TransferRequest:
                    {
                        /* we need differentiation here of SimInventoryItem */
                        Action<Message> mdel;
                        Messages.Transfer.TransferRequest m = Messages.Transfer.TransferRequest.Decode(pck);
                        if(m.SourceType == Messages.Transfer.SourceType.SimInventoryItem)
                        {
                            if(m.Params.Length >= 96)
                            {
                                UUID taskID = new UUID(m.Params, 48);
                                if(taskID.Equals(UUID.Zero))
                                {
                                    m_InventoryRequestQueue.Enqueue(m);
                                }
                                else if (m_MessageRouting.TryGetValue(MessageType.TransferRequest, out mdel))
                                {
                                    mdel(m);
                                }
                            }
                        }
                        else if(m.SourceType == Messages.Transfer.SourceType.Asset)
                        {
                            if(m.Params.Length >= 20)
                            {
                                m_InventoryRequestQueue.Enqueue(m);
                            }
                        }
                        else if (m_MessageRouting.TryGetValue(MessageType.TransferRequest, out mdel))
                        {
                            mdel(m);
                        }
                    }
                    break;

                default:
                    UDPPacketDecoder.PacketDecoderDelegate del;
                    if(mType == MessageType.AgentUpdate)
                    {
                        Interlocked.Increment(ref m_AgentUpdatesReceived);
                    }
                    if(m_PacketDecoder.PacketTypes.TryGetValue(mType, out del))
                    {
                        Message m = del(pck);
                        /* we got a decoder, so we can make use of it */
                        m.ReceivedOnCircuitCode = CircuitCode;
                        m.CircuitAgentID = new UUID(AgentID);
                        m.CircuitAgentOwner = Agent.Owner;
                        m.CircuitSessionID = new UUID(SessionID);
                        m.CircuitSceneID = new UUID(Scene.ID);

                        /* we keep the circuit relatively dumb so that we have no other logic than how to send and receive messages to the viewer.
                         * It merely collects delegates to other objects as well to call specific functions.
                         */
                        Action<Message> mdel;
                        if (m_MessageRouting.TryGetValue(m.Number, out mdel))
                        {
                            mdel(m);
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
        #endregion

        #region Thread control logic
        public void Start()
        {
            lock (this)
            {
                if (!m_TxRunning)
                {
                    m_TxThread = new Thread(TransmitThread);
                    m_TxThread.Start(this);
                    m_TxRunning = true;
                }
                if(!m_TextureDownloadThreadRunning)
                {
                    m_TextureDownloadThread = new Thread(TextureDownloadThread);
                    m_TextureDownloadThreadRunning = true;
                    m_TextureDownloadThread.Start(this);
                }
                if(!m_InventoryThreadRunning)
                {
                    m_InventoryThread = new Thread(FetchInventoryThread);
                    m_InventoryThreadRunning = true;
                    m_InventoryThread.Start(this);
                }
                if (!m_ObjectUpdateThreadRunning)
                {
                    m_ObjectUpdateThread = new Thread(HandleObjectUpdates);
                    m_ObjectUpdateThreadRunning = true;
                    m_ObjectUpdateThread.Start();
                }
                m_EventQueueEnabled = true;
            }
        }

        public void Stop()
        {
            lock (this)
            {
                if (m_TxRunning)
                {
                    m_TxQueue.Enqueue(new CancelTxThread());
                }
                if(null != m_TextureDownloadThread)
                {
                    m_TextureDownloadThreadRunning = false;
                    m_TextureDownloadThread = null;
                }
                if(null != m_InventoryThread)
                {
                    m_InventoryThreadRunning = false;
                    m_InventoryThread = null;
                }
                if (null != m_ObjectUpdateThread)
                {
                    m_ObjectUpdateThreadRunning = false;
                    m_ObjectUpdateThread = null;
                    m_TxObjectQueue.Enqueue(null);
                }
                m_EventQueueEnabled = false;
            }
        }
        #endregion

        public void SendMessage(Message m)
        {
            try
            {
                switch(m.Number)
                {
                    case 0: /* only Event Queue support */
                        if (Attribute.GetCustomAttribute(m.GetType(), typeof(EventQueueGet)) != null)
                        {
                            m_EventQueue.Enqueue(m);
                        }
                        else
                        {
                            m_Log.ErrorFormat("Type {0} misses EventQueueGet attribute", m.GetType().FullName);
                        }
                        break;

                    default:
                        if (Attribute.GetCustomAttribute(m.GetType(), typeof(EventQueueGet)) != null)
                        {
                            m_EventQueue.Enqueue(m);
                        }
                        else
                        {
                            m_TxQueue.Enqueue(m);
                        }
                        break;
                }
            }
            catch(Exception e)
            {
                m_Log.ErrorFormat("{0} at {1}", e.ToString(), e.StackTrace.ToString());
            }
        }

        public void Dispose()
        {
            Scene = null;
            foreach(KeyValuePair<string, UUID> kvp in m_RegisteredCapabilities)
            {
                m_CapsRedirector.Caps[kvp.Key].Remove(kvp.Value);
            }
            if(null != Agent && null != Scene)
            {
                Agent.Circuits.Remove(CircuitCode, Scene.ID);
            }
            m_RegisteredCapabilities.Clear();
            Agent = null;
        }

        [PacketHandler(MessageType.EconomyDataRequest)]
        void HandleEconomyDataRequest(Message m)
        {
            EconomyInfo ei = Scene.EconomyData;
            EconomyData ed = new EconomyData();
            if (ei != null)
            {
                ed.ObjectCapacity = ei.ObjectCapacity;
                ed.ObjectCount = ei.ObjectCount;
                ed.PriceEnergyUnit = ei.PriceEnergyUnit;
                ed.PriceGroupCreate = ei.PriceGroupCreate;
                ed.PriceObjectClaim = ei.PriceObjectClaim;
                ed.PriceObjectRent = ei.PriceObjectRent;
                ed.PriceObjectScaleFactor = ei.PriceObjectScaleFactor;
                ed.PriceParcelClaim = ei.PriceParcelClaim;
                ed.PriceParcelClaimFactor = ei.PriceParcelClaimFactor;
                ed.PriceParcelRent = ei.PriceParcelRent;
                ed.PricePublicObjectDecay = ei.PricePublicObjectDecay;
                ed.PricePublicObjectDelete = ei.PricePublicObjectDelete;
                ed.PriceRentLight = ei.PriceRentLight;
                ed.PriceUpload = ei.PriceUpload;
                ed.TeleportMinPrice = ei.TeleportMinPrice;
                ed.TeleportPriceExponent = ei.TeleportPriceExponent;
            }
            SendMessage(ed);
        }

    }
}
