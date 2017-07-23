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

using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SilverSim.Types.Asset.Format
{
    public enum WearableType : byte
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
        NumWearables = 16,
        Invalid = 255
    }

    public class Wearable : IReferencesAccessor
    {
        public string Name = string.Empty;
        public string Description = string.Empty;
        public WearableType Type = WearableType.Invalid;
        public Dictionary<uint, double> Params = new Dictionary<uint,double>();
        public Dictionary<AvatarTextureIndex, UUID> Textures = new Dictionary<AvatarTextureIndex, UUID>();
        public UUI Creator = new UUI();
        public UUI LastOwner = new UUI();
        public UUI Owner = new UUI();
        public UGI Group = new UGI();
        private InventoryPermissionsData Permissions;
        private InventoryItem.SaleInfoData SaleInfo;

        #region Constructors
        public Wearable()
        {
            SaleInfo.PermMask = (InventoryPermissionsMask)0x7FFFFFFF;
        }

        public Wearable(AssetData asset)
        {
            var lines = Encoding.UTF8.GetString(asset.Data).Split('\n');

            var versioninfo = lines[0].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if(versioninfo[0] != "LLWearable")
            {
                throw new NotAWearableFormatException();
            }

            SaleInfo.PermMask = (InventoryPermissionsMask)0x7FFFFFFF;

            Name = lines[1].Trim();
            Description = lines[2].Trim();

            int idx = 2;
            while(++idx < lines.Length)
            {
                var line = lines[idx].Trim();
                var para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if(para.Length == 2 && para[0] == "type")
                {
                    int i;
                    if(!int.TryParse(para[1], out i))
                    {
                        throw new NotAWearableFormatException();
                    }
                    Type = (WearableType)i;
                }
                else if(para.Length == 2 && para[0] == "parameters")
                {
                    /* we got a parameter block */
                    uint parametercount;
                    if(!uint.TryParse(para[1], out parametercount))
                    {
                        throw new NotAWearableFormatException();
                    }
                    for(uint paranum = 0; paranum < parametercount; ++paranum)
                    {
                        line = lines[++idx].Trim();
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length == 2)
                        {
                            uint index;
                            double v;
                            if(!uint.TryParse(para[0], out index))
                            {
                                throw new NotAWearableFormatException();
                            }
                            if(!double.TryParse(para[1], NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                            {
                                throw new NotAWearableFormatException();
                            }
                            Params[index] = v;
                        }
                    }
                }
                else if(para.Length == 2 && para[0] == "textures")
                {
                    /* we got a textures block */
                    uint texturecount;
                    if(!uint.TryParse(para[1], out texturecount))
                    {
                        throw new NotAWearableFormatException();
                    }
                    for(uint paranum = 0; paranum < texturecount; ++paranum)
                    {
                        line = lines[++idx].Trim();
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length == 2)
                        {
                            uint index;
                            if (!uint.TryParse(para[0], out index))
                            {
                                throw new NotAWearableFormatException();
                            }

                            Textures[(AvatarTextureIndex)index] = para[1];
                        }
                    }
                }
                else if(para.Length == 2 && para[0] == "permissions")
                {
                    if(lines[++idx].Trim() != "{")
                    {
                        throw new NotAWearableFormatException();
                    }
                    if (idx == lines.Length)
                    {
                        throw new NotAWearableFormatException();
                    }
                    while ((line = lines[idx].Trim()) != "}")
                    {
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length < 2)
                        {
                            /* less than two parameters is not valid */
                        }
                        else if(para[0] == "base_mask")
                        {
                            uint val;
                            if (!uint.TryParse(para[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                            {
                                throw new NotAWearableFormatException();
                            }
                            Permissions.Base = (InventoryPermissionsMask)val;
                        }
                        else if(para[0] == "owner_mask")
                        {
                            uint val;
                            if (!uint.TryParse(para[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                            {
                                throw new NotAWearableFormatException();
                            }
                            Permissions.Current = (InventoryPermissionsMask)val;
                        }
                        else if(para[0] == "group_mask")
                        {
                            uint val;
                            if (!uint.TryParse(para[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                            {
                                throw new NotAWearableFormatException();
                            }
                            Permissions.Group = (InventoryPermissionsMask)val;
                        }
                        else if(para[0] == "everyone_mask")
                        {
                            uint val;
                            if (!uint.TryParse(para[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                            {
                                throw new NotAWearableFormatException();
                            }
                            Permissions.EveryOne = (InventoryPermissionsMask)val;
                        }
                        else if(para[0] == "next_owner_mask")
                        {
                            uint val;
                            if (!uint.TryParse(para[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                            {
                                throw new NotAWearableFormatException();
                            }
                            Permissions.NextOwner = (InventoryPermissionsMask)val;
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
                            Group.ID = para[1];
                        }

                        if(++idx == lines.Length)
                        {
                            throw new NotAWearableFormatException();
                        }
                    }
                }
                else if (para.Length == 2 && para[0] == "sale_info")
                {
                    if (lines[++idx].Trim() != "{")
                    {
                        throw new NotAWearableFormatException();
                    }
                    if (idx == lines.Length)
                    {
                        throw new NotAWearableFormatException();
                    }
                    while ((line = lines[idx].Trim()) != "}")
                    {
                        para = line.Split(new char[] { ' ' , '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        if(para.Length < 2)
                        {
                            /* less than two parameters is not valid */
                        }
                        else if(para[0] == "sale_type")
                        {
                            SaleInfo.TypeName = para[1];
                        }
                        else if(para[0] == "sale_price")
                        {
                            int val;
                            if (!int.TryParse(para[1], out val))
                            {
                                throw new NotAWearableFormatException();
                            }
                            SaleInfo.Price = val;
                        }
                        else if (para[0] == "perm_mask")
                        {
                            uint val;
                            if (!uint.TryParse(para[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val))
                            {
                                throw new NotAWearableFormatException();
                            }
                            SaleInfo.PermMask = (InventoryPermissionsMask)val;
                        }

                        if (++idx == lines.Length)
                        {
                            throw new NotAWearableFormatException();
                        }
                    }
                }
            }
        }
        #endregion

        #region References
        public List<UUID> References
        {
            get
            {
                var refs = new List<UUID>();
                foreach(UUID tex in Textures.Values)
                {
                    if(!refs.Contains(tex))
                    {
                        refs.Add(tex);
                    }
                }
                refs.Remove(UUID.Zero);
                return refs;
            }
        }
        #endregion

        #region Operators
        private const string WearableFormat =
                "permissions 0\n" +
                "{{\n" +
                "\tbase_mask\t{0:x8}\n" +
                "\towner_mask\t{1:x8}\n" +
                "\tgroup_mask\t{2:x8}\n" +
                "\teveryone_mask\t{3:x8}\n" +
                "\tnext_owner_mask\t{4:x8}\n" +
                "\tcreator_id\t{5}\n" +
                "\towner_id\t{6}\n" +
                "\tlast_owner_id\t{7}\n" +
                "\tgroup_id\t{8}\n" +
                "}}\n" +
                "sale_info 0\n"+
                "{{\n" +
                "\tsale_type\t{9}\n" +
                "\tsale_price\t{10}\n" +
                "}}\n" +
                "type\t{11}\n";

        public byte[] WearableData
        {
            get
            {
                var fmt = new StringBuilder("LLWearable version 22\n");
                fmt.Append(Name + "\n");
                fmt.Append(Description + "\n");
                fmt.AppendFormat(WearableFormat,
                    (uint)Permissions.Base,
                    (uint)Permissions.Current,
                    (uint)Permissions.Group,
                    (uint)Permissions.EveryOne,
                    (uint)Permissions.NextOwner,
                    Creator.ID,
                    Owner.ID,
                    LastOwner.ID,
                    Group.ID,
                    SaleInfo.TypeName,
                    SaleInfo.Price,
                    (uint)Type);
                fmt.AppendFormat("parameters {0}\n", Params.Count);
                foreach(var kvp in Params)
                {
                    fmt.AppendFormat("{0}\t{1}\n", kvp.Key, kvp.Value);
                }
                fmt.AppendFormat("textures {0}\n", Textures.Count);
                foreach(var kvp in Textures)
                {
                    fmt.AppendFormat("{0}\t{1}\n", (int)kvp.Key, kvp.Value);
                }

                return fmt.ToString().ToUTF8Bytes();
            }
        }

        public AssetData Asset()
        {
            return this;
        }

        public static implicit operator AssetData(Wearable v)
        {
            var asset = new AssetData();

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

        #region Support functions
        private static readonly Color[] EyeColors =
        {
            Color.FromRgb(50, 25, 5),
            Color.FromRgb(109, 55, 15),
            Color.FromRgb(150, 93, 49),
            Color.FromRgb(152, 118, 25),
            Color.FromRgb(95, 179, 107),
            Color.FromRgb(87, 192, 191),
            Color.FromRgb(95, 172, 179),
            Color.FromRgb(128, 128, 128),
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(255, 255, 0),
            Color.FromRgb(0, 255, 0),
            Color.FromRgb(0, 255, 255),
            Color.FromRgb(0, 0, 255),
            Color.FromRgb(255, 0, 255),
            Color.FromRgb(255, 0, 0)
        };

        private static readonly Color[] SkinColors =
        {
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(255, 0, 255),
            Color.FromRgb(255, 0, 0),
            Color.FromRgb(255, 255, 0),
            Color.FromRgb(0, 255, 0),
            Color.FromRgb(0, 255, 255),
            Color.FromRgb(0, 0, 255),
            Color.FromRgb(255, 0, 255)
        };

        private static readonly Color[] PigmentColors =
        {
            Color.FromRgb(252, 215, 200),
            Color.FromRgb(240, 177, 112),
            Color.FromRgb(90, 40, 16),
            Color.FromRgb(29, 9, 6)
        };

        private static readonly Color[] RainbowHairColors =
        {
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(255, 0, 255),
            Color.FromRgb(255, 0, 0),
            Color.FromRgb(255, 255, 0),
            Color.FromRgb(0, 255, 0),
            Color.FromRgb(0, 255, 255),
            Color.FromRgb(0, 0, 255),
            Color.FromRgb(255, 0, 255)
        };

        private static readonly Color[] RedHairColors =
        {
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(118, 47, 19)
        };

        private static readonly Color[] BlondeHairColors =
        {
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(22, 6, 6),
            Color.FromRgb(29, 9, 6),
            Color.FromRgb(45, 21, 11),
            Color.FromRgb(78, 39, 11),
            Color.FromRgb(90, 53, 16),
            Color.FromRgb(136, 92, 21),
            Color.FromRgb(150, 106, 33),
            Color.FromRgb(198, 156, 74),
            Color.FromRgb(233, 192, 103),
            Color.FromRgb(238, 205, 136)
        };

        private static Color CalcColor(double val, Color[] table)
        {
            var paramColor = new Color(0, 0, 0);

            if(table.Length == 1)
            {
                paramColor = table[0];
            }
            else
            {
                int tableLen = table.Length;
                val = val.Clamp(0, 1);
                double step = 1 / ((double)tableLen - 1);

                int indexa = Math.Min((int)(val / step), tableLen - 1);
                int indexb = Math.Min(indexa + 1, tableLen - 1);

                double distance = val - indexa * step;

                if(distance < Double.Epsilon || indexa == indexb)
                {
                    paramColor = table[indexa];
                }
                else
                {
                    Color ca = table[indexa];
                    Color cb = table[indexb];
                    double mix = distance / step;
                    paramColor.R = ca.R.Lerp(cb.R, mix);
                    paramColor.G = ca.R.Lerp(cb.G, mix);
                    paramColor.B = ca.R.Lerp(cb.B, mix);
                }
            }

            return paramColor;
        }

        public Color GetTint()
        {
            var col = new Color(1, 1, 1);
            double val;

            switch (Type)
            {
                case WearableType.Eyes:
                    col = new Color(0, 0, 0);
                    if (Params.TryGetValue(99, out val))
                    {
                        col += CalcColor(val, EyeColors);
                    }
                    if(Params.TryGetValue(98, out val))
                    {
                        col += new Color(val, val, val);
                    }
                    break;

                case WearableType.Skin:
                    col = new Color(0, 0, 0);
                    if (Params.TryGetValue(108, out val))
                    {
                        col += CalcColor(val, SkinColors);
                    }
                    if(Params.TryGetValue(110, out val))
                    {
                        col = col.Lerp(new Color(218, 41, 37), val);
                    }
                    if (!Params.TryGetValue(111, out val))
                    {
                        val = 0.5;
                    }
                    col += CalcColor(val, PigmentColors);
                    /*
            Params[108] = new VisualParam(108, "Rainbow Color", 0, "skin", String.Empty, "None", "Wild", 0f, 0f, 1f, false, null, null, new VisualColorParam(VisualColorOperation.Add, new Color4[] { new Color4(0, 0, 0, 255), new Color4(255, 0, 255, 255), new Color4(255, 0, 0, 255), new Color4(255, 255, 0, 255), new Color4(0, 255, 0, 255), new Color4(0, 255, 255, 255), new Color4(0, 0, 255, 255), new Color4(255, 0, 255, 255) }));
            Params[110] = new VisualParam(110, "Red Skin", 0, "skin", "Ruddiness", "Pale", "Ruddy", 0f, 0f, 0.1f, false, null, null, new VisualColorParam(VisualColorOperation.Blend, new Color4[] { new Color4(218, 41, 37, 255) }));
            Params[111] = new VisualParam(111, "Pigment", 0, "skin", String.Empty, "Light", "Dark", 0.5f, 0f, 1f, false, null, null, new VisualColorParam(VisualColorOperation.Add, new Color4[] { new Color4(252, 215, 200, 255), new Color4(240, 177, 112, 255), new Color4(90, 40, 16, 255), new Color4(29, 9, 6, 255) }));

            Params[116] = new VisualParam(116, "Rosy Complexion", 0, "skin", String.Empty, "Less Rosy", "More Rosy", 0f, 0f, 1f, false, null, null, new VisualColorParam(VisualColorOperation.Add, new Color4[] { new Color4(198, 71, 71, 0), new Color4(198, 71, 71, 255) }));
            Params[117] = new VisualParam(117, "Lip Pinkness", 0, "skin", String.Empty, "Darker", "Pinker", 0f, 0f, 1f, false, null, null, new VisualColorParam(VisualColorOperation.Add, new Color4[] { new Color4(220, 115, 115, 0), new Color4(220, 115, 115, 128) }));
                    */
                    break;

                case WearableType.Hair:
                    col = new Color(0, 0, 0);
                    if(Params.TryGetValue(112, out val))
                    {
                        col += CalcColor(val, RainbowHairColors);
                    }

                    if(Params.TryGetValue(113, out val))
                    {
                        col += CalcColor(val, RedHairColors);
                    }

                    if(Params.TryGetValue(114, out val))
                    {
                        col += CalcColor(val, BlondeHairColors);
                    }

                    if(Params.TryGetValue(115, out val))
                    {
                        col += new Color(val, val, val);
                    }
                    break;

                case WearableType.Shirt:
                    if (Params.TryGetValue(803, out val))
                    {
                        col.R = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(804, out val))
                    {
                        col.G = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(805, out val))
                    {
                        col.B = val.Clamp(0, 1);
                    }
                    break;

                case WearableType.Pants:
                    if (Params.TryGetValue(806, out val))
                    {
                        col.R = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(807, out val))
                    {
                        col.G = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(808, out val))
                    {
                        col.B = val.Clamp(0, 1);
                    }
                    break;

                case WearableType.Shoes:
                    if (Params.TryGetValue(812, out val))
                    {
                        col.R = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(813, out val))
                    {
                        col.G = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(817, out val))
                    {
                        col.B = val.Clamp(0, 1);
                    }
                    break;

                case WearableType.Socks:
                    if (Params.TryGetValue(818, out val))
                    {
                        col.R = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(819, out val))
                    {
                        col.G = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(820, out val))
                    {
                        col.B = val.Clamp(0, 1);
                    }
                    break;

                case WearableType.Undershirt:
                    if (Params.TryGetValue(821, out val))
                    {
                        col.R = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(822, out val))
                    {
                        col.G = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(823, out val))
                    {
                        col.B = val.Clamp(0, 1);
                    }
                    break;

                case WearableType.Underpants:
                    if (Params.TryGetValue(824, out val))
                    {
                        col.R = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(825, out val))
                    {
                        col.G = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(826, out val))
                    {
                        col.B = val.Clamp(0, 1);
                    }
                    break;

                case WearableType.Gloves:
                    if (Params.TryGetValue(827, out val))
                    {
                        col.R = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(829, out val))
                    {
                        col.G = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(830, out val))
                    {
                        col.B = val.Clamp(0, 1);
                    }
                    break;

                case WearableType.Skirt:
                    if (Params.TryGetValue(921, out val))
                    {
                        col.R = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(922, out val))
                    {
                        col.G = val.Clamp(0, 1);
                    }
                    if (Params.TryGetValue(923, out val))
                    {
                        col.B = val.Clamp(0, 1);
                    }
                    break;

                default:
                    break;
            }

            return col;
        }

        #endregion
    }
}
