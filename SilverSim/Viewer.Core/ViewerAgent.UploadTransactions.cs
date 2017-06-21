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

using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Inventory;
using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Viewer.Messages.Transfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        private UInt64 m_NextXferID;
        private UInt64 NextXferID
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
            private readonly SceneInterface m_Scene;

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
            public bool IsCompleted;
            public UUID SceneID;
            public enum TargetAsset
            {
                Scene,
                Agent
            }
            public TargetAsset Target;

            internal AssetUploadTransaction()
            {
            }

            public virtual void OnCompletion()
            {
                var e = OnCompletionEvent;
                if (e != null)
                {
                    foreach (Action<UUID> d in e.GetInvocationList())
                    {
                        d(AssetID);
                    }
                }
            }

            public virtual void OnAbort()
            {
            }

            public event Action<UUID> OnCompletionEvent;

            public bool HasActions
            {
                get
                {
                    var e = OnCompletionEvent;
                    return e != null && e.GetInvocationList().Length != 0;
                }
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

        #region Setup inventory uploads
        private sealed class CreateInventoryItemHandler
        {
            public UUID TransactionID;
            public InventoryItem Item;
            public ViewerAgent Agent;
            public UUID SceneID;
            public UInt32 CallbackID;

            public void OnCompletion(UUID assetid)
            {
                Item.AssetID = assetid;
                Agent.InventoryService.Item.Add(Item);
                Agent.SendMessageAlways(new UpdateCreateInventoryItem(Agent.ID, true, TransactionID, Item, CallbackID), SceneID);
            }
        }

        private readonly object m_AssetTransactionsAddLock = new object();

        internal void SetAssetUploadAsCreateInventoryItem(UUID transactionID, InventoryItem item, UUID sceneID, UInt32 callbackID)
        {
            AssetUploadTransaction transaction;
            lock (m_AssetTransactionsAddLock)
            {
                if (!m_AssetTransactions.TryGetValue(transactionID, out transaction))
                {
                    UInt64 XferID = NextXferID;
                    transaction = new AssetUploadTransaction { SceneID = sceneID };
                    transaction.Target = AssetUploadTransaction.TargetAsset.Agent;
                    transaction.OnCompletionEvent += new CreateInventoryItemHandler { SceneID = sceneID, Agent = this, CallbackID = callbackID, TransactionID = transactionID, Item = item }.OnCompletion;
                    m_AssetTransactions.Add(transactionID, XferID, transaction);
                    transaction.XferID = XferID;
                    return;
                }
            }

            lock (transaction)
            {
                transaction.Target = AssetUploadTransaction.TargetAsset.Agent;
                transaction.OnCompletionEvent += new CreateInventoryItemHandler { SceneID = sceneID, Agent = this, CallbackID = callbackID, TransactionID = transactionID, Item = item }.OnCompletion;
                if (transaction.IsCompleted)
                {
#if DEBUG
                    m_Log.DebugFormat("SetAssetUploadAsCreateInventoryItem(): Completing transaction {0} for {1}", transactionID, Owner.FullName);
#endif
                    AssetUploadCompleted(transaction, transaction.SceneID);
                }
                else
                {
#if DEBUG
                    m_Log.DebugFormat("SetAssetUploadAsCreateInventoryItem(): Appended action to transaction {0} for {1}", transactionID, Owner.FullName);
#endif
                }
            }
        }

        private sealed class UpdateInventoryItemHandler
        {
            public UUID TransactionID;
            public InventoryItem Item;
            public ViewerAgent Agent;
            public UUID SceneID;
            public UInt32 CallbackID;

            public void OnCompletion(UUID assetid)
            {
                Item.AssetID = assetid;
                Agent.InventoryService.Item.Update(Item);
                Agent.SendMessageAlways(new UpdateCreateInventoryItem(Agent.ID, true, TransactionID, Item, CallbackID), SceneID);
            }
        }

        public override void SetAssetUploadAsCompletionAction(UUID transactionID, UUID sceneID, Action<UUID> action)
        {
            AssetUploadTransaction transaction;
            lock (m_AssetTransactionsAddLock)
            {
                if (!m_AssetTransactions.TryGetValue(transactionID, out transaction))
                {
                    UInt64 XferID = NextXferID;
                    transaction = new AssetUploadTransaction { SceneID = sceneID };
                    transaction.Target = AssetUploadTransaction.TargetAsset.Scene;
                    transaction.OnCompletionEvent += action;
                    m_AssetTransactions.Add(transactionID, XferID, transaction);
                    transaction.XferID = XferID;
                    return;
                }
            }

            lock (transaction)
            {
                transaction.Target = AssetUploadTransaction.TargetAsset.Scene;
                transaction.OnCompletionEvent += action;
                if (transaction.IsCompleted)
                {
#if DEBUG
                    m_Log.DebugFormat("SetAssetUploadAsCompletionAction(): Completing transaction {0} for {1}", transactionID, Owner.FullName);
#endif
                    AssetUploadCompleted(transaction, transaction.SceneID);
                }
                else
                {
#if DEBUG
                    m_Log.DebugFormat("SetAssetUploadAsCompletionAction(): Appended action to transaction {0} for {1}", transactionID, Owner.FullName);
#endif
                }
            }
        }

        internal void SetAssetUploadAsUpdateInventoryItem(UUID transactionID, InventoryItem item, UUID sceneID, UInt32 callbackID)
        {
            AssetUploadTransaction transaction;
            lock (m_AssetTransactionsAddLock)
            {
                if (!m_AssetTransactions.TryGetValue(transactionID, out transaction))
                {
                    UInt64 XferID = NextXferID;
                    transaction = new AssetUploadTransaction { SceneID = sceneID };
                    transaction.Target = AssetUploadTransaction.TargetAsset.Agent;
                    transaction.OnCompletionEvent += new UpdateInventoryItemHandler { SceneID = sceneID, Agent = this, CallbackID = callbackID, TransactionID = transactionID, Item = item }.OnCompletion;
                    m_AssetTransactions.Add(transactionID, XferID, transaction);
                    transaction.XferID = XferID;
                    return;
                }
            }

            lock (transaction)
            {
                transaction.Target = AssetUploadTransaction.TargetAsset.Agent;
                transaction.OnCompletionEvent += new UpdateInventoryItemHandler { SceneID = sceneID, Agent = this, CallbackID = callbackID, TransactionID = transactionID, Item = item }.OnCompletion;
                if (transaction.IsCompleted)
                {
#if DEBUG
                    m_Log.DebugFormat("SetAssetUploadAsUpdateInventoryItem(): Completing transaction {0} for {1}", transactionID, Owner.FullName);
#endif
                    AssetUploadCompleted(transaction, transaction.SceneID);
                }
                else
                {
#if DEBUG
                    m_Log.DebugFormat("SetAssetUploadAsUpdateInventoryItem(): Appended action to transaction {0} for {1}", transactionID, Owner.FullName);
#endif
                }
            }
        }
        #endregion

        #region Asset Uploads
        private UUID AddAssetUploadTransaction(UUID transactionID, AssetUploadTransaction t, UUID fromSceneID)
        {
#if DEBUG
            m_Log.DebugFormat("AddAssetUploadTransaction(): Added asset upload transaction {0} for {1}", transactionID, Owner.FullName);
#endif
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

        private AssetData BuildUploadedAsset(AssetUploadTransaction t) => new AssetData()
        {
            Data = BuildUploadedData(t),
            ID = t.AssetID,
            Type = t.AssetType,
            Temporary = t.IsTemporary,
            Local = t.IsLocal
        };

        [PacketHandler(MessageType.AssetUploadRequest)]
        public void HandleAssetUploadRequest(Message m)
        {
            var req = (AssetUploadRequest)m;
            AssetUploadTransaction transaction;

            lock (m_AssetTransactionsAddLock)
            {
                if (!m_AssetTransactions.TryGetValue(req.TransactionID, out transaction))
                {
                    UInt64 XferID = NextXferID;
                    transaction = new AssetUploadTransaction { SceneID = req.CircuitSceneID };
                    m_AssetTransactions.Add(req.TransactionID, XferID, transaction);
                    transaction.XferID = XferID;
                }
            }

            UUID vfileID;
            /* what a crazy work-around in the protocol */
            byte[] md5input = new byte[32];
            req.TransactionID.ToBytes(md5input, 0);
            m_SecureSessionID.ToBytes(md5input, 16);
            using (MD5 md5 = MD5.Create())
            {
                vfileID = new UUID(md5.ComputeHash(md5input), 0);
            }

            transaction.AssetID = vfileID;
            transaction.AssetType = req.AssetType;
            transaction.IsTemporary = req.IsTemporary;
            transaction.IsLocal = req.StoreLocal;
            transaction.Creator = Owner;
            if(req.AssetData.Length > 2)
            {
#if DEBUG
                m_Log.DebugFormat("AssetUploadRequest(): Added asset upload transaction {0} for {1}: Single packet", req.TransactionID, Owner.FullName);
#endif
                transaction.DataBlocks.Add(req.AssetData);
                lock (transaction)
                {
                    if (transaction.HasActions)
                    {
                        AssetUploadCompleted(transaction, m.CircuitSceneID);
                    }
                    else
                    {
                        transaction.IsCompleted = true;
                    }
                }
            }
            else
            {
#if DEBUG
                m_Log.DebugFormat("AssetUploadRequest(): Added asset upload transaction {0} for {1}: Xfer packets", req.TransactionID, Owner.FullName);
#endif
                var reqxfer = new RequestXfer()
                {
                    ID = transaction.XferID,
                    VFileType = (short)transaction.AssetType,
                    VFileID = vfileID,
                    FilePath = 0,
                    Filename = string.Empty
                };
                SendMessageAlways(reqxfer, m.CircuitSceneID);
            }
        }

        [PacketHandler(MessageType.AbortXfer)]
        public void HandleAbortXfer(Message m)
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

        private void AssetUploadCompleted(AssetUploadTransaction transaction, UUID fromSceneID)
        {
#if DEBUG
            m_Log.DebugFormat("AssetUploadCompleted(): transaction {0}", transaction);
#endif
            var data = BuildUploadedAsset(transaction);
            bool success = true;
            try
            {
#if DEBUG
                m_Log.DebugFormat("Storing asset {0} for {1}", data.ID, Owner.FullName);
#endif
                if (transaction.Target == AssetUploadTransaction.TargetAsset.Agent)
                {
                    AssetService.Store(data);
                }
                else
                {
                    Circuits[fromSceneID].Scene.AssetService.Store(data);
                }
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
        private void AddTerrainUploadTransaction(TerrainUploadTransaction t, UUID fromSceneID)
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
        public void HandleSendXferPacket(Message m)
        {
            var req = (SendXferPacket)m;
            var fromSceneID = m.CircuitSceneID;

            AssetUploadTransaction assettransaction;
            if (m_AssetTransactions.TryGetValue(req.ID, out assettransaction) & !assettransaction.IsCompleted)
            {
                if (assettransaction.DataBlocks.Count == 0)
                {
                    /* first segment contains byte count */
                    byte[] data = new byte[req.Data.Length - 4];
                    Buffer.BlockCopy(req.Data, 4, data, 0, data.Length);
                    assettransaction.DataBlocks.Add(data);
                }
                else
                {
                    assettransaction.DataBlocks.Add(req.Data);
                }

                var p = new ConfirmXferPacket()
                {
                    ID = assettransaction.XferID,
                    Packet = req.Packet
                };
                SendMessageAlways(p, fromSceneID);

                if((req.Packet & 0x80000000) != 0)
                {
                    lock (assettransaction)
                    {
                        if (assettransaction.HasActions)
                        {
                            AssetUploadCompleted(assettransaction, fromSceneID);
                        }
                        else
                        {
                            assettransaction.IsCompleted = true;
                        }
                    }
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
