// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Database.MySQL._Migration
{
    public interface IMigrationElement
    {
        string Sql(string tableName);
    }

    public class SqlTable : IMigrationElement
    {
        public string Name { get; private set; }
        public bool IsDynamicRowFormat { get; set; }
        
        public SqlTable(string name)
        {
            IsDynamicRowFormat = false;
            Name = name;
        }

        public string Sql(string tableName)
        {
            throw new NotSupportedException();
        }
    }

    public class PrimaryKeyInfo : IMigrationElement
    {
        public string[] FieldNames { get; private set; }
        
        public PrimaryKeyInfo(params string[] fieldNames)
        {
            FieldNames = fieldNames;
        }

        public string FieldSql()
        {
            List<string> fieldNames = new List<string>();
            foreach (string fName in FieldNames)
            {
                fieldNames.Add("`" + MySqlHelper.EscapeString(fName) + "`");
            }
            return "PRIMARY KEY(" + string.Join(",", fieldNames) + ")";
        }

        public string Sql(string tableName)
        {
            return "ALTER TABLE " + tableName + " ADD " + FieldSql() + ";";
        }
    }

    public class DropPrimaryKeyinfo : IMigrationElement
    {
        public DropPrimaryKeyinfo()
        {

        }

        public string Sql(string tableName)
        {
            return "ALTER TABLE " + tableName + " DROP PRIMARY KEY;";
        }
    }

    public class NamedKeyInfo : IMigrationElement
    {
        public bool IsUnique { get; set; }
        public string Name { get; private set; }
        public string[] FieldNames { get; private set; }

        public NamedKeyInfo(string name, params string[] fieldNames)
        {
            Name = name;
            FieldNames = fieldNames;
        }

        public string FieldSql()
        {
            List<string> fieldNames = new List<string>();
            foreach(string fName in FieldNames)
            {
                fieldNames.Add("`" + MySqlHelper.EscapeString(fName) + "`");
            }
            return "KEY `" + MySqlHelper.EscapeString(Name) + "` (" + string.Join(",", fieldNames) + ")";
        }
        public string Sql(string tableName)
        {
            return "ALTER TABLE " + tableName + " ADD " + FieldSql() + ";";
        }

    }

    public class DropNamedKeyInfo : IMigrationElement
    {
        public string Name { get; private set; }

        public DropNamedKeyInfo(string name)
        {
            Name = name;
        }

        public string Sql(string tableName)
        {
            return "ALTER TABLE DROP KEY `" + MySqlHelper.EscapeString(Name) + "`;";
        }
    }

    #region Table fields
    public interface IColumnInfo
    {
        string Name { get; }
        Type FieldType { get; }
        uint Cardinality { get; }
        bool IsNullAllowed { get; }
        bool IsLong { get; }
        object Default { get; }
        string FieldSql();
    }

    public interface IAddColumn : IColumnInfo
    {
        string Sql(string tableName);
    }

    static class ColumnGenerator
    {
        public static Dictionary<string, string> ColumnSql(this IColumnInfo colInfo)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string notNull = colInfo.IsNullAllowed ? string.Empty : "NOT NULL ";
            string typeSql;
            Type f = colInfo.FieldType;
            if (f == typeof(string))
            {
                typeSql = (colInfo.Cardinality == 0) ?
                    (colInfo.IsLong ? "LONGTEXT" : "TEXT") :
                    "VARCHAR(" + colInfo.Cardinality.ToString() + ")";
            }
            else if (f == typeof(UUI) || f == typeof(UGI))
            {
                typeSql = "VARCHAR(255)";
            }
            else if (f == typeof(UUID))
            {
                typeSql = "CHAR(36)";
            }
            else if (f == typeof(double))
            {
                typeSql = "DOUBLE";
            }
            else if (f == typeof(int) || MySQLUtilities.MySqlSignedTypes.Contains(f))
            {
                typeSql = "INT";
            }
            else if (f == typeof(uint) || MySQLUtilities.MySqlUnsignedTypes.Contains(f))
            {
                typeSql = "INT UNSIGNED";
            }
            else if (f == typeof(bool))
            {
                typeSql = "INT(1) UNSIGNED";
            }
            else if (f == typeof(long))
            {
                typeSql = "BIGINT";
            }
            else if (f == typeof(ulong) || f == typeof(Date))
            {
                typeSql = "BIGINT UNSIGNED";
            }
            else if (f == typeof(Vector3))
            {
                if (colInfo.Default != null && !colInfo.IsNullAllowed)
                {
                    if (colInfo.Default.GetType() != f)
                    {
                        throw new ArgumentException("Default is not a Vector3 for field " + colInfo.Name);
                    }

                    Vector3 v = (Vector3)colInfo.Default;
                    result.Add(colInfo.Name + "X", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.X));
                    result.Add(colInfo.Name + "Y", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Y));
                    result.Add(colInfo.Name + "Z", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Z));
                }
                else
                {
                    result.Add(colInfo.Name + "X", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Y", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Z", "DOUBLE " + notNull);
                }
                return result;
            }
            else if (f == typeof(GridVector))
            {
                if (colInfo.Default != null && !colInfo.IsNullAllowed)
                {
                    if (colInfo.Default.GetType() != f)
                    {
                        throw new ArgumentException("Default is not a GridVector for field " + colInfo.Name);
                    }

                    GridVector v = (GridVector)colInfo.Default;
                    result.Add(colInfo.Name + "X", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.X));
                    result.Add(colInfo.Name + "Y", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Y));
                }
                else
                {
                    result.Add(colInfo.Name + "X", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Y", "DOUBLE " + notNull);
                }
                return result;
            }
            else if (f == typeof(Vector4))
            {
                if (colInfo.Default != null && !colInfo.IsNullAllowed)
                {
                    if (colInfo.Default.GetType() != f)
                    {
                        throw new ArgumentException("Default is not a Vector4 for field " + colInfo.Name);
                    }

                    Vector4 v = (Vector4)colInfo.Default;
                    result.Add(colInfo.Name + "X", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.X));
                    result.Add(colInfo.Name + "Y", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Y));
                    result.Add(colInfo.Name + "Z", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Z));
                    result.Add(colInfo.Name + "W", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.W));
                }
                else
                {
                    result.Add(colInfo.Name + "X", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Y", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Z", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "W", "DOUBLE " + notNull);
                }
                return result;
            }
            else if (f == typeof(Quaternion))
            {
                if (colInfo.Default != null && !colInfo.IsNullAllowed)
                {
                    if (colInfo.Default.GetType() != f)
                    {
                        throw new ArgumentException("Default is not a Quaternion for " + colInfo.Name);
                    }

                    Quaternion v = (Quaternion)colInfo.Default;
                    result.Add(colInfo.Name + "X", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.X));
                    result.Add(colInfo.Name + "Y", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Y));
                    result.Add(colInfo.Name + "Z", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Z));
                    result.Add(colInfo.Name + "W", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.W));
                }
                else
                {
                    result.Add(colInfo.Name + "X", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Y", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Z", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "W", "DOUBLE " + notNull);
                }
                return result;
            }
            else if (f == typeof(EnvironmentController.WLVector2))
            {
                if (colInfo.Default != null && !colInfo.IsNullAllowed)
                {
                    if (colInfo.Default.GetType() != f)
                    {
                        throw new ArgumentException("Default is not a EnvironmentController.WLVector4 for field " + colInfo.Name);
                    }

                    EnvironmentController.WLVector2 v = (EnvironmentController.WLVector2)colInfo.Default;
                    result.Add(colInfo.Name + "X", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.X));
                    result.Add(colInfo.Name + "Y", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Y));
                }
                else
                {
                    result.Add(colInfo.Name + "X", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Y", "DOUBLE " + notNull);
                }
                return result;
            }
            else if (f == typeof(EnvironmentController.WLVector4))
            {
                if (colInfo.Default != null && !colInfo.IsNullAllowed)
                {
                    if (colInfo.Default.GetType() != f)
                    {
                        throw new ArgumentException("Default is not a EnvironmentController.WLVector4 for field " + colInfo.Name);
                    }

                    EnvironmentController.WLVector4 v = (EnvironmentController.WLVector4)colInfo.Default;
                    result.Add(colInfo.Name + "Red", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.X));
                    result.Add(colInfo.Name + "Green", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Y));
                    result.Add(colInfo.Name + "Blue", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.Z));
                    result.Add(colInfo.Name + "Value", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.W));
                }
                else
                {
                    result.Add(colInfo.Name + "Red", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Green", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Blue", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Value", "DOUBLE " + notNull);
                }
                return result;
            }
            else if (f == typeof(Color))
            {
                if (colInfo.Default != null && !colInfo.IsNullAllowed)
                {
                    if (colInfo.Default.GetType() != f)
                    {
                        throw new ArgumentException("Default is not a Color for field " + colInfo.Name);
                    }

                    Color v = (Color)colInfo.Default;
                    result.Add(colInfo.Name + "Red", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.R));
                    result.Add(colInfo.Name + "Green", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.G));
                    result.Add(colInfo.Name + "Blue", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.B));
                }
                else
                {
                    result.Add(colInfo.Name + "Red", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Green", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Blue", "DOUBLE " + notNull);
                }
                return result;
            }
            else if (f == typeof(ColorAlpha))
            {
                if (colInfo.Default != null && !colInfo.IsNullAllowed)
                {
                    if (colInfo.Default.GetType() != f)
                    {
                        throw new ArgumentException("Default is not a ColorAlpha for field " + colInfo.Name);
                    }

                    ColorAlpha v = (ColorAlpha)colInfo.Default;
                    result.Add(colInfo.Name + "Red", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.R));
                    result.Add(colInfo.Name + "Green", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.G));
                    result.Add(colInfo.Name + "Blue", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.B));
                    result.Add(colInfo.Name + "Alpha", string.Format("DOUBLE {0} DEFAULT '{1}'", notNull, v.A));
                }
                else
                {
                    result.Add(colInfo.Name + "Red", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Green", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Blue", "DOUBLE " + notNull);
                    result.Add(colInfo.Name + "Alpha", "DOUBLE " + notNull);
                }
                return result;
            }
            else if (f == typeof(byte[]))
            {
                if(colInfo.Cardinality > 0)
                {
                    typeSql = "BINARY(" + colInfo.Cardinality.ToString() + ")";
                }
                else
                {
                    typeSql = colInfo.IsLong ? "LONGBLOB" : "BLOB";
                    notNull = string.Empty;
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("FieldType " + f.FullName +  " is not supported in field " + colInfo.Name);
            }

            if (colInfo.Default != null && !colInfo.IsNullAllowed)
            {
                if(colInfo.Default.GetType() != colInfo.FieldType &&
                    !(colInfo.Default.GetType() == typeof(UUID) &&
                    colInfo.FieldType == typeof(UUI)) &&
                    !(colInfo.Default.GetType() == typeof(UUID) &&
                    colInfo.FieldType == typeof(UGI)))
                {
                    throw new ArgumentOutOfRangeException("Default does not match expected type in field " + colInfo.Name + " target type=" + colInfo.FieldType.FullName + " defaultType=" + colInfo.Default.GetType().FullName);
                }

                object def = colInfo.Default;
                if(typeof(bool) == f)
                {
                    def = ((bool)def) ? 1 : 0;
                }
                else if(typeof(Date) == f)
                {
                    def = ((Date)def).AsULong;
                }
                else if (MySQLUtilities.MySqlSignedTypes.Contains(f))
                {
                    def = (int)Convert.ChangeType(def, typeof(int));
                }
                else if (MySQLUtilities.MySqlUnsignedTypes.Contains(f))
                {
                    def = (uint)Convert.ChangeType(def, typeof(uint));
                }
                result.Add(colInfo.Name, string.Format("{0} {1} DEFAULT '{2}'",
                    typeSql,
                    notNull,
                    MySqlHelper.EscapeString(def.ToString())));
            }
            else
            {
                result.Add(colInfo.Name, typeSql + " " + notNull);
            }
            return result;
        }
    }

    public class AddColumn<T> : IMigrationElement, IAddColumn
    {
        public string Name { get; private set; }

        public Type FieldType
        {
            get
            {
                return typeof(T);
            }
        }

        public uint Cardinality { get; set; }

        public bool IsNullAllowed { get; set; }
        public bool IsLong { get; set;  }

        public object Default { get; set; }

        public AddColumn(string name)
        {
            Name = name;
            IsLong = false;
            IsNullAllowed = true;
            Default = default(T);
        }

        public string FieldSql()
        {
            List<string> parts = new List<string>();
            foreach(KeyValuePair<string, string> kvp in this.ColumnSql())
            {
                parts.Add("`" + MySqlHelper.EscapeString(kvp.Key) + "` " + kvp.Value);
            }
            return string.Join(",", parts);
        }

        public string Sql(string tableName)
        {
            return string.Format("ALTER TABLE `{0}` ADD ({1})", MySqlHelper.EscapeString(tableName), FieldSql());
        }
    }

    public class DropColumn : IMigrationElement
    {
        public string Name { get; private set; }
        public DropColumn(string name)
        {
            Name = name;
        }

        public string Sql(string tableName)
        {
            throw new NotSupportedException();
        }

        public string Sql(string tableName, Type formerType)
        {
            string[] fieldNames = new string[] { Name };

            if (formerType == typeof(Vector3))
            {
                fieldNames = new string[]
                {
                    Name + "X", Name + "Y", Name + "Z"
                };
            }
            else if (formerType == typeof(GridVector))
            {
                fieldNames = new string[]
                {
                    Name + "X", Name + "Y"
                };
            }
            else if (formerType == typeof(Vector4) || formerType == typeof(Quaternion))
            {
                fieldNames = new string[]
                {
                    Name + "X", Name + "Y", Name + "Z", Name + "W"
                };
            }
            else if (formerType == typeof(EnvironmentController.WLVector4))
            {
                fieldNames = new string[]
                {
                    Name + "Red", Name + "Green", Name + "Blue", Name + "Value"
                };
            }
            else if (formerType == typeof(Color))
            {
                fieldNames = new string[]
                {
                    Name + "Red", Name + "Green", Name + "Blue"
                };
            }
            else if (formerType == typeof(ColorAlpha))
            {
                fieldNames = new string[]
                {
                    Name + "Red", Name + "Green", Name + "Blue", Name + "Alpha"
                };
            }

            for(int i = 0; i < fieldNames.Length; ++i)
            {
                fieldNames[i] = string.Format("DROP COLUMN `{0}`", MySqlHelper.EscapeString(fieldNames[i]));
            }
            return string.Format("ALTER TABLE `{0}` {1};", MySqlHelper.EscapeString(tableName), string.Join(",", fieldNames));
        }
    }

    public interface IChangeColumn : IColumnInfo
    {
        string Sql(string tableName, Type formerType);
        string OldName { get; }
    }

    class FormerFieldInfo : IColumnInfo
    {
        readonly IColumnInfo m_ColumnInfo;
        readonly Type m_OldFieldType;
        public FormerFieldInfo(IColumnInfo columnInfo, Type oldFieldType)
        {
            m_OldFieldType = oldFieldType;
            m_ColumnInfo = columnInfo;
        }

        public uint Cardinality { get { return 0; } }
        public object Default { get { return null; } }
        public Type FieldType { get { return m_OldFieldType; } }
        public bool IsNullAllowed { get { return true; } }
        public bool IsLong { get { return m_ColumnInfo.IsLong; } }

        public string Name { get { return m_ColumnInfo.Name; } }
        public string FieldSql()
        {
            throw new NotSupportedException();
        }
    }

    public class ChangeColumn<T> : IMigrationElement, IChangeColumn
    {
        public string Name { get; private set; }
        public string OldName { get; set; }
        public Type FieldType
        {
            get
            {
                return typeof(T);
            }
        }

        public bool IsNullAllowed { get; set; }
        public bool IsLong { get; set; }
        public uint Cardinality { get; set; }
        public object Default { get; set; }

        public ChangeColumn(string name)
        {
            Name = name;
            OldName = name;
        }

        public string FieldSql()
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, string> kvp in this.ColumnSql())
            {
                parts.Add("`" + MySqlHelper.EscapeString(kvp.Key) + "` " + kvp.Value);
            }
            return string.Join(",", parts);
        }

        public string Sql(string tableName)
        {
            throw new NotSupportedException();
        }

        public string Sql(string tableName, Type formerType)
        {
            FormerFieldInfo oldField = new FormerFieldInfo(this, formerType);
            string[] fieldNames = new string[] { Name };
            List<string> oldFields;
            Dictionary<string, string> newFields;

            oldFields = new List<string>(oldField.ColumnSql().Keys);
            newFields = this.ColumnSql();

            List<string> sqlParts = new List<string>();

            /* remove anything that is not needed anymore */
            foreach (string fieldName in oldFields)
            {
                if (!newFields.ContainsKey(fieldName))
                {
                    sqlParts.Add("DROP COLUMN `" + MySqlHelper.EscapeString(fieldName) + "`");
                }
            }

            foreach(KeyValuePair<string, string> kvp in newFields)
            {
                string sqlPart;
                if(oldFields.Contains(kvp.Key))
                {
                    string oldName = OldName + kvp.Key.Substring(Name.Length);
                    sqlPart = string.Format("CHANGE COLUMN `{0}` `{1}`", 
                        MySqlHelper.EscapeString(oldName),
                        MySqlHelper.EscapeString(kvp.Key));
                }
                else
                {
                    sqlPart = "ADD `" + MySqlHelper.EscapeString(kvp.Key) + "`";
                }
                sqlPart += " " + kvp.Value;
                sqlParts.Add(sqlPart);
            }

            return "ALTER TABLE `" + MySqlHelper.EscapeString(tableName) + "` " + string.Join(",", sqlParts) + ";";
        }
    }
    #endregion

    public class TableRevision : IMigrationElement
    {
        public uint Revision { get; private set; }

        public TableRevision(uint revision)
        {
            Revision = revision;
        }

        public string Sql(string tableName)
        {
            return string.Format("ALTER `{0}` COMMENT='{1}'", MySqlHelper.EscapeString(tableName), Revision);
        }
    }

    public class SqlStatement : IMigrationElement
    {
        public string Statement { get; private set; }

        public SqlStatement(string statement)
        {
            Statement = statement;
        }

        public string Sql(string tableName)
        {
            return Statement;
        }
    }
}
