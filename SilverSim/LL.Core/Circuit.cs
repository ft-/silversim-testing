﻿using log4net;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.IM;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    public abstract partial class Circuit : IDisposable
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL CIRCUIT");
        public UInt32 CircuitCode { get; private set; }
        protected BlockingQueue<Message> m_TxQueue = new BlockingQueue<Message>();
        private bool m_TxRunning = false;
        private Thread m_TxThread = null;
        private int __SequenceNumber = 0;
        private NonblockingQueue<UInt32> m_AckList = new NonblockingQueue<UInt32>();
        private LLUDPServer m_Server;
        public EndPoint RemoteEndPoint;
        private RwLockedDictionary<byte, int> m_PingSendTicks = new RwLockedDictionary<byte, int>();
        public int LastMeasuredLatencyTickCount { get; private set; }
        private uint m_LogoutReplySeqNo = 0;
        private bool m_LogoutReplySent = false;
        private object m_LogoutReplyLock = new object(); /* this is only for guarding access sequence to m_LogoutReply* variables */
        private int m_LogoutReplySentAtTime;
        private int m_LastReceivedPacketAtTime;

        protected Dictionary<MessageType, Action<Message>> m_MessageRouting = new Dictionary<MessageType, Action<Message>>();
        protected Dictionary<string, Action<Message>> m_GenericMessageRouting = new Dictionary<string, Action<Message>>();
        protected Dictionary<GridInstantMessageDialog, Action<Message>> m_IMMessageRouting = new Dictionary<GridInstantMessageDialog, Action<Message>>();

        protected ThreadedClasses.BlockingQueue<UDPPacket> m_TxObjectPool = new BlockingQueue<UDPPacket>();
        protected int m_PacketsReceived = 0;
        protected int m_PacketsSent = 0;
        protected int m_UnackedBytes = 0;
        protected object m_UnackedBytesLock = new object();

        internal LLUDPServer Server
        {
            get
            {
                return m_Server;
            }
        }

        protected uint NextSequenceNumber
        {
            get
            {
                return (uint)Interlocked.Increment(ref __SequenceNumber);
            }
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false)]
        public class IgnoreMethod : Attribute
        {
            public IgnoreMethod()
            {
            }
        }

        public C5.TreeDictionary<uint, UDPPacket> m_UnackedPacketsHash = new C5.TreeDictionary<uint, UDPPacket>();

        public Circuit(            
            LLUDPServer server,
            UInt32 circuitcode)
        {
            m_Server = server;
            CircuitCode = circuitcode;

            m_LastReceivedPacketAtTime = Environment.TickCount;
            uint pcks;
            for (pcks = 0; pcks < 200; ++pcks)
            {
                m_TxObjectPool.Enqueue(new UDPPacket());
            }
        }

        ~Circuit()
        {
            Dispose();
        }

        public abstract void Dispose();

        static Circuit()
        {
            InitializeTransmitQueueRouting();
        }

        #region Receive logic
        protected abstract void CheckForeNewDataToSend();
        protected abstract void OnCircuitSpecificPacketReceived(MessageType mType, UDPPacket p);
        protected abstract void LogMsgOnLogoutCompletion();

        public void PacketReceived(EndPoint ep, UDPPacket pck, List<UInt32> acknumbers)
        {
            /* no need for returning packets here since we are obliqued never to pass them around.
             * We just decode them here to actual messages
             */
            MessageType mType = pck.ReadMessageType();

            m_LastReceivedPacketAtTime = Environment.TickCount;

            Interlocked.Increment(ref m_PacketsReceived);

            /* do we have some acks from the packet's end? */
            if (null != acknumbers)
            {
                int unackedReleasedCount = 0;
                bool ackedObjects = false;
                bool ackedSomethingElse = false;
                foreach (UInt32 ackno in acknumbers)
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
                        if (p_acked.OutQueue == Message.QueueOutType.Object)
                        {
                            m_TxObjectPool.Enqueue(p_acked);
                            ackedObjects = true;
                        }
                        else
                        {
                            ackedSomethingElse = true;
                        }
                        if (null != p_acked.AckMessage)
                        {
                            try
                            {
                                p_acked.AckMessage.OnSendComplete(true);
                            }
                            catch (Exception e)
                            {
                                m_Log.WarnFormat("OnSendCompletion: Exception {0} at {1}", e.ToString(), e.StackTrace.ToString());
                            }
                        }
                    }

                    if (ackedSomethingElse)
                    {
                        m_TxQueue.Enqueue(new AcksReceived());
                    }
                    if (ackedObjects)
                    {
                        CheckForeNewDataToSend();
                    }

                    lock (m_LogoutReplyLock)
                    {
                        if (ackno == m_LogoutReplySeqNo && m_LogoutReplySent)
                        {
                            LogMsgOnLogoutCompletion();
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

            if (pck.IsReliable)
            {
                /* we have to ack */
                switch (mType)
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
            switch (mType)
            {
                case MessageType.PacketAck:
                    /* we decode it here, no need to pass it anywhere else */
                    int unackedReleasedCount = 0;
                    bool ackedObjects = false;
                    bool ackedSomethingElse = false;
                    uint cnt = pck.ReadUInt8();
                    for (uint i = 0; i < cnt; ++i)
                    {
                        uint ackno = pck.ReadUInt32();
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
                            if (p_acked.OutQueue == Message.QueueOutType.Object)
                            {
                                m_TxObjectPool.Enqueue(p_acked);
                                ackedObjects = true;
                            }
                            else
                            {
                                ackedSomethingElse = true;
                            }

                            if (null != p_acked.AckMessage)
                            {
                                try
                                {
                                    p_acked.AckMessage.OnSendComplete(true);
                                }
                                catch (Exception e)
                                {
                                    m_Log.WarnFormat("OnSendCompletion: Exception {0} at {1}", e.ToString(), e.StackTrace.ToString());
                                }
                            }
                        }

                        lock (m_LogoutReplyLock)
                        {
                            if (ackno == m_LogoutReplySeqNo && m_LogoutReplySent)
                            {
                                LogMsgOnLogoutCompletion();
                                TerminateCircuit();
                                return;
                            }
                        }
                    }

                    if (ackedSomethingElse)
                    {
                        m_TxQueue.Enqueue(new AcksReceived());
                    }
                    if (ackedObjects)
                    {
                        CheckForeNewDataToSend();
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
                    if (m_PingSendTicks.Remove(ackPingID, out timesent))
                    {
                        LastMeasuredLatencyTickCount = (timesent - Environment.TickCount) / 2;
                    }
                    break;

                default:
                    OnCircuitSpecificPacketReceived(mType, pck);
                    break;
            }
        }
        #endregion

        #region Thread control logic
        protected abstract void StartSpecificThreads();
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
                StartSpecificThreads();
            }
        }

        protected abstract void StopSpecificThreads();
        public void Stop()
        {
            lock (this)
            {
                if (m_TxRunning)
                {
                    m_TxQueue.Enqueue(new CancelTxThread());
                }
                StopSpecificThreads();
            }
        }
        #endregion

    }
}
