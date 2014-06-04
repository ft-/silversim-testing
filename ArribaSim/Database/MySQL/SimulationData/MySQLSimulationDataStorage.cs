using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Scene.ServiceInterfaces.SimulationData;
using ArribaSim.ServiceInterfaces.Database;
using MySql.Data.MySqlClient;

namespace ArribaSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataStorage : SimulationDataStorageInterface, IDBServiceInterface
    {
        private string m_ConnectionString;
        private MySQLSimulationDataObjectStorage m_ObjectStorage;
        private MySQLSimulationDataParcelStorage m_ParcelStorage;

        public MySQLSimulationDataStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_ObjectStorage = new MySQLSimulationDataObjectStorage(connectionString);
            m_ParcelStorage = new MySQLSimulationDataParcelStorage(connectionString);
        }

        public void VerifyConnection()
        {
            using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
            }
        }

        public void ProcessMigrations()
        {

        }

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
    }
}
