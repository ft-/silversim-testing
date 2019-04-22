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
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Groups.Common.Broker
{
    public sealed partial class GroupsBrokerService : IGroupsInterface
    {
        bool IGroupsInterface.ContainsKey(UGUI requestingAgent, UUID groupID)
        {
            GroupsServiceInterface groupsService;
            return TryGetGroupsService(groupID, out groupsService) && groupsService.Groups.ContainsKey(requestingAgent, groupID);
        }

        bool IGroupsInterface.ContainsKey(UGUI requestingAgent, UGI groupID)
        {
            GroupsBrokerEntry groupsService;
            return TryGetGroupsService(groupID, out groupsService) && groupsService.Groups.ContainsKey(requestingAgent, groupID);
        }

        bool IGroupsInterface.ContainsKey(UGUI requestingAgent, string groupName)
        {
            GroupsServiceInterface groupsService;
            return TryGetGroupsService(requestingAgent, out groupsService) && groupsService.Groups.ContainsKey(requestingAgent, groupName);
        }

        GroupInfo IGroupsInterface.Create(UGUI requestingAgent, GroupInfo group) =>
            GetGroupsService(requestingAgent).Groups.Create(requestingAgent, group);

        void IGroupsInterface.Delete(UGUI requestingAgent, UGI group)
        {
            GetGroupsService(group).Groups.Delete(requestingAgent, group);
        }

        List<DirGroupInfo> IGroupsInterface.GetGroupsByName(UGUI requestingAgent, string query)
        {
            throw new NotImplementedException();
        }

        bool IGroupsInterface.TryGetValue(UGUI requestingAgent, UUID groupID, out UGI ugi)
        {
            GroupsServiceInterface groupsService;
            ugi = default(UGI);
            return TryGetGroupsService(groupID, out groupsService) && groupsService.Groups.TryGetValue(requestingAgent, groupID, out ugi);
        }

        bool IGroupsInterface.TryGetValue(UGUI requestingAgent, UGI groupID, out GroupInfo groupInfo)
        {
            GroupsBrokerEntry groupsService;
            groupInfo = default(GroupInfo);
            return TryGetGroupsService(groupID, out groupsService) && groupsService.Groups.TryGetValue(requestingAgent, groupID, out groupInfo);
        }

        bool IGroupsInterface.TryGetValue(UGUI requestingAgent, string groupName, out GroupInfo groupInfo)
        {
            GroupsServiceInterface groupsService;
            groupInfo = default(GroupInfo);
            return TryGetGroupsService(requestingAgent, out groupsService) && groupsService.Groups.TryGetValue(requestingAgent, groupName, out groupInfo);
        }

        GroupInfo IGroupsInterface.Update(UGUI requestingAgent, GroupInfo group) =>
            GetGroupsService(group.ID).Groups.Update(requestingAgent, group);
    }
}
