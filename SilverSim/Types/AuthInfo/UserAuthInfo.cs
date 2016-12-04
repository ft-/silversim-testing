// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.AuthInfo
{
    public struct UserAuthInfo
    {
        public UUID ID;
        public string PasswordHash;
        public string PasswordSalt;
    }
}