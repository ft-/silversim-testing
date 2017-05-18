// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using System;
using System.Runtime.Serialization;

namespace SilverSim.Types.AuthInfo
{
    [Serializable]
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
            var actpassword = password.StartsWith("$1$") ?
                password.Substring(3) :
                password.ComputeMD5();
            var salted = (actpassword + ":" + PasswordSalt).ComputeMD5();

            if (salted != PasswordHash)
            {
                throw new PasswordAuthenticationFailedException("Could not authenticate your avatar. Please check your username and password, and check the grid if problems persist.");
            }

        }
    }
}