// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using SilverSim.Types.Asset;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Inventory
{
    public class InventoryItem
    {
        #region Inventory Data
        public UUID ID = UUID.Zero;
        public UUID ParentFolderID = UUID.Zero;
        string m_Name = string.Empty;
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                /* ensure that no non-printable characters manage it into our inventory */
                m_Name = value.FilterToAscii7Printable().TrimToMaxLength(63);
            }
        }
        public string Description = string.Empty;
        public InventoryType InventoryType = InventoryType.Unknown;
        public uint Flags;
        public UUI Owner = new UUI();
        public UUI LastOwner = new UUI();
        #endregion

        #region Creator Info
        public UUI Creator = new UUI();
        public Date CreationDate = new Date();
        #endregion

        #region Permissions
        public InventoryPermissionsData Permissions;

        public bool CheckPermissions(UUI accessor, UGI accessorgroup, InventoryPermissionsMask wanted)
        {
            return (IsGroupOwned) ?
                Permissions.CheckGroupPermissions(Creator, Group, accessor, accessorgroup, wanted) :
                Permissions.CheckAgentPermissions(Creator, Owner, accessor, wanted);
        }
        #endregion

        #region SaleInfo
        public struct SaleInfoData
        {
            [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
            public enum SaleType : byte
            {
                NoSale = 0,
                Original = 1,
                Copy = 2,
                Content = 3
            }
            public int Price;
            public SaleType Type;
            public InventoryPermissionsMask PermMask;

            #region Properties
            public string TypeName
            {
                get
                {
                    switch(Type)
                    {
                        default:
                        case SaleType.NoSale: return "not";
                        case SaleType.Original: return "orig";
                        case SaleType.Copy: return "copy";
                        case SaleType.Content: return "cntn";
                    }
                }
                set
                {
                    switch(value)
                    {
                        case "not": Type = SaleType.NoSale; break;
                        case "orig": Type = SaleType.Original; break;
                        case "copy": Type = SaleType.Copy; break;
                        case "cntn": Type = SaleType.Content; break;
                        default:
                            throw new ArgumentException("invalid type name " + value);
                    }
                }
            }
            #endregion
        }
        public SaleInfoData SaleInfo;
        #endregion

        #region Group Information
        public UGI Group = UGI.Unknown;
        public bool IsGroupOwned;
        #endregion

        #region Asset Information
        public UUID AssetID = UUID.Zero;
        public AssetType AssetType = AssetType.Unknown;
        #endregion

        #region Constructors
        public InventoryItem()
        {
            ID = UUID.Random;
        }

        public InventoryItem(UUID id)
        {
            ID = id;
        }

        public InventoryItem(InventoryItem item)
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

        public InventoryItem(UUID id, InventoryItem item)
        {
            AssetID = new UUID(item.AssetID);
            AssetType = item.AssetType;
            CreationDate = item.CreationDate;
            Creator = new UUI(item.Creator);
            Description = item.Description;
            Flags = item.Flags;
            Group = new UGI(item.Group);
            IsGroupOwned = item.IsGroupOwned;
            ID = id;
            InventoryType = item.InventoryType;
            LastOwner = new UUI(item.LastOwner);
            Name = item.Name;
            Owner = new UUI(item.Owner);
            ParentFolderID = new UUID(item.ParentFolderID);
            Permissions = item.Permissions;
            SaleInfo = item.SaleInfo;
        }
        #endregion

        #region Type conversions
        public static string AssetTypeToString(sbyte v)
        {
            switch (v)
            {
                case (sbyte)AssetType.Texture: return "texture";
                case (sbyte)AssetType.Sound: return "sound";
                case (sbyte)AssetType.CallingCard: return "callcard";
                case (sbyte)AssetType.Landmark: return "Landmark";
                case (sbyte)AssetType.Clothing: return "clothing";
                case (sbyte)AssetType.Object: return "object";
                case (sbyte)AssetType.Notecard: return "notecard";
                case (sbyte)AssetType.LSLText: return "lsltext";
                case (sbyte)AssetType.LSLBytecode: return "lslbyte";
                case (sbyte)AssetType.TextureTGA: return "txtr_tga";
                case (sbyte)AssetType.Bodypart: return "bodypart";
                case (sbyte)AssetType.SoundWAV: return "snd_wav";
                case (sbyte)AssetType.ImageTGA: return "img_tga";
                case (sbyte)AssetType.ImageJPEG: return "jpeg";
                case (sbyte)AssetType.Animation: return "animatn";
                case (sbyte)AssetType.Gesture: return "gesture";
                case (sbyte)AssetType.Simstate: return "simstate";
                default: return "unknown";
            }
        }

        public static string InventoryTypeToString(sbyte v)
        {
            switch(v)
            {
                case (sbyte)InventoryType.Snapshot: return "snapshot";
                case (sbyte)InventoryType.Attachable: return "attach";
                case (sbyte)InventoryType.Wearable: return "wearable";
                default: return AssetTypeToString(v);
            }
        }
        #endregion

        #region Properties
        public string AssetTypeName
        {
            get
            {
                return AssetTypeToString((sbyte)AssetType);
            }
            set
            {
                switch(value)
                {
                    case "texture": AssetType = AssetType.Texture; break;
                    case "sound": AssetType = AssetType.Sound; break;
                    case "callcard": AssetType = AssetType.CallingCard; break;
                    case "landmark": AssetType = AssetType.Landmark; break;
                    case "clothing": AssetType = AssetType.Clothing; break;
                    case "object": AssetType = AssetType.Object; break;
                    case "notecard": AssetType = AssetType.Notecard; break;
                    case "lsltext": AssetType = AssetType.LSLText; break;
                    case "lslbyte": AssetType = AssetType.LSLBytecode; break;
                    case "txtr_tga": AssetType = AssetType.TextureTGA; break;
                    case "bodypart": AssetType = AssetType.Bodypart; break;
                    case "snd_wav": AssetType = AssetType.SoundWAV; break;
                    case "img_tga": AssetType = AssetType.ImageTGA; break;
                    case "jpeg": AssetType = AssetType.ImageJPEG; break;
                    case "animatn": AssetType = AssetType.Animation; break;
                    case "gesture": AssetType = AssetType.Gesture; break;
                    case "simstate": AssetType = AssetType.Simstate; break;
                    default: AssetType = AssetType.Unknown; break;
                }
            }
        }

        public string InventoryTypeName
        {
            get
            {
                return InventoryTypeToString((sbyte)InventoryType);
            }
            set
            {
                switch (value)
                {
                    case "texture": InventoryType = InventoryType.Texture; break;
                    case "sound": InventoryType = InventoryType.Sound; break;
                    case "callcard": InventoryType = InventoryType.CallingCard; break;
                    case "landmark": InventoryType = InventoryType.Landmark; break;
                    case "clothing": InventoryType = InventoryType.Clothing; break;
                    case "object": InventoryType = InventoryType.Object; break;
                    case "notecard": InventoryType = InventoryType.Notecard; break;
                    case "lsltext": InventoryType = InventoryType.LSLText; break;
                    case "lslbyte": InventoryType = InventoryType.LSLBytecode; break;
                    case "txtr_tga": InventoryType = InventoryType.TextureTGA; break;
                    case "bodypart": InventoryType = InventoryType.Bodypart; break;
                    case "animatn": InventoryType = InventoryType.Animation; break;
                    case "gesture": InventoryType = InventoryType.Gesture; break;
                    case "simstate": InventoryType = InventoryType.Simstate; break;
                    case "snapshot": InventoryType = InventoryType.Snapshot; break;
                    case "attach": InventoryType = InventoryType.Attachable; break;
                    case "wearable": InventoryType = InventoryType.Wearable; break;
                    default: AssetType = AssetType.Unknown; break;
                }
            }
        }
        #endregion
    }
}
