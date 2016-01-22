// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;

namespace SilverSim.Database.MySQL.Inventory
{
    sealed class MySQLInventoryItemService : InventoryItemServiceInterface
    {
        readonly string m_ConnectionString;

        public MySQLInventoryItemService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override bool ContainsKey(UUID key)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM inventoryitems WHERE ID LIKE ?itemid", connection))
                {
                    cmd.Parameters.AddParameter("?itemid", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override bool TryGetValue(UUID key, out InventoryItem item)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryitems WHERE ID LIKE ?itemid", connection))
                {
                    cmd.Parameters.AddParameter("?itemid", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            item = dbReader.ToItem();
                            return true;
                        }
                    }
                }
            }

            item = default(InventoryItem);
            return false;
        }

        public override InventoryItem this[UUID key]
        {
            get
            {
                InventoryItem item;
                if(!TryGetValue(key, out item))
                {
                    throw new KeyNotFoundException();
                }
                return item;
            }
        }

        public override bool ContainsKey(UUID principalID, UUID key)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND ID LIKE ?itemid", connection))
                {
                    cmd.Parameters.AddParameter("?ownerid", principalID);
                    cmd.Parameters.AddParameter("?itemid", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override bool TryGetValue(UUID principalID, UUID key, out InventoryItem item)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND ID LIKE ?itemid", connection))
                {
                    cmd.Parameters.AddParameter("?ownerid", principalID);
                    cmd.Parameters.AddParameter("?itemid", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            item = dbReader.ToItem();
                            return true;
                        }
                    }
                }
            }

            item = default(InventoryItem);
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override InventoryItem this[UUID principalID, UUID key]
        {
            get 
            {
                InventoryItem item;
                if(!TryGetValue(principalID, key, out item))
                {
                    throw new KeyNotFoundException();
                }
                return item;
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
                newVals["AssetID"] = item.AssetID.ToString();
                newVals["Name"] = item.Name;
                newVals["Description"] = item.Description;
                newVals["BasePermissionsMask"] = item.Permissions.Base;
                newVals["CurrentPermissionsMask"] = item.Permissions.Current;
                newVals["EveryOnePermissionsMask"] = item.Permissions.EveryOne;
                newVals["NextOwnerPermissionsMask"] = item.Permissions.NextOwner;
                newVals["GroupPermissionsMask"] = item.Permissions.Group;
                newVals["SalePrice"] = item.SaleInfo.Price;
                newVals["SaleType"] = item.SaleInfo.Type;
                connection.UpdateSet("inventoryitems", newVals, string.Format("OwnerID LIKE '{0}' AND ID LIKE '{1}'", item.Owner.ID, item.ID));
            }
            IncrementVersion(item.Owner.ID, item.ParentFolderID);
        }

        public override void Delete(UUID principalID, UUID id)
        {
            InventoryItem item = this[principalID, id];
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND ID LIKE ?itemid", connection))
                {
                    cmd.Parameters.AddParameter("?ownerid", principalID);
                    cmd.Parameters.AddParameter("?itemid", id);
                    if (1 > cmd.ExecuteNonQuery())
                    {
                        throw new InventoryItemNotFoundException(id);
                    }
                }
            }
            IncrementVersion(principalID, item.ParentFolderID);
        }

        public override void Move(UUID principalID, UUID id, UUID toFolderID)
        {
            InventoryItem item = this[principalID, id];
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand(string.Format("BEGIN; IF EXISTS (SELECT NULL FROM inventoryfolders WHERE ID LIKE '{0}' AND OwnerID LIKE '{2}')" +
                    "UPDATE inventoryitems SET ParentFolderID = '{0}' WHERE ID = '{1}'; COMMIT", toFolderID, id, principalID),
                    connection))
                {
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new InventoryFolderNotStoredException(id);
                    }
                }
            }
            IncrementVersion(principalID, item.ParentFolderID);
            IncrementVersion(principalID, toFolderID);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void IncrementVersion(UUID principalID, UUID folderID)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("UPDATE inventoryfolders SET Version = Version + 1 WHERE OwnerID LIKE ?ownerid AND ID LIKE ?folderid", connection))
                    {
                        cmd.Parameters.AddParameter("?ownerid", principalID);
                        cmd.Parameters.AddParameter("?folderid", folderID);
                        if (cmd.ExecuteNonQuery() < 1)
                        {
                            throw new InventoryFolderNotStoredException(folderID);
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
