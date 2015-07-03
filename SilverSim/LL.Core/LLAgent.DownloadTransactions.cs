﻿/*

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
using SilverSim.LL.Messages.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        public class DownloadTransferData
        {
            public int Position = 0;
            public uint Packet = 0;
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
                int remainingdatalen = tdata.Data.Length - tdata.Position;
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

        public ulong AddNewFile(string filename, byte[] data)
        {
            ulong xferid = NextXferID;
            DownloadTransferData tdata = new DownloadTransferData(data, xferid);
            m_DownloadTransfers.Add(filename, xferid, tdata);
            return xferid;
        }
    }
}
