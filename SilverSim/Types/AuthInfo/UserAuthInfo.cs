// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Security.Cryptography;
using System.Text;

namespace SilverSim.Types.AuthInfo
{
    public struct UserAuthInfo
    {
        public UUID ID;
        public string PasswordHash;
        public string PasswordSalt;

        public string Password
        {
            get
            {
                throw new NotSupportedException("Password");
            }
            set
            {
                PasswordSalt = UUID.Random.ToString().ComputeMD5();
                PasswordHash = (value.ComputeMD5() + ":" + PasswordSalt).ComputeMD5();
            }
        }
    }
}