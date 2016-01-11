// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Script;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Object
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
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
                case AssetType.ImageJPEG: 
                case AssetType.ImageTGA: InventoryType = InventoryType.Snapshot; break;
                case AssetType.Landmark: InventoryType = InventoryType.Landmark; break;
                case AssetType.LSLBytecode: InventoryType = InventoryType.LSLBytecode; break;
                case AssetType.LSLText: InventoryType = InventoryType.LSLText; break;
                case AssetType.Notecard: InventoryType = InventoryType.Notecard; break;
                case AssetType.Sound: 
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
        readonly object m_PermsGranterLock = new object();
        public PermsGranterInfo PermsGranter 
        { 
            get
            {
                lock(m_PermsGranterLock)
                {
                    PermsGranterInfo permsGranter = m_PermsGranter;
                    return (null != permsGranter) ?
                        new PermsGranterInfo(permsGranter) :
                        new PermsGranterInfo();
                }
            }
            set
            {
                lock(m_PermsGranterLock)
                {
                    m_PermsGranter = (value == null) ?
                        null :
                        new PermsGranterInfo(value);
                }
            }
        }
        #endregion

        #region Fields
        private ScriptInstance m_ScriptInstance;
        #endregion

        #region Properties
        public IScriptState ScriptState { get; set; }

        public ScriptInstance ScriptInstance
        {
            get
            {
                return m_ScriptInstance;
            }
            set
            {
                lock(m_PermsGranterLock)
                {
                    ScriptInstance instance = m_ScriptInstance;
                    if (null != instance)
                    {
                        instance.Abort();
                        instance.Remove();
                    }
                    ScriptState = null;
                    m_ScriptInstance = value;
                }
            }
        }
        public ScriptInstance RemoveScriptInstance
        {
            get
            {
                lock(m_PermsGranterLock)
                {
                    ScriptInstance instance = m_ScriptInstance;
                    m_ScriptInstance = null;
                    ScriptState = null;
                    return instance;
                }
            }
        }
        #endregion

        ~ObjectPartInventoryItem()
        {
            lock(m_PermsGranterLock)
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
