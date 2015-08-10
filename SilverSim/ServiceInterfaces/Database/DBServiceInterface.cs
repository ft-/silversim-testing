// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.ServiceInterfaces.Database
{
    public interface IDBServiceInterface
    {
        void VerifyConnection();
        void ProcessMigrations();
    }
}
