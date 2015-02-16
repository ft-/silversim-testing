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

namespace SilverSim.LL.Core.Capabilities
{
    public class UpdateScriptTask : UploadAssetAbstractCapability
    {
        class TransactionInfo
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

        private LLAgent m_Agent;
        private SceneInterface m_Scene;
        private readonly RwLockedDictionary<UUID, TransactionInfo> m_Transactions = new RwLockedDictionary<UUID, TransactionInfo>();

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

        public UpdateScriptTask(LLAgent agent, SceneInterface scene, string serverURI)
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
                    using (TextReader reader = new StreamReader(data.InputStream))
                    {
                        instance = ScriptLoader.Load(part, item, item.Owner, data);
                    }
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
