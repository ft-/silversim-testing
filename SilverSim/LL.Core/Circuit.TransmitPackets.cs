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

        /* we limit by amount of acks here (concept from TCP, more or less as window approach) */
        int[] m_AckThrottlingCount = new int[(int)Message.QueueOutType.NumQueues];

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
            for(i = 0; i < m_AckThrottlingCount.Length; ++i)
            {
                m_AckThrottlingCount[i] = 0;
            }
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
            Agent.Circuits.Remove(CircuitCode);
            Agent.CheckCircuits();

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
            int lastSimStatsTick = Environment.TickCount;
            byte pingID = 0;
            Thread.CurrentThread.Name = string.Format("LLUDP:Transmitter for CircuitCode {0} / IP {1}", CircuitCode, RemoteEndPoint.ToString());
            Queue<Message> LowPriorityQueue;
            Queue<Message> HighPriorityQueue;
            Queue<Message> MediumPriorityQueue;
            Queue<Message>[] QueueList = new Queue<Message>[(int)Message.QueueOutType.NumQueues];
            Message.QueueOutType qroutidx;

            for (uint qidx = 0; qidx < (uint)Message.QueueOutType.NumQueues; ++qidx)
            {
                QueueList[qidx] = new Queue<Message>();
            }

            HighPriorityQueue = QueueList[(uint)Message.QueueOutType.High];
            MediumPriorityQueue = QueueList[(uint)Message.QueueOutType.Medium];
            LowPriorityQueue = QueueList[(uint)Message.QueueOutType.Low];

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
                        m.OutQueue = qroutidx;
                        QueueList[(uint)m.OutQueue].Enqueue(m);
                    }
                    else if (m.Number == MessageType.LayerData)
                    {
                        Messages.LayerData.LayerData ld =(Messages.LayerData.LayerData)m;
                        switch(ld.LayerType)
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
                    else if(m.Number < MessageType.Low)
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
                catch
                {

                }

                /* make high packets pass low priority packets */
                for(int queueidx = 0; queueidx < QueueList.Length; ++queueidx)
                {
                    Queue<Message> q = QueueList[queueidx];
                    if(q.Count == 0)
                    {
                        continue;
                    }
                    if (m_AckThrottlingCount[queueidx] > 100)
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
                            p.OutQueue = m.OutQueue;
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

                            m_Server.SendPacketTo(p, RemoteEndPoint);
                            Interlocked.Increment(ref m_AckThrottlingCount[queueidx]);

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
