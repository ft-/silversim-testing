/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ArribaSim.Types.Asset.Format
{
    public enum WearableType : uint
    {
        Shape = 0,
        Skin = 1,
        Hair = 2,
        Eyes = 3,
        Shirt = 4,
        Pants = 5,
        Shoes = 6,
        Socks = 7,
        Jacket = 8,
        Gloves = 9,
        Undershirt = 10,
        Underpants = 11,
        Skirt = 12,
        Alpha = 13,
        Tattoo = 14,
        Physics = 15,
        Invalid = 255
    }

    public class Wearable
    {
        public string Name = string.Empty;
        public string Description = string.Empty;
        public WearableType Type = WearableType.Invalid;
        public Dictionary<uint, double> Params = new Dictionary<uint,double>();
        public Dictionary<uint, UUID> Textures = new Dictionary<uint, UUID>();
        public UUI Creator = new UUI();
        public UUI LastOwner = new UUI();
        public UUI Owner = new UUI();
        public UUID GroupID = new UUID();
        InventoryItem.PermissionsData Permissions;
        InventoryItem.SaleInfoData SaleInfo;

        #region Constructors
        public Wearable()
        {
            SaleInfo.PermMask = 0x7FFFFFFF;
        }

        public Wearable(AssetData asset)
        {
            string[] lines = Encoding.UTF8.GetString(asset.Data).Split('\n');

            string[] versioninfo = lines[0].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if(versioninfo[0] != "LLWearable")
            {
                throw new NotAWearableFormat();
            }

            SaleInfo.PermMask = 0x7FFFFFFF;

            Name = lines[1].Trim();
            Description = lines[2].Trim();

            for(int idx = 3; idx < lines.Length; ++idx)
            {
                string line = lines[idx].Trim();
                string[] para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if(para.Length == 2 && para[0] == "type")
                {
                    Type = (WearableType)int.Parse(para[1]);
                }
                else if(para.Length == 2 && para[0] == "parameters")
                {
                    /* we got a parameter block */
                    uint parametercount = uint.Parse(para[1]);
                    for(uint paranum = 0; paranum < parametercount; ++paranum)
                    {
                        line = lines[++idx].Trim();
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length == 2)
                        {
                            Params[uint.Parse(para[0])] = double.Parse(para[1]);
                        }
                    }
                }
                else if(para.Length == 2 && para[0] == "textures")
                {
                    /* we got a textures block */
                    uint texturecount = uint.Parse(para[1]);
                    for(uint paranum = 0; paranum < texturecount; ++paranum)
                    {
                        line = lines[++idx].Trim();
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length == 2)
                        {
                            Textures[uint.Parse(para[0])] = para[1];
                        }
                    }
                }
                else if(para[0] == "permissions")
                {
                    if(lines[++idx].Trim() != "{")
                    {
                        throw new NotAWearableFormat();
                    }
                    if (idx == lines.Length)
                    {
                        throw new NotAWearableFormat();
                    }
                    while ((line = lines[idx].Trim()) != "}")
                    {
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length < 2)
                        {
                        }
                        else if(para[0] == "base_mask")
                        {
                            Permissions.Base = uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "owner_mask")
                        {
                            Permissions.Current = uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "group_mask")
                        {
                            Permissions.Group = uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "everyone_mask")
                        {
                            Permissions.EveryOne = uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "next_owner_mask")
                        {
                            Permissions.NextOwner = uint.Parse(para[1], NumberStyles.HexNumber);
                        }
                        else if(para[0] == "creator_id")
                        {
                            Creator.ID = para[1];
                        }
                        else if(para[0] == "owner_id")
                        {
                            Owner.ID = para[1];
                        }
                        else if(para[0] == "last_owner_id")
                        {
                            LastOwner.ID = para[1];
                        }
                        else if(para[0] == "group_id")
                        {
                            GroupID = para[1];
                        }

                        if(++idx == lines.Length)
                        {
                            throw new NotAWearableFormat();
                        }
                    }
                }
                else if(para[0] == "sale_info")
                {
                    if (lines[++idx].Trim() != "{")
                    {
                        throw new NotAWearableFormat();
                    }
                    if (idx == lines.Length)
                    {
                        throw new NotAWearableFormat();
                    }
                    while ((line = lines[idx].Trim()) != "}")
                    {
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length < 2)
                        {
                        }
                        else if(para[0] == "sale_type")
                        {
                            SaleInfo.TypeName = para[1];
                        }
                        else if(para[0] == "sale_price")
                        {
                            SaleInfo.Price = uint.Parse(para[1]);
                        }
                        else if (para[0] == "perm_mask")
                        {
                            SaleInfo.PermMask = uint.Parse(para[1], NumberStyles.HexNumber);
                        }

                        if (++idx == lines.Length)
                        {
                            throw new NotAWearableFormat();
                        }
                    }
                }
            }
        }
        #endregion

        #region Operators
        private static readonly string WearableFormat =
                "permissions 0\n" +
                "{\n" +
                "\tbase_mask\t{0:x08}\n" +
                "\towner_mask\t{1:x08}\n" +
                "\tgroup_mask\t{2:x08}\n" +
                "\teveryone_mask\t{3:x08}\n" +
                "\tnext_owner_mask\t{4:x08}\n" +
                "\tcreator_id\t{5}\n" +
                "\towner_id\t{6}\n" +
                "\tlast_owner_id\t{7}\n" +
                "\tgroup_id\t{8}\n" +
                "}\n" +
                "sale_info 0\n"+
                "{\n" +
                "\tsale_type\t{9}\n" +
                "\tsale_price\t{10}\n" +
                "}\n" +
                "type\t{11}\n";

        public byte[] WearableData
        {
            get
            {
                string fmt = "LLWearable version 22\n";
                fmt += Name + "\n";
                fmt += Description + "\n";
                fmt += String.Format(WearableFormat,
                    Permissions.Base,
                    Permissions.Current,
                    Permissions.Group,
                    Permissions.EveryOne,
                    Permissions.NextOwner,
                    Creator.ID,
                    Owner.ID,
                    LastOwner.ID,
                    GroupID,
                    SaleInfo.TypeName,
                    SaleInfo.Price,
                    (uint)Type);
                fmt += String.Format("parameters {0}\n", Params.Count);
                foreach(KeyValuePair<uint, double> kvp in Params)
                {
                    fmt += String.Format("{0}\t{1}\n", kvp.Key, kvp.Value);
                }
                fmt += String.Format("textures {0}\n", Textures.Count);
                foreach(KeyValuePair<uint, UUID> kvp in Textures)
                {
                    fmt += String.Format("{0}\t{1}\n", kvp.Key, kvp.Value);
                }

                return Encoding.UTF8.GetBytes(fmt);
            }
        }

        public static implicit operator AssetData(Wearable v)
        {
            AssetData asset = new AssetData();

            switch(v.Type)
            {
                case WearableType.Hair:
                case WearableType.Skin:
                case WearableType.Shape:
                case WearableType.Eyes:
                    asset.Type = AssetType.Bodypart;
                    break;

                default:
                    asset.Type = AssetType.Clothing;
                    break;
            }

            asset.Name = "Wearable";
            asset.Data = v.WearableData;

            return asset;
        }
        #endregion
    }
}
