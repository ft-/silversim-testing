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

using SilverSim.LL.Messages;
using System;
using System.Threading;
using System.Collections.Generic;
using SilverSim.Scene.Types.Scene;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        #region LLUDP Packet transmitter
        enum QueueOutType : uint
        {
            High,
            Resend,
            LandLayerData,
            WindLayerData,
            GenericLayerData,
            Medium,
            Low,
            Asset,
            Texture,

            NumQueues, /* must be last */
        }

        int ThrottleRate = 60125;
        int RateBucket;
        bool m_UseThrottle = false;

        private readonly Dictionary<MessageType, QueueOutType> m_QueueOutTable = new Dictionary<MessageType, QueueOutType>();
        static void InitializeTransmitQueueRouting()
        {
        }

        void InitializeTransmitQueueing()
        {
        }

        const int TRANSMIT_THROTTLE_MTU = 1500;
        const int MAX_DATA_MTU = 1400;

        void HandleThrottlePacket(Message msg)
        {
            Messages.Agent.AgentThrottle m = (Messages.Agent.AgentThrottle)msg;
            if(m.SessionID != SessionID || m.AgentID != AgentID)
            {
                return;
            }
            if (!BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < 7; i++)
                {
                    Array.Reverse(m.Throttles, i * 4, 4);
                }
            }
            int resend = (int)(BitConverter.ToSingle(m.Throttles, 0) * 0.125f);
            int land = (int)(BitConverter.ToSingle(m.Throttles, 4) * 0.125f);
            int wind = (int)(BitConverter.ToSingle(m.Throttles, 8) * 0.125f);
            int cloud = (int)(BitConverter.ToSingle(m.Throttles, 12) * 0.125f);
            int task = (int)(BitConverter.ToSingle(m.Throttles, 16) * 0.125f);
            int texture = (int)(BitConverter.ToSingle(m.Throttles, 20) * 0.125f);
            int asset = (int)(BitConverter.ToSingle(m.Throttles, 24) * 0.125f);

            ThrottleRate = resend + land + wind + cloud + task + texture + asset;
            m_Log.DebugFormat("Reconfigured throttle for {0} to: Total {1} Kbs",
                Agent.Owner.FullName,
                ThrottleRate);
        }

        private void TerminateCircuit()
        {
            if (Agent.IsInScene(Scene))
            {
                try
                {
                    Agent.GridUserService.LoggedOut(Agent.Owner, Scene.ID, Agent.GlobalPosition, Agent.LookAt);
                }
                catch
                {

                }
                try
                {
                    Agent.PresenceService[SessionID, Agent.Owner.ID] = null;
                }
                catch
                {

                }
            }

            SceneInterface scene = Scene;
            if (null != scene)
            {
                LLUDPServer server = (LLUDPServer)scene.UDPServer;
                if (server != null)
                {
                    server.RemoveCircuit(this);
                }
            }

            Stop();
            Agent = null;
            Scene = null;
            return;
        }

        private void TransmitThread(object param)
        {
            int lastAckTick = Environment.TickCount;
            int lastPingTick = Environment.TickCount;
            int lastBucketTick = Environment.TickCount;
            int lastSimStatsTick = Environment.TickCount;
            byte pingID = 0;
            Thread.CurrentThread.Name = string.Format("LLUDP:Transmitter for CircuitCode {0} / IP {1}", CircuitCode, RemoteEndPoint.ToString());
            Queue<Message> LowPriorityQueue;
            Queue<Message> HighPriorityQueue;
            Queue<Message> MediumPriorityQueue;
            Queue<Message>[] QueueList = new Queue<Message>[(int)QueueOutType.NumQueues];
            QueueOutType qroutidx;

            for (uint qidx = 0; qidx < (uint)QueueOutType.NumQueues; ++qidx)
            {
                QueueList[qidx] = new Queue<Message>();
            }

            HighPriorityQueue = QueueList[(uint)QueueOutType.High];
            MediumPriorityQueue = QueueList[(uint)QueueOutType.Medium];
            LowPriorityQueue = QueueList[(uint)QueueOutType.Low];

            while (true)
            {
                Message m;
                try
                {
                    int qcount = 0;
                    foreach(Queue<Message> q in QueueList)
                    {
                        qcount += q.Count;
                    }

                    if (qcount == 0)
                    {
                        m = m_TxQueue.Dequeue(10);
                    }
                    else
                    {
                        m = m_TxQueue.Dequeue(0);
                    }
                    if (m is CancelTxThread)
                    {
                        break;
                    }

                    if(m_QueueOutTable.TryGetValue(m.Number, out qroutidx))
                    {
                        QueueList[(uint)qroutidx].Enqueue(m);
                    }
                    else if (m.Number == MessageType.LayerData)
                    {
                        Messages.LayerData.LayerData ld =(Messages.LayerData.LayerData)m;
                        switch(ld.LayerType)
                        {
                            case Messages.LayerData.LayerData.LayerDataType.Land:
                            case Messages.LayerData.LayerData.LayerDataType.LandExtended:
                                QueueList[(uint)QueueOutType.LandLayerData].Enqueue(m);
                                break;

                            case Messages.LayerData.LayerData.LayerDataType.Wind:
                            case Messages.LayerData.LayerData.LayerDataType.WindExtended:
                                QueueList[(uint)QueueOutType.WindLayerData].Enqueue(m);
                                break;

                            default:
                                QueueList[(uint)QueueOutType.GenericLayerData].Enqueue(m);
                                break;
                        }
                    }
                    else if(m.Number == MessageType.ImageData || m.Number == MessageType.ImagePacket)
                    {
                        QueueList[(uint)QueueOutType.Texture].Enqueue(m);
                    }
                    else if (m.Number == MessageType.TransferPacket || m.Number == MessageType.TransferInfo)
                    {
                        QueueList[(uint)QueueOutType.Asset].Enqueue(m);
                    }
                    else if (m.Number < MessageType.Medium)
                    {
                        HighPriorityQueue.Enqueue(m);
                    }
                    else if(m.Number < MessageType.Low)
                    {
                        MediumPriorityQueue.Enqueue(m);
                    }
                    else
                    {
                        LowPriorityQueue.Enqueue(m);
                    }
                }
                catch
                {

                }

                int bucketCheckTime = Environment.TickCount;
                if (bucketCheckTime != lastBucketTick)
                {
                    int deltatime = bucketCheckTime - lastBucketTick;
                    int decreaseVal = (ThrottleRate * deltatime) / 1000;
                    /* make a min bucket decrease here, prevent stalls */
                    if (decreaseVal < 60 * deltatime)
                    { /* this is roughly around 60kByte/s */
                        decreaseVal = 60 * deltatime;
                    }
                    if (decreaseVal > RateBucket)
                    {
                        decreaseVal = RateBucket;
                    }
                    RateBucket -= decreaseVal;
                    lastBucketTick = bucketCheckTime;
                }

                /* make high packets pass low priority packets */
                for(int queueidx = 0; queueidx < QueueList.Length; ++queueidx)
                {
                    Queue<Message> q = QueueList[queueidx];
                    if(q.Count == 0)
                    {
                        continue;
                    }
                    if(RateBucket >= ThrottleRate && m_UseThrottle)
                    {
                        continue;
                    }
                    try
                    {
                        m = q.Dequeue();
                    }
                    catch
                    {
                        continue;
                    }
                    if (m != null)
                    {
                        try
                        {
                            UDPPacket p = new UDPPacket();
                            p.IsZeroEncoded = m.ZeroFlag || m.ForceZeroFlag;
                            m.Serialize(p);
                            p.Flush();
                            p.IsReliable = m.IsReliable;
                            p.SequenceNumber = NextSequenceNumber;
                            int savedDataLength = p.DataLength;
                            if (!p.IsZeroEncoded)
                            {
                                uint appendableAcks = (MAX_DATA_MTU - 1 - (uint)savedDataLength) / 4;
                                uint curacks = (uint)m_AckList.Count;
                                if (appendableAcks != 0 && curacks != 0)
                                {
                                    p.HasAckFlag = true;
                                    uint cnt = 0;
                                    while (cnt < appendableAcks && cnt < curacks && cnt < 255)
                                    {
                                        p.WriteUInt32BE(m_AckList.Dequeue());
                                        ++cnt;
                                    }
                                    p.WriteUInt8((byte)cnt);
                                }
                            }

                            RateBucket += m_Server.SendPacketTo(p, RemoteEndPoint);

                            p.HasAckFlag = false;
                            p.DataLength = savedDataLength;

                            Interlocked.Increment(ref m_PacketsSent);
                            p.EnqueuedAtTime = Environment.TickCount;
                            p.TransferredAtTime = Environment.TickCount;
                            if (m.IsReliable)
                            {
                                p.IsResent = true;
                                m_UnackedPackets[p.SequenceNumber] = p;
                                lock (m_UnackedBytesLock)
                                {
                                    m_UnackedBytes += p.DataLength;
                                }
                            }
                            if (m.Number == MessageType.LogoutReply)
                            {
                                lock (m_LogoutReplyLock)
                                {
                                    m_LogoutReplySeqNo = p.SequenceNumber;
                                    m_LogoutReplySentAtTime = Environment.TickCount;
                                    m_LogoutReplySent = true;
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            m_Log.DebugFormat("Failed to serialize message of type {0}: {1}\n{2}", m.TypeDescription, e.Message, e.StackTrace.ToString());
                        }
                    }
                }

                if (Environment.TickCount - m_LastReceivedPacketAtTime >= 60000)
                {
                    m_Log.InfoFormat("Packet Timeout for agent {0} {1} ({2}) timed out", Agent.FirstName, Agent.LastName, Agent.ID);
                    TerminateCircuit();
                    return;
                }

                if (Environment.TickCount - m_LogoutReplySentAtTime >= 10000 && m_LogoutReplySent)
                {
                    m_Log.InfoFormat("LogoutReply for agent {0} {1} ({2}) timed out", Agent.FirstName, Agent.LastName, Agent.ID);
                    TerminateCircuit();
                    return;
                }

                if(Environment.TickCount - lastSimStatsTick >= 1000)
                {
                    int deltatime = Environment.TickCount - lastSimStatsTick;
                    lastSimStatsTick = Environment.TickCount;
                    SendSimStats(deltatime);
                }

                if (Environment.TickCount - lastPingTick >= 5000)
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
                        Interlocked.Increment(ref m_PacketsSent);
                    }
                }
                if (Environment.TickCount - lastAckTick >= 1000)
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
                        Interlocked.Increment(ref m_PacketsSent);
                    }
                }
            }
            m_TxRunning = false;
        }
        #endregion
    }
}
