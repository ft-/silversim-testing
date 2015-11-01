﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SilverSim.ServiceInterfaces.UserAgents
{
    public abstract class UserAgentServiceInterface
    {
        public struct UserInfo
        {
            public string FirstName;
            public string LastName;
            public uint UserFlags;
            public Date UserCreated;
            public string UserTitle;
        }

        public UserAgentServiceInterface()
        {

        }

        public abstract void VerifyAgent(UUID sessionID, string token);

        public abstract void VerifyClient(UUID sessionID, string token);

        public abstract List<UUID> NotifyStatus(List<KeyValuePair<UUI, string>> friends, UUI user, bool online);

        public abstract UserInfo GetUserInfo(UUI user);

        public abstract Dictionary<string, string> GetServerURLs(UUI user);

        public abstract string LocateUser(UUI user);

        public abstract UUI GetUUI(UUI user, UUI targetUserID);

        [Serializable]
        public class RequestFailedException : Exception
        {
            public RequestFailedException()
            {

            }

            public RequestFailedException(string message)
                : base(message)
            {

            }

            protected RequestFailedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public RequestFailedException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }
    }
}