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

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Inventory
{
    class MySQLInventoryItemService : InventoryItemServiceInterface
    {
        string m_ConnectionString;

        public MySQLInventoryItemService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override InventoryItem this[UUID PrincipalID, UUID key]
        {
            get 
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND ID LIKE ?itemid", connection))
                    {
                        cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                        cmd.Parameters.AddWithValue("?itemid", key);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return dbReader.ToItem();
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        public override void Add(InventoryItem item)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsertInto("inventoryitems", item.ToDictionary());
            }
            IncrementVersion(item.Owner.ID, item.ParentFolderID);
        }

        public override void Update(InventoryItem item)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                Dictionary<string, object> newVals = new Dictionary<string, object>();
                newVals["AssetID"] = item.AssetID;
                newVals["Name"] = item.Name;
                newVals["Description"] = item.Description;
                newVals["BasePermissionsMask"] = (uint)item.Permissions.Base;
                newVals["CurrentPermissionsMask"] = (uint)item.Permissions.Current;
                newVals["EveryOnePermissionsMask"] = (uint)item.Permissions.EveryOne;
                newVals["NextOwnerPermissionsMask"] = (uint)item.Permissions.NextOwner;
                newVals["GroupPermissionsMask"] = (uint)item.Permissions.Group;
                newVals["SalePrice"] = item.SaleInfo.Price;
                newVals["SaleType"] = (uint)item.SaleInfo.Type;
                connection.UpdateSet("inventoryitems", newVals, string.Format("OwnerID LIKE '{0}' AND ID LIKE '{1}'", item.Owner.ID, item.ID));
            }
            IncrementVersion(item.Owner.ID, item.ParentFolderID);
        }

        public override void Delete(UUID PrincipalID, UUID ID)
        {
            InventoryItem item = this[PrincipalID, ID];
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND ID LIKE ?itemid", connection))
                {
                    cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                    cmd.Parameters.AddWithValue("?itemid", ID);
                    if (1 > cmd.ExecuteNonQuery())
                    {
                        throw new InventoryItemNotFound(ID);
                    }
                }
            }
            IncrementVersion(PrincipalID, item.ID);
        }

        public override void Move(UUID PrincipalID, UUID ID, UUID toFolderID)
        {
            InventoryItem item = this[PrincipalID, ID];
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand(string.Format("BEGIN; IF EXISTS (SELECT NULL FROM inventoryfolders WHERE ID LIKE '{0}' AND OwnerID LIKE '{2}')" +
                    "UPDATE inventoryitems SET ParentFolderID = '{0}' WHERE ID = '{1}'; COMMIT", toFolderID, ID, PrincipalID),
                    connection))
                {
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new InventoryFolderNotStored(ID);
                    }
                }
            }
            IncrementVersion(PrincipalID, item.ParentFolderID);
            IncrementVersion(PrincipalID, toFolderID);
        }

        void IncrementVersion(UUID PrincipalID, UUID folderID)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("UDPATE inventoryfolders SET Version = Version + 1 WHERE OwnerID LIKE ?ownerid AND ID LIKE ?folderid", connection))
                    {
                        cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                        cmd.Parameters.AddWithValue("?folderid", folderID);
                        if (cmd.ExecuteNonQuery() < 1)
                        {
                            throw new InventoryFolderNotStored(folderID);
                        }
                    }
                }
            }
            catch
            {

            }
        }

    }
}
