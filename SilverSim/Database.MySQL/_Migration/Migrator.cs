// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using static SilverSim.Database.MySQL.MySQLUtilities;

namespace SilverSim.Database.MySQL._Migration
{
    public static class Migrator
    {
        static void ExecuteStatement(MySqlConnection conn, string command, ILog log)
        {
            using (MySqlCommand cmd = new MySqlCommand(command, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        static void CreateTable(
            this MySqlConnection conn, 
            SqlTable table,
            PrimaryKeyInfo primaryKey,
            Dictionary<string, IColumnInfo> fields,
            Dictionary<string, NamedKeyInfo> tableKeys,
            uint tableRevision,
            ILog log)
        {
            log.InfoFormat("Creating table '{0}' at revision {1}", table.Name, tableRevision);
            List<string> fieldSqls = new List<string>();
            foreach(IColumnInfo field in fields.Values)
            {
                fieldSqls.Add(field.FieldSql());
            }
            if(null != primaryKey)
            {
                fieldSqls.Add(primaryKey.FieldSql());
            }
            foreach(NamedKeyInfo key in tableKeys.Values)
            {
                fieldSqls.Add(key.FieldSql());
            }

            string cmd = "CREATE TABLE `" + MySqlHelper.EscapeString(table.Name) + "` (";
            cmd += string.Join(",", fieldSqls);
            cmd += ") COMMENT='" + tableRevision.ToString() + "'";
            if(table.IsDynamicRowFormat)
            {
                cmd += " ROW_FORMAT=DYNAMIC";
            }
            cmd += ";";
            ExecuteStatement(conn, cmd, log);
        }

        public static void MigrateTables(this MySqlConnection conn, IMigrationElement[] processTable, ILog log)
        {
            Dictionary<string, IColumnInfo> tableFields = new Dictionary<string, IColumnInfo>();
            PrimaryKeyInfo primaryKey = null;
            Dictionary<string, NamedKeyInfo> tableKeys = new Dictionary<string, NamedKeyInfo>();
            SqlTable table = null;
            uint processingTableRevision = 0;
            uint currentAtRevision = 0;
            bool insideTransaction = false;

            if(processTable.Length == 0)
            {
                throw new MySQLMigrationException("Invalid MySQL migration");
            }

            if(null == processTable[0] as SqlTable)
            {
                throw new MySQLMigrationException("First entry must be table name");
            }

            foreach (IMigrationElement migration in processTable)
            {
                Type migrationType = migration.GetType();

                if (typeof(SqlTable) == migrationType)
                {
                    if(insideTransaction)
                    {
                        ExecuteStatement(conn, string.Format("ALTER TABLE {0} COMMENT='{1}';", table.Name, processingTableRevision), log);
                        ExecuteStatement(conn, "COMMIT", log);
                        insideTransaction = false;
                    }

                    if (null != table && 0 != processingTableRevision)
                    {
                        if(currentAtRevision == 0)
                        {
                            conn.CreateTable(
                                table,
                                primaryKey,
                                tableFields,
                                tableKeys,
                                processingTableRevision,
                                log);
                        }
                        tableKeys.Clear();
                        tableFields.Clear();
                        primaryKey = null;
                    }
                    table = (SqlTable)migration;
                    currentAtRevision = conn.GetTableRevision(table.Name);
                    processingTableRevision = 1;
                }
                else if (typeof(TableRevision) == migrationType)
                {
                    if (insideTransaction)
                    {
                        ExecuteStatement(conn, string.Format("ALTER TABLE {0} COMMENT='{1}';", table.Name, processingTableRevision), log);
                        ExecuteStatement(conn, "COMMIT", log);
                        insideTransaction = false;
                        if (currentAtRevision != 0)
                        {
                            currentAtRevision = processingTableRevision;
                        }
                    }

                    TableRevision rev = (TableRevision)migration;
                    if(rev.Revision != processingTableRevision + 1)
                    {
                        throw new MySQLMigrationException(string.Format("Invalid TableRevision entry. Expected {0}. Got {1}", processingTableRevision + 1, rev.Revision));
                    }

                    processingTableRevision = rev.Revision;

                    if (processingTableRevision - 1 == currentAtRevision && 0 != currentAtRevision)
                    {
                        ExecuteStatement(conn, "BEGIN", log);
                        insideTransaction = true;
                        log.InfoFormat("Migration table '{0}' to revision {1}", table.Name, processingTableRevision);
                    }
                }
                else if (processingTableRevision == 0 || table == null)
                {
                    if (table != null)
                    {
                        throw new MySQLMigrationException("Unexpected processing element for " + table.Name);
                    }
                    else
                    {
                        throw new MySQLMigrationException("Unexpected processing element");
                    }
                }
                else
                {
                    Type[] interfaces = migration.GetType().GetInterfaces();

                    if(interfaces.Contains(typeof(IAddColumn)))
                    {
                        IAddColumn columnInfo = (IAddColumn)migration;
                        if(tableFields.ContainsKey(columnInfo.Name))
                        {
                            throw new ArgumentException("Column " + columnInfo.Name + " was added twice.");
                        }
                        tableFields.Add(columnInfo.Name, columnInfo);
                        if(insideTransaction)
                        {
                            ExecuteStatement(conn, columnInfo.Sql(table.Name), log);
                        }
                    }
                    else if(interfaces.Contains(typeof(IChangeColumn)))
                    {
                        IChangeColumn columnInfo = (IChangeColumn)migration;
                        IColumnInfo oldColumn;
                        if(!tableFields.TryGetValue(columnInfo.Name, out oldColumn))
                        {
                            throw new ArgumentException("Change column for " + columnInfo.Name + " has no preceeding AddColumn");
                        }
                        if(insideTransaction)
                        {
                            ExecuteStatement(conn, columnInfo.Sql(table.Name, oldColumn.FieldType), log);
                        }
                        tableFields[columnInfo.Name] = columnInfo;
                    }
                    else if(migrationType == typeof(DropColumn))
                    {
                        DropColumn columnInfo = (DropColumn)migration;
                        if (insideTransaction)
                        {
                            ExecuteStatement(conn, columnInfo.Sql(table.Name), log);
                        }
                        tableFields.Remove(columnInfo.Name);
                    }
                    else if(migrationType == typeof(PrimaryKeyInfo))
                    {
                        if(null != primaryKey && insideTransaction)
                        {
                            ExecuteStatement(conn, "ALTER TABLE `" + MySqlHelper.EscapeString(table.Name) + "` DROP PRIMARY KEY;", log);
                        }
                        primaryKey = (PrimaryKeyInfo)migration;
                        if (insideTransaction)
                        {
                            ExecuteStatement(conn, primaryKey.Sql(table.Name), log);
                        }
                    }
                    else if(migrationType == typeof(DropPrimaryKeyinfo))
                    {
                        if (null != primaryKey && insideTransaction)
                        {
                            ExecuteStatement(conn, ((DropPrimaryKeyinfo)migration).Sql(table.Name), log);
                        }
                        primaryKey = null;
                    }
                    else if(migrationType == typeof(NamedKeyInfo))
                    {
                        NamedKeyInfo namedKey = (NamedKeyInfo)migration;
                        tableKeys.Add(namedKey.Name, namedKey);
                        if (insideTransaction)
                        {
                            ExecuteStatement(conn, namedKey.Sql(table.Name), log);
                        }
                    }
                    else if(migrationType == typeof(DropNamedKeyInfo))
                    {
                        DropNamedKeyInfo namedKey = (DropNamedKeyInfo)migration;
                        tableKeys.Remove(namedKey.Name);
                        if (insideTransaction)
                        {
                            ExecuteStatement(conn, namedKey.Sql(table.Name), log);
                        }
                    }
                    else
                    {
                        throw new MySQLMigrationException("Invalid type " + migrationType.FullName + " in migration list");
                    }
                }
            }

            if (null != table && 0 != processingTableRevision)
            {
                if (currentAtRevision == 0)
                {
                    conn.CreateTable(
                        table,
                        primaryKey,
                        tableFields,
                        tableKeys,
                        processingTableRevision,
                        log);
                }
            }
        }
    }
}
