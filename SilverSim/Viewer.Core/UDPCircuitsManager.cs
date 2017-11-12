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
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.PortControl;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.IM;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Circuit;
using SilverSim.Viewer.Messages.Region;
using SilverSim.Viewer.Messages.Test;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    #region Rx Buffer
    public class UDPReceivePacket : UDPPacket
    {
        public EndPoint RemoteEndPoint = new IPEndPoint(0, 0);
    }
    #endregion

    #region LLUDP Server
    public partial class UDPCircuitsManager : IUDPCircuitsManager
    {
        private static readonly ILog m_Log = LogManager.GetLogger("UDP CIRCUITS MANAGER");
        private readonly IPAddress m_BindAddress;
        private readonly Socket m_UdpSocket;
        private readonly RwLockedDoubleDictionary<EndPoint, uint, Circuit> m_Circuits = new RwLockedDoubleDictionary<EndPoint, uint, Circuit>();
        private bool m_InboundRunning;
        private readonly IMServiceInterface m_IMService;
        private readonly ChatServiceInterface m_ChatService;
        private readonly BlockingQueue<IScriptEvent> m_ChatQueue = new BlockingQueue<IScriptEvent>();
        private readonly RwLockedDictionary<UUID, ViewerAgent> m_Agents = new RwLockedDictionary<UUID, ViewerAgent>();
        private readonly Thread m_ChatThread;
        private readonly object m_UseCircuitCodeProcessingLock = new object();

        public SceneInterface Scene { get; private set; }
        public bool LogAssetFailures;
        public bool LogTransferPacket;

        private readonly List<IPortControlServiceInterface> m_PortControlServices;

        public int LocalPort { get; }

        public UDPCircuitsManager(IPAddress bindAddress, int port, IMServiceInterface imService, ChatServiceInterface chatService, SceneInterface scene,
            List<IPortControlServiceInterface> portControlServices)
        {
            m_PortControlServices = portControlServices;
            Scene = scene;
            m_IMService = imService;
            m_ChatService = chatService;
            m_BindAddress = bindAddress;
            LocalPort = port;
            var ep = new IPEndPoint(m_BindAddress, LocalPort);
            /* trigger early init of UDPPacketDecoder. We do not want this to happen on first teleport */
            AgentCircuit.m_PacketDecoder.CheckInit();
            m_UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

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

            /* handle Bind before starting anything else */
            m_UdpSocket.Bind(ep);

            LocalPort = ((IPEndPoint)m_UdpSocket.LocalEndPoint).Port;

            foreach(IPortControlServiceInterface portControl in m_PortControlServices)
            {
                portControl.EnablePort(new AddressFamily[] { AddressFamily.InterNetwork }, ProtocolType.Udp, LocalPort);
            }

            if (m_ChatService != null)
            {
                m_ChatThread = ThreadManager.CreateThread(ChatSendHandler);
                m_ChatThread.Start();
            }
            m_Log.InfoFormat("Initialized UDP Circuits Manager at {0}:{1}", bindAddress.ToString(), LocalPort);
        }

        public void SendMessageToCircuit(UInt32 circuitcode, Message m)
        {
            Circuit circuit;
            if(m_Circuits.TryGetValue(circuitcode, out circuit))
            {
                circuit.SendMessage(m);
            }
        }

        public void Shutdown()
        {
            Stop();
            m_ChatQueue.Enqueue(new ShutdownEvent());
            m_UdpSocket.Dispose();
            foreach (var portControl in m_PortControlServices)
            {
                try
                {
                    portControl.DisablePort(new AddressFamily[] { AddressFamily.InterNetwork }, ProtocolType.Udp, LocalPort);
                }
                catch(Exception e)
                {
                    m_Log.DebugFormat("Failed to disable port {0}: {1}: {2}", LocalPort, e.GetType().FullName, e.Message);
                }
            }
            Scene = null;
        }

        #region Chat Thread
        public void ChatSendHandler()
        {
            Thread.CurrentThread.Name = "Chat:Routing Thread for " + Scene.ID.ToString();
            while(true)
            {
                var ev = m_ChatQueue.Dequeue();
                if(ev is ShutdownEvent)
                {
                    break;
                }
                else if(ev is ListenEvent)
                {
                    m_ChatService?.Send((ListenEvent)ev);
                }
            }
        }
        #endregion

        private readonly object m_ThreadControlLock = new object();
        public void Start()
        {
            lock(m_ThreadControlLock)
            {
                if(m_InboundRunning)
                {
                    return;
                }
                try
                {
                    m_InboundRunning = true;
                    int i;
                    /* follow recommendations for improving UDP receive performance */
                    for (i = 0; i < 5; ++i)
                    {
                        BeginUdpReceive();
                    }
                    m_Log.InfoFormat("Started at {0}:{1}", m_BindAddress.ToString(), LocalPort);
                }
                catch
                {
                    m_InboundRunning = false;
                    throw;
                }
            }
        }

        public void Stop()
        {
            lock(m_ThreadControlLock)
            {
                if(m_InboundRunning)
                {
                    m_Log.InfoFormat("Stopped at {0}:{1}", m_BindAddress.ToString(), LocalPort);
                }
                m_InboundRunning = false;
            }
            foreach (var c in m_Circuits.Values)
            {
                c.Stop();
            }
        }

        #region UDP Receive Handler
        private void BeginUdpReceive()
        {
            var pck = new UDPReceivePacket();

            m_UdpSocket.BeginReceiveFrom(pck.Data, 0, pck.Data.Length, SocketFlags.None, ref pck.RemoteEndPoint,
                UdpReceiveEndHandler, pck);
        }

        private bool VerifyEndpointAddress(AgentCircuit circ, EndPoint newEp)
        {
            if (circ.RemoteEndPoint.AddressFamily != newEp.AddressFamily)
            {
                return false;
            }

            if (circ.RemoteEndPoint.AddressFamily == AddressFamily.InterNetwork ||
                circ.RemoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var ep1 = circ.RemoteEndPoint as IPEndPoint;
                var ep2 = newEp as IPEndPoint;
                if(ep1 == null || ep2 == null)
                {
                    return false;
                }
                return ep1.Address.Equals(ep2.Address);
            }

            foreach(var service in m_PortControlServices)
            {
                if(service.CheckIfMappedAddress(newEp, circ.RemoteEndPoint))
                {
                    return true;
                }
            }

            return false;
        }

        public event Action<IPAddress, int> OnNetTest;
        private int m_NetTestSeqNumber;

        public void SendNetTest(IPAddress address)
        {
            var p = new UDPPacket
            {
                OutQueue = Message.QueueOutType.High,
                IsZeroEncoded = false,
                SequenceNumber = (uint)Interlocked.Increment(ref m_NetTestSeqNumber)
            };
            p.WriteMessageNumber(MessageType.NetTest);
            new NetTest { Port = (ushort)LocalPort }.Serialize(p);
            p.Flush();
            SendPacketTo(p, new IPEndPoint(address, LocalPort));
        }

        private void UdpReceiveEndHandler(IAsyncResult ar)
        {
            Circuit circuit;
            var pck = (UDPReceivePacket)ar.AsyncState;
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
            if(!m_Circuits.TryGetValue(pck.RemoteEndPoint, out circuit) || circuit.ForceUseCircuitCode)
            {
                try
                {
                    /* check whether we got an UseCircuitCode */
                    var mType = pck.ReadMessageType();
                    if (MessageType.UseCircuitCode == mType)
                    {
                        var circuitcode = pck.ReadUInt32();
                        /* it is, so we have actually to look for the circuitcode and set up the remote endpoint here */
                        lock (m_UseCircuitCodeProcessingLock)
                        {
                            if (m_Circuits.TryGetValue(circuitcode, out circuit))
                            {
                                circuit.ForceUseCircuitCode = false;
                                var sessionID = pck.ReadUUID();
                                var agentID = pck.ReadUUID();
                                var acircuit = circuit as AgentCircuit;
                                if (acircuit != null)
                                {
                                    /* there it is check for SessionID and AgentID */
                                    if (!acircuit.SessionID.Equals(sessionID) ||
                                        !acircuit.AgentID.Equals(agentID) ||
                                        !VerifyEndpointAddress(acircuit, pck.RemoteEndPoint))
                                    {
                                        /* no match on SessionID or AgentID */
                                        m_Log.DebugFormat("Unmatched UseCircuitCode for AgentID {0} SessionID {1} CircuitCode {2} received", agentID, sessionID, circuitcode);
                                    }
                                    else
                                    {
                                        /* it matches, so we have to change the actual key */
                                        var endpoint = new IPEndPoint(0, 0);
                                        var ep = endpoint.Create(pck.RemoteEndPoint.Serialize());
                                        m_Circuits.Remove(circuit.CircuitCode);
                                        m_Circuits.Add(ep, circuit.CircuitCode, circuit);
                                        circuit.RemoteEndPoint = ep;
                                        try
                                        {
                                            circuit.Start();

                                            var scene = Scene;
                                            RegionOptionFlags regionFlags = scene.RegionSettings.AsFlags;
                                            var rh = new RegionHandshake
                                            {
                                                RegionFlags = regionFlags,
                                                SimAccess = scene.Access,
                                                SimName = scene.Name,
                                                SimOwner = scene.Owner.ID,
                                                IsEstateManager = scene.IsEstateManager(new UUI(acircuit.Agent.ID, acircuit.Agent.FirstName, acircuit.Agent.LastName, acircuit.Agent.HomeURI)),
                                                WaterHeight = scene.RegionSettings.WaterHeight,
                                                BillableFactor = 1,
                                                TerrainStartHeight00 = scene.RegionSettings.Elevation1SW,
                                                TerrainStartHeight01 = scene.RegionSettings.Elevation2SW,
                                                TerrainStartHeight10 = scene.RegionSettings.Elevation1NW,
                                                TerrainStartHeight11 = scene.RegionSettings.Elevation2NW,
                                                TerrainHeightRange00 = scene.RegionSettings.Elevation1SE,
                                                TerrainHeightRange01 = scene.RegionSettings.Elevation2SE,
                                                TerrainHeightRange10 = scene.RegionSettings.Elevation1NE,
                                                TerrainHeightRange11 = scene.RegionSettings.Elevation2NE,
                                                TerrainBase0 = UUID.Zero,
                                                TerrainBase1 = UUID.Zero,
                                                TerrainBase2 = UUID.Zero,
                                                TerrainBase3 = UUID.Zero,
                                                TerrainDetail0 = scene.RegionSettings.TerrainTexture1,
                                                TerrainDetail1 = scene.RegionSettings.TerrainTexture2,
                                                TerrainDetail2 = scene.RegionSettings.TerrainTexture3,
                                                TerrainDetail3 = scene.RegionSettings.TerrainTexture4,
                                                RegionID = scene.ID,
                                                CacheID = UUID.Random,
                                                CPUClassID = 9,
                                                CPURatio = 1,
                                                ColoName = string.Empty,
                                                ProductSKU = VersionInfo.SimulatorVersion,
                                                ProductName = scene.ProductName
                                            };
                                            rh.RegionExtData.Add(new RegionHandshake.RegionExtDataEntry
                                            {
                                                RegionFlagsExtended = (ulong)regionFlags,
                                                RegionProtocols = 1 /* 0 => no SSB, 1 => SSB */
                                            });

                                            /* Immediate Ack */
                                            SendPacketTo(UDPPacket.PacketAckImmediate(pck.SequenceNumber), ep);

                                            circuit.SendMessage(rh);
                                            return;
                                        }
                                        catch (Exception e)
                                        {
                                            m_Log.DebugFormat("UseCircuitCode Exception {0} {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
                                            circuit.Stop();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                m_Log.DebugFormat("UseCircuitCode received for unknown circuit {0}", circuitcode);
                            }
                        }
                    }
                    else if(MessageType.NetTest == mType && (uint)m_NetTestSeqNumber == pck.SequenceNumber ||
                        pck.ReadUInt16() == (ushort)LocalPort)
                    {
                        OnNetTest?.Invoke(((IPEndPoint)pck.RemoteEndPoint).Address, LocalPort);
                        return;
                    }
                }
                catch
                {
                    /* no action required */
                }

#if DEBUG
                m_Log.DebugFormat("Unmatched endpoint address {0} for UDP server at port {1}", pck.RemoteEndPoint.ToString(), LocalPort);
#endif
                return;
            }

            /* here we spend time on decoding */
            if(pck.IsUndersized)
            {
                /* packet is undersized so we throw it away as well */
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
                m_Log.ErrorFormat("Exception {0} => {1} at {2}", e.GetType().Name, e.ToString(), e.StackTrace);
            }
            /* return the buffer to the pool */
        }
        #endregion

        protected internal int SendPacketTo(UDPPacket p, EndPoint ep)
        {
            m_UdpSocket.SendTo(p.Data, 0, p.DataLength, SocketFlags.None, ep);
            return p.DataLength;
        }

        public void RouteIM(GridInstantMessage im)
        {
            m_IMService?.Send(im);
        }

        public void RouteChat(ListenEvent ev)
        {
            m_ChatService?.Send(ev);
        }

        public void AddCircuit(Circuit c)
        {
            m_Circuits.Add(c.RemoteEndPoint, c.CircuitCode, c);
            try
            {
                var ac = c as AgentCircuit;
                if (ac != null)
                {
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
            var ac = c as AgentCircuit;
            if (ac != null)
            {
                m_Agents.Remove(ac.AgentID);
            }
            m_Circuits.Remove(c.RemoteEndPoint, c.CircuitCode);
        }

        public void SendMessageToAgent(UUID agentID, Message m)
        {
            try
            {
                var agent = m_Agents[agentID];
                agent.Circuits[Scene.ID].SendMessage(m);
            }
            catch
            {
                /* no action required */
            }
        }

        public ICircuit UseSimCircuit(IPEndPoint ep, UUID sessionID, SceneInterface thisScene, UUID remoteSceneID, uint circuitcode, GridVector remoteLocation, Vector3 remoteOffset)
        {
            var circuit = new SimCircuit(this, circuitcode, remoteSceneID, sessionID, remoteLocation, remoteOffset)
            {
                Scene = thisScene
            };
            AddCircuit(circuit);
            var useCircuitCode = new UseCircuitCode
            {
                SessionID = sessionID,
                AgentID = thisScene.ID,
                CircuitCode = circuitcode
            };
            circuit.SendMessage(useCircuitCode);
            return circuit;
        }
    }
    #endregion
}
