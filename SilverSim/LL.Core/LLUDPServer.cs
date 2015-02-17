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
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    #region Rx Buffer
    public class UDPReceivePacket : UDPPacket
    {
        public EndPoint RemoteEndPoint = new IPEndPoint(0, 0);

        public UDPReceivePacket()
            : base()
        {

        }
    }
    #endregion

    #region LLUDP Server
    public partial class LLUDPServer : IDisposable, ILLUDPServer
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LLUDP SERVER");
        IPAddress m_BindAddress;
        int m_BindPort;
        Socket m_UdpSocket;
        NonblockingQueue<UDPReceivePacket> m_InboundBufferQueue = new NonblockingQueue<UDPReceivePacket>();
        RwLockedDoubleDictionary<EndPoint, uint, Circuit> m_Circuits = new RwLockedDoubleDictionary<EndPoint, uint, Circuit>();
        bool m_InboundRunning = false;
        IMServiceInterface m_IMService;
        ChatServiceInterface m_ChatService;
        BlockingQueue<IScriptEvent> m_ChatQueue = new BlockingQueue<IScriptEvent>();
        RwLockedDictionary<UUID, LLAgent> m_Agents = new RwLockedDictionary<UUID, LLAgent>();
        Thread m_ChatThread;
        Dictionary<MessageType, Action<Message>> m_Routing = new Dictionary<MessageType, Action<Message>>();
        private object m_UseCircuitCodeProcessingLock = new object();
        
        public SceneInterface Scene { get; private set; }
        public bool LogAssetFailures = false;
        public bool LogTransferPacket = true;

        public LLUDPServer(IPAddress bindAddress, int port, IMServiceInterface imService, ChatServiceInterface chatService, SceneInterface scene)
        {
            Scene = scene;
            m_IMService = imService;
            m_ChatService = chatService;
            m_BindAddress = bindAddress;
            m_BindPort = port;
            IPEndPoint ep = new IPEndPoint(m_BindAddress, m_BindPort);
            m_UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            for (int i = 0; i < 100; ++i)
            {
                m_InboundBufferQueue.Enqueue(new UDPReceivePacket());
            }

            try
            {
                if (m_UdpSocket.Ttl < 128)
                {
                    m_UdpSocket.Ttl = 128;
                }
            }
            catch (SocketException)
            {
                m_Log.Debug("Failed to increase default TTL");
            }

            /* since Win 2000, there is a WSAECONNRESET, we do not want that in our code */
            try
            {
                const int SIO_UDP_CONNRESET = -1744830452;

                m_UdpSocket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null);
            }
            catch (SocketException)
            {
                /* however, mono does not have an idea about what this is all about, so we catch that here */
            }

            m_ChatThread = new Thread(ChatSendHandler);
            m_ChatThread.Start();
            InitRouting();
            m_UdpSocket.Bind(ep);
            m_Log.InfoFormat("Initialized UDP Server at {0}:{1}", bindAddress.ToString(), port);
        }

        public void SendMessageToCircuit(UInt32 circuitcode, Message m)
        {
            Circuit circuit;
            if(m_Circuits.TryGetValue(circuitcode, out circuit))
            {
                circuit.SendMessage(m);
            }
        }

        public void Dispose()
        {
            Stop();
            m_ChatQueue.Enqueue(new ShutdownEvent());
            Scene = null;
        }

        public void Shutdown()
        {
            Stop();
            m_ChatQueue.Enqueue(new ShutdownEvent());
            Scene = null;
        }

        #region Chat Thread
        public void ChatSendHandler()
        {
            Thread.CurrentThread.Name = "Chat:Routing Thread";
            while(true)
            {
                IScriptEvent ev = m_ChatQueue.Dequeue();
                if(ev is ShutdownEvent)
                {
                    break;
                }
                else if(ev is ListenEvent)
                {
                    m_ChatService.Send((ListenEvent)ev);
                }
            }
        }
        #endregion

        public void Start()
        {
            lock(this)
            {
                if(m_InboundRunning)
                {
                    return;
                }
                try
                {
                    m_InboundRunning = true;
                    BeginUdpReceive();
                    m_Log.InfoFormat("Started at {0}:{1}", m_BindAddress.ToString(), m_BindPort);
                }
                catch(Exception e)
                {
                    m_InboundRunning = false;
                    throw e;
                }
            }
        }

        public void Stop()
        {
            lock(this)
            {
                if(m_InboundRunning)
                {
                    m_Log.InfoFormat("Stopped at {0}:{1}", m_BindAddress.ToString(), m_BindPort);
                }
                m_InboundRunning = false;
            }
            foreach(Circuit c in m_Circuits.Values)
            {
                c.Stop();
                c.Dispose();
            }
        }

        #region UDP Receive Handler
        void BeginUdpReceive()
        {
            UDPReceivePacket pck;

            try
            {
                pck = m_InboundBufferQueue.Dequeue();
                pck.Reset();
            }
            catch
            {
                pck = new UDPReceivePacket();
            }
            
            m_UdpSocket.BeginReceiveFrom(pck.Data, 0, pck.Data.Length, SocketFlags.None, ref pck.RemoteEndPoint,
                UdpReceiveEndHandler, pck);
        }

        void UdpReceiveEndHandler(IAsyncResult ar)
        {
            Circuit circuit;
            UDPReceivePacket pck = (UDPReceivePacket)ar.AsyncState;
            try
            {
                pck.DataLength = m_UdpSocket.EndReceiveFrom(ar, ref pck.RemoteEndPoint);
            }
            catch
            {
                return;
            }
            finally
            {
                if (m_InboundRunning)
                {
                    BeginUdpReceive();
                }
            }

            pck.TransferredAtTime = Environment.TickCount;
            pck.EnqueuedAtTime = Environment.TickCount;

            /* we do not want to spend time on decoding packets that are unknown where they belong */
            if(!m_Circuits.TryGetValue(pck.RemoteEndPoint, out circuit))
            {
                try
                {
                    /* check whether we got an UseCircuitCode */
                    MessageType mType = pck.ReadMessageType();
                    if (MessageType.UseCircuitCode == mType)
                    {
                        UInt32 circuitcode = pck.ReadUInt32();
                        /* it is, so we have actually to look for the circuitcode and set up the remote endpoint here */
                        lock (m_UseCircuitCodeProcessingLock)
                        {
                            if (m_Circuits.TryGetValue(circuitcode, out circuit))
                            {
                                UUID sessionID = pck.ReadUUID();
                                UUID agentID = pck.ReadUUID();
                                /* there it is check for SessionID and AgentID */
                                if (!circuit.SessionID.Equals(sessionID))
                                {
                                    /* no match on SessionID */
                                }
                                else if (!circuit.AgentID.Equals(agentID))
                                {
                                    /* no match on AgentID */
                                }
                                else
                                {
                                    /* it matches, so we have to change the actual key */
                                    IPEndPoint endpoint = new IPEndPoint(0, 0);
                                    EndPoint ep = endpoint.Create(pck.RemoteEndPoint.Serialize());
                                    m_Circuits.Remove(circuit.CircuitCode);
                                    m_Circuits.Add(ep, circuit.CircuitCode, circuit);
                                    circuit.RemoteEndPoint = ep;
                                    try
                                    {
                                        circuit.Start();

                                        SceneInterface scene = Scene;
                                        Messages.Region.RegionHandshake rh = new Messages.Region.RegionHandshake();
                                        rh.RegionFlags = 0;
                                        rh.SimAccess = scene.RegionData.Access;
                                        rh.SimName = scene.Name;
                                        rh.SimOwner = scene.Owner.ID;
                                        rh.IsEstateManager = scene.IsEstateManager(new UUI(circuit.Agent.ID, circuit.Agent.FirstName, circuit.Agent.LastName, circuit.Agent.HomeURI));
                                        rh.WaterHeight = scene.RegionSettings.WaterHeight;
                                        rh.BillableFactor = 1;
                                        rh.TerrainStartHeight00 = scene.RegionSettings.Elevation1SW;
                                        rh.TerrainStartHeight01 = scene.RegionSettings.Elevation2SW;
                                        rh.TerrainStartHeight10 = scene.RegionSettings.Elevation1NW;
                                        rh.TerrainStartHeight11 = scene.RegionSettings.Elevation2NW;
                                        rh.TerrainHeightRange00 = scene.RegionSettings.Elevation1SE;
                                        rh.TerrainHeightRange01 = scene.RegionSettings.Elevation2SE;
                                        rh.TerrainHeightRange10 = scene.RegionSettings.Elevation1NE;
                                        rh.TerrainHeightRange11 = scene.RegionSettings.Elevation2NE;
                                        rh.TerrainBase0 = UUID.Zero;
                                        rh.TerrainBase1 = UUID.Zero;
                                        rh.TerrainBase2 = UUID.Zero;
                                        rh.TerrainBase3 = UUID.Zero;
                                        rh.TerrainDetail0 = scene.RegionSettings.TerrainTexture1;
                                        rh.TerrainDetail1 = scene.RegionSettings.TerrainTexture2;
                                        rh.TerrainDetail2 = scene.RegionSettings.TerrainTexture3;
                                        rh.TerrainDetail3 = scene.RegionSettings.TerrainTexture4;
                                        rh.RegionID = scene.ID;
                                        rh.CacheID = UUID.Random;
                                        rh.CPUClassID = 9;
                                        rh.CPURatio = 1;
                                        rh.ColoName = "";
                                        rh.ProductSKU = VersionInfo.SimulatorVersion;
                                        rh.ProductName = VersionInfo.ProductName;

                                        Messages.Region.RegionHandshake.RegionExtDataEntry entry = new Messages.Region.RegionHandshake.RegionExtDataEntry();
                                        entry.RegionFlagsExtended = 0;
                                        entry.RegionProtocols = 0; /* 0 => no SSB, 1 => SSB */
                                        rh.RegionExtData.Add(entry);

                                        /* Immediate Ack */
                                        SendPacketTo(UDPPacket.PacketAckImmediate(pck.SequenceNumber), ep);
                                        
                                        circuit.SendMessage(rh);
                                    }
                                    catch(Exception e)
                                    {
                                        m_Log.DebugFormat("UseCircuitCode Exception {0} {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
                                        circuit.Stop();
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {

                }

                /* back to pool with that packet. Packet holds nothing of interest. */
                m_InboundBufferQueue.Enqueue(pck);
                return;
            }

            /* here we spend time on decoding */
            if(pck.IsUndersized)
            {
                /* packet is undersized so we throw it away as well */
                m_InboundBufferQueue.Enqueue(pck);
                return;
            }

            /* now we know that the packet is at least valid
             * We can pass it to the circuit handler.
             */

            /* we decode the ack numbers here, the code does not need to be implemented in the UDP Circuit Handler */
            List<UInt32> acknumbers = null;

            if (pck.HasAckFlag)
            {
                try
                {
                    acknumbers = pck.Acks;
                }
                catch
                {
                    /* packet is undersized so we throw it away as well */
                    m_InboundBufferQueue.Enqueue(pck);
                    return;
                }
            }

            try
            {
                circuit.PacketReceived(pck.RemoteEndPoint, pck, acknumbers);
            }
            catch(Exception e)
            {
                /* we catch all issues here */
                m_Log.ErrorFormat("Exception {0} => {1} at {2}", e.GetType().Name, e.ToString(), e.StackTrace.ToString());
            }
            /* return the buffer to the pool */
            m_InboundBufferQueue.Enqueue(pck);
        }
        #endregion

        protected internal int SendPacketTo(UDPPacket p, EndPoint ep)
        {
            m_UdpSocket.SendTo(p.Data, 0, p.DataLength, SocketFlags.None, ep);
            return p.DataLength;
        }

        void HandleAgentUpdateMessage(Message m)
        {
            LLAgent agent;
            if (m_Agents.TryGetValue(m.CircuitAgentID, out agent))
            {
                agent.HandleAgentUpdateMessage(m);
            }
        }

        void HandleAgentMessage(Message m)
        {
            LLAgent agent;
            if (m_Agents.TryGetValue(m.CircuitAgentID, out agent))
            {
                agent.HandleAgentMessage(m);
            }
        }

        void InitRouting()
        {
            /* Objects */
            m_Routing[MessageType.ObjectGrab] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectGrabUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDeGrab] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectAdd] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDelete] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDuplicate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDuplicateOnRay] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.MultipleObjectUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectRotation] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectClickAction] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RequestMultipleObjects] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectFlagUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectImage] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectMaterial] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectShape] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectExtraParams] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectOwner] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectGroup] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectBuy] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.BuyObjectInventory] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectPermissions] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectSaleInfo] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectName] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDescription] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectCategory] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectSelect] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDeselect] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectAttach] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDetach] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDrop] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectLink] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDelink] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectExportSelected] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RequestObjectPropertiesFamily] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RequestPayPrice] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectIncludeInSearch] = Scene.HandleSimulatorMessage;

            /* Regions */
            m_Routing[MessageType.RegionHandleRequest] = Scene.HandleSimulatorMessage;

            /* Mute List */
            m_Routing[MessageType.MuteListRequest] = HandleAgentMessage;
            m_Routing[MessageType.UpdateMuteListEntry] = HandleAgentMessage;
            m_Routing[MessageType.RemoveMuteListEntry] = HandleAgentMessage;

            /* Scripts */
            m_Routing[MessageType.GetScriptRunning] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.SetScriptRunning] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ScriptReset] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ScriptAnswerYes] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RevokePermissions] = Scene.HandleSimulatorMessage;

            /* God */
            m_Routing[MessageType.RequestGodlikePowers] = HandleAgentMessage;

            /* Agent Update */
            m_Routing[MessageType.AgentUpdate] = HandleAgentUpdateMessage;
            m_Routing[MessageType.AgentFOV] = HandleAgentMessage;
            m_Routing[MessageType.AgentHeightWidth] = HandleAgentMessage;
            m_Routing[MessageType.AgentSetAppearance] = HandleAgentMessage;
            m_Routing[MessageType.AgentAnimation] = HandleAgentMessage;
            m_Routing[MessageType.AgentRequestSit] = HandleAgentMessage;
            m_Routing[MessageType.AgentDataUpdateRequest] = HandleAgentMessage;

            /* Economy */
            m_Routing[MessageType.EconomyDataRequest] = HandleSimulatorMessageLocally;
            m_Routing[MessageType.MoneyBalanceRequest] = HandleAgentMessage;

            /* Appearance */
            m_Routing[MessageType.AgentWearablesRequest] = HandleAgentMessage;
            m_Routing[MessageType.AgentIsNowWearing] = HandleAgentMessage;
            m_Routing[MessageType.AgentCachedTexture] = HandleAgentMessage;
            m_Routing[MessageType.ViewerEffect] = HandleAgentMessage;
            m_Routing[MessageType.RezSingleAttachmentFromInv] = HandleAgentMessage;
            m_Routing[MessageType.RezMultipleAttachmentFromInv] = HandleAgentMessage;
            m_Routing[MessageType.DetachAttachmentIntoInv] = HandleAgentMessage;
            m_Routing[MessageType.CreateNewOutfitAttachments] = HandleAgentMessage;

            /* Agent State */
            m_Routing[MessageType.AgentPause] = HandleAgentMessage;
            m_Routing[MessageType.AgentResume] = HandleAgentMessage;
            m_Routing[MessageType.SetAlwaysRun] = HandleAgentMessage;

            /* Region Handshake */
            m_Routing[MessageType.RegionHandshakeReply] = HandleAgentMessage;
            m_Routing[MessageType.CompleteAgentMovement] = HandleAgentMessage;

            /* Logout Request */
            m_Routing[MessageType.LogoutRequest] = HandleAgentMessage;

            /* Parcel */
            m_Routing[MessageType.ParcelInfoRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelObjectOwnersRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelPropertiesRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelPropertiesRequestByID] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelPropertiesUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelReturnObjects] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelSetOtherCleanTime] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelDisableObjects] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelSelectObjects] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelBuyPass] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelDeedToGroup] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelReclaim] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelClaim] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelJoin] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelDivide] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelRelease] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelBuy] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelGodForceOwner] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelAccessListRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelAccessListUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelDwellRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelGodMarkAsContent] = Scene.HandleSimulatorMessage;

            m_Routing[MessageType.RequestRegionInfo] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.GodUpdateRegionInfo] = Scene.HandleSimulatorMessage;

            /* Undo/Redo logic */
            m_Routing[MessageType.Undo] = HandleAgentMessage;
            m_Routing[MessageType.Redo] = HandleAgentMessage;

            /* Land */
            m_Routing[MessageType.ModifyLand] = HandleAgentMessage;
            m_Routing[MessageType.UndoLand] = HandleAgentMessage;

            /* Object Inventory */
            m_Routing[MessageType.UpdateTaskInventory] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RemoveTaskInventory] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RequestTaskInventory] = Scene.HandleSimulatorMessage;

            /* Rez and Derez */
            m_Routing[MessageType.DeRezObject] = HandleAgentMessage;
            m_Routing[MessageType.RezObject] = HandleAgentMessage;
            m_Routing[MessageType.RezObjectFromNotecard] = HandleAgentMessage;
            m_Routing[MessageType.RezScript] = HandleAgentMessage;
            m_Routing[MessageType.RezRestoreToWorld] = HandleAgentMessage;

            m_Routing[MessageType.ParcelMediaCommandMessage] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelMediaUpdate] = Scene.HandleSimulatorMessage;

            /* Sound */
            m_Routing[MessageType.SoundTrigger] = Scene.HandleSimulatorMessage;

        }

        public void RouteReceivedMessage(Message m)
        {
            Action<Message> action;
            if(m_Routing.TryGetValue(m.Number, out action))
            {
                action(m);
            }
        }

        public void RouteIM(GridInstantMessage im)
        {
            if (null != m_IMService)
            {
                m_IMService.Send(im);
            }
        }

        public void RouteChat(ListenEvent ev)
        {
            if (null != m_ChatService)
            {
                m_ChatService.Send(ev);
            }
        }

        public void AddCircuit(Circuit c)
        {
            m_Circuits.Add(c.RemoteEndPoint, c.CircuitCode, c);
            try
            {
                m_Agents.Add(c.AgentID, c.Agent);
            }
            catch
            {
                m_Circuits.Remove(c.RemoteEndPoint, c.CircuitCode);
                throw;
            }
        }

        public void RemoveCircuit(Circuit c)
        {
            m_Agents.Remove(c.AgentID);
            m_Circuits.Remove(c.RemoteEndPoint, c.CircuitCode);
        }

        public void SendMessageToAgent(UUID agentID, Message m)
        {
            try
            {
                LLAgent agent = m_Agents[agentID];
                agent.Circuits[Scene.ID].SendMessage(m);
            }
            catch
            {

            }
        }
    }
    #endregion
}
