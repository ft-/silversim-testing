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
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Viewer.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public abstract partial class Circuit : ICircuit
    {
        public bool ForceUseCircuitCode;
        private static readonly ILog m_Log = LogManager.GetLogger("LL CIRCUIT");
        public UInt32 CircuitCode { get; }
        protected BlockingQueue<Message> m_TxQueue = new BlockingQueue<Message>();
        private bool m_TxRunning;
        private Thread m_TxThread;
        private int __SequenceNumber;
        private readonly NonblockingQueue<UInt32> m_AckList = new NonblockingQueue<UInt32>();
        public EndPoint RemoteEndPoint;
        private readonly RwLockedDictionary<byte, int> m_PingSendTicks = new RwLockedDictionary<byte, int>();
        public int LastMeasuredLatencyTickCount { get; private set; }
        private uint m_LogoutReplySeqNo;
        private bool m_LogoutReplySent;
        private readonly object m_LogoutReplyLock = new object(); /* this is only for guarding access sequence to m_LogoutReply* variables */
        private int m_LogoutReplySentAtTime;
        private int m_LastReceivedPacketAtTime;

        private int m_KickUserSentAtTime;
        private bool m_KickUserSent;

        protected readonly BlockingQueue<UDPPacket> m_TxObjectPool = new BlockingQueue<UDPPacket>();
        protected int m_PacketsReceived;
        protected int m_PacketsSent;
        protected int m_UnackedBytes;
        protected readonly object m_UnackedBytesLock = new object();

        public string RemoteIP
        {
            get
            {
                IPAddress ipAddr = ((IPEndPoint)RemoteEndPoint).Address;
                if (ipAddr.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    byte[] b = ipAddr.GetAddressBytes();
                    if (b[0] == 0 &&
                        b[1] == 0 &&
                        b[2] == 0 &&
                        b[3] == 0 &&
                        b[4] == 0 &&
                        b[5] == 0 &&
                        b[6] == 0 &&
                        b[7] == 0 &&
                        b[8] == 0 &&
                        b[9] == 0 &&
                        b[10] == 0xFF &&
                        b[11] == 0xFF)
                    {
                        return string.Format("{0}.{1}.{2}.{3}", b[12], b[13], b[14], b[15]);
                    }
                }
                return ipAddr.ToString();
            }
        }

        internal UDPCircuitsManager Server { get; }

        protected uint NextSequenceNumber => (uint)Interlocked.Increment(ref __SequenceNumber);

        [AttributeUsage(AttributeTargets.Method, Inherited = false)]
        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public sealed class IgnoreMethodAttribute : Attribute
        {
        }

        public C5.TreeDictionary<uint, UDPPacket> m_UnackedPacketsHash = new C5.TreeDictionary<uint, UDPPacket>();

        protected Circuit(
            UDPCircuitsManager server,
            UInt32 circuitcode)
        {
            Server = server;
            CircuitCode = circuitcode;

            m_LastReceivedPacketAtTime = Environment.TickCount;
            uint pcks;
            for (pcks = 0; pcks < 200; ++pcks)
            {
                m_TxObjectPool.Enqueue(new UDPPacket());
            }
            InitializeTransmitQueueing();
        }

        static Circuit()
        {
            InitializeTransmitQueueRouting();
        }

        #region Receive logic
        protected abstract void CheckForNewDataToSend();
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
            if (acknumbers != null)
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
                            p_acked = m_UnackedPacketsHash[ackno];
                            m_UnackedPacketsHash.Remove(ackno);
                        }
                    }

                    if (p_acked != null)
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
                        if (p_acked.AckMessage != null)
                        {
                            try
                            {
                                p_acked.AckMessage.OnSendComplete(true);
                            }
                            catch (Exception e)
                            {
                                m_Log.WarnFormat("OnSendCompletion: Exception {0} at {1}", e.ToString(), e.StackTrace);
                            }
                        }
                    }

                    if (ackedSomethingElse)
                    {
                        m_TxQueue.Enqueue(new AcksReceived());
                    }
                    if (ackedObjects)
                    {
                        CheckForNewDataToSend();
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
                        Server.SendPacketTo(UDPPacket.PacketAckImmediate(pck.SequenceNumber), RemoteEndPoint);
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
                                p_acked = m_UnackedPacketsHash[ackno];
                                m_UnackedPacketsHash.Remove(ackno);
                            }
                        }
                        if (p_acked != null)
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

                            if (p_acked.AckMessage != null)
                            {
                                try
                                {
                                    p_acked.AckMessage.OnSendComplete(true);
                                }
                                catch (Exception e)
                                {
                                    m_Log.WarnFormat("OnSendCompletion: Exception {0} at {1}", e.ToString(), e.StackTrace);
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
                        CheckForNewDataToSend();
                    }

                    lock (m_UnackedBytesLock)
                    {
                        m_UnackedBytes -= unackedReleasedCount;
                    }
                    break;

                case MessageType.StartPingCheck:
                    byte pingID = pck.ReadUInt8();
                    pck.ReadUInt32();

                    var newpck = new UDPPacket();
                    newpck.WriteMessageNumber(MessageType.CompletePingCheck);
                    newpck.WriteUInt8(pingID);
                    newpck.SequenceNumber = NextSequenceNumber;
                    Server.SendPacketTo(newpck, ep);
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
                                Value = m_UnackedPacketsHash[keyval];
                            }
                            if (Environment.TickCount - Value.TransferredAtTime > 1000)
                            {
                                if (Value.ResentCount < 5)
                                {
                                    Value.TransferredAtTime = Environment.TickCount;
                                    Server.SendPacketTo(Value, RemoteEndPoint);
                                }
                                ++Value.ResentCount;
                            }
                        }
                    }
                    catch
                    {
                        /* no action required */
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
        private readonly object m_ThreadControlLock = new object();

        protected abstract void StartSpecificThreads();

        public void Start()
        {
            lock (m_ThreadControlLock)
            {
                if (!m_TxRunning)
                {
                    m_TxThread = ThreadManager.CreateThread(TransmitThread);
                    m_TxThread.Start(this);
                    m_TxRunning = true;
                }
                StartSpecificThreads();
            }
        }

        protected abstract void StopSpecificThreads();

        public void Stop()
        {
            lock (m_ThreadControlLock)
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
