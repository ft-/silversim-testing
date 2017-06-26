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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Groups;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.IM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace SilverSim.Viewer.GroupChat
{
    [Description("Viewer GroupChat Handler")]
    [PluginName("ViewerGroupChatServer")]
    public class ViewerGroupChatServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL GROUPCHAT");

        [IMMessageHandler(GridInstantMessageDialog.GroupNotice)]
        [IMMessageHandler(GridInstantMessageDialog.GroupNoticeInventoryAccepted)]
        [IMMessageHandler(GridInstantMessageDialog.SessionGroupStart)]
        [IMMessageHandler(GridInstantMessageDialog.SessionSend)]
        [IMMessageHandler(GridInstantMessageDialog.SessionDrop)]
        [IMMessageHandler(GridInstantMessageDialog.SessionAdd)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        private readonly BlockingQueue<KeyValuePair<SceneInterface, GridInstantMessage>> IMGroupNoticeQueue = new BlockingQueue<KeyValuePair<SceneInterface, GridInstantMessage>>();
        private IMRouter m_IMRouter;

        private bool m_ShutdownGroupChat;

        public void Startup(ConfigurationLoader loader)
        {
            m_IMRouter = loader.IMRouter;
            ThreadManager.CreateThread(HandlerThread).Start();
            ThreadManager.CreateThread(IMThread).Start();
        }

        public void IMThread()
        {
            Thread.CurrentThread.Name = "Groups IM Thread";

            while (!m_ShutdownGroupChat)
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

                var groupsService = req.Key.GroupsService;

                if (groupsService == null)
                {
                    continue;
                }

                var gim = req.Value;
                List<GroupMember> gmems;
                try
                {
                    gmems = groupsService.Members[gim.FromAgent, gim.FromGroup];
                }
                catch
                {
                    continue;
                }

                foreach (var gmem in gmems)
                {
                    if (gim.Dialog == GridInstantMessageDialog.GroupNotice &&
                        (!gmem.IsAcceptNotices ||
                            0 == (GetGroupPowers(gmem.Principal, groupsService, gim.FromGroup) & GroupPowers.ReceiveNotices)))
                    {
                        continue;
                    }

                    GridInstantMessage ngim = gim.Clone();
                    try
                    {
                        m_IMRouter.SendSync(ngim);
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
            Thread.CurrentThread.Name = "GroupChat Handler Thread";

            while (!m_ShutdownGroupChat)
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
                var scene = req.Key.Scene;
                if (scene == null)
                {
                    continue;
                }

                try
                {
                    switch (m.Number)
                    {
                        case MessageType.ImprovedInstantMessage:
                            {
                                var im = (ImprovedInstantMessage)m;
                                switch (im.Dialog)
                                {
                                    case GridInstantMessageDialog.GroupNotice:
                                        HandleGroupNotice(req.Key.Agent, scene, im);
                                        break;

                                    case GridInstantMessageDialog.GroupNoticeInventoryAccepted:
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

        #region Group Notice
        private void HandleGroupNotice(ViewerAgent agent, SceneInterface scene, ImprovedInstantMessage m)
        {
            /* no validation needed with IM, that is already done in circuit */
            var groupsService = scene.GroupsService;

            if (groupsService == null)
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

            if ((GetGroupPowers(agent.Owner, groupsService, group) & GroupPowers.SendNotices) == 0)
            {
                return;
            }

            InventoryItem item = null;

            if (m.BinaryBucket.Length >= 1 && m.BinaryBucket[0] > 0)
            {
                try
                {
                    var iv = LlsdXml.Deserialize(new MemoryStream(m.BinaryBucket));
                    if (iv is Map)
                    {
                        var binBuck = (Map)iv;
                        var itemID = binBuck["item_id"].AsUUID;
                        var ownerID = binBuck["owner_id"].AsUUID;
                        item = agent.InventoryService.Item[ownerID, itemID];
                    }
                }
                catch
                {
                    /* do not expose exceptions to caller */
                }
            }

            var gn = new GroupNotice()
            {
                ID = UUID.Random,
                Group = group,
                FromName = agent.Owner.FullName
            };
            var submsg = m.Message.Split(new char[] { '|' }, 2);
            gn.Subject = submsg.Length > 1 ? submsg[0] : string.Empty;
            gn.Message = submsg.Length > 1 ? submsg[1] : submsg[0];
            gn.HasAttachment = item != null;
            gn.AttachmentType = item != null ? item.AssetType : Types.Asset.AssetType.Unknown;
            gn.AttachmentName = item != null ? item.Name : string.Empty;
            gn.AttachmentItemID = item != null ? item.ID : UUID.Zero;
            gn.AttachmentOwner = item != null ? item.Owner : UUI.Unknown;

            var gim = new GridInstantMessage()
            {
                FromAgent = agent.Owner,
                Dialog = GridInstantMessageDialog.GroupNotice,
                IsFromGroup = true,
                Message = m.Message,
                IMSessionID = gn.ID,
                BinaryBucket = m.BinaryBucket
            };
            try
            {
                groupsService.Notices.Add(agent.Owner, gn);
                IMGroupNoticeQueue.Enqueue(new KeyValuePair<SceneInterface, GridInstantMessage>(scene, gim));
            }
            catch
            {
                /* do not expose exceptions to caller */
            }
        }
        #endregion

        #region Utility
        private GroupPowers GetGroupPowers(UUI agent, GroupsServiceInterface groupsService, UGI group)
        {
            if (groupsService == null)
            {
                return GroupPowers.None;
            }
            var roles = groupsService.Roles[agent, group, agent];
            var powers = GroupPowers.None;
            foreach (var role in roles)
            {
                powers |= role.Powers;
            }

            return GroupPowers.None;
        }
        #endregion

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_ShutdownGroupChat = true;
        }
    }
}
