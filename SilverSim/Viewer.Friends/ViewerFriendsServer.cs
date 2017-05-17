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
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Friends;
using SilverSim.Types.IM;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Friend;
using SilverSim.Viewer.Messages.IM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace SilverSim.Viewer.Friends
{
    [Description("Viewer Friends Handler")]
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
        readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        IMServiceInterface m_IMService;
        readonly string m_IMServiceName;
        List<IFriendsServicePlugin> m_FriendsPlugins;
        List<IUserAgentServicePlugin> m_UserAgentPlugins;
        SceneList m_Scenes;

        bool m_ShutdownFriends;

        public ViewerFriendsServer(string imService)
        {
            m_IMServiceName = imService;
        }

        public void Startup(ConfigurationLoader loader)
        { 
            m_Scenes = loader.Scenes;
            m_IMService = loader.GetService<IMServiceInterface>(m_IMServiceName);
            m_FriendsPlugins = loader.GetServicesByValue<IFriendsServicePlugin>();
            m_UserAgentPlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
            ThreadManager.CreateThread(HandlerThread).Start();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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

        void HandleFriendshipOffered(ImprovedInstantMessage m)
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
            var fi = new FriendInfo()
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

        void HandleAcceptFriendship(Message m)
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
            var fi = new FriendInfo()
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

            var gim = new GridInstantMessage()
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

        void HandleDeclineFriendship(Message m)
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
            var fi = new FriendInfo()
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

            var gim = new GridInstantMessage()
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

        void HandleTerminateFriendship(Message m)
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
            var fi = new FriendInfo()
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

        void HandleGrantUserRights(Message m)
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
            var fi = new FriendInfo()
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

        public ShutdownOrder ShutdownOrder
        {
            get 
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_ShutdownFriends = true;
        }

        public bool TryGetFriendsService(UUI agent, out FriendsServiceInterface friendsService)
        {
            friendsService = null;
            if(null == agent.HomeURI)
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

            if(null == userAgentService)
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

    [PluginName("ViewerFriendsServer")]
    public class Factory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerFriendsServer(ownSection.GetString("IMService", "IMService"));
        }
    }
}
