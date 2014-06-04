using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Scene.ServiceInterfaces.SimulationData;
using ArribaSim.Types;
using ArribaSim.Scene.Types.Parcel;

using MySql.Data.MySqlClient;

namespace ArribaSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataParcelStorage : SimulationDataParcelStorageInterface
    {
        private string m_ConnectionString;

        public MySQLSimulationDataParcelStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override ParcelInfo this[UUID parcelID]
        {
            get
            {
                return null;
            }
        }

        public override List<UUID> ParcelsInRegion(UUID key)
        {
            return new List<UUID>();
        }

        public override void Store(ParcelInfo parcel)
        {

        }

    }
}
