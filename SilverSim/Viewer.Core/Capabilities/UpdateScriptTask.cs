﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scripting.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using ThreadedClasses;
using System.IO;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core.Capabilities
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
    public class UpdateScriptTask : UploadAssetAbstractCapability
    {
        sealed class TransactionInfo
        {
            public UUID TaskID;
            public UUID ItemID;
            public bool IsScriptRunning;
            public UUID ExperienceID;

            public TransactionInfo(UUID taskID, UUID itemID, bool isScriptRunning, UUID experienceID)
            {
                TaskID = taskID;
                ItemID = itemID;
                IsScriptRunning = isScriptRunning;
                ExperienceID = experienceID;
            }
        }

        readonly ViewerAgent m_Agent;
        readonly SceneInterface m_Scene;
        readonly RwLockedDictionary<UUID, TransactionInfo> m_Transactions = new RwLockedDictionary<UUID, TransactionInfo>();

        public override string CapabilityName
        {
            get
            {
                return "UpdateScriptTask";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public UpdateScriptTask(ViewerAgent agent, SceneInterface scene, string serverURI)
            : base(agent.Owner, serverURI)
        {
            m_Agent = agent;
            m_Scene = scene;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            UUID transaction = UUID.Random;
            UUID experienceID = UUID.Zero;
            if(reqmap.ContainsKey("experience"))
            {
                experienceID = reqmap["experience"].AsUUID;
            }
            m_Transactions.Add(transaction, new TransactionInfo(reqmap["task_id"].AsUUID, reqmap["item_id"].AsUUID, reqmap["is_script_running"].AsBoolean, experienceID));
            return transaction;
        }

        public override Map UploadedData(UUID transactionID, AssetData data)
        {
            KeyValuePair<UUID, TransactionInfo> kvp;
            if (m_Transactions.RemoveIf(transactionID, delegate(TransactionInfo v) { return true; }, out kvp))
            {
                Map m = new Map();
                ObjectPartInventoryItem item;
                ObjectPart part = m_Scene.Primitives[kvp.Value.TaskID];
                try
                {
                    item = part.Inventory[kvp.Value.ItemID];
                }
                catch
                {
                    throw new UrlNotFoundException();
                }

                if (item.AssetType != data.Type)
                {
                    throw new UrlNotFoundException();
                }

                if(!part.CheckPermissions(m_Agent.Owner, m_Agent.Group, InventoryPermissionsMask.Modify))
                {
                    throw new UploadErrorException("Not allowed to modify script");
                }
                if(!item.CheckPermissions(m_Agent.Owner, m_Agent.Group, InventoryPermissionsMask.Modify))
                {
                    throw new UploadErrorException("Not allowed to modify script");
                }

                UUID oldAssetID = item.AssetID;
                item.AssetID = data.ID;
                data.Creator = item.Creator;
                data.Name = item.Name;
                item.ID = UUID.Random;

                try
                {
                    m_Scene.AssetService.Store(data);
                }
                catch
                {
                    throw new UploadErrorException("Failed to store asset");
                }

                ScriptInstance instance;
                try
                {
                    instance = item.RemoveScriptInstance;
                    if(instance != null)
                    {
                        instance.Abort();
                        instance.Remove();
                        ScriptLoader.Remove(oldAssetID, instance);
                    }
                    item.ScriptInstance.Remove();
                    part.Inventory.Remove(kvp.Value.ItemID);
                    part.Inventory.Add(item.ID, item.Name, item);
                }
                catch
                {
                    throw new UploadErrorException("Failed to store inventory item");
                }

                try
                {
                    instance = ScriptLoader.Load(part, item, item.Owner, data);
                    item.ScriptInstance = instance;
                    item.ScriptInstance.IsRunning = kvp.Value.IsScriptRunning;
                    m.Add("compiled", true);
                }
                catch (CompilerException e)
                {
                    AnArray errors = new AnArray();
                    foreach (KeyValuePair<int, string> line in e.Messages)
                    {
                        errors.Add(string.Format("{0}:{1}", kvp.Key, kvp.Value));
                    }
                    m.Add("errors", errors);
                    m.Add("compiled", false);
                }

                return m;
            }
            else
            {
                throw new UrlNotFoundException();
            }
        }

        protected override UUID NewAssetID
        {
            get
            {
                return UUID.Random;
            }
        }

        protected override bool AssetIsLocal
        {
            get
            {
                return false;
            }
        }

        protected override bool AssetIsTemporary
        {
            get
            {
                return false;
            }
        }

        protected override AssetType NewAssetType
        {
            get
            {
                return AssetType.Notecard;
            }
        }
    }
}
