// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Inventory
{
    class MySQLInventoryFolderService : InventoryFolderServiceInterface
    {
        string m_ConnectionString;

        public MySQLInventoryFolderService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override InventoryFolder this[UUID key]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryfolders WHERE ID LIKE ?folderid", connection))
                    {
                        cmd.Parameters.AddWithValue("?folderid", key);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return dbReader.ToFolder();
                            }
                        }
                    }
                }

                throw new InventoryFolderNotFound(key);
            }
        }

        public override InventoryFolder this[UUID PrincipalID, UUID key]
        {
            get 
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryfolders WHERE OwnerID LIKE ?ownerid AND ID LIKE ?folderid", connection))
                    {
                        cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                        cmd.Parameters.AddWithValue("?folderid", key);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return dbReader.ToFolder();
                            }
                        }
                    }
                }

                throw new InventoryFolderNotFound(key);
            }
        }

        public override InventoryFolder this[UUID PrincipalID, AssetType type]
        {
            get 
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    if (type == AssetType.RootFolder)
                    {
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryfolders WHERE OwnerID LIKE ?ownerid AND ParentFolderID = ?parentfolderid", connection))
                        {
                            cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                            cmd.Parameters.AddWithValue("?parentfolderid", UUID.Zero);
                            using (MySqlDataReader dbReader = cmd.ExecuteReader())
                            {
                                if (dbReader.Read())
                                {
                                    return dbReader.ToFolder();
                                }
                            }
                        }
                    }
                    else
                    {
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryfolders WHERE OwnerID LIKE ?ownerid AND InventoryType = ?type", connection))
                        {
                            cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                            cmd.Parameters.AddWithValue("?type", (int)type);
                            using (MySqlDataReader dbReader = cmd.ExecuteReader())
                            {
                                if (dbReader.Read())
                                {
                                    return dbReader.ToFolder();
                                }
                            }
                        }
                    }
                }

                throw new InventoryFolderTypeNotFound(type);
            }
        }

        public override List<InventoryFolder> getFolders(UUID PrincipalID, UUID key)
        {
            List<InventoryFolder> folders = new List<InventoryFolder>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryfolders WHERE OwnerID LIKE ?ownerid AND ParentFolderID LIKE ?folderid", connection))
                {
                    cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                    cmd.Parameters.AddWithValue("?folderid", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while(dbReader.Read())
                        {
                            folders.Add(dbReader.ToFolder());
                        }
                    }
                }
            }

            return folders;
        }

        public virtual new List<InventoryFolder> getInventorySkeleton(UUID PrincipalID)
        {
            List<InventoryFolder> folders = new List<InventoryFolder>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryfolders WHERE OwnerID LIKE ?ownerid", connection))
                {
                    cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            folders.Add(dbReader.ToFolder());
                        }
                    }
                }
            }

            return folders;
        }

        public override List<InventoryItem> getItems(UUID PrincipalID, UUID key)
        {
            List<InventoryItem> items = new List<InventoryItem>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND ParentFolderID LIKE ?folderid", connection))
                {
                    cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                    cmd.Parameters.AddWithValue("?folderid", key);
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

        public override void Add(InventoryFolder folder)
        {
            Dictionary<string, object> newVals = new Dictionary<string, object>();
            newVals["ID"] = folder.ID;
            newVals["ParentFolderID"] = folder.ParentFolderID;
            newVals["OwnerID"] = folder.Owner.ID;
            newVals["Name"] = folder.Name;
            newVals["InventoryType"] = (int)folder.InventoryType;
            newVals["Version"] = folder.Version;

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                try
                {
                    connection.InsertInto("inventoryfolders", newVals);
                }
                catch
                {
                    throw new InventoryFolderNotStored(folder.ID);
                }
            }

            if (folder.ParentFolderID != UUID.Zero)
            {
                IncrementVersionNoExcept(folder.Owner.ID, folder.ParentFolderID);
            }
        }

        public override void Update(InventoryFolder folder)
        {
            Dictionary<string, object> newVals = new Dictionary<string, object>();
            newVals["Version"] = folder.Version;
            newVals["Name"] = folder.Name;
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                try
                {
                    connection.UpdateSet("inventoryfolders", newVals, string.Format("OwnerID LIKE '{0}' AND ID LIKE '{1}'", folder.Owner.ID, folder.ID));
                }
                catch
                {
                    throw new InventoryFolderNotStored(folder.ID);
                }
            }
            IncrementVersionNoExcept(folder.Owner.ID, folder.ParentFolderID);
        }

        public override void Move(UUID PrincipalID, UUID folderID, UUID toFolderID)
        {
            InventoryFolder thisfolder = this[PrincipalID, folderID];
            if(folderID == toFolderID)
            {
                throw new ArgumentException();
            }
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand(string.Format("BEGIN; IF EXISTS (SELECT NULL FROM inventoryfolders WHERE ID LIKE '{0}')" +
                    "UPDATE inventoryfolders SET ParentFolderID = '{0}' WHERE ID = '{1}'; COMMIT", toFolderID, folderID),
                    connection))
                {
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new InventoryFolderNotStored(folderID);
                    }
                }
            }
            IncrementVersionNoExcept(PrincipalID, toFolderID);
            IncrementVersionNoExcept(PrincipalID, thisfolder.ParentFolderID);
        }

        #region Delete and Purge
        public override void Delete(UUID PrincipalID, UUID folderID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                PurgeOrDelete(PrincipalID, folderID, connection, true);
            }
        }

        public override void Purge(UUID PrincipalID, UUID folderID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                PurgeOrDelete(PrincipalID, folderID, connection, false);
            }
        }

        public override void Purge(UUID folderID)
        {
            InventoryFolder folder = this[folderID];
            Purge(folder.Owner.ID, folderID);
        }

        void PurgeOrDelete(UUID PrincipalID, UUID folderID, MySqlConnection connection, bool deleteFolder)
        {
            List<UUID> folders;
            InventoryFolder thisfolder = this[PrincipalID, folderID];

            using (MySqlCommand cmd = new MySqlCommand("BEGIN", connection))
            {
                cmd.ExecuteNonQuery();
            }

            try
            {
                if (deleteFolder)
                {
                    folders = new List<UUID>();
                    folders.Add(folderID);
                }
                else
                {
                    folders = getFolderIDs(PrincipalID, folderID, connection);
                }

                for (int index = 0; index < folders.Count; ++index)
                {
                    foreach (UUID folder in getFolderIDs(PrincipalID, folders[index], connection))
                    {
                        if (!folders.Contains(folder))
                        {
                            folders.Insert(0, folder);
                        }
                    }
                }

                foreach (UUID folder in folders)
                {
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND ID LIKE ?folderid", connection))
                    {
                        cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                        cmd.Parameters.AddWithValue("?folderid", folderID);
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch
                        {
                        }
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM inventoryfolders WHERE OwnerID LIKE ?ownerid AND ParentFolderID LIKE ?folderid", connection))
                    {
                        cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                        cmd.Parameters.AddWithValue("?folderid", folderID);
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch
                        {
                        }
                    }
                }

                using (MySqlCommand cmd = new MySqlCommand("UPDATE inventoryfolders SET Version = Version + 1 WHERE OwnerID LIKE ?ownerid AND ID LIKE ?folderid", connection))
                {
                    cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                    if (deleteFolder)
                    {
                        cmd.Parameters.AddWithValue("?folderid", thisfolder.ParentFolderID);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("?folderid", folderID);
                    }
                }

                using (MySqlCommand cmd = new MySqlCommand("COMMIT", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                using (MySqlCommand cmd = new MySqlCommand("ROLLBACK", connection))
                {
                    cmd.ExecuteNonQuery();
                }
                throw;
            }
        }

        List<UUID> getFolderIDs(UUID PrincipalID, UUID key, MySqlConnection connection)
        {
            List<UUID> folders = new List<UUID>();
            using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM inventoryfolders WHERE OwnerID LIKE ?ownerid AND ParentFolderID LIKE ?folderid", connection))
            {
                cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                cmd.Parameters.AddWithValue("?folderid", key);
                using (MySqlDataReader dbReader = cmd.ExecuteReader())
                {
                    while (dbReader.Read())
                    {
                        folders.Add(dbReader.GetUUID("ID"));
                    }
                }
            }

            return folders;
        }

        #endregion

        public override void IncrementVersion(UUID PrincipalID, UUID folderID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE inventoryfolders SET Version = Version + 1 WHERE OwnerID LIKE ?ownerid AND ID LIKE ?folderid", connection))
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

        void IncrementVersionNoExcept(UUID PrincipalID, UUID folderID)
        {
            try
            {
                IncrementVersion(PrincipalID, folderID);
            }
            catch
            {

            }
        }
    }
}
