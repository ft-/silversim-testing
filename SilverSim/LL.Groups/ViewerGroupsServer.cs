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
using SilverSim.LL.Messages.Agent;
using SilverSim.LL.Messages.Groups;
using SilverSim.LL.Messages.IM;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using SilverSim.Types.Groups;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Groups
{
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
        [IMMessageHandler(GridInstantMessageDialog.GroupNotice)]
        [IMMessageHandler(GridInstantMessageDialog.GroupNoticeInventoryAccepted)]
        [IMMessageHandler(GridInstantMessageDialog.SessionGroupStart)]
        [IMMessageHandler(GridInstantMessageDialog.SessionSend)]
        BlockingQueue<KeyValuePair<Circuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<Circuit, Message>>();
        BlockingQueue<KeyValuePair<UUID, SceneInterface>> AgentGroupDataUpdateQueue = new BlockingQueue<KeyValuePair<UUID, SceneInterface>>();

        BlockingQueue<KeyValuePair<SceneInterface, GridInstantMessage>> IMGroupNoticeQueue = new BlockingQueue<KeyValuePair<SceneInterface, GridInstantMessage>>();

        bool m_ShutdownGroups = false;

        public ViewerGroupsServer()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start();
            new Thread(IMThread).Start();
            new Thread(AgentGroupDataUpdateQueueThread).Start();
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

                GroupsServiceInterface groupsService = req.Value.GroupsService;

                if (null == groupsService)
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

        public void IMThread()
        {
            Thread.CurrentThread.Name = "Groups IM Thread";

            while(!m_ShutdownGroups)
            {
                KeyValuePair<SceneInterface, GridInstantMessage> req;
                try
                {
                    req = IMGroupNoticeQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                GroupsServiceInterface groupsService = req.Key.GroupsService;

                if(null == groupsService)
                {
                    continue;
                }

                GridInstantMessage gim = req.Value;
                List<GroupMember> gmems;
                try
                {
                    gmems = groupsService.Members[gim.FromAgent, gim.FromGroup];
                }
                catch
                {
                    continue;
                }

                foreach(GroupMember gmem in gmems)
                {
                    if(gim.Dialog == GridInstantMessageDialog.GroupNotice)
                    {
                        if(!gmem.IsAcceptNotices)
                        {
                            continue;
                        }
                        else if(0 == (GetGroupPowers(gmem.Principal, groupsService, gim.FromGroup) & GroupPowers.ReceiveNotices))
                        {
                            continue;
                        }
                    }

                    GridInstantMessage ngim = gim.Clone();
                    try
                    {
                        IMRouter.SendSync(ngim);
                    }
                    catch
                    {
                        /* just ignore in case something bad happens */
                    }
                }
            }
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

                        case MessageType.GroupMembersRequest:
                            HandleGroupMembersRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.ImprovedInstantMessage:
                            {
                                ImprovedInstantMessage im = (ImprovedInstantMessage)m;
                                switch(im.Dialog)
                                {
                                    case GridInstantMessageDialog.GroupInvitationAccept:
                                        break;

                                    case GridInstantMessageDialog.GroupInvitationDecline:
                                        break;

                                    case GridInstantMessageDialog.GroupNotice:
                                        HandleGroupNotice(req.Key.Agent, scene, im);
                                        break;

                                    case GridInstantMessageDialog.GroupNoticeInventoryAccepted:
                                        break;
                                }
                            }
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
        void SendAllAgentsGroupDataUpdate(SceneInterface scene, GroupsServiceInterface groupsService, UGI group)
        {
            foreach(IAgent agent in scene.Agents)
            {
                SendAgentGroupDataUpdate(agent, scene, groupsService, group);
            }
        }

        public void TriggerOnRootAgent(UUID agent, SceneInterface scene)
        {
            AgentGroupDataUpdateQueue.Enqueue(new KeyValuePair<UUID, SceneInterface>(agent, scene));
        }

        void SendAgentGroupDataUpdate(IAgent agent, SceneInterface scene, GroupsServiceInterface groupsService, UGI group)
        {
            try
            {
                List<GroupMembership> gmems = groupsService.Memberships[agent.Owner, agent.Owner];
                if (group == null || gmems.Count(gmem => gmem.Group.ID == group.ID) != 0)
                { /* still a lot work with that check but we are at least gentle with the viewer here */
                    AgentGroupDataUpdate update = new AgentGroupDataUpdate();
                    update.AgentID = agent.Owner.ID;
                    foreach (GroupMembership gmem in gmems)
                    {
                        AgentGroupDataUpdate.GroupDataEntry d = new AgentGroupDataUpdate.GroupDataEntry();
                        d.ListInProfile = gmem.ListInProfile;
                        d.GroupID = gmem.Group.ID;
                        d.GroupPowers = gmem.GroupPowers;
                        d.AcceptNotices = gmem.AcceptNotices;
                        d.GroupInsigniaID = gmem.GroupInsigniaID;
                        d.Contribution = gmem.Contribution;
                        d.GroupName = gmem.Group.GroupName;
                        update.GroupData.Add(d);
                    }
                    agent.SendMessageAlways(update, scene.ID);
                }
            }
            catch
#if DEBUG
                (Exception e)
#endif
            {
#if DEBUG
                m_Log.Debug("Exception when sending AgentGroupDataUpdate", e);
#endif
            }
        }
        #endregion

        #region Group Notice
        void HandleGroupNotice(LLAgent agent, SceneInterface scene, ImprovedInstantMessage m)
        {
            /* no validation needed with IM, that is already done in circuit */
            GroupsServiceInterface groupsService = scene.GroupsService;

            if(groupsService == null)
            {
                return;
            }
            
            UGI group;
            try
            {
                group = groupsService.Groups[agent.Owner, m.ToAgentID];
            }
            catch
            {
                return;
            }

            if((GetGroupPowers(agent.Owner, groupsService, group) & GroupPowers.SendNotices) == 0)
            {
                return;
            }

            InventoryItem item = null;

            if(m.BinaryBucket.Length >= 1 && m.BinaryBucket[0] > 0)
            {
                try
                {
                    IValue iv = LLSD_XML.Deserialize(new MemoryStream(m.BinaryBucket));
                    if(iv is Map)
                    {
                        Map binBuck = (Map)iv;
                        UUID itemID = binBuck["item_id"].AsUUID;
                        UUID ownerID = binBuck["owner_id"].AsUUID;
                        item = agent.InventoryService.Item[ownerID, itemID];
                    }
                }
                catch
                {

                }
            }

            GroupNotice gn = new GroupNotice();
            gn.ID = UUID.Random;
            gn.Group = group;
            gn.FromName = agent.Owner.FullName;
            string[] submsg = m.Message.Split(new char[] {'|'}, 2);
            gn.Subject = submsg.Length > 1 ? submsg[0] : string.Empty;
            gn.Message = submsg.Length > 1 ? submsg[1] : submsg[0];
            gn.HasAttachment = item != null;
            gn.AttachmentType = item != null ? item.AssetType : Types.Asset.AssetType.Unknown;
            gn.AttachmentName = item != null ? item.Name : string.Empty;
            gn.AttachmentItemID = item != null ? item.ID : UUID.Zero;
            gn.AttachmentOwner = item !=  null ? item.Owner : UUI.Unknown;

            GridInstantMessage gim = new GridInstantMessage();
            gim.FromAgent = agent.Owner;
            gim.Dialog = GridInstantMessageDialog.GroupNotice;
            gim.IsFromGroup = true;
            gim.Message = m.Message;
            gim.IMSessionID = gn.ID;
            gim.BinaryBucket = m.BinaryBucket;

            try
            {
                groupsService.Notices.Add(agent.Owner, gn);
                IMGroupNoticeQueue.Enqueue(new KeyValuePair<SceneInterface, GridInstantMessage>(scene, gim));
            }
            catch
            {
            }
        }

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
            else if((GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID)) & GroupPowers.ReceiveNotices) != 0)
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if(null == groupsService)
            {
                return;
            }

            GroupInfo groupinfo = new GroupInfo();
            groupinfo.ID.GroupName = req.Name;
            groupinfo.Founder = agent.Owner;
            groupinfo.Charter = req.Charter;
            groupinfo.IsShownInList = req.ShowInList;
            groupinfo.InsigniaID = req.InsigniaID;
            groupinfo.MembershipFee = req.MembershipFee;
            groupinfo.IsOpenEnrollment = req.OpenEnrollment;
            groupinfo.IsAllowPublish = req.AllowPublish;
            groupinfo.IsMaturePublish = req.MaturePublish;

            CreateGroupReply reply = new CreateGroupReply();
            reply.AgentID = req.AgentID;

            EconomyServiceInterface economyService = scene.EconomyService;
            try
            {
                if (null != economyService && scene.EconomyData.PriceGroupCreate > 0)
                {
                    economyService.ChargeAmount(agent.Owner, EconomyServiceInterface.TransactionType.GroupCreate, scene.EconomyData.PriceGroupCreate, delegate()
                    {
                        groupinfo = groupsService.Groups.Create(agent.Owner, groupinfo);
                    });
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

        void HandleUpdateGroupInfo(LLAgent agent, SceneInterface scene, Message m)
        {
            UpdateGroupInfo req = (UpdateGroupInfo)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
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

        void HandleJoinGroupRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            JoinGroupRequest req = (JoinGroupRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            GroupsServiceInterface groupsService = scene.GroupsService;
            if(null == groupsService)
            {
                return;
            }

            JoinGroupReply reply = new JoinGroupReply();
            reply.AgentID = req.AgentID;
            reply.GroupID = req.GroupID;
            reply.Success = false;
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
                GroupMember member = new GroupMember();
                try
                {
                    GroupMember gmem = groupsService.Members.Add(agent.Owner, new UGI(req.GroupID), agent.Owner, UUID.Zero, "");
                    reply.Success = true;
                }
                catch(Exception e)
                {
                    m_Log.Info("JoinGroupRequest", e);
                }
                agent.SendMessageAlways(reply, scene.ID);
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
            {
                return;
            }

            AgentDropGroup dropGroup = new AgentDropGroup();
            dropGroup.AgentID = req.AgentID;
            dropGroup.GroupID = req.GroupID;
            LeaveGroupReply reply = new LeaveGroupReply();
            reply.AgentID = req.AgentID;
            reply.GroupID = req.GroupID;
            reply.Success = false;
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if(null == groupsService)
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
                gr = new GroupRole();
                gr.Title = "";
                gr.Powers = GroupPowers.None;
            }

            GroupProfileReply reply = new GroupProfileReply();
            reply.AgentID = req.AgentID;
            reply.GroupID = req.GroupID;
            reply.Name = ginfo.ID.GroupName;
            reply.Charter = ginfo.Charter;
            reply.ShowInList = ginfo.IsShownInList;
            reply.InsigniaID = ginfo.InsigniaID;
            reply.FounderID = ginfo.Founder.ID;
            reply.MembershipFee = ginfo.MembershipFee;
            reply.OpenEnrollment = ginfo.IsOpenEnrollment;
            reply.Money = 0;
            reply.GroupMembershipCount = ginfo.MemberCount;
            reply.GroupRolesCount = ginfo.RoleCount;
            reply.AllowPublish = ginfo.IsAllowPublish;
            reply.MaturePublish = ginfo.IsMaturePublish;
            reply.OwnerRoleID = ginfo.OwnerRoleID;

            reply.MemberTitle = gr.Title;
            reply.PowersMask = gr.Powers;

            agent.SendMessageAlways(reply, scene.ID);
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
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
                        try
                        {
                            GroupRolemember grm = groupsService.Rolemembers[agent.Owner, new UGI(req.GroupID), req.RoleID, agent.Owner];
                        }
                        catch
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
                        GroupRolemember grm = new GroupRolemember();
                        grm.Group = new UGI(req.GroupID);
                        grm.Principal = principalUUI;
                        grm.RoleID = req.RoleID;
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
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
            foreach(GroupRole role in rolemembers)
            {
                GroupRoleDataReply.RoleDataEntry d = new GroupRoleDataReply.RoleDataEntry();
                d.Name = role.Name;
                d.Powers = role.Powers;
                d.RoleID = role.ID;
                d.Title = role.Title;
                d.Members = role.Members;
                d.Description = role.Description;

                if(messageFill + d.SizeInMessage > 1400)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                    reply = null;
                }
                if(null == reply)
                {
                    reply = new GroupRoleDataReply();
                    reply.AgentID = req.AgentID;
                    reply.RequestID = req.RequestID;
                    reply.GroupID = req.GroupID;
                    reply.RoleCount = rolemembers.Count;
                    messageFill = 0;
                }

                reply.RoleData.Add(d);
                messageFill += d.SizeInMessage;
            }
            if(null != reply)
            {
                agent.SendMessageAlways(reply, scene.ID);
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
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
            foreach (GroupRolemember role in rolemembers)
            {
                GroupRoleMembersReply.MemberDataEntry d = new GroupRoleMembersReply.MemberDataEntry();
                d.MemberID = role.Principal.ID;
                d.RoleID = role.RoleID;

                if (messageFill + d.SizeInMessage > 1400)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                    reply = null;
                }
                if (null == reply)
                {
                    reply = new GroupRoleMembersReply();
                    reply.AgentID = req.AgentID;
                    reply.RequestID = req.RequestID;
                    reply.GroupID = req.GroupID;
                    reply.TotalPairs = (uint)rolemembers.Count;
                    messageFill = 0;
                }

                reply.MemberData.Add(d);
                messageFill += d.SizeInMessage;
            }
            if (null != reply)
            {
                agent.SendMessageAlways(reply, scene.ID);
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
            {
                return;
            }

            GroupPowers powers = GetGroupPowers(agent.Owner, groupsService, new UGI(req.GroupID));
            bool haveChanges = false;

            foreach(GroupRoleUpdate.RoleDataEntry gru in req.RoleData)
            {
                switch(gru.UpdateType)
                {
                    case GroupRoleUpdate.RoleUpdateType.Create:
                        if((powers & GroupPowers.CreateRole) != 0)
                        {
                            GroupRole info = new GroupRole();
                            info.Group = new UGI(req.GroupID);
                            info.ID = UUID.Random;
                            info.Name = gru.Name;
                            info.Description = gru.Description;
                            info.Title = gru.Title;
                            info.Powers = gru.Powers;
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
                }
            }

            if(haveChanges)
            {
                SendAllAgentsGroupDataUpdate(scene, groupsService, new UGI(req.GroupID));
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
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
                foreach(GroupRolemembership grm in grms)
                {
                    GroupTitlesReply.GroupDataEntry d = new GroupTitlesReply.GroupDataEntry();
                    d.RoleID = grm.RoleID;
                    d.Selected = grm.RoleID == gam.SelectedRoleID;
                    d.Title = grm.GroupTitle;

                    if(d.SizeInMessage + messageFill > 1400)
                    {
                        agent.SendMessageAlways(reply, scene.ID);
                        reply = null;
                    }
                    if(null == reply)
                    {
                        reply = new GroupTitlesReply();
                        reply.AgentID = req.AgentID;
                        reply.GroupID = req.GroupID;
                        reply.RequestID = req.RequestID;
                        messageFill = 0;
                    }

                    messageFill += d.SizeInMessage;
                    reply.GroupData.Add(d);
                }
            }
            catch(Exception e)
            {
                m_Log.Info("GroupTitlesRequest", e);
                if (null == reply)
                {
                    reply = new GroupTitlesReply();
                    reply.AgentID = req.AgentID;
                    reply.GroupID = req.GroupID;
                }
            }
            if(null != reply)
            {
                agent.SendMessageAlways(reply, scene.ID);
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
            {
                return;
            }

            try
            {
                GroupRolemember grm = groupsService.Rolemembers[agent.Owner, new UGI(req.GroupID), req.TitleRoleID, agent.Owner];
            }
            catch(Exception e)
            {
                m_Log.Info("GroupTitleUpdate", e);
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if(null == groupsService)
            {
                return;
            }

            try
            {
                if (req.GroupID != UUID.Zero)
                {
                    GroupMember gmem = groupsService.Members[agent.Owner, new UGI(req.GroupID), agent.Owner];
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

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
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

        void HandleSetGroupAcceptNotices(LLAgent agent, SceneInterface scene, Message m)
        {
            SetGroupAcceptNotices req = (SetGroupAcceptNotices)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            GroupsServiceInterface groupsService = scene.GroupsService;
            if(null == groupsService)
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
        GroupPowers GetGroupPowers(UUI agent, GroupsServiceInterface groupsService, UGI group)
        {
            if(null == groupsService)
            {
                return GroupPowers.None;
            }
            List<GroupRole> roles = groupsService.Roles[agent, group, agent];
            GroupPowers powers = GroupPowers.None;
            foreach(GroupRole role in roles)
            {
                powers |= role.Powers;
            }

            return GroupPowers.None;
        }
        #endregion

        #region Capability
        static string PowersToString(GroupPowers powers)
        {
            UInt64 p = (UInt64)powers;
            return p.ToString("X");
        }

        void HandleGroupMembersRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            GroupMembersRequest req = (GroupMembersRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            GroupsServiceInterface groupsService = scene.GroupsService;
            if (null == groupsService)
            {
                return;
            }

            List<GroupMember> gmems;
            UGI group = new UGI(req.GroupID);
            try
            {

                gmems = groupsService.Members[agent.Owner, group];
            }
            catch
            {
                gmems = new List<GroupMember>();
            }

            Map membersmap = new Map();
            GroupInfo ginfo = groupsService.Groups[agent.Owner, group];
            List<GroupRolemember> ownerGroupRoleMembers = groupsService.Rolemembers[agent.Owner, group, ginfo.OwnerRoleID];

            GroupMembersReply reply = null;
            int messageFill = 0;

            foreach (GroupMember gmem in gmems)
            {
                GroupMembersReply.MemberDataEntry d = new GroupMembersReply.MemberDataEntry();
                d.AgentID = gmem.Principal.ID;
                d.AgentPowers = GroupPowers.None;
                d.Contribution = 0;
                d.IsOwner = false;
                d.OnlineStatus = "offline";
                d.Title = string.Empty;
                try
                {
                    GroupMembership gam = groupsService.Memberships[gmem.Principal, group, gmem.Principal];
                    d.AgentPowers = gam.GroupPowers;
                    d.Title = gam.GroupTitle;

                    foreach (GroupRolemember grm in ownerGroupRoleMembers)
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

                }
                if(null != reply && d.SizeInMessage + messageFill > 1400)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                    reply = null;
                }

                if(null == reply)
                {
                    reply = new GroupMembersReply();
                    reply.AgentID = req.AgentID;
                    reply.GroupID = req.GroupID;
                    reply.MemberCount = gmems.Count;
                    reply.RequestID = req.RequestID;
                    messageFill = 0;
                }
                reply.MemberData.Add(d);
                messageFill += d.SizeInMessage;
            }

            if(null != reply)
            {
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        [CapabilityHandler("GroupMemberData")]
        public void HandleGroupMemberDataCapability(LLAgent agent, Circuit circuit, HttpRequest req)
        {
            if(req.Method != "POST")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            SceneInterface scene = circuit.Scene;
            if(null == scene)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }
            GroupsServiceInterface groupsService = scene.GroupsService;
            if(null == groupsService)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            IValue iv;
            UGI group;
            try
            {
                iv = LLSD_XML.Deserialize(req.Body);
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

            Map res = new Map();
            res.Add("agent_id", agent.ID);
            res.Add("group_id", group.ID);

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

            Map membersmap = new Map();
            GroupInfo ginfo = groupsService.Groups[agent.Owner, group];
            List<GroupRole> groupRoles = groupsService.Roles[agent.Owner, group];
            List<GroupRolemember> ownerGroupRoleMembers = groupsService.Rolemembers[agent.Owner, group, ginfo.OwnerRoleID];
            List<string> groupTitles = new List<string>();
            groupTitles.Add("");
            GroupPowers defaultPowers = GroupPowers.None;
            foreach(GroupRole role in groupRoles)
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

            foreach(GroupMember gmem in gmems)
            {
                Map outmap = new Map();
                outmap.Add("donated_square_meters", 0);
                membersmap.Add(gmem.Principal.ID.ToString(), outmap);
                try
                {
                    GroupMembership gam = groupsService.Memberships[gmem.Principal, group, gmem.Principal];
                    outmap.Add("powers", PowersToString(gam.GroupPowers));
                    if (groupTitles.Contains(gam.GroupTitle))
                    {
                        int i = groupTitles.IndexOf(gam.GroupTitle);
                        if(i >= 0)
                        {
                            outmap.Add("title", i);
                        }
                    }

                    foreach (GroupRolemember grm in ownerGroupRoleMembers)
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

                }
            }
            res.Add("members", membersmap);
            AnArray groupTitlesArray = new AnArray();
            foreach(string s in groupTitles)
            {
                groupTitlesArray.Add(s);
            }
            Map defaultsMap = new Map();
            defaultsMap.Add("default_powers", PowersToString(defaultPowers));
            res.Add("defaults", defaultsMap);
            res.Add("titles", groupTitlesArray);

            HttpResponse httpres = req.BeginResponse("application/llsd+xml");
            LLSD_XML.Serialize(res, httpres.GetOutputStream());
            httpres.Close();
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
