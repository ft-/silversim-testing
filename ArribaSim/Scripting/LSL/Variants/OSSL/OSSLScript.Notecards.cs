/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Scene.Types.Object;
using ArribaSim.Scene.Types.Scene;
using ArribaSim.Types;
using ArribaSim.Types.Asset.Format;
using ArribaSim.Types.Asset;
using ArribaSim.Types.Inventory;
using System;
using System.Reflection;

namespace ArribaSim.Scripting.LSL.Variants.OSSL
{
    public partial class OSSLScript
    {
        public void osMakeNotecard(string notecardName, AnArray contents)
        {
            CheckThreatLevel(MethodBase.GetCurrentMethod().Name, ThreatLevelType.High);
            string nc = string.Empty;

            foreach(IValue val in contents)
            {
                if(!string.IsNullOrEmpty(nc))
                {
                    nc += "\n";
                }
                nc += val.ToString();
            }
            osMakeNotecard(notecardName, nc);
        }

        public void osMakeNotecard(string notecardName, string contents)
        {
            CheckThreatLevel(MethodBase.GetCurrentMethod().Name, ThreatLevelType.High);
            Notecard nc = new Notecard();
            nc.Text = contents;
            AssetData asset = nc;
            asset.ID = UUID.Random;
            asset.Name = notecardName;
            asset.Creator = Part.Group.Owner;
            asset.Description = "osMakeNotecard";
            Part.Group.Scene.AssetService.Store(asset);
            ObjectPartInventoryItem item = new ObjectPartInventoryItem(asset);
            item.ParentFolderID = Part.ID;

            for(uint i = 0; i < 1000; ++i)
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
                    Part.Inventory.Add(item.ID, item.Name, item);
                }
                catch
                {
                    return;
                }
            }
            throw new Exception(string.Format("Could not store notecard with name {0}", notecardName));
        }

        public string osGetNotecard(string name)
        {
            ObjectPartInventoryItem item;
            if (Part.Inventory.TryGetValue(name, out item))
            {
                if (item.InventoryType != InventoryType.Notecard)
                {
                    throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                }
                else
                {
                    Notecard nc = Part.Group.Scene.GetService<NotecardCache>()[item.AssetID];
                    return nc.Text;
                }
            }
            else
            {
                throw new Exception(string.Format("Inventory item {0} does not exist", name));
            }
        }

        public string osGetNotecardLine(string name, int line)
        {
            ObjectPartInventoryItem item;
            if (Part.Inventory.TryGetValue(name, out item))
            {
                if (item.InventoryType != InventoryType.Notecard)
                {
                    throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                }
                else
                {
                    Notecard nc = Part.Group.Scene.GetService<NotecardCache>()[item.AssetID];
                    string[] lines = nc.Text.Split('\n');
                    if(line >= lines.Length || line < 0)
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

        public int osGetNumberOfNotecardLines(string name)
        {
            ObjectPartInventoryItem item;
            if (Part.Inventory.TryGetValue(name, out item))
            {
                if (item.InventoryType != InventoryType.Notecard)
                {
                    throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                }
                else
                {
                    Notecard nc = Part.Group.Scene.GetService<NotecardCache>()[item.AssetID];
                    return nc.Text.Split('\n').Length;
                }
            }
            else
            {
                throw new Exception(string.Format("Inventory item {0} does not exist", name));
            }
        }
    }
}
