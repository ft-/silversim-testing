// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        private object m_UseCircuitCodeProcessingLock = new object();
        
        public SceneInterface Scene { get; private set; }
        public bool LogAssetFailures = false;
        public bool LogTransferPacket = false;

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
            foreach (Circuit c in m_Circuits.Values)
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
                                if (circuit is AgentCircuit)
                                {
                                    AgentCircuit acircuit = (AgentCircuit)circuit;
                                    /* there it is check for SessionID and AgentID */
                                    if (!acircuit.SessionID.Equals(sessionID))
                                    {
                                        /* no match on SessionID */
                                    }
                                    else if (!acircuit.AgentID.Equals(agentID))
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
                                            rh.IsEstateManager = scene.IsEstateManager(new UUI(acircuit.Agent.ID, acircuit.Agent.FirstName, acircuit.Agent.LastName, acircuit.Agent.HomeURI));
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
                                        catch (Exception e)
                                        {
                                            m_Log.DebugFormat("UseCircuitCode Exception {0} {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
                                            circuit.Stop();
                                        }
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
                if (c is AgentCircuit)
                {
                    AgentCircuit ac = (AgentCircuit)c;
                    m_Agents.Add(ac.AgentID, ac.Agent);
                }
            }
            catch
            {
                m_Circuits.Remove(c.RemoteEndPoint, c.CircuitCode);
                throw;
            }
        }

        public void RemoveCircuit(Circuit c)
        {
            if (c is AgentCircuit)
            {
                AgentCircuit ac = (AgentCircuit)c;
                m_Agents.Remove(ac.AgentID);
            }
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
