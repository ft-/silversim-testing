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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Friends;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.CallingCard;
using SilverSim.Viewer.Messages.Friend;
using SilverSim.Viewer.Messages.IM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace SilverSim.Viewer.Friends
{
    [Description("Viewer Friends Handler")]
    [PluginName("ViewerFriendsServer")]
    public class ViewerFriendsServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("VIEWER FRIENDS");

        /* even though some one may look out here for loading friends list,
         * it is not here since it is part of the login protocol.
         */
        [PacketHandler(MessageType.AcceptFriendship)]
        [PacketHandler(MessageType.DeclineFriendship)]
        [PacketHandler(MessageType.TerminateFriendship)]
        [PacketHandler(MessageType.GrantUserRights)]
        [IMMessageHandler(GridInstantMessageDialog.FriendshipOffered)]
        [PacketHandler(MessageType.AcceptCallingCard)]
        [PacketHandler(MessageType.DeclineCallingCard)]
        [PacketHandler(MessageType.OfferCallingCard)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        private IMServiceInterface m_IMService;
        private readonly string m_IMServiceName;
        private List<IFriendsServicePlugin> m_FriendsPlugins;
        private List<IUserAgentServicePlugin> m_UserAgentPlugins;
        private SceneList m_Scenes;

        private bool m_ShutdownFriends;

        public ViewerFriendsServer(IConfig ownSection)
        {
            m_IMServiceName = ownSection.GetString("IMService", "IMService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_IMService = loader.GetService<IMServiceInterface>(m_IMServiceName);
            m_FriendsPlugins = loader.GetServicesByValue<IFriendsServicePlugin>();
            m_UserAgentPlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
            ThreadManager.CreateThread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Friends Handler Thread";

            while (!m_ShutdownFriends)
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

                try
                {
                    var m = req.Value;

                    switch (m.Number)
                    {
                        case MessageType.AcceptFriendship:
                            HandleAcceptFriendship(m);
                            break;

                        case MessageType.DeclineFriendship:
                            HandleDeclineFriendship(m);
                            break;

                        case MessageType.TerminateFriendship:
                            HandleTerminateFriendship(m);
                            break;

                        case MessageType.GrantUserRights:
                            HandleGrantUserRights(m);
                            break;

                        case MessageType.OfferCallingCard:
                            HandleOfferCallingCard(m);
                            break;

                        case MessageType.DeclineCallingCard:
                            HandleDeclineCallingCard(m);
                            break;

                        case MessageType.ImprovedInstantMessage:
                            {
                                var im = (ImprovedInstantMessage)m;
                                switch (im.Dialog)
                                {
                                    case GridInstantMessageDialog.FriendshipOffered:
                                        HandleFriendshipOffered(im);
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
                catch(Exception e)
                {
                    m_Log.ErrorFormat("Exception during friendship handling: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                }
            }
        }

        private void HandleOfferCallingCard(Message m)
        {
            var req = (OfferCallingCard)m;
            if(req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(req.CircuitSceneID, out scene))
            {
                return;
            }

            IAgent agent;
            if (!scene.Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            UUI destagent;
            if (scene.AvatarNameService.TryGetValue(req.DestID, out destagent))
            {
                if (agent.IsActiveGod && agent.IsInScene(scene))
                {
                    /* take calling card */
                    var vagent = agent as ViewerAgent;
                    vagent?.CreateCallingCard(destagent, true);
                }
                else if (scene.AvatarNameService.TryGetValue(req.DestID, out destagent))
                {
                    var gim = new GridInstantMessage
                    {
                        RegionID = req.CircuitSceneID,
                        FromAgent = agent.Owner,
                        ToAgent = destagent,
                        Dialog = GridInstantMessageDialog.OfferCallingCard,
                        OnResult = (im, success) => { }
                    };
                    scene.GetService<IMServiceInterface>()?.Send(gim);
                }
            }
        }

        private void HandleDeclineCallingCard(Message m)
        {
            var req = (DeclineCallingCard)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(req.CircuitSceneID, out scene))
            {
                return;
            }

            IAgent agent;
            if (!scene.Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            InventoryServiceInterface inventoryService = agent.InventoryService;
            if(inventoryService == null)
            {
                return;
            }

            InventoryFolder folder;
            if (inventoryService.Folder.TryGetValue(req.AgentID, AssetType.TrashFolder, out folder))
            {
                inventoryService.Item.Move(req.AgentID, req.TransactionID, folder.ID);
            }
        }

        private void HandleFriendshipOffered(ImprovedInstantMessage m)
        {
            if(m.CircuitAgentID != m.AgentID ||
                m.CircuitSessionID != m.SessionID)
            {
                return;
            }

            SceneInterface scene;
            if(!m_Scenes.TryGetValue(m.CircuitSceneID, out scene))
            {
                return;
            }

            IAgent agent;
            if(!scene.Agents.TryGetValue(m.AgentID, out agent))
            {
                return;
            }

            if(agent.KnownFriends.ContainsKey(m.ToAgentID))
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "AlreadyYourFriend", "This person is already your friend."), m.CircuitSceneID);
                return;
            }

            if (agent.FriendsService == null)
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "FriendsServiceNotAccessible", "The friends service is not accessible."), m.CircuitSceneID);
                return;
            }

            var thisAgent = agent.Owner;
            UUI otherAgent;
            var foreignagent = true;

            if(!scene.AvatarNameService.TryGetValue(m.ToAgentID, out otherAgent))
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "OtherPersonIdentityIsNotKnown", "Other person's identity is not known."), m.CircuitSceneID);
                return;
            }

            FriendsServiceInterface otherFriendsService;

            if ((otherAgent.HomeURI == null && thisAgent.HomeURI == null) ||
                otherAgent.HomeURI.Equals(thisAgent.HomeURI))
            {
                /* same user service including friends */
                otherFriendsService = agent.FriendsService;
                foreignagent = false;
            }
            else if(!TryGetFriendsService(otherAgent, out otherFriendsService))
            {
                return;
            }
            var fi = new FriendInfo
            {
                User = thisAgent,
                Friend = otherAgent,
                FriendGivenFlags = 0,
                UserGivenFlags = 0,
                Secret = string.Empty
            };
            fi.User.HomeURI = null;
            fi.Friend.HomeURI = null;
            if(foreignagent)
            {
                otherFriendsService.StoreOffer(fi);
            }
            agent.FriendsService.StoreOffer(fi);

            var gim = (GridInstantMessage)m;
            gim.FromAgent = thisAgent;
            gim.ToAgent = otherAgent;
            gim.IMSessionID = thisAgent.ID;

            m_IMService.Send(gim);
        }

        private void HandleAcceptFriendship(Message m)
        {
            var req = (AcceptFriendship)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(m.CircuitSceneID, out scene))
            {
                return;
            }

            IAgent agent;
            if (!scene.Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            if (agent.FriendsService == null)
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "FriendsServiceNotAccessible", "The friends service is not accessible."), m.CircuitSceneID);
                return;
            }

            FriendsServiceInterface otherFriendsService;
            var thisAgent = agent.Owner;
            UUI otherAgent;
            var foreignagent = true;

            /* the transaction id is re-used for storing the agent */
            if (!scene.AvatarNameService.TryGetValue(req.TransactionID, out otherAgent))
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "OtherPersonIdentityIsNotKnown", "Other person's identity is not known."), m.CircuitSceneID);
                return;
            }

            if ((otherAgent.HomeURI == null && thisAgent.HomeURI == null) ||
                otherAgent.HomeURI.Equals(thisAgent.HomeURI))
            {
                /* same user service including friends */
                otherFriendsService = agent.FriendsService;
                foreignagent = false;
            }
            else if (!TryGetFriendsService(otherAgent, out otherFriendsService))
            {
                return;
            }
            var fi = new FriendInfo
            {
                User = thisAgent,
                Friend = otherAgent,
                FriendGivenFlags = FriendRightFlags.SeeOnline,
                UserGivenFlags = FriendRightFlags.SeeOnline,
                Secret = string.Empty
            };
            fi.User.HomeURI = null;
            fi.Friend.HomeURI = null;
            if (foreignagent)
            {
                otherFriendsService.Store(fi);
            }
            agent.FriendsService.Delete(fi);

            var gim = new GridInstantMessage
            {
                FromAgent = thisAgent,
                ToAgent = otherAgent,
                Dialog = GridInstantMessageDialog.FriendshipAccepted,
                Message = "Friendship accepted",
                IMSessionID = otherAgent.ID,
                ParentEstateID = scene.ParentEstateID,
                RegionID = scene.ID
            };
            m_IMService.Send(gim);
        }

        private void HandleDeclineFriendship(Message m)
        {
            var req = (DeclineFriendship)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(m.CircuitSceneID, out scene))
            {
                return;
            }

            IAgent agent;
            if (!scene.Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            if (agent.FriendsService == null)
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "FriendsServiceNotAccessible", "The friends service is not accessible."), m.CircuitSceneID);
                return;
            }

            FriendsServiceInterface otherFriendsService;
            var thisAgent = agent.Owner;
            UUI otherAgent;
            var foreignagent = true;

            /* the transaction id is re-used for storing the agent */
            if (!scene.AvatarNameService.TryGetValue(req.TransactionID, out otherAgent))
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "OtherPersonIdentityIsNotKnown", "Other person's identity is not known."), m.CircuitSceneID);
                return;
            }

            if ((otherAgent.HomeURI == null && thisAgent.HomeURI == null) ||
                otherAgent.HomeURI.Equals(thisAgent.HomeURI))
            {
                /* same user service including friends */
                otherFriendsService = agent.FriendsService;
                foreignagent = false;
            }
            else if (!TryGetFriendsService(otherAgent, out otherFriendsService))
            {
                return;
            }
            var fi = new FriendInfo
            {
                User = thisAgent,
                Friend = otherAgent,
                FriendGivenFlags = 0,
                UserGivenFlags = 0,
                Secret = string.Empty
            };
            fi.User.HomeURI = null;
            fi.Friend.HomeURI = null;
            if (foreignagent)
            {
                otherFriendsService.Delete(fi);
            }
            agent.FriendsService.Delete(fi);

            var gim = new GridInstantMessage
            {
                FromAgent = thisAgent,
                ToAgent = otherAgent,
                Dialog = GridInstantMessageDialog.FriendshipDeclined,
                Message = "Friendship declined",
                IMSessionID = otherAgent.ID,
                ParentEstateID = scene.ParentEstateID,
                RegionID = scene.ID
            };
            m_IMService.Send(gim);
        }

        private void HandleTerminateFriendship(Message m)
        {
            var req = (TerminateFriendship)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(m.CircuitSceneID, out scene))
            {
                return;
            }

            IAgent agent;
            if (!scene.Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            if (agent.FriendsService == null)
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "FriendsServiceNotAccessible", "The friends service is not accessible."), m.CircuitSceneID);
                return;
            }

            FriendsServiceInterface otherFriendsService;
            var thisAgent = agent.Owner;
            UUI otherAgent;
            var foreignagent = true;

            /* the transaction id is re-used for storing the agent */
            if (!scene.AvatarNameService.TryGetValue(req.OtherID, out otherAgent))
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "OtherPersonIdentityIsNotKnown", "Other person's identity is not known."), m.CircuitSceneID);
                return;
            }

            if ((otherAgent.HomeURI == null && thisAgent.HomeURI == null) ||
                otherAgent.HomeURI.Equals(thisAgent.HomeURI))
            {
                /* same user service including friends */
                otherFriendsService = agent.FriendsService;
                foreignagent = false;
            }
            else if (!TryGetFriendsService(otherAgent, out otherFriendsService))
            {
                return;
            }
            var fi = new FriendInfo
            {
                User = thisAgent,
                Friend = otherAgent,
                FriendGivenFlags = 0,
                UserGivenFlags = 0,
                Secret = string.Empty
            };
            fi.User.HomeURI = null;
            fi.Friend.HomeURI = null;
            if (foreignagent)
            {
                otherFriendsService.Delete(fi);
            }
            agent.FriendsService.Delete(fi);

#warning Implement Terminate remote message
        }

        private void HandleGrantUserRights(Message m)
        {
            var req = (GrantUserRights)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            SceneInterface scene;
            if (!m_Scenes.TryGetValue(m.CircuitSceneID, out scene))
            {
                return;
            }

            IAgent agent;
            if (!scene.Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            if (agent.FriendsService == null)
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "FriendsServiceNotAccessible", "The friends service is not accessible."), m.CircuitSceneID);
                return;
            }

            FriendsServiceInterface otherFriendsService;
            var thisAgent = agent.Owner;
            UUI otherAgent;
            var foreignagent = true;
            GrantUserRights.RightsEntry rightsEntry;

            if(req.Rights.Count == 0)
            {
                return;
            }
            rightsEntry = req.Rights[0];

            /* the transaction id is re-used for storing the agent */
            if (!scene.AvatarNameService.TryGetValue(rightsEntry.AgentRelated, out otherAgent))
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "OtherPersonIdentityIsNotKnown", "Other person's identity is not known."), m.CircuitSceneID);
                return;
            }

            if ((otherAgent.HomeURI == null && thisAgent.HomeURI == null) ||
                otherAgent.HomeURI.Equals(thisAgent.HomeURI))
            {
                /* same user service including friends */
                otherFriendsService = agent.FriendsService;
                foreignagent = false;
            }
            else if (!TryGetFriendsService(otherAgent, out otherFriendsService))
            {
                return;
            }
            var fi = new FriendInfo
            {
                User = thisAgent,
                Friend = otherAgent,
                FriendGivenFlags = rightsEntry.RelatedRights
            };
            FriendStatus fs;
            if(agent.KnownFriends.TryGetValue(otherAgent.ID, out fs))
            {
                fi.Secret = fs.Secret;
            }
            if (foreignagent)
            {
                otherFriendsService.StoreRights(fi);
            }
            agent.FriendsService.StoreRights(fi);

#warning TODO: send GrantUserRights to friend
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_ShutdownFriends = true;
        }

        public bool TryGetFriendsService(UUI agent, out FriendsServiceInterface friendsService)
        {
            friendsService = null;
            if(agent.HomeURI == null)
            {
                return false;
            }

            string[] handlerType;
            var homeURI = agent.HomeURI.ToString();
            try
            {
                handlerType = ServicePluginHelo.HeloRequest_HandleType(homeURI);
            }
            catch
            {
                return false;
            }

            UserAgentServiceInterface userAgentService = null;

            foreach(var service in m_UserAgentPlugins)
            {
                if(handlerType.Contains(service.Name))
                {
                    userAgentService = service.Instantiate(homeURI);
                    break;
                }
            }

            if(userAgentService == null)
            {
                return false;
            }

            string friendsUri;
            Dictionary<string, string> serviceurls;
            try
            {
                serviceurls = userAgentService.GetServerURLs(agent);
            }
            catch
            {
                return false;
            }

            if(!serviceurls.TryGetValue("FriendsServerURI", out friendsUri))
            {
                return false;
            }

            try
            {
                handlerType = ServicePluginHelo.HeloRequest_HandleType(friendsUri);
            }
            catch
            {
                return false;
            }

            foreach (var service in m_FriendsPlugins)
            {
                if (handlerType.Contains(service.Name))
                {
                    friendsService = service.Instantiate(friendsUri);
                    return true;
                }
            }

            return false;
        }
    }
}
