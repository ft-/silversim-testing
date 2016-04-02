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
    public partial class MySQLInventoryService : IInventoryItemServiceInterface
    {
        bool IInventoryItemServiceInterface.ContainsKey(UUID key)
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

        bool IInventoryItemServiceInterface.TryGetValue(UUID key, out InventoryItem item)
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

        InventoryItem IInventoryItemServiceInterface.this[UUID key]
        {
            get
            {
                InventoryItem item;
                if(!Item.TryGetValue(key, out item))
                {
                    throw new KeyNotFoundException();
                }
                return item;
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        List<InventoryItem> IInventoryItemServiceInterface.this[UUID principalID, List<UUID> itemids]
        {
            get
            {
                if(null == itemids || itemids.Count == 0)
                {
                    throw new ArgumentOutOfRangeException("itemids");
                }
                List<InventoryItem> items = new List<InventoryItem>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    List<string> matchStrings = new List<string>();
                    foreach(UUID itemid in itemids)
                    {
                        matchStrings.Add(string.Format("\"{0}\"", itemid.ToString()));
                    }
                    string qStr = string.Join(",", matchStrings);
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND ID IN (" + qStr + ")", connection))
                    {
                        cmd.Parameters.AddParameter("?ownerid", principalID);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while (dbReader.Read())
                            {
                                items.Add(dbReader.ToItem());
                            }
                        }
                    }
                }

                return items;
            }
        }

        bool IInventoryItemServiceInterface.ContainsKey(UUID principalID, UUID key)
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

        bool IInventoryItemServiceInterface.TryGetValue(UUID principalID, UUID key, out InventoryItem item)
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
        InventoryItem IInventoryItemServiceInterface.this[UUID principalID, UUID key]
        {
            get 
            {
                InventoryItem item;
                if(!Item.TryGetValue(principalID, key, out item))
                {
                    throw new KeyNotFoundException();
                }
                return item;
            }
        }

        void IInventoryItemServiceInterface.Add(InventoryItem item)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsertInto("inventoryitems", item.ToDictionary());
            }
            IncrementVersion(item.Owner.ID, item.ParentFolderID);
        }

        void IInventoryItemServiceInterface.Update(InventoryItem item)
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

        void IInventoryItemServiceInterface.Delete(UUID principalID, UUID id)
        {
            InventoryItem item = Item[principalID, id];
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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        List<UUID> IInventoryItemServiceInterface.Delete(UUID principalID, List<UUID> itemids)
        {
            List<UUID> deleted = new List<UUID>();
            foreach(UUID id in itemids)
            {
                try
                {
                    Item.Delete(principalID, id);
                    deleted.Add(id);
                }
                catch
                {
                    /* nothing else to do */
                }
            }
            return deleted;
        }

        void IInventoryItemServiceInterface.Move(UUID principalID, UUID id, UUID toFolderID)
        {
            InventoryItem item = Item[principalID, id];
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
                /* nothing to do here */
            }
        }

    }
}
