// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Inventory;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace SilverSim.Types.Asset.Format
{
    public class Notecard : IReferencesAccessor
    {
        public NotecardInventory Inventory;
        public string Text = string.Empty;

        #region Constructors
        public Notecard()
        {

        }

        public Notecard(AssetData asset)
        {
            using(Stream assetdata = asset.InputStream)
            {
                string line = ReadLine(assetdata);
                string[] versioninfo = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
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
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public List<UUID> References
        {
            get
            {
                List<UUID> reflist = new List<UUID>();
                foreach(NotecardInventoryItem item in Inventory.Values)
                {
                    if (!reflist.Contains(item.AssetID))
                    {
                        reflist.Add(item.AssetID);
                    }
                }

                if(Text.StartsWith("<llsd>"))
                {
                    byte[] d = Text.ToUTF8Bytes();
                    /* could be an agent appearance notecard, so let us try that */
                    Map im;
                    using (Stream i = new MemoryStream(d))
                    {
                        im = LlsdXml.Deserialize(i) as Map;
                    }
                    if (null != im &&
                        im.ContainsKey("serial") && 
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
                                foreach (IValue w in (AnArray)im["wearables"])
                                {
                                    AnArray aw = w as AnArray;
                                    if (aw != null)
                                    {
                                        foreach (IValue wi in aw)
                                        {
                                            Map awm = wi as Map;
                                            if (null != awm)
                                            {
                                                try
                                                {
                                                    UUID assetID = awm["asset"].AsUUID;
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
                            AnArray attarray = im["attachments"] as AnArray;
                            if (attarray != null)
                            {
                                foreach (IValue a in attarray)
                                {
                                    Map am = a as Map;
                                    if (null != am)
                                    {
                                        try
                                        {
                                            UUID assetID = am["asset"].AsUUID;
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
                return reflist;
            }
        }
        #endregion

        #region Notecard Parser

        string ReadLine(Stream stream)
        {
            int c;
            string data = string.Empty;

            while('\n' != (c = stream.ReadByte()) && c != -1)
            {
                data += ((char)c).ToString();
            }

            data = data.Trim();
            return data;
        }

        private void ReadInventoryPermissions(Stream assetdata, ref NotecardInventoryItem item)
        {
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                string line = ReadLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                string[] data = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
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

        void ReadInventorySaleInfo(Stream assetdata, ref NotecardInventoryItem item)
        {
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                string line = ReadLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                string[] data = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
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

        NotecardInventoryItem ReadInventoryItem(Stream assetdata)
        {
            NotecardInventoryItem item = new NotecardInventoryItem();
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                string line = ReadLine(assetdata);
                if(line == "}")
                {
                    return item;
                }

                string[] data = line.Split(new char[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
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

        NotecardInventoryItem ReadInventoryItems(Stream assetdata)
        {
            NotecardInventoryItem item = null;
            uint extcharindex = 0;
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                string line = ReadLine(assetdata);
                if(line == "}")
                {
                    if(item == null)
                    {
                        throw new NotANotecardFormatException();
                    }
                    return item;
                }

                string[] data = line.Split(new char[] {'\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
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

        void ReadInventory(Stream assetdata)
        {
            Inventory = new NotecardInventory();
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                string line = ReadLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                string[] data = line.Split(new char[] { ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if(data[0] == "count")
                {
                    uint count;
                    if (!uint.TryParse(data[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out count))
                    {
                        throw new NotANotecardFormatException();
                    }
                    for (uint i = 0; i < count; ++i)
                    {
                        NotecardInventoryItem item = ReadInventoryItems(assetdata);
                        Inventory.Add(item.ID, item);
                    }
                }
                else
                {
                    throw new NotANotecardFormatException();
                }
            }
        }

        void ReadNotecard(Stream assetdata)
        {
            if(ReadLine(assetdata) != "{")
            {
                throw new NotANotecardFormatException();
            }
            while(true)
            {
                string line = ReadLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                string[] data = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
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
                    byte[] buffer = new byte[datalen];
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

        [SuppressMessage("Gendarme.Rules.Correctness", "ProvideCorrectArgumentsToFormattingMethodsRule")] /* gendarme does not catch all */
        static string ItemToString(NotecardInventoryItem item)
        {
            return string.Format(ItemFormatString,
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
        }

        public AssetData Asset()
        {
            return (AssetData)this;
        }

        public static implicit operator AssetData(Notecard v)
        {
            string notecard = "Linden text 2\n{\n";
            NotecardInventory inventory = v.Inventory;
            if(inventory != null)
            {
                notecard += String.Format("LLEmbeddedItems\n{{\ncount {0}\n", v.Inventory.Count);

                foreach(NotecardInventoryItem item in inventory.Values)
                {
                    notecard += ItemToString(item);
                }

                notecard += "}\n";
            }
            byte[] TextData = v.Text.ToUTF8Bytes();
            notecard += String.Format("Text length {0}\n", TextData.Length);
            byte[] NotecardHeader = notecard.ToUTF8Bytes();

            AssetData asset = new AssetData();
            asset.Data = new byte[TextData.Length + NotecardHeader.Length + 2];
            Buffer.BlockCopy(NotecardHeader, 0, asset.Data, 0, NotecardHeader.Length);
            Buffer.BlockCopy(TextData, 0, asset.Data, NotecardHeader.Length, TextData.Length);
            asset.Data[NotecardHeader.Length + TextData.Length] = (byte)'}';
            asset.Data[NotecardHeader.Length + TextData.Length + 1] = (byte)'\n';

            asset.Type = AssetType.Notecard;
            asset.Name = "Notecard";

            return asset;
        }

        public static implicit operator string[](Notecard v)
        {
            return v.Text.Split('\n');
        }
        #endregion
    }
}
