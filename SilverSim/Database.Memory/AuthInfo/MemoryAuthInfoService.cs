// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AuthInfo;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.AuthInfo;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.AuthInfo
{
    #region Service implementation
    public class MemoryAuthInfoService : AuthInfoServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        class AuthToken
        {
            public UUID UserID;
            public Date ValidUntil;

            public AuthToken(UUID userID, int lifetime_in_minutes)
            {
                ValidUntil = Date.UnixTimeToDateTime(Date.Now.AsULong + (ulong)lifetime_in_minutes * 60);
            }
        }

        readonly RwLockedDictionary<UUID, UserAuthInfo> m_AuthInfos = new RwLockedDictionary<UUID, UserAuthInfo>();
        readonly RwLockedDictionary<UUID, AuthToken> m_Tokens = new RwLockedDictionary<UUID, AuthToken>();

        public MemoryAuthInfoService()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            m_AuthInfos.Remove(accountID);
            List<UUID> tokenIds = new List<UUID>();
            foreach(KeyValuePair<UUID, AuthToken> kvp in m_Tokens)
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

        public override UUID AddToken(UUID principalId, int lifetime_in_minutes)
        {
            UUID newTokenId = UUID.Random;
            AuthToken tok = new AuthToken(principalId, lifetime_in_minutes);
            m_Tokens.Add(newTokenId, tok);
            return newTokenId;
        }

        public override void VerifyToken(UUID principalId, UUID token, int lifetime_extension_in_minutes)
        {
            AuthToken tok;
            ulong now = Date.Now.AsULong;
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
    }
    #endregion

    #region Factory
    [PluginName("AuthInfo")]
    public class MemoryAuthInfoServiceFactory : IPluginFactory
    {
        public MemoryAuthInfoServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryAuthInfoService();
        }
    }
    #endregion
}
