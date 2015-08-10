// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Scripting.LSL.API.Notecards
{
    public partial class Notecard_API
    {
        #region osMakeNotecard
        [APILevel(APIFlags.OSSL)]
        [LSLTooltip("Creates a notecard with text in the prim that contains the script. Contents can be either a list or a string.")]
        public void osMakeNotecard(
            ScriptInstance Instance, 
            [LSLTooltip("Name of notecard to be created")]
            string notecardName, 
            [LSLTooltip("Contents for the notecard. string is also allowed here.")]
            AnArray contents)
        {
            string nc = string.Empty;

            foreach(IValue val in contents)
            {
                if(!string.IsNullOrEmpty(nc))
                {
                    nc += "\n";
                }
                nc += val.ToString();
            }
            makeNotecard(Instance, notecardName, nc);
        }

        public void makeNotecard(
            ScriptInstance Instance, 
            string notecardName, 
            string contents)
        {
            lock (Instance)
            {
                Notecard nc = new Notecard();
                nc.Text = contents;
                AssetData asset = nc;
                asset.ID = UUID.Random;
                asset.Name = notecardName;
                asset.Creator = Instance.Part.ObjectGroup.Owner;
                Instance.Part.ObjectGroup.Scene.AssetService.Store(asset);
                ObjectPartInventoryItem item = new ObjectPartInventoryItem(asset);
                item.ParentFolderID = Instance.Part.ID;

                for (uint i = 0; i < 1000; ++i)
                {
                    if (i == 0)
                    {
                        item.Name = notecardName;
                    }
                    else
                    {
                        item.Name = string.Format("{0} {1}", notecardName, i);
                    }
                    try
                    {
                        Instance.Part.Inventory.Add(item.ID, item.Name, item);
                    }
                    catch
                    {
                        return;
                    }
                }
            }
            throw new Exception(string.Format("Could not store notecard with name {0}", notecardName));
        }
        #endregion

        #region osGetNotecard
        [APILevel(APIFlags.OSSL)]
        [LSLTooltip("read the entire contents of a notecard directly.\nIt does not use the dataserver event.")]
        public string osGetNotecard(
            ScriptInstance Instance, 
            [LSLTooltip("name of notecard in inventory")]
            string name)
        {
            lock (Instance)
            {
                ObjectPartInventoryItem item;
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        Notecard nc = Instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[item.AssetID];
                        return nc.Text;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion

        #region osGetNotecardLine
        [APILevel(APIFlags.OSSL)]
        [LSLTooltip("read a line of a notecard directly.\nIt does not use the dataserver event.")]
        public string osGetNotecardLine(
            ScriptInstance Instance, 
            [LSLTooltip("name of notecard in inventory")]
            string name, 
            [LSLTooltip("line number (starting at 0)")]
            int line)
        {
            ObjectPartInventoryItem item;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        Notecard nc = Instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[item.AssetID];
                        string[] lines = nc.Text.Split('\n');
                        if (line >= lines.Length || line < 0)
                        {
                            return EOF;
                        }
                        return lines[line];
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion

        #region osGetNumberOfNotecardLines
        [APILevel(APIFlags.OSSL)]
        [LSLTooltip("read number of lines of a notecard directly.\nIt does not use the dataserver event.")]
        public int osGetNumberOfNotecardLines(
            ScriptInstance Instance,
            [LSLTooltip("name of notecard in inventory")]
            string name)
        {
            ObjectPartInventoryItem item;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        Notecard nc = Instance.Part.ObjectGroup.Scene.GetService<NotecardCache>()[item.AssetID];
                        return nc.Text.Split('\n').Length;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion
    }
}
