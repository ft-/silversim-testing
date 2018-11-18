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

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Groups.Common.Broker
{
    internal sealed partial class GroupsBrokerEntry : IGroupMembersInterface
    {
        private const int MemberCacheTimeout = 10;

        private class GroupMemberCache
        {
            public List<GroupMember> GroupMembers;
            public long ExpiryTickCount;
        }

        private readonly RwLockedDictionary<UGUI_UGI, GroupMemberCache> m_GroupMemberCache = new RwLockedDictionary<UGUI_UGI, GroupMemberCache>();

        private readonly RwLockedDictionary<UGUI_UGUI, GroupMemberCache> m_PrincipalGroupMemberCache = new RwLockedDictionary<UGUI_UGUI, GroupMemberCache>();

        private struct UGUI_UGI : IEquatable<UGUI_UGI>
        {
            public UGUI RequestingAgent;
            public UGI Group;

            public bool Equals(UGUI_UGI other)
            {
                return RequestingAgent == other.RequestingAgent && Group == other.Group;
            }

            public override int GetHashCode()
            {
                return RequestingAgent.GetHashCode() ^ Group.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if(obj is UGUI_UGI)
                {
                    return Equals((UGUI_UGI)obj);
                }
                return false;
            }
        }

        private struct UGUI_UGUI : IEquatable<UGUI_UGUI>
        {
            public UGUI RequestingAgent;
            public UGUI Principal;

            public bool Equals(UGUI_UGUI other)
            {
                return RequestingAgent == other.RequestingAgent && Principal == other.Principal;
            }

            public override int GetHashCode()
            {
                return RequestingAgent.GetHashCode() ^ Principal.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is UGUI_UGI)
                {
                    return Equals((UGUI_UGI)obj);
                }
                return false;
            }
        }

        List<GroupMember> IGroupMembersInterface.this[UGUI requestingAgent, UGI group]
        {
            get
            {
                GroupMemberCache cache;
                UGUI_UGI cacheId = new UGUI_UGI { RequestingAgent = requestingAgent, Group = group };
                if (m_GroupMemberCache.TryGetValue(cacheId, out cache) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, cache.ExpiryTickCount) < m_ClockSource.SecsToTicks(MemberCacheTimeout))
                {
                    return cache.GroupMembers;
                }

                cache = new GroupMemberCache
                {
                    GroupMembers = InnerGroupsService.Members[requestingAgent, group]
                };
                cache.ExpiryTickCount = m_ClockSource.TickCount;
                m_GroupMemberCache[cacheId] = cache;
                return cache.GroupMembers;
            }
        }

        List<GroupMember> IGroupMembersInterface.this[UGUI requestingAgent, UGUI principal]
        {
            get
            {
                GroupMemberCache cache;
                UGUI_UGUI cacheId = new UGUI_UGUI { RequestingAgent = requestingAgent, Principal = principal };
                if (m_PrincipalGroupMemberCache.TryGetValue(cacheId, out cache) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, cache.ExpiryTickCount) < m_ClockSource.SecsToTicks(MemberCacheTimeout))
                {
                    return cache.GroupMembers;
                }

                cache = new GroupMemberCache
                {
                    GroupMembers = InnerGroupsService.Members[requestingAgent, principal]
                };
                cache.ExpiryTickCount = m_ClockSource.TickCount;
                m_PrincipalGroupMemberCache[cacheId] = cache;
                return cache.GroupMembers;
            }
        }

        GroupMember IGroupMembersInterface.Add(UGUI requestingAgent, UGI group, UGUI principal, UUID roleID, string accessToken) =>
            InnerGroupsService.Members.Add(requestingAgent, group, principal, roleID, accessToken);

        bool IGroupMembersInterface.ContainsKey(UGUI requestingAgent, UGI group, UGUI principal)
        {
            GroupMember gmem;
            return Members.TryGetValue(requestingAgent, group, principal, out gmem);
        }

        void IGroupMembersInterface.Delete(UGUI requestingAgent, UGI group, UGUI principal)
        {
            Members.Delete(requestingAgent, group, principal);
            m_PrincipalGroupMemberCache.Remove(new UGUI_UGUI { RequestingAgent = requestingAgent, Principal = principal });
            m_GroupMemberCache.Remove(new UGUI_UGI { RequestingAgent = requestingAgent, Group = group });
        }

        void IGroupMembersInterface.SetContribution(UGUI requestingagent, UGI group, UGUI principal, int contribution)
        {
            Members.SetContribution(requestingagent, group, principal, contribution);
            m_PrincipalGroupMemberCache.Remove(new UGUI_UGUI { RequestingAgent = requestingagent, Principal = principal });
            m_GroupMemberCache.Remove(new UGUI_UGI { RequestingAgent = requestingagent, Group = group });
        }

        bool IGroupMembersInterface.TryGetValue(UGUI requestingAgent, UGI group, UGUI principal, out GroupMember gmem)
        {
            GroupMemberCache cache;
            UGUI_UGI groupCacheId = new UGUI_UGI { RequestingAgent = requestingAgent, Group = group };
            UGUI_UGUI principalCacheId = new UGUI_UGUI { RequestingAgent = requestingAgent, Principal = principal };

            if (m_PrincipalGroupMemberCache.TryGetValue(principalCacheId, out cache) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, cache.ExpiryTickCount) < m_ClockSource.SecsToTicks(MemberCacheTimeout))
            {
                foreach (GroupMember gmem_it in cache.GroupMembers)
                {
                    if (gmem_it.Group.Equals(group))
                    {
                        gmem = gmem_it;
                        return true;
                    }
                }
            }

            if (m_GroupMemberCache.TryGetValue(groupCacheId, out cache) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, cache.ExpiryTickCount) < m_ClockSource.SecsToTicks(MemberCacheTimeout))
            {
                foreach (GroupMember gmem_it in cache.GroupMembers)
                {
                    if (gmem_it.Group.Equals(group))
                    {
                        gmem = gmem_it;
                        return true;
                    }
                }
            }

            foreach (GroupMember gmem_it in Members[requestingAgent, principal])
            {
                if (gmem_it.Principal.Equals(principal))
                {
                    gmem = gmem_it;
                    return true;
                }
            }
            gmem = default(GroupMember);
            return false;
        }

        void IGroupMembersInterface.Update(UGUI requestingagent, UGI group, UGUI principal, bool acceptNotices, bool listInProfile)
        {
            Members.Update(requestingagent, group, principal, acceptNotices, listInProfile);
            m_PrincipalGroupMemberCache.Remove(new UGUI_UGUI { RequestingAgent = requestingagent, Principal = principal });
            m_GroupMemberCache.Remove(new UGUI_UGI { RequestingAgent = requestingagent, Group = group });
        }
    }
}
