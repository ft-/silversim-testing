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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public const string EOF = "\n\n\n";

        public UUID llGetNotecardLine(string name, int line)
        {
            lock (this)
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
                        UUID query = UUID.Random;

                        Notecard nc = Part.Group.Scene.GetService<NotecardCache>()[item.AssetID];
                        string[] lines = nc.Text.Split('\n');
                        DataserverEvent e = new DataserverEvent();
                        if (line >= lines.Length || line < 0)
                        {
                            e.Data = EOF;
                            e.QueryID = query;
                            Part.PostEvent(e);
                            return query;
                        }

                        e.Data = lines[line];
                        e.QueryID = query;
                        Part.PostEvent(e);
                        return query;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }

        public UUID llGetNumberOfNotecardLines(string name)
        {
            ObjectPartInventoryItem item;
            lock (this)
            {
                if (Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        UUID query = UUID.Random;
                        Notecard nc = Part.Group.Scene.GetService<NotecardCache>()[item.AssetID];
                        DataserverEvent e = new DataserverEvent();
                        e.Data = nc.Text.Split('\n').Length.ToString();
                        e.QueryID = query;
                        return query;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
    }
}
