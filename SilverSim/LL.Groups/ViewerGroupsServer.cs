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

using log4net;
using Nini.Config;
using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Groups;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Groups
{
    public class ViewerGroupsServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL GROUPS");

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
        BlockingQueue<KeyValuePair<Circuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<Circuit, Message>>();

        bool m_ShutdownGroups = false;

        public ViewerGroupsServer()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Groups Handler Thread";

            while (!m_ShutdownGroups)
            {
                KeyValuePair<Circuit, Message> req;
                try
                {
                    req = RequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;
                SceneInterface scene = req.Key.Scene;
                if (scene == null)
                {
                    continue;
                }

                try
                {
                    switch(m.Number)
                    {
                        case MessageType.GroupNoticesListRequest:
                            HandleGroupNoticesListRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.CreateGroupRequest:
                            HandleCreateGroupRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.UpdateGroupInfo:
                            HandleUpdateGroupInfo(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupRoleChanges:
                            HandleGroupRoleChanges(req.Key.Agent, scene, m);
                            break;

                        case MessageType.JoinGroupRequest:
                            HandleJoinGroupRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.EjectGroupMemberRequest:
                            HandleEjectGroupMemberRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.LeaveGroupRequest:
                            HandleLeaveGroupRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.InviteGroupRequest:
                            HandleInviteGroupRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupProfileRequest:
                            HandleGroupProfileRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupAccountSummaryRequest:
                            HandleGroupAccountSummaryRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupAccountDetailsRequest:
                            HandleGroupAccountDetailsRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupAccountTransactionsRequest:
                            HandleGroupAccountTransactionsRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupActiveProposalsRequest:
                            HandleGroupActiveProposalsRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupVoteHistoryRequest:
                            HandleGroupVoteHistoryRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.StartGroupProposal:
                            HandleStartGroupProposal(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupProposalBallot:
                            HandleGroupProposalBallot(req.Key.Agent, scene, m);
                            break;

                        case MessageType.ActivateGroup:
                            HandleActivateGroup(req.Key.Agent, scene, m);
                            break;

                        case MessageType.SetGroupContribution:
                            HandleSetGroupContribution(req.Key.Agent, scene, m);
                            break;

                        case MessageType.SetGroupAcceptNotices:
                            HandleSetGroupContribution(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupRoleDataRequest:
                            HandleGroupRoleDataRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupRoleMembersRequest:
                            HandleGroupRoleMembersRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupTitlesRequest:
                            HandleGroupTitlesRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupTitleUpdate:
                            HandleGroupTitleUpdate(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GroupRoleUpdate:
                            HandleGroupRoleUpdate(req.Key.Agent, scene, m);
                            break;
                    }
                }
                catch (Exception e)
                {
                    m_Log.Debug("Unexpected exception " + e.Message, e);
                }
            }
        }

        #region Group Notice
        void HandleGroupNoticesListRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupNoticesListRequest req = (GroupNoticesListRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
            GroupsServiceInterface groupsService = scene.GroupsService;

            if(groupsService == null)
            {
                GroupNoticesListReply reply = new GroupNoticesListReply();
                reply.AgentID = req.AgentID;
                reply.GroupID = req.GroupID;
                agent.SendMessageAlways(reply, scene.ID);
            }
            else if((GetGroupPowers(agent, groupsService, new UGI(req.GroupID)) & GroupPowers.ReceiveNotices) != 0)
            {
                List<GroupNotice> notices;
                try
                {
                    notices = groupsService.Notices.GetNotices(agent.Owner, new UGI(req.GroupID));
                }
                catch
                {
                    notices = new List<GroupNotice>();
                }

                GroupNoticesListReply reply = null;
                int messageFill = 0;

                foreach(GroupNotice notice in notices)
                {
                    GroupNoticesListReply.GroupNoticeData d = new GroupNoticesListReply.GroupNoticeData();
                    d.NoticeID = notice.ID;
                    d.Timestamp = notice.Timestamp;
                    d.FromName = notice.FromName;
                    d.Subject = notice.Subject;
                    d.HasAttachment = notice.HasAttachment;
                    d.AssetType = notice.AttachmentType;

                    if(reply != null && messageFill + d.SizeInMessage > 1400)
                    {
                        agent.SendMessageAlways(reply, scene.ID);
                        reply = null;
                    }

                    if(null == reply)
                    {
                        reply = new GroupNoticesListReply();
                        reply.AgentID = req.AgentID;
                        reply.GroupID = req.GroupID;
                        messageFill = 0;
                    }

                    reply.Data.Add(d);
                    messageFill += d.SizeInMessage;
                }
                if(null != reply)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                }
            }
            else
            {
                GroupNoticesListReply reply = new GroupNoticesListReply();
                reply.AgentID = req.AgentID;
                reply.GroupID = req.GroupID;
                agent.SendMessageAlways(reply, scene.ID);
            }
        }
        #endregion

        #region Groups
        void HandleCreateGroupRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            CreateGroupRequest req = (CreateGroupRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleUpdateGroupInfo(LLAgent agent, SceneInterface scene, Message m)
        {
            UpdateGroupInfo req = (UpdateGroupInfo)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleJoinGroupRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            JoinGroupRequest req = (JoinGroupRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleLeaveGroupRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            LeaveGroupRequest req = (LeaveGroupRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleInviteGroupRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            InviteGroupRequest req = (InviteGroupRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGroupProfileRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupProfileRequest req = (GroupProfileRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region GroupRole
        void HandleGroupRoleChanges(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupRoleChanges req = (GroupRoleChanges)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGroupRoleDataRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupRoleDataRequest req = (GroupRoleDataRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGroupRoleMembersRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupRoleMembersRequest req = (GroupRoleMembersRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGroupRoleUpdate(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupRoleUpdate req = (GroupRoleUpdate)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Group members
        void HandleEjectGroupMemberRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            EjectGroupMemberRequest req = (EjectGroupMemberRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Group Titles
        void HandleGroupTitlesRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupTitlesRequest req = (GroupTitlesRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGroupTitleUpdate(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupTitleUpdate req = (GroupTitleUpdate)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Group Account
        void HandleGroupAccountSummaryRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupAccountSummaryRequest req = (GroupAccountSummaryRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGroupAccountDetailsRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupAccountDetailsRequest req = (GroupAccountDetailsRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGroupAccountTransactionsRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupAccountTransactionsRequest req = (GroupAccountTransactionsRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Active Group Selection
        void HandleActivateGroup(LLAgent agent, SceneInterface scene, Message m)
        {
            ActivateGroup req = (ActivateGroup)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Group Proposals
        void HandleGroupActiveProposalsRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupActiveProposalsRequest req = (GroupActiveProposalsRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGroupVoteHistoryRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupVoteHistoryRequest req = (GroupVoteHistoryRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleStartGroupProposal(LLAgent agent, SceneInterface scene, Message m)
        {
            StartGroupProposal req = (StartGroupProposal)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleGroupProposalBallot(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupProposalBallot req = (GroupProposalBallot)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Member group params
        void HandleSetGroupContribution(LLAgent agent, SceneInterface scene, Message m)
        {
            SetGroupContribution req = (SetGroupContribution)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        void HandleSetGroupAcceptNotices(LLAgent agent, SceneInterface scene, Message m)
        {
            SetGroupAcceptNotices req = (SetGroupAcceptNotices)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Utility
        GroupPowers GetGroupPowers(LLAgent agent, GroupsServiceInterface groupsService, UGI group)
        {
            if(null == groupsService)
            {
                return GroupPowers.None;
            }
            List<GroupRole> roles = groupsService.Roles[agent.Owner, group, agent.Owner];
            GroupPowers powers = GroupPowers.None;
            foreach(GroupRole role in roles)
            {
                powers |= role.Powers;
            }

            return GroupPowers.None;
        }
        #endregion

        #region Capability
        [CapabilityHandler("GroupMemberData")]
        public void HandleGroupMemberDataCapability(LLAgent agent, Circuit circuit, HttpRequest req)
        {
            req.ErrorResponse(System.Net.HttpStatusCode.InternalServerError, "Internal Server Error");
        }
        #endregion

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_ShutdownGroups = true;
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
