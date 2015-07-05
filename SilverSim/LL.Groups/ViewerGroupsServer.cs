/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using Nini.Config;
using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;

namespace SilverSim.LL.Groups
{
    public class ViewerGroupsServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender
    {
        public ViewerGroupsServer()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        [PacketHandler(MessageType.GroupNoticesListRequest)]
        [PacketHandler(MessageType.CreateGroupRequest)]
        [PacketHandler(MessageType.UpdateGroupInfo)]
        [PacketHandler(MessageType.GroupRoleChanges)]
        [PacketHandler(MessageType.JoinGroupRequest)]
        [PacketHandler(MessageType.EjectGroupMemberRequest)]
        [PacketHandler(MessageType.LeaveGroupRequest)]
        [PacketHandler(MessageType.InviteGroupRequest)]
        [PacketHandler(MessageType.GroupProfileRequest)]
        [PacketHandler(MessageType.GroupAccountSummaryRequest)]
        [PacketHandler(MessageType.GroupAccountDetailsRequest)]
        [PacketHandler(MessageType.GroupAccountTransactionsRequest)]
        [PacketHandler(MessageType.GroupActiveProposalsRequest)]
        [PacketHandler(MessageType.GroupVoteHistoryRequest)]
        [PacketHandler(MessageType.StartGroupProposal)]
        [PacketHandler(MessageType.GroupProposalBallot)]
        [PacketHandler(MessageType.ActivateGroup)]
        [PacketHandler(MessageType.SetGroupContribution)]
        [PacketHandler(MessageType.SetGroupAcceptNotices)]
        [PacketHandler(MessageType.GroupRoleDataRequest)]
        [PacketHandler(MessageType.GroupRoleMembersRequest)]
        [PacketHandler(MessageType.GroupTitlesRequest)]
        [PacketHandler(MessageType.GroupTitleUpdate)]
        [PacketHandler(MessageType.GroupRoleUpdate)]
        public void HandleMessage(Message m)
        {

        }

        [CapabilityHandler("GroupMemberData")]
        public void HandleGroupMemberDataCapability(LLAgent agent, Circuit circuit, HttpRequest req)
        {
            req.ErrorResponse(System.Net.HttpStatusCode.InternalServerError, "Internal Server Error");
        }
    }

    [PluginName("ViewerGroupsServer")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerGroupsServer();
        }
    }
}
