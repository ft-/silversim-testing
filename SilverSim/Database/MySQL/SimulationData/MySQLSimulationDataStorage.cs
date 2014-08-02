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

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.ServiceInterfaces.Database;
using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using System.Reflection;

namespace SilverSim.Database.MySQL.SimulationData
{
    #region Service Implementation
    public class MySQLSimulationDataStorage : SimulationDataStorageInterface, IDBServiceInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string m_ConnectionString;
        private MySQLSimulationDataObjectStorage m_ObjectStorage;
        private MySQLSimulationDataParcelStorage m_ParcelStorage;

        #region Constructor
        public MySQLSimulationDataStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_ObjectStorage = new MySQLSimulationDataObjectStorage(connectionString);
            m_ParcelStorage = new MySQLSimulationDataParcelStorage(connectionString);
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        #region Properties
        public override SimulationDataObjectStorageInterface Objects
        {
            get
            {
                return m_ObjectStorage;
            }
        }

        public override SimulationDataParcelStorageInterface Parcels
        {
            get
            {
                return m_ParcelStorage;
            }
        }
        #endregion

        #region IDBServiceInterface
        public void VerifyConnection()
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
            }
        }

        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "primitems", MigrationsPrimitems, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "prims", MigrationsPrims, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "parcels", MigrationsParcels, m_Log);
        }

        private static readonly string[] MigrationsPrims =
        {

        };

        public static readonly string[] MigrationsPrimitems =
        {
            "CREATE TABLE %tablename% (" +
                    "InventoryID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                    "InventoryType INT(11) NOT NULL," +
                    "Name VARCHAR(64) NOT NULL," +
                    "Owner VARCHAR(255) NOT NULL," +
                    "AssetID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                    "AssetType INT(11) NOT NULL," + 
                    "CreationDate BIGINT(20) NOT NULL," +
                    "Creator VARCHAR(255)," +
                    "Description VARCHAR(255)," + 
                    "Flags INT(11) NOT NULL," +
                    "GroupID VARCHAR(255) NOT NULL," +
                    "GroupOwned INT(1) NOT NULL DEFAULT '0'," +
                    "ParentFolderID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," + 
                    "BasePermissions INT(11) NOT NULL UNSIGNED DEFAULT '0'," +
                    "CurrentPermissions INT(11) NOT NULL UNSIGNED DEFAULT '0'," +
                    "EveryOnePermissions INT(11) NOT NULL UNSIGNED DEFAULT '0'," +
                    "GroupPermissions INT(11) NOT NULL UNSIGNED DEFAULT '0'," +
                    "NextOwnerPermissions INT(11) NOT NULL UNSIGNED DEFAULT '0'," +
                    "SaleType INT(1) NOT NULL DEFAULT '0'," +
                    "SalePrice INT(11) NOT NULL DEFAULT '0'," +
                    "SalePermMask INT(11) NOT NULL UNSIGNED DEFAULT '0')"
        };

        private static readonly string[] MigrationsParcels =
        {

        };
        #endregion
    }
    #endregion

    #region Factory
    public class MySQLSimulationDataServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public MySQLSimulationDataServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLSimulationDataStorage(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
