// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Transfer;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        public class DownloadTransferData
        {
            public int Position;
            public uint Packet;
            public byte[] Data;
            public ulong XferID;

            public DownloadTransferData(byte[] data, ulong xferid)
            {
                Data = data;
                XferID = xferid;
            }
        }

        internal RwLockedDoubleDictionary<string, ulong, DownloadTransferData> m_DownloadTransfers = new RwLockedDoubleDictionary<string, ulong, DownloadTransferData>();

        [PacketHandler(MessageType.RequestXfer)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleRequestXfer(Message m)
        {
            RequestXfer req = (RequestXfer)m;
            DownloadTransferData tdata;
            if(m_DownloadTransfers.TryGetValue(req.Filename, out tdata))
            {
                if(tdata.Position != 0)
                {
                    return;
                }

                SendXferPacket res = new SendXferPacket();
                if(tdata.Data.Length > 1400)
                {
                    res.Data = new byte[1400 + 4];
                    Buffer.BlockCopy(tdata.Data, 0, res.Data, 4, 1400);
                    tdata.Position += 1400;
                }
                else
                {
                    res.Data = new byte[tdata.Data.Length + 4];
                    Buffer.BlockCopy(tdata.Data, 0, res.Data, 4, tdata.Data.Length);
                    m_DownloadTransfers.Remove(req.Filename);
                    tdata.Position += res.Data.Length;
                }
                tdata.Data[0] = (byte)(tdata.Data.Length & 0xFF);
                tdata.Data[1] = (byte)((tdata.Data.Length >> 8) & 0xFF);
                tdata.Data[2] = (byte)((tdata.Data.Length >> 16) & 0xFF);
                tdata.Data[3] = (byte)((tdata.Data.Length >> 24) & 0xFF);
                res.Packet = 0;
                res.ID = tdata.XferID;
                SendMessageAlways(res, req.CircuitSceneID);
            }
        }

        [PacketHandler(MessageType.ConfirmXferPacket)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleConfirmXferPacket(Message m)
        {
            ConfirmXferPacket req = (ConfirmXferPacket)m;
            DownloadTransferData tdata;
            if (m_DownloadTransfers.TryGetValue(req.ID, out tdata))
            {
                if (tdata.Packet != req.Packet || tdata.Position == 0)
                {
                    return;
                }

                SendXferPacket res = new SendXferPacket();
                res.Packet = ++tdata.Packet;
                if (tdata.Data.Length > 1400)
                {
                    res.Data = new byte[1400];
                    Buffer.BlockCopy(tdata.Data, 0, res.Data, 0, 1400);
                    tdata.Position += 1400;
                }
                else
                {
                    res.Data = new byte[tdata.Data.Length];
                    Buffer.BlockCopy(tdata.Data, 0, res.Data, 0, tdata.Data.Length);
                    m_DownloadTransfers.Remove(req.ID);
                    tdata.Position += res.Data.Length;
                }
                res.ID = tdata.XferID;
                SendMessageAlways(res, req.CircuitSceneID);
            }
        }

        public override ulong AddNewFile(string filename, byte[] data)
        {
            ulong xferid = NextXferID;
            DownloadTransferData tdata = new DownloadTransferData(data, xferid);
            m_DownloadTransfers.Add(filename, xferid, tdata);
            return xferid;
        }
    }
}
