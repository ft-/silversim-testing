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
                PasswordSalt = CalcMD5(UUID.Random.ToString());
                PasswordHash = CalcMD5(CalcMD5(value) + ":" + PasswordSalt);
            }
        }

        static string CalcMD5(string data)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(UTF8NoBOM.GetBytes(data)).ToHexString().ToLower();
            }
        }

        static readonly UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}