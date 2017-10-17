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

#pragma warning disable IDE0018
#pragma warning disable RCS1029
#pragma warning disable RCS1163

using log4net;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Economy.Transactions;
using SilverSim.Types.Groups;
using SilverSim.Types.IM;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Groups;
using SilverSim.Viewer.Messages.IM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;

namespace SilverSim.Viewer.Groups
{
    [Description("Viewer Groups Handler")]
    [PluginName("ViewerGroupsServer")]
    public class ViewerGroupsServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown, ITriggerOnRootAgentActions
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
        [PacketHandler(MessageType.GroupMembersRequest)]
        [PacketHandler(MessageType.GroupRoleMembersRequest)]
        [PacketHandler(MessageType.GroupTitlesRequest)]
        [PacketHandler(MessageType.GroupTitleUpdate)]
        [PacketHandler(MessageType.GroupRoleUpdate)]
        [IMMessageHandler(GridInstantMessageDialog.GroupInvitationAccept)]
        [IMMessageHandler(GridInstantMessageDialog.GroupInvitationDecline)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();
        private readonly BlockingQueue<KeyValuePair<UUID, SceneInterface>> AgentGroupDataUpdateQueue = new BlockingQueue<KeyValuePair<UUID, SceneInterface>>();

        private bool m_ShutdownGroups;

        public void Startup(ConfigurationLoader loader)
        {
            ThreadManager.CreateThread(HandlerThread).Start();
            ThreadManager.CreateThread(AgentGroupDataUpdateQueueThread).Start();
        }

        private void SendAgentDataUpdate(IAgent agent, GroupsServiceInterface groupsService, SceneInterface scene)
        {
            var adu = new AgentDataUpdate();
            try
            {
                GroupRole gr;
                GroupActiveMembership gm = groupsService.ActiveMembership[agent.Owner, agent.Owner];
                adu.ActiveGroupID = groupsService.ActiveGroup[agent.Owner, agent.Owner].ID;
                if (adu.ActiveGroupID != UUID.Zero)
                {
                    gr = groupsService.Roles[agent.Owner, gm.Group, gm.SelectedRoleID];
                    adu.GroupName = gm.Group.GroupName;
                    adu.GroupTitle = gr.Title;
                    adu.GroupPowers = gr.Powers;
                }
            }
            catch
#if DEBUG
 (Exception e)
#endif
            {
#if DEBUG
                m_Log.Debug("SendAgentDataUpdate", e);
#endif
                adu.ActiveGroupID = UUID.Zero;
                adu.GroupName = string.Empty;
                adu.GroupTitle = string.Empty;
                adu.GroupPowers = GroupPowers.None;
            }
            adu.AgentID = agent.Owner.ID;
            adu.FirstName = agent.Owner.FirstName;
            adu.LastName = agent.Owner.LastName;
            foreach(var cagent in scene.Agents)
            {
                cagent.SendMessageAlways(adu, scene.ID);
            }
        }

        public void AgentGroupDataUpdateQueueThread()
        {
            Thread.CurrentThread.Name = "Groups AgentGroupDataUpdate Thread";
            while (!m_ShutdownGroups)
            {
                KeyValuePair<UUID, SceneInterface> req;
                try
                {
                    req = AgentGroupDataUpdateQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                var groupsService = req.Value.GroupsService;

                if (groupsService == null)
                {
                    continue;
                }

                IAgent agent;
                try
                {
                    agent = req.Value.Agents[req.Key];
                }
                catch
                {
                    continue;
                }
                SendAgentGroupDataUpdate(agent, req.Value, groupsService, null);
            }
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Groups Handler Thread";

            while (!m_ShutdownGroups)
            {
                KeyValuePair<AgentCircuit, Message> req;
                try
                {
                    req = RequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;
                SceneInterface scene = req.Key?.Scene;
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
                            HandleSetGroupAcceptNotices(req.Key.Agent, scene, m);
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

                        case MessageType.GroupMembersRequest:
                            HandleGroupMembersRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.ImprovedInstantMessage:
                            {
                                var im = (ImprovedInstantMessage)m;
                                switch(im.Dialog)
                                {
                                    case GridInstantMessageDialog.GroupInvitationAccept:
                                        break;

                                    case GridInstantMessageDialog.GroupInvitationDecline:
                                        break;

                                    default:
                                        break;
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    m_Log.Debug("Unexpected exception " + e.Message, e);
                }
            }
        }

        #region Agent Data Update Handling
        private void SendAllAgentsGroupDataUpdate(SceneInterface scene, GroupsServiceInterface groupsService, UGI group)
        {
            foreach(var agent in scene.Agents)
            {
                SendAgentGroupDataUpdate(agent, scene, groupsService, group);
            }
        }

        public void TriggerOnRootAgent(UUID agent, SceneInterface scene)
        {
            AgentGroupDataUpdateQueue.Enqueue(new KeyValuePair<UUID, SceneInterface>(agent, scene));
        }

        private void SendAgentGroupDataUpdate(IAgent agent, SceneInterface scene, GroupsServiceInterface groupsService, UGI group)
        {
            try
            {
                List<GroupMembership> gmems = groupsService.Memberships[agent.Owner, agent.Owner];
                if (group == null || gmems.Count(gmem => gmem.Group.ID == group.ID) != 0)
                { /* still a lot work with that check but we are at least gentle with the viewer here */
                    var update = new AgentGroupDataUpdate()
                    {
                        AgentID = agent.Owner.ID
                    };
                    foreach (var gmem in gmems)
                    {
                        update.GroupData.Add(new AgentGroupDataUpdate.GroupDataEntry()
                        {
                            ListInProfile = gmem.IsListInProfile,
                            GroupID = gmem.Group.ID,
                            GroupPowers = gmem.GroupPowers,
                            AcceptNotices = gmem.IsAcceptNotices,
                            GroupInsigniaID = gmem.GroupInsigniaID,
                            Contribution = gmem.Contribution,
                            GroupName = gmem.Group.GroupName
                        });
                    }
                    agent.SendMessageAlways(update, scene.ID);
                }
            }
            catch
#if DEBUG
                (Exception e)
#endif
            {
                /* do not expose exceptions to caller */
#if DEBUG
                m_Log.Debug("Exception when sending AgentGroupDataUpdate", e);
#endif
            }
        }
        #endregion

        #region Group notices
        private void HandleGroupNoticesListRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupNoticesListRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
            var groupsService = scene.GroupsService;

            if(groupsService == null ||
                (GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID)) & GroupPowers.ReceiveNotices) == 0)
            {
                var reply = new GroupNoticesListReply()
                {
                    AgentID = req.AgentID,
                    GroupID = req.GroupID
                };
                agent.SendMessageAlways(reply, scene.ID);
            }
            else
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

                foreach(var notice in notices)
                {
                    var d = new GroupNoticesListReply.GroupNoticeData()
                    {
                        NoticeID = notice.ID,
                        Timestamp = notice.Timestamp,
                        FromName = notice.FromName,
                        Subject = notice.Subject,
                        HasAttachment = notice.HasAttachment,
                        AssetType = notice.AttachmentType
                    };
                    if (reply != null && messageFill + d.SizeInMessage > 1400)
                    {
                        agent.SendMessageAlways(reply, scene.ID);
                        reply = null;
                    }

                    if(reply == null)
                    {
                        reply = new GroupNoticesListReply()
                        {
                            AgentID = req.AgentID,
                            GroupID = req.GroupID
                        };
                        messageFill = 0;
                    }

                    reply.Data.Add(d);
                    messageFill += d.SizeInMessage;
                }
                if(reply != null)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                }
            }
        }
        #endregion

        #region Groups
        private void HandleCreateGroupRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (CreateGroupRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if(groupsService == null)
            {
                return;
            }

            var groupinfo = new GroupInfo();
            groupinfo.ID.GroupName = req.Name;
            groupinfo.Founder = agent.Owner;
            groupinfo.Charter = req.Charter;
            groupinfo.IsShownInList = req.ShowInList;
            groupinfo.InsigniaID = req.InsigniaID;
            groupinfo.MembershipFee = req.MembershipFee;
            groupinfo.IsOpenEnrollment = req.OpenEnrollment;
            groupinfo.IsAllowPublish = req.AllowPublish;
            groupinfo.IsMaturePublish = req.MaturePublish;

            var reply = new CreateGroupReply
            {
                AgentID = req.AgentID
            };
            var economyService = scene.EconomyService;
            try
            {
                if (economyService != null && scene.EconomyData.PriceGroupCreate > 0)
                {
                    economyService.ChargeAmount(agent.Owner, new GroupCreateTransaction
                    {
                        Group = groupinfo.ID,
                        Founder = groupinfo.Founder
                    }, scene.EconomyData.PriceGroupCreate, () =>
                        groupinfo = groupsService.Groups.Create(agent.Owner, groupinfo));
                }
                else
                {
                    groupinfo = groupsService.Groups.Create(agent.Owner, groupinfo);
                }
                reply.GroupID = groupinfo.ID.ID;
                reply.Success = true;
                agent.SendMessageAlways(reply, scene.ID);
            }
            catch(Exception e)
            {
                reply.GroupID = UUID.Zero;
                reply.Success = false;
                reply.Message = e.Message;
                agent.SendMessageAlways(reply, scene.ID);
                return;
            }
            SendAgentGroupDataUpdate(agent, scene, groupsService, groupinfo.ID);
        }

        private void HandleUpdateGroupInfo(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (UpdateGroupInfo)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            GroupInfo ginfo;
            try
            {
                ginfo = groupsService.Groups[agent.Owner, new UGI(req.GroupID)];
            }
            catch(Exception e)
            {
                m_Log.Info("UpdateGroupInfo: Get", e);
                return;
            }
            if ((GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID)) & GroupPowers.ChangeOptions) != 0)
            {
                ginfo.IsOpenEnrollment = req.OpenEnrollment;
                ginfo.MembershipFee = req.MembershipFee;
            }
            if ((GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID)) & GroupPowers.ChangeIdentity) != 0)
            {
                ginfo.Charter = req.Charter;
                ginfo.IsShownInList = req.ShowInList;
                ginfo.InsigniaID = req.InsigniaID;
                ginfo.IsAllowPublish = req.AllowPublish;
                ginfo.IsMaturePublish = req.MaturePublish;
            }

            try
            {
                groupsService.Groups.Update(agent.Owner, ginfo);
            }
            catch(Exception e)
            {
                m_Log.Info("UpdateGroupInfo: Update", e);
                return;
            }
            SendAllAgentsGroupDataUpdate(scene, groupsService, ginfo.ID);
        }

        private void HandleJoinGroupRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (JoinGroupRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if(groupsService == null)
            {
                return;
            }

            var reply = new JoinGroupReply()
            {
                AgentID = req.AgentID,
                GroupID = req.GroupID,
                Success = false
            };
            GroupInfo ginfo;

            try
            {
                ginfo = groupsService.Groups[agent.Owner, new UGI(req.GroupID)];
            }
            catch(Exception e)
            {
                m_Log.Info("JoinGroupRequest", e);
                agent.SendMessageAlways(reply, scene.ID);
                return;
            }

            if (!ginfo.IsOpenEnrollment)
            {
                agent.SendMessageAlways(reply, scene.ID);
                return;
            }
            else
            {
                try
                {
                    groupsService.Members.Add(agent.Owner, new UGI(req.GroupID), agent.Owner, UUID.Zero, "");
                    reply.Success = true;
                }
                catch(Exception e)
                {
                    m_Log.Info("JoinGroupRequest", e);
                }
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        private void HandleLeaveGroupRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (LeaveGroupRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            var dropGroup = new AgentDropGroup()
            {
                AgentID = req.AgentID,
                GroupID = req.GroupID
            };
            var reply = new LeaveGroupReply()
            {
                AgentID = req.AgentID,
                GroupID = req.GroupID,
                Success = false
            };
            try
            {
                groupsService.Members.Delete(agent.Owner, new UGI(req.GroupID), agent.Owner);
                reply.Success = true;
            }
            catch(Exception e)
            {
                m_Log.Info("LeaveGroupRequest", e);
            }
            if(reply.Success)
            {
                agent.SendMessageAlways(dropGroup, scene.ID);
            }
            agent.SendMessageAlways(reply, scene.ID);
        }

        private void HandleInviteGroupRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (InviteGroupRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        private void HandleGroupProfileRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupProfileRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if(groupsService == null)
            {
                return;
            }

            GroupInfo ginfo;
            GroupActiveMembership gam;
            GroupRole gr;
            try
            {
                ginfo = groupsService.Groups[agent.Owner, new UGI(req.GroupID)];
            }
            catch(Exception e)
            {
                m_Log.Info("GroupProfileRequest", e);
                return;
            }

            try
            {
                gam = groupsService.ActiveMembership[agent.Owner, agent.Owner];
                gr = groupsService.Roles[agent.Owner, gam.Group, gam.SelectedRoleID];
            }
            catch(Exception e)
            {
                if (!(e is KeyNotFoundException))
                {
                    m_Log.Info("GroupProfileRequest", e);
                }
                gr = new GroupRole()
                {
                    Title = "",
                    Powers = GroupPowers.None
                };
            }

            var reply = new GroupProfileReply()
            {
                AgentID = req.AgentID,
                GroupID = req.GroupID,
                Name = ginfo.ID.GroupName,
                Charter = ginfo.Charter,
                ShowInList = ginfo.IsShownInList,
                InsigniaID = ginfo.InsigniaID,
                FounderID = ginfo.Founder.ID,
                MembershipFee = ginfo.MembershipFee,
                OpenEnrollment = ginfo.IsOpenEnrollment,
                Money = 0,
                GroupMembershipCount = ginfo.MemberCount,
                GroupRolesCount = ginfo.RoleCount,
                AllowPublish = ginfo.IsAllowPublish,
                MaturePublish = ginfo.IsMaturePublish,
                OwnerRoleID = ginfo.OwnerRoleID,

                MemberTitle = gr.Title,
                PowersMask = gr.Powers
            };
            agent.SendMessageAlways(reply, scene.ID);
        }
        #endregion

        #region GroupRole
        private void HandleGroupRoleChanges(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupRoleChanges)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            UUI principalUUI;
            try
            {
                principalUUI = scene.AvatarNameService[req.MemberID];
            }
            catch(Exception e)
            {
                m_Log.Info("GroupRoleChanges", e);
                return;
            }

            switch(req.Change)
            {
                case GroupRoleChanges.ChangeType.Add:
                    if((GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID)) & GroupPowers.AssignMemberLimited) != 0)
                    {
                        if(!groupsService.Rolemembers.ContainsKey(agent.Owner, new UGI(req.GroupID), req.RoleID, agent.Owner))
                        {
                            break;
                        }
                    }
                    else if ((GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID)) & GroupPowers.AssignMember) == 0)
                    {
                        break;
                    }

                    try
                    {
                        var grm = new GroupRolemember()
                        {
                            Group = new UGI(req.GroupID),
                            Principal = principalUUI,
                            RoleID = req.RoleID
                        };
                        groupsService.Rolemembers.Add(agent.Owner, grm);
                    }
                    catch
                    {
                        break;
                    }
                    SendAllAgentsGroupDataUpdate(scene, groupsService, new UGI(req.GroupID));
                    break;

                case GroupRoleChanges.ChangeType.Remove:
                    if ((GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID)) & GroupPowers.RemoveMember) == 0)
                    {
                        break;
                    }

                    try
                    {
                        groupsService.Rolemembers.Delete(agent.Owner, new UGI(req.GroupID), req.RoleID, principalUUI);
                    }
                    catch
                    {
                        break;
                    }
                    SendAllAgentsGroupDataUpdate(scene, groupsService, new UGI(req.GroupID));
                    break;

                default:
                    break;
            }
        }

        private void HandleGroupRoleDataRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupRoleDataRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            List<GroupRole> rolemembers;
            try
            {
                rolemembers = groupsService.Roles[agent.Owner, new UGI(req.GroupID)];
            }
            catch(Exception e)
            {
                m_Log.Info("GroupRoleDataRequest", e);
                rolemembers = new List<GroupRole>();
            }

            GroupRoleDataReply reply = null;
            int messageFill = 0;
            foreach(var role in rolemembers)
            {
                var d = new GroupRoleDataReply.RoleDataEntry()
                {
                    Name = role.Name,
                    Powers = role.Powers,
                    RoleID = role.ID,
                    Title = role.Title,
                    Members = role.Members,
                    Description = role.Description
                };
                if (messageFill + d.SizeInMessage > 1400)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                    reply = null;
                }
                if(reply == null)
                {
                    reply = new GroupRoleDataReply()
                    {
                        AgentID = req.AgentID,
                        RequestID = req.RequestID,
                        GroupID = req.GroupID,
                        RoleCount = rolemembers.Count
                    };
                    messageFill = 0;
                }

                reply.RoleData.Add(d);
                messageFill += d.SizeInMessage;
            }
            if(reply != null)
            {
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        private void HandleGroupRoleMembersRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupRoleMembersRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            List<GroupRolemember> rolemembers;
            try
            {
                rolemembers = groupsService.Rolemembers[agent.Owner, new UGI(req.GroupID)];
            }
            catch(Exception e)
            {
                m_Log.Info("GroupRoleMembersRequest", e);
                rolemembers = new List<GroupRolemember>();
            }

            GroupRoleMembersReply reply = null;
            int messageFill = 0;
            foreach (var role in rolemembers)
            {
                var d = new GroupRoleMembersReply.MemberDataEntry()
                {
                    MemberID = role.Principal.ID,
                    RoleID = role.RoleID
                };
                if (messageFill + d.SizeInMessage > 1400)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                    reply = null;
                }
                if (reply == null)
                {
                    reply = new GroupRoleMembersReply()
                    {
                        AgentID = req.AgentID,
                        RequestID = req.RequestID,
                        GroupID = req.GroupID,
                        TotalPairs = (uint)rolemembers.Count
                    };
                    messageFill = 0;
                }

                reply.MemberData.Add(d);
                messageFill += d.SizeInMessage;
            }
            if (reply != null)
            {
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        private void HandleGroupRoleUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupRoleUpdate)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            var powers = GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID));
            bool haveChanges = false;

            foreach(var gru in req.RoleData)
            {
                switch(gru.UpdateType)
                {
                    case GroupRoleUpdate.RoleUpdateType.Create:
                        if((powers & GroupPowers.CreateRole) != 0)
                        {
                            var info = new GroupRole()
                            {
                                Group = new UGI(req.GroupID),
                                ID = UUID.Random,
                                Name = gru.Name,
                                Description = gru.Description,
                                Title = gru.Title,
                                Powers = gru.Powers
                            };
                            try
                            {
                                groupsService.Roles.Add(agent.Owner, info);
                                haveChanges = true;
                            }
                            catch(Exception e)
                            {
                                m_Log.Info("GroupRoleUpdate.Create", e);
                            }
                        }
                        break;

                    case GroupRoleUpdate.RoleUpdateType.Delete:
                        if((powers & GroupPowers.DeleteRole) != 0)
                        {
                            try
                            {
                                groupsService.Roles.Delete(agent.Owner, new UGI(req.GroupID), gru.RoleID);
                                haveChanges = true;
                            }
                            catch(Exception e)
                            {
                                m_Log.Info("GroupRoleUpdate.Delete", e);
                            }
                        }
                        break;

                    case GroupRoleUpdate.RoleUpdateType.UpdateAll:
                    case GroupRoleUpdate.RoleUpdateType.UpdateData:
                    case GroupRoleUpdate.RoleUpdateType.UpdatePowers:
                        if ((powers & GroupPowers.RoleProperties) != 0)
                        {
                            GroupRole role;
                            try
                            {
                                role = groupsService.Roles[agent.Owner, new UGI(req.GroupID), gru.RoleID];
                            }
                            catch(Exception e)
                            {
                                m_Log.Info("GroupRoleUpdate.Update", e);
                                break;
                            }

                            if(gru.UpdateType == GroupRoleUpdate.RoleUpdateType.UpdateAll ||
                                gru.UpdateType == GroupRoleUpdate.RoleUpdateType.UpdateData)
                            {
                                role.Description = gru.Description;
                                role.Title = gru.Title;
                                role.Name = gru.Name;
                            }
                            if(gru.UpdateType == GroupRoleUpdate.RoleUpdateType.UpdateAll ||
                                gru.UpdateType == GroupRoleUpdate.RoleUpdateType.UpdatePowers)
                            {
                                role.Powers = gru.Powers;
                            }
                            try
                            {
                                groupsService.Roles.Update(agent.Owner, role);
                                haveChanges = true;
                            }
                            catch(Exception e)
                            {
                                m_Log.Info("GroupRoleUpdate.Update", e);
                            }
                        }
                        break;

                    default:
                        break;
                }
            }

            if(haveChanges)
            {
                SendAllAgentsGroupDataUpdate(scene, groupsService, new UGI(req.GroupID));
            }
        }
        #endregion

        #region Group members
        private void HandleEjectGroupMemberRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (EjectGroupMemberRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Group Titles
        private void HandleGroupTitlesRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupTitlesRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            GroupActiveMembership gam;
            List<GroupRolemembership> grms;
            GroupTitlesReply reply = null;
            int messageFill = 0;
            try
            {
                gam = groupsService.ActiveMembership[agent.Owner, agent.Owner];
                grms = groupsService.Rolemembers[agent.Owner, agent.Owner];
                foreach(var grm in grms)
                {
                    var d = new GroupTitlesReply.GroupDataEntry()
                    {
                        RoleID = grm.RoleID,
                        Selected = grm.RoleID == gam.SelectedRoleID,
                        Title = grm.GroupTitle
                    };
                    if (d.SizeInMessage + messageFill > 1400)
                    {
                        agent.SendMessageAlways(reply, scene.ID);
                        reply = null;
                    }
                    if(reply == null)
                    {
                        reply = new GroupTitlesReply()
                        {
                            AgentID = req.AgentID,
                            GroupID = req.GroupID,
                            RequestID = req.RequestID
                        };
                        messageFill = 0;
                    }

                    messageFill += d.SizeInMessage;
                    reply.GroupData.Add(d);
                }
            }
            catch(Exception e)
            {
                m_Log.Info("GroupTitlesRequest", e);
                if (reply == null)
                {
                    reply = new GroupTitlesReply()
                    {
                        AgentID = req.AgentID,
                        GroupID = req.GroupID
                    };
                }
            }
            if(reply != null)
            {
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        private void HandleGroupTitleUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupTitleUpdate)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            if(!groupsService.Rolemembers.ContainsKey(agent.Owner, new UGI(req.GroupID), req.TitleRoleID, agent.Owner))
            {
                return;
            }

            try
            {
                groupsService.ActiveGroup[agent.Owner, new UGI(req.GroupID), agent.Owner] = req.TitleRoleID;
            }
            catch
            {
                return;
            }

            SendAllAgentsGroupDataUpdate(scene, groupsService, new UGI(req.GroupID));
            SendAgentDataUpdate(agent, groupsService, scene);
        }
        #endregion

        #region Group Account
        private void HandleGroupAccountSummaryRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupAccountSummaryRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        private void HandleGroupAccountDetailsRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupAccountDetailsRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        private void HandleGroupAccountTransactionsRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupAccountTransactionsRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Active Group Selection
        private void HandleActivateGroup(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (ActivateGroup)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if(groupsService == null)
            {
                return;
            }

            try
            {
                if (req.GroupID != UUID.Zero)
                {
                    var gmem = groupsService.Members[agent.Owner, new UGI(req.GroupID), agent.Owner];
                    groupsService.ActiveGroup[agent.Owner, agent.Owner] = gmem.Group;
                }
                else
                {
                    groupsService.ActiveGroup[agent.Owner, agent.Owner] = UGI.Unknown;
                }
            }
            catch(Exception e)
            {
                m_Log.Info("ActivateGroup", e);
                return;
            }
            SendAgentGroupDataUpdate(agent, scene, groupsService, new UGI(req.GroupID));
            SendAgentDataUpdate(agent, groupsService, scene);
        }
        #endregion

        #region Group Proposals
        private void HandleGroupActiveProposalsRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupActiveProposalsRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        private void HandleGroupVoteHistoryRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupVoteHistoryRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        private void HandleStartGroupProposal(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (StartGroupProposal)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }

        private void HandleGroupProposalBallot(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupProposalBallot)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
        }
        #endregion

        #region Member group params
        private void HandleSetGroupContribution(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (SetGroupContribution)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            if ((GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID)) & GroupPowers.Accountable) != 0)
            {
                try
                {
                    groupsService.Members.SetContribution(agent.Owner, new UGI(req.GroupID), agent.Owner, req.Contribution);
                }
                catch(Exception e)
                {
                    m_Log.Info("SetGroupContribution", e);
                    return;
                }
                SendAgentGroupDataUpdate(agent, scene, groupsService, new UGI(req.GroupID));
            }
        }

        private void HandleSetGroupAcceptNotices(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (SetGroupAcceptNotices)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if(groupsService == null)
            {
                return;
            }

            if ((GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID)) & GroupPowers.ChangeOptions) != 0)
            {
                try
                {
                    groupsService.Members.Update(agent.Owner, new UGI(req.GroupID), agent.Owner, req.AcceptNotices, req.ListInProfile);
                }
                catch(Exception e)
                {
                    m_Log.Info("SetGroupAcceptNotices", e);
                    return;
                }
                SendAgentGroupDataUpdate(agent, scene, groupsService, new UGI(req.GroupID));
            }
        }
        #endregion

        #region Utility
        private GroupPowers GetGroupPowers(UUI agent, GroupsServiceInterface groupsService, UGI group)
        {
            if(groupsService == null)
            {
                return GroupPowers.None;
            }
            var roles = groupsService.Roles[agent, group, agent];
            var powers = GroupPowers.None;
            foreach(var role in roles)
            {
                powers |= role.Powers;
            }

            return GroupPowers.None;
        }
        #endregion

        #region Capability
        private static string PowersToString(GroupPowers powers)
        {
            var p = (UInt64)powers;
            return p.ToString("X");
        }

        private void HandleGroupMembersRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (GroupMembersRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            var groupsService = scene.GroupsService;
            if (groupsService == null)
            {
                return;
            }

            List<GroupMember> gmems;
            var group = new UGI(req.GroupID);
            try
            {
                gmems = groupsService.Members[agent.Owner, group];
            }
            catch
            {
                gmems = new List<GroupMember>();
            }

            var ginfo = groupsService.Groups[agent.Owner, group];
            var ownerGroupRoleMembers = groupsService.Rolemembers[agent.Owner, group, ginfo.OwnerRoleID];

            GroupMembersReply reply = null;
            int messageFill = 0;

            foreach (var gmem in gmems)
            {
                var d = new GroupMembersReply.MemberDataEntry()
                {
                    AgentID = gmem.Principal.ID,
                    AgentPowers = GroupPowers.None,
                    Contribution = 0,
                    IsOwner = false,
                    OnlineStatus = "offline",
                    Title = string.Empty
                };
                try
                {
                    var gam = groupsService.Memberships[gmem.Principal, group, gmem.Principal];
                    d.AgentPowers = gam.GroupPowers;
                    d.Title = gam.GroupTitle;

                    foreach (var grm in ownerGroupRoleMembers)
                    {
                        if (grm.Principal.EqualsGrid(gmem.Principal))
                        {
                            d.IsOwner = true;
                            break;
                        }
                    }
                }
                catch
                {
                    /* do not expose exceptions to caller */
                }
                if (reply != null && d.SizeInMessage + messageFill > 1400)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                    reply = null;
                }

                if(reply == null)
                {
                    reply = new GroupMembersReply()
                    {
                        AgentID = req.AgentID,
                        GroupID = req.GroupID,
                        MemberCount = gmems.Count,
                        RequestID = req.RequestID
                    };
                    messageFill = 0;
                }
                reply.MemberData.Add(d);
                messageFill += d.SizeInMessage;
            }

            if(reply != null)
            {
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        [CapabilityHandler("GroupMemberData")]
        public void HandleGroupMemberDataCapability(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            if (req.CallerIP != circuit.RemoteIP)
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (req.Method != "POST")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            var scene = circuit.Scene;
            if(scene == null)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }
            var groupsService = scene.GroupsService;
            if(groupsService == null)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            IValue iv;
            UGI group;
            try
            {
                iv = LlsdXml.Deserialize(req.Body);
                group = new UGI(((Map)iv)["group_id"].AsUUID);
            }
            catch(Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD-XML received at GroupMemberData", e);
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }
            if(!(iv is Map))
            {
                m_Log.WarnFormat("Invalid LLSD-XML received at GroupMemberData");
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                return;
            }

            var res = new Map
            {
                ["agent_id"] = agent.ID,
                ["group_id"] = group.ID
            };
            List<GroupMember> gmems;
            try
            {
                gmems = groupsService.Members[circuit.Agent.Owner, group];
            }
            catch
            {
                gmems = new List<GroupMember>();
            }

            res.Add("member_count", gmems.Count);

            var membersmap = new Map();
            var ginfo = groupsService.Groups[agent.Owner, group];
            var groupRoles = groupsService.Roles[agent.Owner, group];
            var ownerGroupRoleMembers = groupsService.Rolemembers[agent.Owner, group, ginfo.OwnerRoleID];
            var groupTitles = new List<string>
            {
                ""
            };
            var defaultPowers = GroupPowers.None;
            foreach(var role in groupRoles)
            {
                if(role.ID == UUID.Zero)
                {
                    groupTitles[0] = role.Title;
                    defaultPowers = role.Powers;
                }
                else if(!groupTitles.Contains(role.Title))
                {
                    groupTitles.Add(role.Title);
                }
            }

            foreach(var gmem in gmems)
            {
                var outmap = new Map
                {
                    { "donated_square_meters", 0 }
                };
                membersmap.Add(gmem.Principal.ID.ToString(), outmap);
                try
                {
                    var gam = groupsService.Memberships[gmem.Principal, group, gmem.Principal];
                    outmap.Add("powers", PowersToString(gam.GroupPowers));
                    if (groupTitles.Contains(gam.GroupTitle))
                    {
                        int i = groupTitles.IndexOf(gam.GroupTitle);
                        if(i >= 0)
                        {
                            outmap.Add("title", i);
                        }
                    }

                    foreach (var grm in ownerGroupRoleMembers)
                    {
                        if (grm.Principal.EqualsGrid(gmem.Principal))
                        {
                            outmap.Add("owner", true);
                            break;
                        }
                    }
                }
                catch
                {
                    /* do not expose exceptions to caller */
                }
            }
            res.Add("members", membersmap);
            var groupTitlesArray = new AnArray();
            foreach(var s in groupTitles)
            {
                groupTitlesArray.Add(s);
            }
            var defaultsMap = new Map
            {
                { "default_powers", PowersToString(defaultPowers) }
            };
            res.Add("defaults", defaultsMap);
            res.Add("titles", groupTitlesArray);

            using (var httpres = req.BeginResponse("application/llsd+xml"))
            {
                LlsdXml.Serialize(res, httpres.GetOutputStream());
            }
        }
        #endregion

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_ShutdownGroups = true;
        }
    }
}
