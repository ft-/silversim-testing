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

using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Viewer.Messages.Transfer;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        UInt64 m_NextXferID;
        UInt64 NextXferID
        {
            get
            {
                lock(m_DataLock)
                {
                    if(++m_NextXferID == 0)
                    {
                        m_NextXferID = 1;
                    }
                    return m_NextXferID;
                }
            }
        }

        public class UploadTransaction
        {
            public List<byte[]> DataBlocks = new List<byte[]>();
            public UInt64 XferID;
            public string Filename;

            internal UploadTransaction()
            {

            }
        }

        public class TerrainUploadTransaction : UploadTransaction
        {
            readonly SceneInterface m_Scene;

            internal TerrainUploadTransaction(SceneInterface scene)
            {
                m_Scene = scene;
            }

            public virtual void OnCompletion(byte[] data)
            {
                using (var input = new MemoryStream(BuildUploadedData(this)))
                {
                    input.LoadLLRawStream(
                        (int)m_Scene.SizeX,
                        (int)m_Scene.SizeY,
                        m_Scene.Terrain.Patch.Update);
                }
            }

            public virtual void OnAbort()
            {

            }
        }

        public class AssetUploadTransaction : UploadTransaction
        {
            public UUID AssetID;
            public AssetType AssetType;
            public UUI Creator;
            public bool IsLocal;
            public bool IsTemporary;

            internal AssetUploadTransaction()
            {
            }

            public virtual void OnCompletion()
            {
            }

            public virtual void OnAbort()
            {

            }
        }

        internal RwLockedDoubleDictionary<UUID, UInt64, AssetUploadTransaction> m_AssetTransactions = new RwLockedDoubleDictionary<UUID, UInt64, AssetUploadTransaction>();
        internal RwLockedDoubleDictionary<UUID, UInt64, TerrainUploadTransaction> m_TerrainTransactions = new RwLockedDoubleDictionary<UUID, ulong, TerrainUploadTransaction>();

        #region Data Builder
        internal static byte[] BuildUploadedData(UploadTransaction t)
        {
            int dataLength = 0;
            byte[] data;

            foreach (var block in t.DataBlocks)
            {
                dataLength += block.Length;
            }

            data = (t.DataBlocks.Count > 1) ?
                /* multipart asset transfers have a CRC */
                new byte[dataLength - 4] :
                new byte[dataLength];

            int dataOffset = 0;
            foreach (var block in t.DataBlocks)
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
            var m = new RequestXfer()
            {
                ID = t.XferID,
                VFileType = (short)t.AssetType,
                VFileID = t.AssetID,
                FilePath = 0,
                Filename = t.Filename
            };
            SendMessageAlways(m, fromSceneID);

            return transactionID;
        }

        AssetData BuildUploadedAsset(AssetUploadTransaction t)
        {
            var asset = new AssetData()
            {
                Data = BuildUploadedData(t),
                ID = t.AssetID,
                Type = t.AssetType,
                Temporary = t.IsTemporary,
                Local = t.IsLocal
            };
            return asset;
        }

        [PacketHandler(MessageType.AssetUploadRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleAssetUploadRequest(Message m)
        {
            var req = (AssetUploadRequest)m;
            AssetUploadTransaction transaction;

            if (!m_AssetTransactions.TryGetValue(req.TransactionID, out transaction))
            {
                UInt64 XferID = NextXferID;
                transaction = new AssetUploadTransaction();
                m_AssetTransactions.Add(req.TransactionID, XferID, transaction);
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
                var reqxfer = new RequestXfer()
                {
                    ID = transaction.XferID,
                    VFileType = (short)transaction.AssetType,
                    VFileID = transaction.AssetID,
                    FilePath = 0,
                    Filename = string.Empty
                };
                SendMessageAlways(reqxfer, m.CircuitSceneID);
            }
        }

        [PacketHandler(MessageType.AbortXfer)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleAbortXfer(Message m)
        {
            var req = (AbortXfer)m;
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
            var data = BuildUploadedAsset(transaction);
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
            var req = new AssetUploadComplete()
            {
                AssetID = data.ID,
                AssetType = data.Type,
                Success = success
            };
            SendMessageAlways(req, fromSceneID);
        }
        #endregion

        #region Terrain Uploads
        void AddTerrainUploadTransaction(TerrainUploadTransaction t, UUID fromSceneID)
        {
            var id = UUID.Random;
            t.XferID = NextXferID;
            m_TerrainTransactions.Add(id, t.XferID, t);
            var m = new RequestXfer()
            {
                ID = t.XferID,
                VFileType = 0,
                VFileID = id,
                FilePath = 0,
                Filename = t.Filename
            };
            SendMessageAlways(m, fromSceneID);
        }
        #endregion

        [PacketHandler(MessageType.SendXferPacket)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleSendXferPacket(Message m)
        {
            var req = (SendXferPacket)m;
            var fromSceneID = m.CircuitSceneID;

            AssetUploadTransaction assettransaction;
            if (m_AssetTransactions.TryGetValue(req.ID, out assettransaction))
            {
                assettransaction.DataBlocks.Add(req.Data);

                var p = new ConfirmXferPacket()
                {
                    ID = assettransaction.XferID,
                    Packet = req.Packet
                };
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

                var p = new ConfirmXferPacket()
                {
                    ID = terraintransaction.XferID,
                    Packet = req.Packet
                };
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
