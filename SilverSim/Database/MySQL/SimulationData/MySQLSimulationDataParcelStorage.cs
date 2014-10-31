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

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataParcelStorage : SimulationDataParcelStorageInterface
    {
        private string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");

        public MySQLSimulationDataParcelStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override ParcelInfo this[UUID regionID, UUID parcelID]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ParcelID FROM parcels WHERE RegionID LIKE ?regionID AND ParcelID LIKE ?parcelID", connection))
                    {
                        cmd.Parameters.AddWithValue("?regionID", regionID);
                        cmd.Parameters.AddWithValue("?parcelID", parcelID);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (!dbReader.Read())
                            {
                                throw new KeyNotFoundException();
                            }

                            ParcelInfo pi = new ParcelInfo((int)dbReader["BitmapWidth"], (int)dbReader["BitmapHeight"]);
                            pi.Area = (int)dbReader["Area"];
                            pi.AuctionID = (uint)dbReader["AuctionID"];
                            pi.AuthBuyer = new UUI((string)dbReader["AuthBuyer"]);
                            pi.Category = (ParcelCategory)(uint)dbReader["Category"];
                            pi.ClaimDate = Date.UnixTimeToDateTime((ulong)dbReader["ClaimDate"]);
                            pi.ClaimPrice = (int)dbReader["ClaimPrice"];
                            pi.ID = (string)dbReader["ParcelID"];
                            pi.Group = new UGI((string)dbReader["Group"]);
                            pi.GroupOwned = MySQLUtilities.GetBoolean(dbReader, "IsGroupOwned");
                            pi.Description =(string)dbReader["Description"];
                            pi.Flags = (ParcelFlags)(uint)dbReader["Flags"];
                            pi.LandingType = (TeleportLandingType)(uint)dbReader["LandingType"];
                            pi.LandingPosition = MySQLUtilities.GetVector(dbReader, "LandingPosition");
                            pi.LandingLookAt = MySQLUtilities.GetVector(dbReader, "LandingLookAt");
                            pi.Name = (string)dbReader["Name"];
#if defs
        public ParcelStatus Status = ParcelStatus.Leased;
        public int LocalID = 0;
        public URI MusicURI = null;
        public URI MediaURI = null;
        public UUID MediaID;
        public UUI Owner = new UUI();
        public UUID SnapshotID = UUID.Zero;
        public Int32 SalePrice;
        public Int32 OtherCleanTime;
        public byte MediaAutoScale;
        public Int32 RentPrice = 0;
        public Vector3 AABBMin;
        public Vector3 AABBMax;
        public double ParcelPrimBonus;
        public Int32 PassPrice;
        public double PassHours;
        public Int32 ActualArea;
        public Int32 BillableArea;
        public double Dwell;

        internal byte[,] m_LandBitmap;
#endif
                            return pi;
                        }
                    }
                }
            }
        }

        public override List<UUID> ParcelsInRegion(UUID key)
        {
            List<UUID> parcels = new List<UUID>();
            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using(MySqlCommand cmd = new MySqlCommand("SELECT ParcelID FROM parcels WHERE RegionID LIKE ?regionID", connection))
                {
                    cmd.Parameters.AddWithValue("?regionID", key);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            parcels.Add(new UUID((string)dbReader["ParcelID"]));
                        }
                    }
                }
            }
            return parcels;
        }

        public override void Store(ParcelInfo parcel)
        {

        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        #region Migrations
        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "parcels", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ParcelID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "LocalID int(11) NOT NULL DEFAULT '0'," +
                "Bitmap LONGBLOB," +
                "BitmapWidth INT(11) NOT NULL DEFAULT '0'," +
                "BitmapHeight INT(11) NOT NULL DEFAULT '0'," +
                "`Name` VARCHAR(255) NOT NULL DEFAULT ''," +
                "Description TEXT," +
                "`Owner` VARCHAR(255) NOT NULL DEFAULT ''," +
                "IsGroupOwned TINYINT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "Area BIGINT(20) UNSIGNED NOT NULL," +
                "AuctionID INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "AuthBuyer VARCHAR(255) NOT NULL DEFAULT ''," +
                "Category INT(11) NOT NULL DEFAULT '0'," +
                "ClaimDate BIGINT(20) NOT NULL DEFAULT '0'," +
                "ClaimPrice INT(11) NOT NULL DEFAULT '0'," +
                "`Group` VARCHAR(255) NOT NULL DEFAULT ''," +
                "Flags INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "LandingType INT(11) NOT NULL DEFAULT '0'," +
                "LandingPositionX DOUBLE NOT NULL DEFAULT '0'," +
                "LandingPositionY DOUBLE NOT NULL DEFAULT '0'," +
                "LandingPositionZ DOUBLE NOT NULL DEFAULT '0'," +
                "LandingLookAtX DOUBLE NOT NULL DEFAULT '0'," +
                "LandingLookAtY DOUBLE NOT NULL DEFAULT '0'," +
                "LandingLookAtZ DOUBLE NOT NULL DEFAULT '0'," +
                "`Status` INT(11) NOT NULL DEFAULT '0'," +
                "MusicURI VARCHAR(255) NOT NULL DEFAULT ''," +
                "MediaURI VARCHAR(255) NOT NULL DEFAULT ''," +
                "MediaID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "SnapshotID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "SalePrice INT(11) NOT NULL DEFAULT '0'," +
                "OtherCleanTime INT(11) NOT NULL DEFAULT '0'," +
                "MediaAutoScale INT(11) NOT NULL DEFAULT '0'," +
                "RentPrice INT(11) NOT NULL DEFAULT '0'," +
                "AABBMinX DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMinY DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMinZ DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMaxX DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMaxY DOUBLE NOT NULL DEFAULT '0'," +
                "AABBMaxZ DOUBLE NOT NULL DEFAULT '0'," +
                "ParcelPrimBonus DOUBLE NOT NULL DEFAULT '1'," +
                "PassPrice INT(11) NOT NULL DEFAULT '0'," +
                "PassHours DOUBLE NOT NULL DEFAULT '0'," +
                "ActualArea INT(11) NOT NULL DEFAULT '0'," +
                "BillableArea INT(11) NOT NULL DEFAULT '0'," +
                "PRIMARY KEY(RegionID, ParcelID)," +
                "KEY ParcelNames (RegionID, `Name`)," +
                "UNIQUE KEY LocalIDs (RegionID, LocalID))"
        };
        #endregion
    }
}
