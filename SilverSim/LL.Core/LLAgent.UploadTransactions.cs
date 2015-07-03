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
using SilverSim.LL.Messages.Transfer;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        UInt64 m_NextXferID;
        UInt64 NextXferID
        {
            get
            {
                lock(this)
                {
                    if(++m_NextXferID == 0)
                    {
                        m_NextXferID = 1;
                    }
                    return m_NextXferID;
                }
            }
        }

        class UploadTransaction
        {
            public List<byte[]> DataBlocks = new List<byte[]>();
            public UInt64 XferID;
            public string Filename;

            public UploadTransaction()
            {

            }
        }

        class TerrainUploadTransaction : UploadTransaction
        {
            public TerrainUploadTransaction()
            {

            }

            public virtual void OnCompletion(byte[] data)
            {

            }

            public virtual void OnAbort()
            {

            }
        }

        class AssetUploadTransaction : UploadTransaction
        {
            public UUID AssetID;
            public AssetType AssetType;
            public UUI Creator;
            public bool IsLocal;
            public bool IsTemporary;

            public AssetUploadTransaction()
            {

            }

            public virtual void OnCompletion()
            {

            }

            public virtual void OnAbort()
            {

            }
        }

        readonly RwLockedDoubleDictionary<UUID, UInt64, AssetUploadTransaction> m_AssetTransactions = new RwLockedDoubleDictionary<UUID, UInt64, AssetUploadTransaction>();
        readonly RwLockedDoubleDictionary<UUID, UInt64, TerrainUploadTransaction> m_TerrainTransactions = new RwLockedDoubleDictionary<UUID, ulong, TerrainUploadTransaction>();

        #region Data Builder
        byte[] BuildUploadedData(UploadTransaction t)
        {
            int dataLength = 0;
            byte[] data;

            foreach (byte[] block in t.DataBlocks)
            {
                dataLength += block.Length;
            }

            if (t.DataBlocks.Count > 1)
            {
                /* multipart asset transfers have a CRC */
                data = new byte[dataLength - 4];
            }
            else
            {
                data = new byte[dataLength];
            }

            int dataOffset = 0;
            foreach (byte[] block in t.DataBlocks)
            {
                int remainingLength = data.Length - dataOffset;
                if (block.Length < remainingLength)
                {
                    remainingLength = block.Length;
                }
                if (remainingLength > 0)
                {
                    Buffer.BlockCopy(block, 0, data, dataOffset, remainingLength);
                }
                dataOffset += block.Length;
            }

            return data;
        }
        #endregion

        #region Asset Uploads
        UUID AddAssetUploadTransaction(UUID transactionID, AssetUploadTransaction t, UUID fromSceneID)
        {
            UInt64 XferID = NextXferID;
            m_AssetTransactions.Add(transactionID, XferID, t);
            t.XferID = XferID;
            RequestXfer m = new RequestXfer();
            m.ID = t.XferID;
            m.VFileType = (short)t.AssetType;
            m.VFileID = t.AssetID;
            m.FilePath = 0;
            m.Filename = t.Filename;
            SendMessageAlways(m, fromSceneID);

            return transactionID;
        }

        AssetData BuildUploadedAsset(AssetUploadTransaction t)
        {
            AssetData asset = new AssetData();
            asset.Data = BuildUploadedData(t);
            asset.ID = t.AssetID;
            asset.Type = t.AssetType;
            asset.Temporary = t.IsTemporary;
            asset.Local = t.IsLocal;
            return asset;
        }

        [PacketHandler(MessageType.AssetUploadRequest)]
        void HandleAssetUploadRequest(Message m)
        {
            AssetUploadRequest req = (AssetUploadRequest)m;
            AssetUploadTransaction transaction;

            if (!m_AssetTransactions.TryGetValue(req.TransactionID, out transaction))
            {
                UInt64 XferID = NextXferID;
                m_AssetTransactions.Add(req.TransactionID, XferID, transaction = new AssetUploadTransaction());
                transaction.XferID = XferID;
            }

            transaction.AssetID = UUID.Random;
            transaction.AssetType = req.AssetType;
            transaction.IsTemporary = req.IsTemporary;
            transaction.IsLocal = req.StoreLocal;
            transaction.Creator = Owner;
            if(req.AssetData.Length > 2)
            {
                transaction.DataBlocks.Add(req.AssetData);
                AssetUploadCompleted(transaction, m.CircuitSceneID);
            }
            else
            {
                RequestXfer reqxfer = new RequestXfer();
                reqxfer.ID = transaction.XferID;
                reqxfer.VFileType = (short)transaction.AssetType;
                reqxfer.VFileID = transaction.AssetID;
                reqxfer.FilePath = 0;
                reqxfer.Filename = "";
                SendMessageAlways(reqxfer, m.CircuitSceneID);
            }
        }

        [PacketHandler(MessageType.AbortXfer)]
        void HandleAbortXfer(Message m)
        {
            AbortXfer req = (AbortXfer)m;
            AssetUploadTransaction assettransaction;
            if (m_AssetTransactions.TryGetValue(req.ID, out assettransaction))
            {
                assettransaction.OnAbort();
                m_AssetTransactions.Remove(req.ID);
            }
            TerrainUploadTransaction terraintransaction;
            if(m_TerrainTransactions.TryGetValue(req.ID, out terraintransaction))
            {
                terraintransaction.OnAbort();
                m_TerrainTransactions.Remove(req.ID);
            }
        }

        void AssetUploadCompleted(AssetUploadTransaction transaction, UUID fromSceneID)
        {
            AssetData data = BuildUploadedAsset(transaction);
            bool success = true;
            try
            {
                AssetService.Store(data);
            }
            catch
            {
                SendAlertMessage("Could not upload asset", fromSceneID);
                success = false;
            }
            if (success)
            {
                try
                {
                    transaction.OnCompletion();
                }
                catch
                {
                    success = false;
                }
            }
            m_AssetTransactions.Remove(transaction.XferID);
            AssetUploadComplete req = new AssetUploadComplete();
            req.AssetID = data.ID;
            req.AssetType = data.Type;
            req.Success = success;
            SendMessageAlways(req, fromSceneID);
        }
        #endregion

        #region Terrain Uploads
        void AddTerrainUploadTransaction(TerrainUploadTransaction t, UUID fromSceneID)
        {
            UUID id = UUID.Random;
            t.XferID = NextXferID;
            m_TerrainTransactions.Add(id, t.XferID, t);
            RequestXfer m = new RequestXfer();
            m.ID = t.XferID;
            m.VFileType = 0;
            m.VFileID = id;
            m.FilePath = 0;
            m.Filename = t.Filename;
            SendMessageAlways(m, fromSceneID);
        }
        #endregion

        [PacketHandler(MessageType.SendXferPacket)]
        void HandleSendXferPacket(Message m)
        {
            SendXferPacket req = (SendXferPacket)m;
            UUID fromSceneID = m.CircuitSceneID;

            AssetUploadTransaction assettransaction;
            if (m_AssetTransactions.TryGetValue(req.ID, out assettransaction))
            {
                assettransaction.DataBlocks.Add(req.Data);

                ConfirmXferPacket p = new ConfirmXferPacket();
                p.ID = assettransaction.XferID;
                p.Packet = req.Packet;
                SendMessageAlways(p, fromSceneID);

                if((req.Packet & 0x80000000) != 0)
                {
                    AssetUploadCompleted(assettransaction, fromSceneID);
                }
            }

            TerrainUploadTransaction terraintransaction;
            if (m_TerrainTransactions.TryGetValue(req.ID, out terraintransaction))
            {
                terraintransaction.DataBlocks.Add(req.Data);

                ConfirmXferPacket p = new ConfirmXferPacket();
                p.ID = terraintransaction.XferID;
                p.Packet = req.Packet;
                SendMessageAlways(p, fromSceneID);

                if ((req.Packet & 0x80000000) != 0)
                {
                    terraintransaction.OnCompletion(BuildUploadedData(terraintransaction));
                    m_TerrainTransactions.Remove(req.ID);
                }
            }
        }

    }
}
