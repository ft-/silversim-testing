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
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        [Serializable]
        public class BadHttpResponseException : Exception
        {
            public BadHttpResponseException()
            {
            }

            public BadHttpResponseException(string message)
                : base(message)
            {
            }

            protected BadHttpResponseException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public BadHttpResponseException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        [Serializable]
        public class HttpUnauthorizedException : HttpException
        {
            public string AuthenticationRealm { get; }
            public string AuthenticationType { get; }
            public Dictionary<string, string> AuthenticationParameters { get; }

            public HttpUnauthorizedException(string type, string realm, Dictionary<string, string> authparams) : base(401, "Not authorized")
            {
                AuthenticationType = type;
                AuthenticationRealm = realm;
                AuthenticationParameters = authparams;
            }

            public HttpUnauthorizedException()
            {
            }

            public HttpUnauthorizedException(string message) : base(message)
            {
            }

            public HttpUnauthorizedException(string message, int hr) : base(message, hr)
            {
            }

            public HttpUnauthorizedException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public HttpUnauthorizedException(int httpCode, string message, Exception innerException) : base(httpCode, message, innerException)
            {
            }

            public HttpUnauthorizedException(int httpCode, string message) : base(httpCode, message)
            {
            }

            public HttpUnauthorizedException(int httpCode, string message, int hr) : base(httpCode, message, hr)
            {
            }

            protected HttpUnauthorizedException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException(nameof(info));
                }
                AuthenticationRealm = info.GetString("AuthenticationRealm");
                AuthenticationType = info.GetString("AuthenticationType");
                AuthenticationParameters = (Dictionary<string, string>)info.GetValue("AuthenticationParameters", typeof(Dictionary<string, string>));
            }

            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("AuthenticationRealm", AuthenticationRealm);
                info.AddValue("AuthenticationType", AuthenticationType);
                info.AddValue("AuthenticationParameters", AuthenticationParameters);
            }
        }
    }
}
