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
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    public class Circuit : IDisposable
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL CIRCUIT");
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
                    }
                }
            }
        }

        public RwLockedDictionary<UInt32, UDPPacket> m_UnackedPackets = new RwLockedDictionary<uint, UDPPacket>();
        public Circuit(LLUDPServer server, UInt32 circuitcode, CapsHttpRedirector capsredirector, UUID regionSeedID)
        {
            m_Server = server;
            CircuitCode = circuitcode;
            m_CapsRedirector = capsredirector;
            AddCapability("SEED", regionSeedID, RegionSeedHandler);
            Scene = server.Scene;
        }

        ~Circuit()
        {
            Scene = null;
            Dispose();
        }

        #region Receive Logic
        public void PacketReceived(EndPoint ep, UDPPacket pck, List<UInt32> acknumbers)
        {
            /* no need for returning packets here since we are obliqued never to pass them around.
             * We just decode them here to actual messages
             */
            MessageType mType = pck.ReadMessageType();

            /* do we have some acks from the packet's end? */
            if(null != acknumbers)
            {
                foreach(UInt32 ackno in acknumbers)
                {
                    m_UnackedPackets.Remove(ackno);
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
                        m_UnackedPackets.Remove(pck.ReadUInt32());
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
                        ev.Type = ListenEvent.ChatType.Say;
                        switch(pck.ReadUInt8())
                        {
                            case 0: ev.Type = ListenEvent.ChatType.Whisper; break;
                            case 2: ev.Type = ListenEvent.ChatType.Shout; break;
                            default: break;
                        }
                        ev.Channel = pck.ReadInt32();
                        ev.GlobalPosition = Agent.GlobalPosition;
                        ev.Name = Agent.Name;
                        ev.TargetID = UUID.Zero;
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
                        im.ParentEstateID = (int)pck.ReadUInt32();
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
                        m_Server.RouteReceivedMessage(m);
                    }
                    else
                    {
                        /* Ignore we have no decoder for that */
                    }
                    break;
            }
        }
        #endregion

        #region Transmit Logic
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
            }
        }

        private void TransmitThread(object param)
        {
            int lastAckTick = Environment.TickCount;
            int lastPingTick = Environment.TickCount;
            byte pingID = 0;
            Thread.CurrentThread.Name = string.Format("LLUDP:Transmitter for CircuitCode {0} / IP {1}", CircuitCode, RemoteEndPoint.ToString());

            while (true)
            {
                try
                {
                    Message m = m_TxQueue.Dequeue(5000);
                    if (m is CancelTxThread)
                    {
                        break;
                    }
                }
                catch
                {
                }

                if(Environment.TickCount - lastPingTick < 5000)
                {
                    if (!m_PingSendTicks.ContainsKey(pingID))
                    {
                        lastPingTick = Environment.TickCount;
                        UDPPacket p = new UDPPacket();
                        p.WriteMessageType(MessageType.StartPingCheck);
                        p.WriteUInt8(pingID++);
                        m_PingSendTicks[pingID] = Environment.TickCount;
                        p.WriteUInt32(0);
                        m_Server.SendPacketTo(p, RemoteEndPoint);
                    }
                }
                if (Environment.TickCount - lastAckTick < 1000)
                {
                    lastAckTick = Environment.TickCount;
                    /* check for acks to be send */
                    int c = m_AckList.Count;
                    while (c > 0)
                    {
                        UDPPacket p = new UDPPacket();
                        p.WriteMessageType(MessageType.PacketAck);
                        if (c > 100)
                        {
                            p.WriteUInt8(100);
                            for (int i = 0; i < 100; ++i)
                            {
                                p.WriteUInt32(m_AckList.Dequeue());
                            }
                            c -= 100;
                        }
                        else
                        {
                            p.WriteUInt8((byte)c);
                            for (int i = 0; i < c; ++i)
                            {
                                p.WriteUInt32(m_AckList.Dequeue());
                            }
                            c = 0;
                        }
                        p.SequenceNumber = NextSequenceNumber;
                        m_Server.SendPacketTo(p, RemoteEndPoint);
                    }
                }
            }
            m_TxRunning = false;
        }


        void SendPacket(UDPPacket p)
        {
            p.EnqueuedAtTime = (uint)Environment.TickCount;
        }

        public void SendMessage(Message m)
        {
            try
            {
                UDPPacket p = new UDPPacket();
                m.Serialize(p);
                SendPacket(p);
            }
            catch(Exception e)
            {
                m_Log.ErrorFormat("{0} at {1}", e.ToString(), e.StackTrace.ToString());
            }
        }
        #endregion

        #region Capabilities registration
        public void AddCapability(string type, UUID id, Action<HttpRequest> del)
        {
            m_RegisteredCapabilities.Add(type, id);
            try
            {
                m_CapsRedirector.Caps[type].Add(id, del);
            }
            catch
            {
                m_RegisteredCapabilities.Remove(type);
                throw;
            }
        }

        public bool RemoveCapability(string type)
        {
            UUID id;
            if (m_RegisteredCapabilities.Remove(type, out id))
            {
                return m_CapsRedirector.Caps[type].Remove(id);
            }
            return false;
        }

        public void Dispose()
        {
            Scene = null;
            foreach(KeyValuePair<string, UUID> kvp in m_RegisteredCapabilities)
            {
                m_CapsRedirector.Caps[kvp.Key].Remove(kvp.Value);
            }
            m_RegisteredCapabilities.Clear();
        }
        
        public void RegionSeedHandler(HttpRequest httpreq)
        {
            IValue o;
            if(httpreq.Method != "POST")
            {
                httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed").Close();
                return;
            }

            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch(Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                httpreq.BeginResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type").Close();
                return;
            }
            if(!(o is AnArray))
            {
                httpreq.BeginResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type").Close();
                return;
            }

            Dictionary<string, string> capsUri = new Dictionary<string, string>();
            foreach(IValue v in (AnArray)o)
            {
                UUID capsID;
                if(v.ToString() == "SEED")
                {

                }
                else if (m_RegisteredCapabilities.TryGetValue(v.ToString(), out capsID))
                {
                    capsUri[v.ToString()] = string.Format("http://{0}:{1}/CAPS/{2}/{3}", m_CapsRedirector.ExternalHostName, m_CapsRedirector.Port,
                        v.ToString(), capsID);
                }
            }

            HttpResponse res = httpreq.BeginResponse();
            Stream tw = res.GetOutputStream();
            tw.Write(m_Header, 0, m_Header.Length);
            foreach(KeyValuePair<string, string> kvp in capsUri)
            {
                byte[] data = Encoding.UTF8.GetBytes(string.Format("<key>{0}</key><string>{1}</string>",
                        System.Xml.XmlConvert.EncodeName(kvp.Key),
                        System.Xml.XmlConvert.EncodeName(kvp.Value))
                    );
                tw.Write(data, 0, data.Length);
            }
            tw.Write(m_Footer, 0, m_Footer.Length);
            res.Close();
        }

        private static readonly byte[] m_Header = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><llsd><map>");
        private static readonly byte[] m_Footer = Encoding.UTF8.GetBytes("</map></llsd>");
        #endregion
    }
}
