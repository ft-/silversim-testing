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
        public ObjectInventoryUpdateInfo UpdateInfo { get; }

        #region Constructors
        public ObjectPartInventoryItem()
        {
            UpdateInfo = new ObjectInventoryUpdateInfo(this);
        }

        public ObjectPartInventoryItem(UUID id)
            : base(id)
        {
            UpdateInfo = new ObjectInventoryUpdateInfo(this);
        }

        public ObjectPartInventoryItem(AssetData asset)
        {
            AssetID = asset.ID;
            AssetType = asset.Type;
            Name = asset.Name;
            Flags = 0;
            switch (AssetType)
            {
                case AssetType.Animation:
                    InventoryType = InventoryType.Animation;
                    break;
                case AssetType.Bodypart:
                case AssetType.Clothing:
                    InventoryType = InventoryType.Wearable;
                    break;
                case AssetType.CallingCard:
                    InventoryType = InventoryType.CallingCard;
                    break;
                case AssetType.Gesture:
                    InventoryType = InventoryType.Gesture;
                    break;
                case AssetType.ImageJPEG:
                case AssetType.ImageTGA:
                    InventoryType = InventoryType.Snapshot;
                    break;
                case AssetType.Landmark:
                    InventoryType = InventoryType.Landmark;
                    break;
                case AssetType.LSLBytecode:
                case AssetType.LSLText:
                    InventoryType = InventoryType.LSL;
                    break;
                case AssetType.Notecard:
                    InventoryType = InventoryType.Notecard;
                    break;
                case AssetType.Sound:
                case AssetType.SoundWAV:
                    InventoryType = InventoryType.Sound;
                    break;
                case AssetType.Texture:
                case AssetType.TextureTGA:
                    InventoryType = InventoryType.Texture;
                    break;
                default:
                    InventoryType = InventoryType.Unknown;
                    break;
            }
            Permissions.Base = InventoryPermissionsMask.Every;
            Permissions.Current = InventoryPermissionsMask.Every;
            Permissions.EveryOne = InventoryPermissionsMask.None;
            Permissions.Group = InventoryPermissionsMask.None;
            Permissions.NextOwner = InventoryPermissionsMask.Every;
            UpdateInfo = new ObjectInventoryUpdateInfo(this);
        }

        public ObjectPartInventoryItem(InventoryItem item)
            : base(item.ID)
        {
            AssetID = new UUID(item.AssetID);
            AssetType = item.AssetType;
            CreationDate = item.CreationDate;
            Creator = new UGUI(item.Creator);
            Description = item.Description;
            Flags = item.Flags;
            Group = new UGI(item.Group);
            IsGroupOwned = item.IsGroupOwned;
            InventoryType = item.InventoryType;
            LastOwner = new UGUI(item.LastOwner);
            Name = item.Name;
            Owner = new UGUI(item.Owner);
            ParentFolderID = new UUID(item.ParentFolderID);
            Permissions.Base = item.Permissions.Base;
            Permissions.Current = item.Permissions.Current;
            Permissions.EveryOne = item.Permissions.EveryOne;
            Permissions.Group = item.Permissions.Group;
            Permissions.NextOwner = item.Permissions.NextOwner;
            SaleInfo = item.SaleInfo;
            UpdateInfo = new ObjectInventoryUpdateInfo(this);
        }

        public ObjectPartInventoryItem(UUID id, InventoryItem item)
            : base(id)
        {
            AssetID = new UUID(item.AssetID);
            AssetType = item.AssetType;
            CreationDate = item.CreationDate;
            Creator = new UGUI(item.Creator);
            Description = item.Description;
            Flags = item.Flags;
            Group = new UGI(item.Group);
            IsGroupOwned = item.IsGroupOwned;
            InventoryType = item.InventoryType;
            LastOwner = new UGUI(item.LastOwner);
            Name = item.Name;
            Owner = new UGUI(item.Owner);
            ParentFolderID = new UUID(item.ParentFolderID);
            Permissions.Base = item.Permissions.Base;
            Permissions.Current = item.Permissions.Current;
            Permissions.EveryOne = item.Permissions.EveryOne;
            Permissions.Group = item.Permissions.Group;
            Permissions.NextOwner = item.Permissions.NextOwner;
            SaleInfo = item.SaleInfo;
            UpdateInfo = new ObjectInventoryUpdateInfo(this);
        }
        #endregion

        #region Perms Granting
        private UUID m_ExperienceID = UUID.Zero;

        public UUID ExperienceID
        {
            get
            {
                UUID id;
                lock(m_PermsGranterLock)
                {
                    id = m_ExperienceID;
                }
                return id;
            }
            set
            {
                lock(m_PermsGranterLock)
                {
                    m_ExperienceID = value;
                }
            }
        }

        public class PermsGranterInfo
        {
            public UGUI PermsGranter = UGUI.Unknown;
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

        public UUID NextOwnerAssetID = UUID.Zero;

        private PermsGranterInfo m_PermsGranter;
        private readonly object m_PermsGranterLock = new object();
        public PermsGranterInfo PermsGranter
        {
            get
            {
                lock(m_PermsGranterLock)
                {
                    PermsGranterInfo permsGranter = m_PermsGranter;
                    return (permsGranter != null) ?
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

        #region Collision Filter
        private readonly object m_CollisionFilterLock = new object();
        public struct CollisionFilterParam
        {
            public string Name;
            public UUID ID;
            public CollisionFilterEnum Type;

            public byte[] DbSerialization
            {
                get
                {
                    byte[] strdata = Name.ToUTF8Bytes();
                    byte[] sdata = new byte[strdata.Length + 17];
                    Buffer.BlockCopy(strdata, 0, sdata, 17, strdata.Length);
                    ID.ToBytes(sdata, 1);
                    sdata[0] = (byte)Type;
                    return sdata;
                }
                set
                {
                    if (value.Length < 17)
                    {
                        Type = CollisionFilterEnum.Accept;
                        ID = UUID.Zero;
                        Name = string.Empty;
                    }
                    else
                    {
                        Type = (CollisionFilterEnum)value[0];
                        ID = new UUID(value, 1);
                        Name = value.FromUTF8Bytes(17, value.Length - 17);
                    }
                }
            }
        }

        private string m_CollisionFilterName = string.Empty;
        private UUID m_CollisionFilterId = UUID.Zero;

        public enum CollisionFilterEnum : byte
        {
            Reject,
            Accept
        }

        private CollisionFilterEnum m_CollisionFilterType = CollisionFilterEnum.Accept;

        public CollisionFilterParam CollisionFilter
        {
            get
            {
                lock (m_CollisionFilterLock)
                {
                    return new CollisionFilterParam
                    {
                        Name = m_CollisionFilterName,
                        ID = m_CollisionFilterId,
                        Type = m_CollisionFilterType
                    };
                }
            }
            set
            {
                lock (m_CollisionFilterLock)
                {
                    m_CollisionFilterName = value.Name;
                    m_CollisionFilterId = value.ID;
                    m_CollisionFilterType = value.Type;
                }
            }
        }
        #endregion

        #region Properties
        public IScriptState ScriptState { get; set; }

        public ScriptInstance ScriptInstance
        {
            get { return m_ScriptInstance; }
            set
            {
                lock (m_PermsGranterLock)
                {
                    ScriptInstance instance = m_ScriptInstance;
                    if (instance != null)
                    {
                        instance.Abort();
                        instance.Remove();
                    }

                    ScriptState = value?.ScriptState;
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

        public override void SetNewID(UUID id)
        {
            ID = id;
            UpdateInfo?.UpdateIDs();
        }
    }
}
