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

        string m_Description = string.Empty;
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value.FilterToNonControlChars().TrimToMaxLength(127);
            }
        }

        public InventoryType InventoryType = InventoryType.Unknown;
        public InventoryFlags Flags;
        public UUI Owner = new UUI();
        public UUI LastOwner = new UUI();
        #endregion

        #region Creator Info
        public UUI Creator = new UUI();
        public Date CreationDate = new Date();
        #endregion

        #region Permissions
        public InventoryPermissionsData Permissions;

        public bool CheckPermissions(UUI accessor, UGI accessorgroup, InventoryPermissionsMask wanted) => (IsGroupOwned) ?
                Permissions.CheckGroupPermissions(Creator, Group, accessor, accessorgroup, wanted) :
                Permissions.CheckAgentPermissions(Creator, Owner, accessor, wanted);
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
                        case "not":
                            Type = SaleType.NoSale;
                            break;
                        case "orig":
                            Type = SaleType.Original;
                            break;
                        case "copy":
                            Type = SaleType.Copy;
                            break;
                        case "cntn":
                            Type = SaleType.Content;
                            break;
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

        #region Properties
        public string AssetTypeName
        {
            get
            {
                return AssetType.AssetTypeToString();
            }
            set
            {
                AssetType = value.StringToAssetType();
            }
        }

        public string InventoryTypeName
        {
            get
            {
                return InventoryType.InventoryTypeToString();
            }
            set
            {
                InventoryType = value.StringToInventoryType();
            }
        }
        #endregion
    }
}
