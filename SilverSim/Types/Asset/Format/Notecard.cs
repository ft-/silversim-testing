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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SilverSim.Types.Inventory;

namespace SilverSim.Types.Asset.Format
{
    public class Notecard : IReferencesAccessor
    {
        public NotecardInventory Inventory = null;
        public string Text = string.Empty;

        #region Constructors
        public Notecard()
        {

        }

        public Notecard(AssetData asset)
        {
            using(MemoryStream assetdata = new MemoryStream(asset.Data))
            {
                string line = readLine(assetdata);
                string[] versioninfo = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if(versioninfo.Length < 2 || versioninfo[0] != "Linden" || versioninfo[1] != "text")
                {
                    /* Viewers handle notecards without this header as plain text notecard */
                    Text = Encoding.UTF8.GetString(asset.Data);
                    return;
                }
                readNotecard(assetdata);
            }
        }
        #endregion

        #region References
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
                return reflist;
            }
        }
        #endregion

        #region Notecard Parser

        private string readLine(MemoryStream stream)
        {
            int c;
            string data = string.Empty;

            while('\n' != (c = stream.ReadByte()) && c != -1)
            {
                data += (char)c;
            }

            data = data.Trim();
            return data;
        }

        private void readInventoryPermissions(MemoryStream assetdata, ref NotecardInventoryItem item)
        {
            if(readLine(assetdata) != "{")
            {
                throw new NotANotecardFormat();
            }
            while(true)
            {
                string line = readLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                string[] data = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if(data[0] == "base_mask")
                {
                        item.Permissions.Base = (InventoryItem.PermissionsMask)uint.Parse(data[1], NumberStyles.HexNumber);
                }
                else if(data[0] == "owner_mask")
                {
                    item.Permissions.Current = (InventoryItem.PermissionsMask)uint.Parse(data[1], NumberStyles.HexNumber);
                }
                else if(data[0] == "group_mask")
                {
                    item.Permissions.Group = (InventoryItem.PermissionsMask)uint.Parse(data[1], NumberStyles.HexNumber);
                }
                else if(data[0] == "everyone_mask")
                {
                    item.Permissions.EveryOne = (InventoryItem.PermissionsMask)uint.Parse(data[1], NumberStyles.HexNumber);
                }
                else if(data[0] == "next_owner_mask")
                {
                    item.Permissions.NextOwner = (InventoryItem.PermissionsMask)uint.Parse(data[1], NumberStyles.HexNumber);
                }
                else if(data[0] == "creator_id")
                {
                    item.Creator.ID = data[1];
                }
                else if(data[0] == "owner_id")
                {
                    item.Owner.ID = data[1];
                }
                else if(data[0] == "last_owner_id")
                {
                    item.LastOwner.ID = data[1];
                }
                else if(data[0] == "group_id")
                {
                    item.Group.ID = data[1];
                }
                else
                {
                    throw new NotANotecardFormat();
                }
            }
        }

        private void readInventorySaleInfo(MemoryStream assetdata, ref NotecardInventoryItem item)
        {
            if(readLine(assetdata) != "{")
            {
                throw new NotANotecardFormat();
            }
            while(true)
            {
                string line = readLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                string[] data = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if(data[0] == "sale_type")
                {
                    item.SaleInfo.TypeName = data[1];
                }
                else if(data[0] == "sale_price")
                {
                    item.SaleInfo.Price = int.Parse(data[1]);
                }
                else if(data[0] == "perm_mask")
                {
                    item.SaleInfo.PermMask = (InventoryItem.PermissionsMask)uint.Parse(data[1]);
                }
                else
                {
                    throw new NotANotecardFormat();
                }
            }
        }

        private NotecardInventoryItem readInventoryItem(MemoryStream assetdata)
        {
            NotecardInventoryItem item = new NotecardInventoryItem();
            if(readLine(assetdata) != "{")
            {
                throw new NotANotecardFormat();
            }
            while(true)
            {
                string line = readLine(assetdata);
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
                    readInventoryPermissions(assetdata, ref item);  
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
                    item.Flags = uint.Parse(data[1], NumberStyles.HexNumber);
                }
                else if(data[0] == "sale_info")   
                {
                    readInventorySaleInfo(assetdata, ref item);
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
                    item.CreationDate = Date.UnixTimeToDateTime(ulong.Parse(data[1]));
                }
                else
                {
                    throw new NotANotecardFormat();
                }       
            }
        }

        private NotecardInventoryItem readInventoryItems(MemoryStream assetdata)
        {
            NotecardInventoryItem item = null;
            uint extcharindex = 0;
            if(readLine(assetdata) != "{")
            {
                throw new NotANotecardFormat();
            }
            while(true)
            {
                string line = readLine(assetdata);
                if(line == "}")
                {
                    if(item == null)
                    {
                        throw new NotANotecardFormat();
                    }
                    return item;
                }

                string[] data = line.Split(new char[] {'\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if(data.Length == 4 && data[0] == "ext" && data[1] == "char" && data[2] == "index")
                {
                    extcharindex = uint.Parse(data[3]);
                }
                else if(data[0] == "inv_item")
                {
                    item = readInventoryItem(assetdata);
                    item.ExtCharIndex = extcharindex;
                }
                else
                {
                    throw new NotANotecardFormat();
                }
            }
        }

        private void readInventory(MemoryStream assetdata)
        {
            Inventory = new NotecardInventory();
            if(readLine(assetdata) != "{")
            {
                throw new NotANotecardFormat();
            }
            while(true)
            {
                string line = readLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                string[] data = line.Split(new char[] { ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                if(data[0] == "count")
                {
                    uint count = uint.Parse(data[1]);
                    for(uint i = 0; i < count; ++i)
                    {
                        NotecardInventoryItem item = readInventoryItems(assetdata);
                        Inventory.Add(item.ID, item);
                    }
                }
                else
                {
                    throw new NotANotecardFormat();
                }
            }
        }

        private void readNotecard(MemoryStream assetdata)
        {
            if(readLine(assetdata) != "{")
            {
                throw new NotANotecardFormat();
            }
            while(true)
            {
                string line = readLine(assetdata);
                if(line == "}")
                {
                    return;
                }

                string[] data = line.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                if(data[0] == "LLEmbeddedItems")
                {
                    readInventory(assetdata);
                }
                else if(data[0] == "Text" && data.Length == 3)
                {
                    int datalen = int.Parse(data[2]);
                    byte[] buffer = new byte[datalen];
                    if(datalen != assetdata.Read(buffer, 0, datalen))
                    {
                        throw new NotANotecardFormat();
                    }
                    Text = Encoding.UTF8.GetString(buffer);
                }
                else
                {
                    throw new NotANotecardFormat();
                }
            }
        }

        #endregion

        #region Operators
        private static readonly string ItemFormatString =
                "{\n"+
                "ext char index {0}\n" +
                "\tinv_item\t0\n" +
                "\t{\n" +
                "\t\titem_id\t{1}\n" +
                "\t\tparent_id\t{2}\n" +
                "\t\tpermissions 0\n" +
                "\t\t{\n" +
                "\t\t\tbase_mask\t{3:x08}\n" +
                "\t\t\towner_mask\t{4:x08}\n" +
                "\t\t\tgroup_mask\t{5:x08}\n" +
                "\t\t\teveryone_mask\t{6:x08}\n" +
                "\t\t\tnext_owner_mask\t{7:x08}\n" +
                "\t\t\tcreator_id\t{8}\n" +
                "\t\t\towner_id\t{9}\n" +
                "\t\t\tlast_owner_id\t{10}\n" +
                "\t\t\tgroup_id\t{11}\n" +
                "\t\t}\n" +
                "\t\tasset_id\t{12}\n" +
                "\t\ttype\t{13}\n" +
                "\t\tinv_type\t{14}\n" +
                "\t\tflags\t{15:x08}\n" +
                "\t\tsale_info 0\n" +
                "\t\t{\n" +
                "\t\t\tsale_type\t{16}\n" +
                "\t\t\tsale_price\t{17}\n" +
                "\t\t}\n" +
                "\t\tname\t{18}|\n" +
                "\t\tdesc\t{19}|\n" +
                "\t\tcreation_date\t{20\\n" +
                "\t}\n" +
                "}\n";

        private static string ItemToString(NotecardInventoryItem item)
        {
            return string.Format(ItemFormatString,
                item.ExtCharIndex,
                item.ID, item.ParentFolderID,
                item.Permissions.Base,
                item.Permissions.Current,
                item.Permissions.Group,
                item.Permissions.EveryOne,
                item.Permissions.NextOwner,
                item.Creator.ID,
                item.Owner.ID,
                item.LastOwner.ID,
                item.AssetID,
                item.AssetTypeName,
                item.InventoryTypeName,
                item.Flags,
                item.SaleInfo.Price,
                item.SaleInfo.TypeName,
                item.Name,
                item.Description,
                item.CreationDate.AsULong
                );
        }

        public static implicit operator AssetData(Notecard v)
        {
            string notecard = "Linden text 2\n{\n";
            NotecardInventory inventory = v.Inventory;
            if(inventory != null)
            {
                notecard += String.Format("LLEmbeddedItems\n{\ncount {0}\n", v.Inventory.Count);

                foreach(NotecardInventoryItem item in inventory.Values)
                {
                    notecard += ItemToString(item);
                }

                notecard += "}\n";
            }
            byte[] TextData = Encoding.UTF8.GetBytes(v.Text);
            notecard += String.Format("Text length {0}\n", TextData.Length);
            byte[] NotecardHeader = Encoding.UTF8.GetBytes(notecard);

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
