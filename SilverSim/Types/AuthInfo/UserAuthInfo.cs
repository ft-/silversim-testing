// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace SilverSim.Types.AuthInfo
{
    public class PasswordAuthenticationFailedException : Exception
    {
        public PasswordAuthenticationFailedException()
        {

        }

        public PasswordAuthenticationFailedException(string message)
             : base(message)
        {

        }

        protected PasswordAuthenticationFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {

        }

        public PasswordAuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
        {

        }
    }


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

        public void CheckPassword(string password)
        {
            string actpassword;
            actpassword = password.StartsWith("$1$") ?
                password.Substring(3) :
                password.ComputeMD5();
            string salted = (actpassword + ":" + PasswordSalt).ComputeMD5();

            if (salted != PasswordHash)
            {
                throw new PasswordAuthenticationFailedException("Could not authenticate your avatar. Please check your username and password, and check the grid if problems persist.");
            }

        }
    }
}