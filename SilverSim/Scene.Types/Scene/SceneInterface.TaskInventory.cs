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

using SilverSim.LL.Messages;
using SilverSim.LL.Messages.TaskInventory;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        [PacketHandler(MessageType.RequestTaskInventory)]
        void HandleRequestTaskInventory(Message m)
        {
            RequestTaskInventory req = (RequestTaskInventory)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            IAgent agent;
            try
            {
                agent = Agents[req.AgentID];
            }
            catch
            {
                return;
            }

            ObjectPart part;
            try
            {
                part = Primitives[req.LocalID];
            }
            catch
            {
                return;
            }

            List<ObjectPartInventoryItem> items = part.Inventory.Values;
            if(items.Count == 0)
            {
                ReplyTaskInventoryNone res = new ReplyTaskInventoryNone();
                res.Serial = (short)part.Inventory.InventorySerial;
                res.TaskID = part.ID;
                agent.SendMessageAlways(res, ID);
            }
            else
            {
                using(MemoryStream ms = new MemoryStream())
                {
                    using(TextWriter w = new StreamWriter(ms, UTF8NoBOM))
                    {
                        w.Write(InvFile_Header);
                        w.Write(string.Format(InvFile_NameValueLine, "obj_id", part.ID));
                        w.Write(string.Format(InvFile_NameValueLine, "parent_id", UUID.Zero));
                        w.Write(string.Format(InvFile_NameValueLine, "type", "category"));
                        w.Write(string.Format(InvFile_NameValueLine, "name", "Contents|"));
                        w.Write(InvFile_SectionEnd);
                        foreach (ObjectPartInventoryItem item in items)
                        {
                            w.Write(string.Format(
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
                                "\t}}\n", item.ID, part.ID,
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
                                            item.Flags,
                                            item.SaleInfo.TypeName,
                                            item.SaleInfo.Price,
                                            item.Name,
                                            item.Description,
                                            item.CreationDate.AsULong));
                        }
                    }

                    string fname = "inventory_" + UUID.Random.ToString() + ".tmp";
                    ulong xferid = agent.AddNewFile(fname, ms.GetBuffer());

                    ReplyTaskInventory res = new ReplyTaskInventory();
                    res.Serial = (short)part.Inventory.InventorySerial;
                    res.Filename = fname;
                    res.TaskID = part.ID;
                    agent.SendMessageAlways(res, ID);
                }
            }
        }

        const string InvFile_Header = "\tinv_object\t0\n\t{\n";
        const string InvFile_SectionEnd = "\t}\n";
        const string InvFile_NameValueLine = "\t\t{0}\t{1}\n";
    }
}
