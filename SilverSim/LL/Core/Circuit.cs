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
using SilverSim.Main.Common.Caps;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using System.Net;
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

        private Thread m_TextureDownloadThread;
        private bool m_TextureDownloadThreadRunning = false;
        private BlockingQueue<Messages.Image.RequestImage> m_TextureDownloadQueue = new BlockingQueue<Messages.Image.RequestImage>();

        private Thread m_InventoryThread;
        private bool m_InventoryThreadRunning = false;
        private BlockingQueue<Message> m_InventoryRequestQueue = new BlockingQueue<Message>();
        public string GatekeeperURI { get; protected set; }

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
                            m_ChatService = null;
                        }
                        
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
                                m_ChatListener = m_ChatService.AddAgentListen(0, "", UUID.Zero, "", ChatGetAgentUUID, ChatGetAgentPosition, ChatListenerAction);
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
            SendMessage(cfs);
        }
        #endregion

        public RwLockedDictionary<UInt32, UDPPacket> m_UnackedPackets = new RwLockedDictionary<uint, UDPPacket>();
        public Circuit(LLUDPServer server, UInt32 circuitcode, CapsHttpRedirector capsredirector, UUID regionSeedID, Dictionary<string, string> serviceURLs, string gatekeeperURI)
        {
            m_Server = server;
            CircuitCode = circuitcode;
            m_CapsRedirector = capsredirector;
            GatekeeperURI = gatekeeperURI;
            
            /* the following two capabilities are mandatory */
            AddCapability("SEED", regionSeedID, RegionSeedHandler);
            AddCapability("EventQueueGet", regionSeedID, Cap_EventQueueGet);

            SetupDefaultCapabilities(regionSeedID, server.Scene.CapabilitiesConfig, serviceURLs);
            Scene = server.Scene;
            m_LastReceivedPacketAtTime = Environment.TickCount;
        }

        ~Circuit()
        {
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

            /* do we have some acks from the packet's end? */
            if(null != acknumbers)
            {
                foreach(UInt32 ackno in acknumbers)
                {
                    m_UnackedPackets.Remove(ackno);

                    lock (m_LogoutReplyLock)
                    {
                        if (ackno == m_LogoutReplySeqNo && m_LogoutReplySent)
                        {
                            m_Log.InfoFormat("Logout of agent {0} completed", Agent.ID);
                            Stop();
                            ((LLUDPServer)Scene.UDPServer).RemoveCircuit(this);
                            Agent = null;
                            Scene = null;
                            return;
                        }
                    }
                }
            }

            if(pck.IsReliable)
            {
                /* we have to ack */
                m_AckList.Enqueue(pck.SequenceNumber);
            }

            /* we know the message type now, so we have to decode it when possible */
            switch(mType)
            { 
                case MessageType.PacketAck:
                    /* we decode it here, no need to pass it anywhere else */

                    for(uint i = 0; i < pck.ReadUInt8(); ++i)
                    {
                        uint ackno = pck.ReadUInt32();
                        m_UnackedPackets.Remove(ackno);

                        lock (m_LogoutReplyLock)
                        {
                            if (ackno == m_LogoutReplySeqNo && m_LogoutReplySent)
                            {
                                m_Log.InfoFormat("Logout of agent {0} completed", Agent.ID);
                                Stop();
                                ((LLUDPServer)Scene.UDPServer).RemoveCircuit(this);
                                Agent = null;
                                Scene = null;
                                return;
                            }
                        }
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
                    /* TODO: decode here */
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
                        /* TODO: specific decoder for ListenEvent */
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
                                else
                                {
                                    m_Server.RouteReceivedMessage(m);
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
                        else
                        {
                            m_Server.RouteReceivedMessage(m);
                        }
                    }
                    break;

                default:
                    UDPPacketDecoder.PacketDecoderDelegate del;
                    if(m_PacketDecoder.PacketTypes.TryGetValue(mType, out del))
                    {
                        Message m = del(pck);
                        /* we got a decoder, so we can make use of it */
                        m.ReceivedOnCircuitCode = CircuitCode;
                        m.CircuitAgentID = new UUID(AgentID);
                        m.CircuitSessionID = new UUID(SessionID);
                        m.CircuitSceneID = new UUID(Scene.ID);

                        /* we keep the circuit relatively dumb so that we have no other logic than how to send and receive messages to the viewer */
                        switch(m.Number)
                        {
                            case MessageType.RequestImage:
                                m_TextureDownloadQueue.Enqueue((Messages.Image.RequestImage)m);
                                break;

                            case MessageType.CopyInventoryItem:
                            case MessageType.ChangeInventoryItemFlags:
                            case MessageType.CreateInventoryFolder:
                            case MessageType.CreateInventoryItem:
                            case MessageType.FetchInventory:
                            case MessageType.FetchInventoryDescendents:
                            case MessageType.MoveInventoryFolder:
                            case MessageType.MoveInventoryItem:
                            case MessageType.PurgeInventoryDescendents:
                            case MessageType.RemoveInventoryFolder:
                            case MessageType.RemoveInventoryItem:
                            case MessageType.UpdateInventoryFolder:
                            case MessageType.UpdateInventoryItem:
                            case MessageType.LinkInventoryItem:
                            case MessageType.RemoveInventoryObjects:
                                m_InventoryRequestQueue.Enqueue(m);
                                break;

                            case MessageType.UUIDGroupNameRequest:
                                GroupNameLookup((Messages.Names.UUIDGroupNameRequest)m);
                                break;

                            case MessageType.UUIDNameRequest:
                                UserNameLookup((Messages.Names.UUIDNameRequest)m);
                                break;

                            default:
                                m_Server.RouteReceivedMessage(m);
                                break;
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
                    case MessageType.EnableSimulator:
                    case MessageType.DisableSimulator:
                    case MessageType.CrossedRegion:
                    case MessageType.TeleportFinish:
                    case 0: /* only Event Queue support */
                        if(m is Messages.Console.SimConsoleResponse)
                        {
                            if(!Scene.IsSimConsoleAllowed(Agent.Owner))
                            {
                                /* no messages when not allowed */
                                break;
                            }
                        }
                        m_EventQueue.Enqueue(m);
                        break;

                    default:
                        m_TxQueue.Enqueue(m);
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
        
    }
}
