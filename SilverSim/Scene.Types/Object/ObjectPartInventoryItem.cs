// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Script;
using System;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectPartInventoryItem : InventoryItem
    {
        #region Constructors
        public ObjectPartInventoryItem()
        {

        }

        public ObjectPartInventoryItem(AssetData asset)
        {
            AssetID = asset.ID;
            AssetType = asset.Type;
            Creator = asset.Creator;
            Name = asset.Name;
            Flags = 0;
            ID = UUID.Random;
            switch(AssetType)
            {
                case AssetType.Animation: InventoryType = InventoryType.Animation; break;
                case AssetType.Bodypart: InventoryType = InventoryType.Bodypart; break;
                case AssetType.CallingCard: InventoryType = InventoryType.CallingCard; break;
                case AssetType.Clothing: InventoryType = InventoryType.Clothing; break;
                case AssetType.Gesture: InventoryType = InventoryType.Gesture; break;
                case AssetType.ImageJPEG: InventoryType = InventoryType.Snapshot; break;
                case AssetType.ImageTGA: InventoryType = InventoryType.Snapshot; break;
                case AssetType.Landmark: InventoryType = InventoryType.Landmark; break;
                case AssetType.LSLBytecode: InventoryType = InventoryType.LSLBytecode; break;
                case AssetType.LSLText: InventoryType = InventoryType.LSLText; break;
                case AssetType.Notecard: InventoryType = InventoryType.Notecard; break;
                case AssetType.Sound: InventoryType = InventoryType.Sound; break;
                case AssetType.SoundWAV: InventoryType = InventoryType.Sound; break;
                case AssetType.Texture: InventoryType = InventoryType.Texture; break;
                case AssetType.TextureTGA: InventoryType = InventoryType.TextureTGA; break;
                default: InventoryType = InventoryType.Unknown; break;
            }
            Owner = asset.Creator;
            Permissions.Base = InventoryPermissionsMask.Every;
            Permissions.Current = InventoryPermissionsMask.Every;
            Permissions.EveryOne = InventoryPermissionsMask.None;
            Permissions.Group = InventoryPermissionsMask.None;
            Permissions.NextOwner = InventoryPermissionsMask.Every;
        }

        public ObjectPartInventoryItem(InventoryItem item)
        {
            AssetID = new UUID(item.AssetID);
            AssetType = item.AssetType;
            CreationDate = item.CreationDate;
            Creator = new UUI(item.Creator);
            Description = item.Description;
            Flags = item.Flags;
            Group = new UGI(item.Group);
            IsGroupOwned = item.IsGroupOwned;
            ID = new UUID(item.ID);
            InventoryType = item.InventoryType;
            LastOwner = new UUI(item.LastOwner);
            Name = item.Name;
            Owner = new UUI(item.Owner);
            ParentFolderID = new UUID(item.ParentFolderID);
            Permissions = item.Permissions;
            SaleInfo = item.SaleInfo;
        }
        #endregion

        #region Perms Granting
        public class PermsGranterInfo
        {
            public UUI PermsGranter = UUI.Unknown;
            public ScriptPermissions PermsMask;

            public PermsGranterInfo()
            {

            }

            public PermsGranterInfo(PermsGranterInfo i)
            {
                PermsGranter = i.PermsGranter;
                PermsMask = i.PermsMask;
            }
        }

        private PermsGranterInfo m_PermsGranter;
        public PermsGranterInfo PermsGranter 
        { 
            get
            {
                lock(this)
                {
                    if(null != m_PermsGranter)
                    {
                        return new PermsGranterInfo(m_PermsGranter);
                    }
                    else
                    {
                        return new PermsGranterInfo();
                    }
                }
            }
            set
            {
                lock(this)
                {
                    if(value == null)
                    {
                        m_PermsGranter = null;
                    }
                    else
                    {
                        m_PermsGranter = new PermsGranterInfo(value);
                    }
                }
            }
        }
        #endregion

        #region Fields
        private ScriptInstance m_ScriptInstance;
        #endregion

        #region Properties
        IScriptState m_ScriptState;
        public IScriptState ScriptState
        {
            get
            {
                return m_ScriptState;
            }
            set
            {
                lock(this)
                {
                    m_ScriptState = value;
                }
            }
        }

        public ScriptInstance ScriptInstance
        {
            get
            {
                return m_ScriptInstance;
            }
            set
            {
                lock(this)
                {
                    ScriptInstance instance = m_ScriptInstance;
                    if (null != instance)
                    {
                        instance.Abort();
                        instance.Remove();
                    }
                    m_ScriptState = null;
                    m_ScriptInstance = value;
                }
            }
        }
        public ScriptInstance RemoveScriptInstance
        {
            get
            {
                lock(this)
                {
                    ScriptInstance instance = m_ScriptInstance;
                    m_ScriptInstance = null;
                    m_ScriptState = null;
                    return instance;
                }
            }
        }
        #endregion

        ~ObjectPartInventoryItem()
        {
            lock(this)
            {
                if (m_ScriptInstance != null)
                {
                    m_ScriptInstance.Abort();
                    m_ScriptInstance.Remove();
                }
            }
        }
    }
}
