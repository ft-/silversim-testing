using SilverSim.Viewer.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public abstract partial class Circuit
    {
        protected abstract void SendViaEventQueueGet(Message m);

        [IgnoreMethod]
        public void SendMessage(Message m)
        {
            if (m.IsReliable && m_CircuitIsClosing)
            {
                m.OnSendComplete(false);
                return;
            }

            try
            {
                switch (m.Number)
                {
#if HOWTODEAL
                    case MessageType.DisableSimulator:
                        break;
#endif
                    case 0: /* only Event Queue support */
                        if (Attribute.GetCustomAttribute(m.GetType(), typeof(EventQueueGetAttribute)) != null)
                        {
                            SendViaEventQueueGet(m);
                        }
                        else
                        {
                            m_Log.ErrorFormat("Type {0} misses EventQueueGet attribute", m.GetType().FullName);
                        }
                        break;

                    default:
                        if (Attribute.GetCustomAttribute(m.GetType(), typeof(EventQueueGetAttribute)) != null)
                        {
                            SendViaEventQueueGet(m);
                        }
                        else
                        {
                            m_TxQueue.Enqueue(m);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                m_Log.ErrorFormat("{0} at {1}", e.ToString(), e.StackTrace);
            }

            /* Unreliable message direct acknowledge */
            if (!m.IsReliable)
            {
                try
                {
                    m.OnSendComplete(true);
                }
                catch (Exception e)
                {
                    m_Log.ErrorFormat("OnSendCompletion: {0} at {1}", e.ToString(), e.StackTrace);
                }
            }
        }


        #region LLUDP Packet transmitter

        /* we limit by amount of acks here (concept from TCP, more or less as window approach) */
        protected int[] m_AckThrottlingCount = new int[(int)Message.QueueOutType.NumQueues];

        private static readonly Dictionary<MessageType, Message.QueueOutType> m_QueueOutTable = new Dictionary<MessageType, Message.QueueOutType>();
        static void InitializeTransmitQueueRouting()
        {
            /* viewers do not wait long for this, so we give them higher priority */
            m_QueueOutTable.Add(MessageType.ImageData, Message.QueueOutType.TextureStart);
            m_QueueOutTable.Add(MessageType.ImageNotInDatabase, Message.QueueOutType.TextureStart);

            m_QueueOutTable.Add(MessageType.ImagePacket, Message.QueueOutType.Texture);
            m_QueueOutTable.Add(MessageType.TransferPacket, Message.QueueOutType.Asset);
            m_QueueOutTable.Add(MessageType.TransferInfo, Message.QueueOutType.Asset);
        }

        void InitializeTransmitQueueing()
        {
            int i;
            for (i = 0; i < m_AckThrottlingCount.Length; ++i)
            {
                m_AckThrottlingCount[i] = 0;
            }
        }

        protected const int TRANSMIT_THROTTLE_MTU = 1500;
        protected const int MAX_DATA_MTU = 1400;

        protected bool m_CircuitIsClosing;

        protected abstract void SendSimStats(int dt);

        public event Action OnTerminateCircuit;

        private void TerminateCircuit()
        {
            m_CircuitIsClosing = true;

            /* events are not exactly thread-safe, so we have to take the value first */
            var ev = OnTerminateCircuit;
            if (null != ev)
            {
                foreach (Action d in ev.GetInvocationList())
                {
                    d.Invoke();
                }
            }

            lock (m_UnackedPacketsHash)
            {
                foreach (UDPPacket unacked in m_UnackedPacketsHash.Values)
                {
                    if (unacked.AckMessage != null)
                    {
                        try
                        {
                            unacked.AckMessage.OnSendComplete(false);
                        }
                        catch
                        {

                        }
                    }
                }
            }

            UDPCircuitsManager server = m_Server;
            if (null != server)
            {
                server.RemoveCircuit(this);
            }

            Stop();
            return;
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        private void TransmitThread(object param)
        {
            int lastAckTick = Environment.TickCount;
            int lastPingTick = Environment.TickCount;
            int lastSimStatsTick = Environment.TickCount;
            byte pingID = 0;
            Thread.CurrentThread.Name = string.Format("LLUDP:Transmitter for CircuitCode {0} / IP {1}", CircuitCode, RemoteEndPoint.ToString());
            Queue<Message> LowPriorityQueue;
            Queue<Message> HighPriorityQueue;
            Queue<Message> MediumPriorityQueue;
            Queue<Message>[] QueueList = new Queue<Message>[(int)Message.QueueOutType.NumQueues];
            Message.QueueOutType qroutidx;
            int[] QueueCount = new int[(int)Message.QueueOutType.NumQueues];

            for (uint qidx = 0; qidx < (uint)Message.QueueOutType.NumQueues; ++qidx)
            {
                QueueList[qidx] = new Queue<Message>();
            }

            HighPriorityQueue = QueueList[(uint)Message.QueueOutType.High];
            MediumPriorityQueue = QueueList[(uint)Message.QueueOutType.Medium];
            LowPriorityQueue = QueueList[(uint)Message.QueueOutType.Low];

            int qcount;
            int timeout = 10;
            Message m;
            CancelTxThread cancelmsg;

            while (true)
            {
                foreach (Queue<Message> q in QueueList)
                {
                    if (q.Count > 0)
                    {
                        timeout = 0;
                    }
                }

                qcount = m_TxQueue.Count + 1;
                m = null;
                cancelmsg = null;

                while (qcount > 0)
                {
                    try
                    {
                        m = m_TxQueue.Dequeue(timeout);
                    }
                    catch
                    {
                        break;
                    }
                    timeout = 0;
                    --qcount;
                    cancelmsg = m as CancelTxThread;
                    if (null != cancelmsg)
                    {
                        break;
                    }

                    if (m is AcksReceived)
                    {

                    }
                    else if (m_QueueOutTable.TryGetValue(m.Number, out qroutidx))
                    {
                        m.OutQueue = qroutidx;
                        QueueList[(uint)m.OutQueue].Enqueue(m);
                    }
                    else if (m.Number == MessageType.LayerData)
                    {
                        Messages.LayerData.LayerData ld = (Messages.LayerData.LayerData)m;
                        switch (ld.LayerType)
                        {
                            case Messages.LayerData.LayerData.LayerDataType.Land:
                            case Messages.LayerData.LayerData.LayerDataType.LandExtended:
                                m.OutQueue = Message.QueueOutType.LandLayerData;
                                QueueList[(uint)m.OutQueue].Enqueue(m);
                                break;

                            case Messages.LayerData.LayerData.LayerDataType.Wind:
                            case Messages.LayerData.LayerData.LayerDataType.WindExtended:
                                m.OutQueue = Message.QueueOutType.WindLayerData;
                                QueueList[(uint)m.OutQueue].Enqueue(m);
                                break;

                            default:
                                m.OutQueue = Message.QueueOutType.GenericLayerData;
                                QueueList[(uint)m.OutQueue].Enqueue(m);
                                break;
                        }
                    }
                    else if (m.Number < MessageType.Medium)
                    {
                        m.OutQueue = Message.QueueOutType.High;
                        HighPriorityQueue.Enqueue(m);
                    }
                    else if (m.Number < MessageType.Low)
                    {
                        m.OutQueue = Message.QueueOutType.Medium;
                        MediumPriorityQueue.Enqueue(m);
                    }
                    else
                    {
                        m.OutQueue = Message.QueueOutType.Low;
                        LowPriorityQueue.Enqueue(m);
                    }
                }
                if (null != cancelmsg)
                {
                    break;
                }


                for (uint qidx = 0; qidx < (uint)Message.QueueOutType.NumQueues; ++qidx)
                {
                    QueueCount[qidx] = QueueList[qidx].Count;
                }

                do
                {
                    /* make high packets pass low priority packets */
                    for (int queueidx = 0; queueidx < QueueList.Length; ++queueidx)
                    {
                        Queue<Message> q = QueueList[queueidx];
                        if (q.Count == 0)
                        {
                            continue;
                        }
                        if (m_AckThrottlingCount[queueidx] > 100)
                        {
                            QueueCount[queueidx] = 0;
                            continue;
                        }
                        try
                        {
                            m = q.Dequeue();
                            if (QueueCount[queueidx] > 0)
                            {
                                --QueueCount[queueidx];
                            }
                        }
                        catch
                        {
                            QueueCount[queueidx] = 0;
                            continue;
                        }
                        if (m != null)
                        {
                            try
                            {
                                UDPPacket p = new UDPPacket();
                                p.OutQueue = m.OutQueue;
                                p.IsZeroEncoded = m.ZeroFlag || m.ForceZeroFlag;
                                m.Serialize(p);
                                p.Flush();
                                p.IsReliable = m.IsReliable;
                                p.AckMessage = p.IsReliable ?
                                    m : 
                                    null;
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

                                if (p.IsReliable)
                                {
                                    Interlocked.Increment(ref m_AckThrottlingCount[queueidx]);
                                }
                                m_Server.SendPacketTo(p, RemoteEndPoint);

                                p.HasAckFlag = false;
                                p.DataLength = savedDataLength;

                                Interlocked.Increment(ref m_PacketsSent);
                                p.EnqueuedAtTime = Environment.TickCount;
                                p.TransferredAtTime = Environment.TickCount;
                                if (m.IsReliable)
                                {
                                    p.IsResent = true;
                                    lock (m_UnackedPacketsHash)
                                    {
                                        m_UnackedPacketsHash.Add(p.SequenceNumber, p);
                                    }
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
                            catch (Exception e)
                            {
                                m_Log.DebugFormat("Failed to serialize message of type {0}: {1}\n{2}", m.TypeDescription, e.Message, e.StackTrace);
                            }
                        }
                    }

                    qcount = 0;
                    for (int queueidx = 0; queueidx < QueueCount.Length; ++queueidx)
                    {
                        qcount += QueueCount[queueidx];
                    }
                } while (qcount != 0);

                if (Environment.TickCount - m_LastReceivedPacketAtTime >= 60000)
                {
                    LogMsgOnTimeout();
                    TerminateCircuit();
                    return;
                }

                if (Environment.TickCount - m_LogoutReplySentAtTime >= 10000 && m_LogoutReplySent)
                {
                    LogMsgLogoutReply();
                    TerminateCircuit();
                    return;
                }

                if (Environment.TickCount - lastSimStatsTick >= 1000)
                {
                    int deltatime = Environment.TickCount - lastSimStatsTick;
                    lastSimStatsTick = Environment.TickCount;
                    SendSimStats(deltatime);
                }

                if (Environment.TickCount - lastPingTick >= 5000 &&
                    !m_PingSendTicks.ContainsKey(pingID))
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

        protected abstract void LogMsgOnTimeout();
        protected abstract void LogMsgLogoutReply();

        #endregion
    }
}
