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

using SilverSim.Types.Inventory;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SilverSim.Types.Asset.Format
{
    public class Notecard : IReferencesAccessor
    {
        public NotecardInventory Inventory = new NotecardInventory();
        public string Text = string.Empty;

        #region Constructors
        public Notecard()
        {
        }

        public Notecard(AssetData asset)
        {
            using(var assetdata = asset.InputStream)
            {
                var line = ReadLine(assetdata);
                var versioninfo = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if(versioninfo.Length < 2 || versioninfo[0] != "Linden" || versioninfo[1] != "text")
                {
                    /* Viewers handle notecards without this header as plain text notecard */
                    Text = Encoding.UTF8.GetString(asset.Data);
                    return;
                }
                ReadNotecard(assetdata);
            }
        }
        #endregion

        #region References
        public List<UUID> References
        {
            get
            {
                var reflist = new List<UUID>();
                foreach(var item in Inventory.Values)
                {
                    if (!reflist.Contains(item.AssetID))
                    {
                        reflist.Add(item.AssetID);
                    }
                }

                if(Text.StartsWith("<llsd>"))
                {
                    var d = Text.ToUTF8Bytes();
                    /* could be an agent appearance notecard, so let us try that */
                    Map im;
                    using (var i = new MemoryStream(d))
                    {
                        im = LlsdXml.Deserialize(i) as Map;
                    }
                    if (im?.ContainsKey("serial") == true &&
                        im.ContainsKey("height") &&
                        im.ContainsKey("wearables") &&
                        im.ContainsKey("textures") &&
                        im.ContainsKey("visualparams") &&
                        im.ContainsKey("attachments"))
                    {
                        try
                        {
                            if (im["wearables"] is AnArray)
                            {
                                foreach (var w in (AnArray)im["wearables"])
                                {
                                    var aw = w as AnArray;
                                    if (aw != null)
                                    {
                                        foreach (var wi in aw)
                                        {
                                            var awm = wi as Map;
                                            if (awm != null)
                                            {
                                                try
                                                {
                                                    var assetID = awm["asset"].AsUUID;
                                                    if (!reflist.Contains(assetID))
                                                    {
                                                        reflist.Add(assetID);
                                                    }
                                                }
                                                catch
                                                {
                                                    /* no action required */
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            var attarray = im["attachments"] as AnArray;
                            if (attarray != null)
                            {
                                foreach (var a in attarray)
                                {
                                    var am = a as Map;
                                    if (am != null)
                                    {
                                        try
                                        {
                                            var assetID = am["asset"].AsUUID;
                                            if (!reflist.Contains(assetID))
                                            {
                                                reflist.Add(assetID);
                                            }
                                        }
                                        catch
                                        {
                                            /* no action required */
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            /* no action required */
                        }
                    }
                }
                reflist.Remove(UUID.Zero);
                return reflist;
            }
        }
        #endregion

        #region Notecard Parser

        private string ReadLine(Stream stream)
        {
            int c;
            var data = new StringBuilder();

            while('\n' != (c = stream.ReadByte()) && c != -1)
            {
                data.Append((char)c);
            }

            return data.ToString().Trim();
        }

        private void ReadInventoryPermissions(Stream assetdata, ref NotecardInventoryItem item)
        {
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                var line = ReadLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                var data = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                uint uval;
                switch(data[0])
                {
                    case "base_mask":
                        if (!uint.TryParse(data[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uval))
                        {
                            throw new NotANotecardFormatException();
                        }
                        item.Permissions.Base = (InventoryPermissionsMask)uval;
                        break;

                    case "owner_mask":
                        if (!uint.TryParse(data[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uval))
                        {
                            throw new NotANotecardFormatException();
                        }
                        item.Permissions.Current = (InventoryPermissionsMask)uval;
                        break;

                    case "group_mask":
                        if (!uint.TryParse(data[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uval))
                        {
                            throw new NotANotecardFormatException();
                        }
                        item.Permissions.Group = (InventoryPermissionsMask)uval;
                        break;

                    case "everyone_mask":
                        if (!uint.TryParse(data[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uval))
                        {
                            throw new NotANotecardFormatException();
                        }
                        item.Permissions.EveryOne = (InventoryPermissionsMask)uval;
                        break;

                    case "next_owner_mask":
                        if (!uint.TryParse(data[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uval))
                        {
                            throw new NotANotecardFormatException();
                        }
                        item.Permissions.NextOwner = (InventoryPermissionsMask)uval;
                        break;

                    case "creator_id":
                        item.Creator.ID = data[1];
                        break;

                    case "owner_id":
                        item.Owner.ID = data[1];
                        break;

                    case "last_owner_id":
                        item.LastOwner.ID = data[1];
                        break;

                    case "group_id":
                        item.Group.ID = data[1];
                        break;

                    default:
                        throw new NotANotecardFormatException();
                }
            }
        }

        private void ReadInventorySaleInfo(Stream assetdata, ref NotecardInventoryItem item)
        {
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                var line = ReadLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                var data = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                int ival;
                uint uval;
                switch(data[0])
                {
                    case "sale_type":
                        item.SaleInfo.TypeName = data[1];
                        break;

                    case "sale_price":
                        if (!int.TryParse(data[1], out ival))
                        {
                            throw new NotANotecardFormatException();
                        }
                        item.SaleInfo.Price = ival;
                        break;

                    case "perm_mask":
                        if (!uint.TryParse(data[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uval))
                        {
                            throw new NotANotecardFormatException();
                        }
                        item.SaleInfo.PermMask = (InventoryPermissionsMask)uval;
                        break;

                    default:
                        throw new NotANotecardFormatException();
                }
            }
        }

        private NotecardInventoryItem ReadInventoryItem(Stream assetdata)
        {
            var item = new NotecardInventoryItem();
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                var line = ReadLine(assetdata);
                if(line == "}")
                {
                    return item;
                }

                var data = line.Split(new char[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if(data[0] == "item_id")
                {
                    item.ID = data[1];
                }
                else if(data[0] == "parent_id")
                {
                    item.ParentFolderID = data[1];
                }
                else if(data[0] == "permissions")
                {
                    ReadInventoryPermissions(assetdata, ref item);
                }
                else if(data[0] == "asset_id" && data.Length == 2)
                {
                    item.AssetID = data[1];
                }
                else if(data[0] == "type" && data.Length == 2)
                {
                    item.AssetTypeName = data[1];
                }
                else if(data[0] == "inv_type" && data.Length == 2)
                {
                    item.InventoryTypeName = data[1];
                }
                else if(data[0] == "flags" && data.Length == 2)
                {
                    uint uval;
                    if (!uint.TryParse(data[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uval))
                    {
                        throw new NotANotecardFormatException();
                    }
                    item.Flags = (InventoryFlags)uval;
                }
                else if(data[0] == "sale_info")
                {
                    ReadInventorySaleInfo(assetdata, ref item);
                }
                else if(data[0] == "name" && data.Length > 1)
                {
                    item.Name = line.Substring(5, line.Length - 6).Trim();
                }
                else if(data[0] == "desc" && data.Length > 1)
                {
                    item.Description = line.Substring(5, line.Length - 6).Trim();
                }
                else if(data[0] == "creation_date" && data.Length == 2)
                {
                    ulong uval;
                    if (!ulong.TryParse(data[1], out uval))
                    {
                        throw new NotANotecardFormatException();
                    }
                    item.CreationDate = Date.UnixTimeToDateTime(uval);
                }
                else
                {
                    throw new NotANotecardFormatException();
                }
            }
        }

        private NotecardInventoryItem ReadInventoryItems(Stream assetdata)
        {
            NotecardInventoryItem item = null;
            uint extcharindex = 0;
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                var line = ReadLine(assetdata);
                if(line == "}")
                {
                    if(item == null)
                    {
                        throw new NotANotecardFormatException();
                    }
                    return item;
                }

                var data = line.Split(new char[] {'\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if(data.Length == 4 && data[0] == "ext" && data[1] == "char" && data[2] == "index")
                {
                    if(!uint.TryParse(data[3], out extcharindex))
                    {
                        throw new NotANotecardFormatException();
                    }
                }
                else if(data[0] == "inv_item")
                {
                    item = ReadInventoryItem(assetdata);
                    item.ExtCharIndex = extcharindex;
                }
                else
                {
                    throw new NotANotecardFormatException();
                }
            }
        }

        private void ReadInventory(Stream assetdata)
        {
            Inventory = new NotecardInventory();
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                var line = ReadLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                var data = line.Split(new char[] { ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if(data[0] == "count")
                {
                    uint count;
                    if (!uint.TryParse(data[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out count))
                    {
                        throw new NotANotecardFormatException();
                    }
                    for (uint i = 0; i < count; ++i)
                    {
                        var item = ReadInventoryItems(assetdata);
                        Inventory.Add(item.ID, item);
                    }
                }
                else
                {
                    throw new NotANotecardFormatException();
                }
            }
        }

        private void ReadNotecard(Stream assetdata)
        {
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                var line = ReadLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                var data = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if(data[0] == "LLEmbeddedItems")
                {
                    ReadInventory(assetdata);
                }
                else if(data[0] == "Text" && data.Length == 3)
                {
                    int datalen;
                    if(!int.TryParse(data[2], out datalen))
                    {
                        throw new NotANotecardFormatException();
                    }
                    var buffer = new byte[datalen];
                    if(datalen != assetdata.Read(buffer, 0, datalen))
                    {
                        throw new NotANotecardFormatException();
                    }
                    Text = Encoding.UTF8.GetString(buffer);
                }
                else
                {
                    throw new NotANotecardFormatException();
                }
            }
        }

        #endregion

        #region Operators
        private const string ItemFormatString =
                "{{\n"+
                "ext char index {0}\n" +
                "\tinv_item\t0\n" +
                "\t{{\n" +
                "\t\titem_id\t{1}\n" +
                "\t\tparent_id\t{2}\n" +
                "\t\tpermissions 0\n" +
                "\t\t{{\n" +
                "\t\t\tbase_mask\t{3:x8}\n" +
                "\t\t\towner_mask\t{4:x8}\n" +
                "\t\t\tgroup_mask\t{5:x8}\n" +
                "\t\t\teveryone_mask\t{6:x8}\n" +
                "\t\t\tnext_owner_mask\t{7:x8}\n" +
                "\t\t\tcreator_id\t{8}\n" +
                "\t\t\towner_id\t{9}\n" +
                "\t\t\tlast_owner_id\t{10}\n" +
                "\t\t\tgroup_id\t{11}\n" +
                "\t\t}}\n" +
                "\t\tasset_id\t{12}\n" +
                "\t\ttype\t{13}\n" +
                "\t\tinv_type\t{14}\n" +
                "\t\tflags\t{15:x8}\n" +
                "\t\tsale_info 0\n" +
                "\t\t{{\n" +
                "\t\t\tsale_type\t{16}\n" +
                "\t\t\tsale_price\t{17}\n" +
                "\t\t}}\n" +
                "\t\tname\t{18}|\n" +
                "\t\tdesc\t{19}|\n" +
                "\t\tcreation_date\t{20}\n" +
                "\t}}\n" +
                "}}\n";

        private static string ItemToString(NotecardInventoryItem item) => string.Format(ItemFormatString,
                item.ExtCharIndex,
                item.ID, item.ParentFolderID,
                (uint)item.Permissions.Base,
                (uint)item.Permissions.Current,
                (uint)item.Permissions.Group,
                (uint)item.Permissions.EveryOne,
                (uint)item.Permissions.NextOwner,
                item.Creator.ID,
                item.Owner.ID,
                item.LastOwner.ID,
                item.Group.ID,
                item.AssetID,
                item.AssetTypeName,
                item.InventoryTypeName,
                (uint)item.Flags,
                item.SaleInfo.TypeName,
                item.SaleInfo.Price,
                item.Name,
                item.Description,
                item.CreationDate.AsULong
                );

        public AssetData Asset() => this;

        public static implicit operator AssetData(Notecard v)
        {
            var notecard = new StringBuilder("Linden text version 2\n{\n");
            var inventory = v.Inventory;
            notecard.AppendFormat("LLEmbeddedItems version 1\n{{\ncount {0}\n", inventory != null ? v.Inventory.Count : 0);

            if (inventory != null)
            {
                foreach (var item in inventory.Values)
                {
                    notecard.Append(ItemToString(item));
                }
            }

            notecard.Append("}\n");
            var TextData = v.Text.ToUTF8Bytes();
            notecard.AppendFormat("Text length {0}\n", TextData.Length);
            var NotecardHeader = notecard.ToString().ToUTF8Bytes();

            var asset = new AssetData()
            {
                Data = new byte[TextData.Length + NotecardHeader.Length + 2]
            };
            Buffer.BlockCopy(NotecardHeader, 0, asset.Data, 0, NotecardHeader.Length);
            Buffer.BlockCopy(TextData, 0, asset.Data, NotecardHeader.Length, TextData.Length);
            asset.Data[NotecardHeader.Length + TextData.Length] = (byte)'}';
            asset.Data[NotecardHeader.Length + TextData.Length + 1] = (byte)'\n';

            asset.Type = AssetType.Notecard;
            asset.Name = "Notecard";

            return asset;
        }

        public static implicit operator string[] (Notecard v) => v.Text.Split('\n');
        #endregion
    }
}
