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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AuthInfo;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.AuthInfo;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.Memory.AuthInfo
{
    #region Service implementation
    [Description("Memory AuthInfo backend")]
    public class MemoryAuthInfoService : AuthInfoServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        class AuthToken
        {
            public readonly UUID UserID;
            public readonly UUID SessionID;
            public Date ValidUntil;

            public AuthToken(UUID userID, UUID sessionID, int lifetime_in_minutes)
            {
                UserID = userID;
                SessionID = sessionID;
                ValidUntil = Date.UnixTimeToDateTime(Date.Now.AsULong + (ulong)lifetime_in_minutes * 60);
            }
        }

        readonly RwLockedDictionary<UUID, UserAuthInfo> m_AuthInfos = new RwLockedDictionary<UUID, UserAuthInfo>();
        readonly RwLockedDictionary<UUID, AuthToken> m_Tokens = new RwLockedDictionary<UUID, AuthToken>();

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            m_AuthInfos.Remove(accountID);
            var tokenIds = new List<UUID>();
            foreach(var kvp in m_Tokens)
            {
                if(kvp.Value.UserID == accountID)
                {
                    tokenIds.Add(kvp.Key);
                }
            }
            foreach(UUID id in tokenIds)
            {
                m_Tokens.Remove(id);
            }
        }

        public override UserAuthInfo this[UUID accountid]
        {
            get
            {
                return m_AuthInfos[accountid];
            }
        }

        public override void Store(UserAuthInfo info)
        {
            m_AuthInfos[info.ID] = info;
        }

        public override UUID AddToken(UUID principalId, UUID sessionid, int lifetime_in_minutes)
        {
            var newTokenId = UUID.Random;
            var tok = new AuthToken(principalId, sessionid, lifetime_in_minutes);
            m_Tokens.Add(newTokenId, tok);
            return newTokenId;
        }

        public override void VerifyToken(UUID principalId, UUID token, int lifetime_extension_in_minutes)
        {
            AuthToken tok;
            var now = Date.Now.AsULong;
            if (m_Tokens.TryGetValue(token, out tok) && tok.UserID == principalId)
            {
                if (tok.ValidUntil.AsULong >= now)
                {
                    tok.ValidUntil = Date.UnixTimeToDateTime(now + (ulong)lifetime_extension_in_minutes * 60);
                    m_Tokens[token] = tok;
                    return;
                }
                else
                {
                    m_Tokens.Remove(token);
                }
            }
            throw new VerifyTokenFailedException();
        }

        public override void ReleaseToken(UUID accountId, UUID secureSessionId)
        {
            m_Tokens.RemoveIf(secureSessionId, delegate (AuthToken tok) { return accountId == tok.UserID; });
        }

        public override void ReleaseTokenBySession(UUID accountId, UUID sessionId)
        {
            foreach(var kvp in m_Tokens)
            {
                if(kvp.Value.SessionID == sessionId && kvp.Value.UserID == accountId)
                {
                    m_Tokens.Remove(kvp.Key);
                    return;
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("AuthInfo")]
    public class MemoryAuthInfoServiceFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryAuthInfoService();
        }
    }
    #endregion
}
