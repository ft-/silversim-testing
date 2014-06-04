using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace ArribaSim.Database.MySQL
{
    public static class MySQLUtilities
    {
        public static void ReplaceInsertInto(MySqlConnection connection, string tablename, Dictionary<string, object> vals)
        {
            string q1 = "REPLACE INSERT INTO ?tablename (";
            string q2 = ") VALUES (";
            bool first = true;
            foreach(KeyValuePair<string, object> val in vals)
            {
                if(!first)
                {
                    q1 += ",";
                    q2 += ",";
                }
                q1 += val.Key.ToString();
                q2 += "?" + val.Key.ToString();
            }
            using(MySqlCommand command = new MySqlCommand(q1 + q2 + ")", connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}
