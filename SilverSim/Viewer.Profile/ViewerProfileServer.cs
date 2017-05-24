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
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Profile;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.Profile;
using SilverSim.Viewer.Messages.Search;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Timers;

namespace SilverSim.Viewer.Profile
{
    [Description("Viewer Profile Handler")]
    [PluginName("ViewerProfileServer")]
    public class ViewerProfileServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL PROFILE");

        [PacketHandler(MessageType.DirClassifiedQuery)]
        [PacketHandler(MessageType.ClassifiedInfoRequest)]
        [PacketHandler(MessageType.ClassifiedInfoUpdate)]
        [PacketHandler(MessageType.ClassifiedDelete)]
        [PacketHandler(MessageType.ClassifiedGodDelete)]
        [PacketHandler(MessageType.AvatarPropertiesRequest)]
        [PacketHandler(MessageType.AvatarPropertiesUpdate)]
        [PacketHandler(MessageType.AvatarInterestsUpdate)]
        [PacketHandler(MessageType.AvatarNotesUpdate)]
        [PacketHandler(MessageType.PickInfoUpdate)]
        [PacketHandler(MessageType.PickDelete)]
        [PacketHandler(MessageType.PickGodDelete)]
        [PacketHandler(MessageType.UserInfoRequest)]
        [PacketHandler(MessageType.UpdateUserInfo)]
        [GenericMessageHandler("avatarclassifiedsrequest")]
        [GenericMessageHandler("avatarpicksrequest")]
        [GenericMessageHandler("pickinforequest")]
        [GenericMessageHandler("avatarnotesrequest")]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();
        private bool m_ShutdownProfile;
        private List<IUserAgentServicePlugin> m_UserAgentServices;
        private List<IProfileServicePlugin> m_ProfileServices;
        private readonly System.Timers.Timer m_CleanupTimer = new System.Timers.Timer(10000);

        private sealed class ProfileServiceData
        {
            public ProfileServiceInterface ProfileService;
            public UserAgentServiceInterface UserAgentService;
            public int TicksAt;

            public ProfileServiceData(UserAgentServiceInterface userAgent, ProfileServiceInterface profileService)
            {
                UserAgentService = userAgent;
                ProfileService = profileService;
                TicksAt = Environment.TickCount;
            }
        }

        private readonly RwLockedDictionary<string, ProfileServiceData> m_LastKnownProfileServices = new RwLockedDictionary<string, ProfileServiceData>();
        private readonly RwLockedDictionary<UUID, KeyValuePair<UUI, int>> m_ClassifiedQueryCache = new RwLockedDictionary<UUID, KeyValuePair<UUI, int>>();

        public void CleanupTimer(object sender, ElapsedEventArgs e)
        {
            var removeList = new List<string>();
            var removeClassifiedList = new List<UUID>();
            foreach(KeyValuePair<string, ProfileServiceData> kvp in m_LastKnownProfileServices)
            {
                if(Environment.TickCount - kvp.Value.TicksAt > 60000)
                {
                    removeList.Add(kvp.Key);
                }
            }
            foreach(string rem in removeList)
            {
                m_LastKnownProfileServices.Remove(rem);
            }

            /* remove classifieds query caches after half an hour */
            foreach (var kvp in m_ClassifiedQueryCache)
            {
                if(Environment.TickCount - kvp.Value.Value > 1800000)
                {
                    removeClassifiedList.Add(kvp.Key);
                }
            }
            foreach(var classifiedid in removeClassifiedList)
            {
                m_ClassifiedQueryCache.Remove(classifiedid);
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_UserAgentServices = loader.GetServicesByValue<IUserAgentServicePlugin>();
            m_ProfileServices = loader.GetServicesByValue<IProfileServicePlugin>();
            m_CleanupTimer.Elapsed += CleanupTimer;
            m_CleanupTimer.Start();
            ThreadManager.CreateThread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Profile Handler Thread";

            while (!m_ShutdownProfile)
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
                SceneInterface scene = req.Key.Scene;
                if(scene == null)
                {
                    continue;
                }
                try
                {
                    switch (m.Number)
                    {
                        case MessageType.AvatarPropertiesRequest:
                            HandleAvatarPropertiesRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.AvatarPropertiesUpdate:
                            HandleAvatarPropertiesUpdate(req.Key.Agent, scene, m);
                            break;

                        case MessageType.AvatarInterestsUpdate:
                            HandleAvatarInterestsUpdate(req.Key.Agent, scene, m);
                            break;

                        case MessageType.UserInfoRequest:
                            HandleUserInfoRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.UpdateUserInfo:
                            HandleUpdateUserInfo(req.Key.Agent, scene, m);
                            break;

                        case MessageType.PickInfoUpdate:
                            HandlePickInfoUpdate(req.Key.Agent, scene, m);
                            break;

                        case MessageType.PickDelete:
                            HandlePickDelete(req.Key.Agent, scene, m);
                            break;

                        case MessageType.PickGodDelete:
                            HandlePickGodDelete(req.Key.Agent, scene, m);
                            break;

                        case MessageType.AvatarNotesUpdate:
                            HandleAvatarNotesUpdate(req.Key.Agent, scene, m);
                            break;

                        case MessageType.DirClassifiedQuery:
                            HandleDirClassifiedQuery(req.Key.Agent, scene, m);
                            break;

                        case MessageType.ClassifiedInfoRequest:
                            HandleClassifiedInfoRequest(req.Key.Agent, scene, m);
                            break;

                        case MessageType.ClassifiedInfoUpdate:
                            HandleClassifiedInfoUpdate(req.Key.Agent, scene, m);
                            break;

                        case MessageType.ClassifiedDelete:
                            HandleClassifiedDelete(req.Key.Agent, scene, m);
                            break;

                        case MessageType.ClassifiedGodDelete:
                            HandleClassifiedGodDelete(req.Key.Agent, scene, m);
                            break;

                        case MessageType.GenericMessage:
                            {
                                GenericMessage gm = (GenericMessage)m;
                                switch(gm.Method)
                                {
                                    case "avatarclassifiedsrequest":
                                        HandleAvatarClassifiedsRequest(req.Key.Agent, scene, gm);
                                        break;

                                    case "avatarpicksrequest":
                                        HandleAvatarPicksRequest(req.Key.Agent, scene, gm);
                                        break;

                                    case "pickinforequest":
                                        HandlePickInfoRequest(req.Key.Agent, scene, gm);
                                        break;

                                    case "avatarnotesrequest":
                                        HandleAvatarNotesRequest(req.Key.Agent, scene, gm);
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

        #region Lookup actual service for profile
        private ProfileServiceData LookupProfileService(SceneInterface scene, UUID agentID, out UUI agentUUI)
        {
            ProfileServiceData serviceData = null;
            ProfileServiceInterface profileService = null;
            UserAgentServiceInterface userAgentService = null;
            agentUUI = UUI.Unknown;

            if(profileService == null)
            {
                try
                {
                    IAgent agent = scene.Agents[agentID];
                    agentUUI = agent.Owner;
                    profileService = agent.ProfileService;
                    userAgentService = agent.UserAgentService;
                    if(profileService == null)
                    {
                        profileService = new DummyProfileService();
                    }
                    if(userAgentService == null)
                    {
                        userAgentService = new DummyUserAgentService();
                    }
                    serviceData = new ProfileServiceData(userAgentService, profileService);
                }
                catch
                {
                    agentUUI = UUI.Unknown;
                }
            }

            if(profileService == null && userAgentService == null)
            {
                UUI uui;
                try
                {
                    uui = scene.AvatarNameService[agentID];
                    agentUUI = uui;

                    if (!m_LastKnownProfileServices.TryGetValue(uui.HomeURI.ToString(), out serviceData))
                    {
                        foreach (IUserAgentServicePlugin userAgentPlugin in m_UserAgentServices)
                        {
                            if (userAgentPlugin.IsProtocolSupported(uui.HomeURI.ToString()))
                            {
                                userAgentService = userAgentPlugin.Instantiate(uui.HomeURI.ToString());
                                break;
                            }
                        }

                        Dictionary<string, string> urls = userAgentService.GetServerURLs(uui);
                        if (urls.ContainsKey("ProfileServerURI"))
                        {
                            string profileServerURI = urls["ProfileServerURI"];
                            foreach (IProfileServicePlugin profilePlugin in m_ProfileServices)
                            {
                                if (profilePlugin.IsProtocolSupported(profileServerURI))
                                {
                                    profileService = profilePlugin.Instantiate(profileServerURI);
                                }
                            }
                        }

                        if (userAgentService != null)
                        {
                            if (profileService == null)
                            {
                                profileService = new DummyProfileService();
                            }

                            serviceData = new ProfileServiceData(userAgentService, profileService);
                            m_LastKnownProfileServices.Add(uui.HomeURI.ToString(), serviceData);
                        }
                    }
                }
                catch
                {
                    agentUUI = UUI.Unknown;
                }
            }

            return serviceData;
        }
        #endregion

        #region Classifieds
        private void HandleDirClassifiedQuery(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (DirClassifiedQuery)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

        }

        private void HandleAvatarClassifiedsRequest(ViewerAgent agent, SceneInterface scene, GenericMessage m)
        {
            if(m.AgentID != m.CircuitAgentID ||
                m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            if (m.ParamList.Count < 1)
            {
                return;
            }
            string arg = Encoding.UTF8.GetString(m.ParamList[0]);
            UUID targetuuid;
            if (!UUID.TryParse(arg, out targetuuid))
            {
                return;
            }

            ProfileServiceData serviceData;
            UUI uui;
            try
            {
                serviceData = LookupProfileService(scene, targetuuid, out uui);
            }
            catch
            {
                return;
            }

            int messageFill = 0;
            AvatarClassifiedReply reply = null;

            Dictionary<UUID, string> classifieds;
            try
            {
                classifieds = serviceData.ProfileService.Classifieds.GetClassifieds(uui);
            }
            catch
            {
                reply = new AvatarClassifiedReply()
                {
                    AgentID = m.AgentID,
                    TargetID = targetuuid
                };
                agent.SendMessageAlways(reply, scene.ID);
                return;
            }
            foreach (KeyValuePair<UUID, string> classified in classifieds)
            {
                int entryLen = classified.Value.Length + 18;
                if ((entryLen + messageFill > 1400 || reply.Data.Count == 255) && reply != null)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                    reply = null;
                }
                if (reply == null)
                {
                    reply = new AvatarClassifiedReply()
                    {
                        AgentID = m.AgentID,
                        TargetID = targetuuid
                    };
                    messageFill = 0;
                }

                var d = new AvatarClassifiedReply.ClassifiedData()
                {
                    ClassifiedID = classified.Key,
                    Name = classified.Value
                };
                reply.Data.Add(d);
                m_ClassifiedQueryCache[classified.Key] = new KeyValuePair<UUI, int>(uui, Environment.TickCount);
                messageFill += entryLen;
            }

            if (reply != null)
            {
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        private void HandleClassifiedInfoRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (ClassifiedInfoRequest)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            KeyValuePair<UUI, int> kvp;
            if(!m_ClassifiedQueryCache.TryGetValue(req.ClassifiedID, out kvp))
            {
                return;
            }

            ProfileServiceData serviceData;
            UUI uui;
            try
            {
                serviceData = LookupProfileService(scene, kvp.Key.ID, out uui);
            }
            catch
            {
                return;
            }

            try
            {
                ProfileClassified cls = serviceData.ProfileService.Classifieds[kvp.Key, req.ClassifiedID];
                var reply = new ClassifiedInfoReply()
                {
                    AgentID = req.AgentID,

                    ClassifiedID = cls.ClassifiedID,
                    CreatorID = cls.Creator.ID,
                    CreationDate = cls.CreationDate,
                    ExpirationDate = cls.ExpirationDate,
                    Category = cls.Category,
                    Name = cls.Name,
                    Description = cls.Description,
                    ParcelID = cls.ParcelID,
                    ParentEstate = cls.ParentEstate,
                    SnapshotID = cls.SnapshotID,
                    SimName = cls.SimName,
                    PosGlobal = cls.GlobalPos,
                    ParcelName = cls.ParcelName,
                    ClassifiedFlags = cls.Flags,
                    PriceForListing = cls.Price
                };
                agent.SendMessageAlways(reply, scene.ID);
            }
            catch
            {
                /* do not expose exceptions to caller */
            }
        }

        private void HandleClassifiedInfoUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (ClassifiedInfoUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(agent.ProfileService == null)
            {
                return;
            }

            try
            {
                var classified = agent.ProfileService.Classifieds[agent.Owner, req.ClassifiedID];
                classified.ClassifiedID = req.ClassifiedID;
                classified.Category = req.Category;
                classified.Name = req.Name;
                classified.Description = req.Description;
                classified.ParcelID = req.ParcelID;
                classified.ParentEstate = req.ParentEstate;
                classified.SnapshotID = req.SnapshotID;
                classified.GlobalPos = req.PosGlobal;
                classified.Flags = req.ClassifiedFlags;
                classified.Price = req.PriceForListing;
                agent.ProfileService.Classifieds.Update(classified);
            }
            catch
            {
                agent.SendAlertMessage("Error updating classified", scene.ID);
            }
        }

        private void HandleClassifiedDelete(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (ClassifiedDelete)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(agent.ProfileService == null)
            {
                return;
            }

            try
            {
                agent.ProfileService.Classifieds.Delete(req.ClassifiedID);
            }
            catch
            {
                agent.SendAlertMessage("Error deleting classified", scene.ID);
            }
        }

        private void HandleClassifiedGodDelete(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (ClassifiedGodDelete)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

        }
        #endregion

        #region Notes
        private void HandleAvatarNotesRequest(ViewerAgent agent, SceneInterface scene, GenericMessage m)
        {
            if (m.AgentID != m.CircuitAgentID ||
                m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            if(m.ParamList.Count < 1)
            {
                return;
            }
            string arg = Encoding.UTF8.GetString(m.ParamList[0]);
            UUID targetuuid;
            if(!UUID.TryParse(arg, out targetuuid))
            {
                return;
            }

            UUI targetuui;
            try
            {
                targetuui = scene.AvatarNameService[targetuuid];
            }
            catch
            {
                targetuui = new UUI(targetuuid);
            }

            var reply = new AvatarNotesReply()
            {
                AgentID = m.AgentID,
                TargetID = targetuui.ID
            };
            try
            {
                reply.Notes = agent.ProfileService.Notes[agent.Owner, targetuui];
            }
            catch /* yes, we are catching a NullReferenceException here too */
            {
                reply.Notes = string.Empty;
            }
            agent.SendMessageAlways(reply, scene.ID);
        }

        private void HandleAvatarNotesUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (AvatarNotesUpdate)m;
            if(req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if (agent.ProfileService == null)
            {
                return;
            }

            try
            {
                agent.ProfileService.Notes[agent.Owner, new UUI(req.TargetID)] = req.Notes;
            }
            catch
            {
                agent.SendAlertMessage("Error updating notes", scene.ID);
            }
        }
        #endregion

        #region Picks
        private void HandleAvatarPicksRequest(ViewerAgent agent, SceneInterface scene, GenericMessage m)
        {
            if(m.AgentID != m.CircuitAgentID ||
                m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            if (m.ParamList.Count < 1)
            {
                return;
            }
            string arg = Encoding.UTF8.GetString(m.ParamList[0]);
            UUID targetuuid;
            if (!UUID.TryParse(arg, out targetuuid))
            {
                return;
            }

            ProfileServiceData serviceData;
            UUI uui;
            try
            {
                serviceData = LookupProfileService(scene, targetuuid, out uui);
            }
            catch
            {
                return;
            }

            int messageFill = 0;
            AvatarPicksReply reply = null;

            Dictionary<UUID, string> picks;
            try
            {
                picks = serviceData.ProfileService.Picks.GetPicks(uui);
            }
            catch
            {
                reply = new AvatarPicksReply()
                {
                    AgentID = m.AgentID,
                    TargetID = targetuuid
                };
                agent.SendMessageAlways(reply, scene.ID);
                return;
            }
            foreach(var pick in picks)
            {
                int entryLen = pick.Value.Length + 18;
                if((entryLen + messageFill > 1400 || reply.Data.Count == 255) && reply != null)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                    reply = null;
                }
                if(reply == null)
                {
                    reply = new AvatarPicksReply()
                    {
                        AgentID = m.AgentID,
                        TargetID = targetuuid
                    };
                    messageFill = 0;
                }

                var d = new AvatarPicksReply.PickData()
                {
                    PickID = pick.Key,
                    Name = pick.Value
                };
                reply.Data.Add(d);
                messageFill += entryLen;
            }

            if(reply != null)
            {
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        private void HandlePickInfoRequest(ViewerAgent agent, SceneInterface scene, GenericMessage m)
        {
            if (m.AgentID != m.CircuitAgentID ||
                m.SessionID != m.CircuitSessionID)
            {
                return;
            }

            if (m.ParamList.Count < 2)
            {
                return;
            }
            string arg = Encoding.UTF8.GetString(m.ParamList[0]);
            UUID targetuuid;
            if (!UUID.TryParse(arg, out targetuuid))
            {
                return;
            }

            arg = Encoding.UTF8.GetString(m.ParamList[1]);
            UUID pickid;
            if (!UUID.TryParse(arg, out pickid))
            {
                return;
            }

            ProfileServiceData serviceData;
            UUI uui;
            try
            {
                serviceData = LookupProfileService(scene, targetuuid, out uui);
            }
            catch
            {
                return;
            }

            try
            {
                var pick = serviceData.ProfileService.Picks[uui, pickid];
                var reply = new PickInfoReply()
                {
                    AgentID = m.AgentID,
                    CreatorID = pick.Creator.ID,
                    Description = pick.Description,
                    IsEnabled = pick.Enabled,
                    Name = pick.Name,
                    OriginalName = pick.OriginalName,
                    ParcelID = pick.ParcelID,
                    PickID = pick.PickID,
                    PosGlobal = pick.GlobalPosition,
                    SnapshotID = pick.SnapshotID,
                    SortOrder = pick.SortOrder,
                    TopPick = pick.TopPick,
                    User = string.Empty
                };
                agent.SendMessageAlways(reply, scene.ID);
            }
            catch
            {
                /* do not expose exceptions to caller */
            }
        }

        private void HandlePickInfoUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (PickInfoUpdate)m;
            if(req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(agent.ProfileService == null)
            {
                return;
            }

            try
            {
                var pick = agent.ProfileService.Picks[agent.Owner, req.PickID];
                pick.TopPick = req.TopPick;
                pick.ParcelID = req.ParcelID;
                pick.Name = req.Name;
                pick.Description = req.Description;
                pick.SnapshotID = req.SnapshotID;
                pick.GlobalPosition = req.PosGlobal;
                pick.SortOrder = req.SortOrder;
                pick.Enabled = req.IsEnabled;
                agent.ProfileService.Picks.Update(pick);
            }
            catch
            {
                agent.SendAlertMessage("Error updating pick", scene.ID);
            }
        }

        private void HandlePickDelete(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (PickDelete)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if (agent.ProfileService == null)
            {
                return;
            }

            try
            {
                agent.ProfileService.Picks.Delete(req.PickID);
            }
            catch
            {
                agent.SendAlertMessage("Error deleting pick", scene.ID);
            }
        }

        private void HandlePickGodDelete(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (PickGodDelete)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

        }
        #endregion

        #region User Info
        private void HandleUserInfoRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (UserInfoRequest)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            ProfilePreferences prefs;
            try
            {
                prefs = agent.ProfileService.Preferences[agent.Owner];
            }
            catch /* yes, we are catching a NullReferenceException here too */
            {
                prefs = new ProfilePreferences()
                {
                    IMviaEmail = false,
                    Visible = false,
                    User = agent.Owner
                };
            }

            var reply = new UserInfoReply()
            {
                AgentID = req.AgentID,
                DirectoryVisibility = (prefs.Visible) ?
                "default" :
                "hidden",
                EMail = string.Empty,
                IMViaEmail = prefs.IMviaEmail
            };
            agent.SendMessageAlways(reply, scene.ID);
        }

        private void HandleUpdateUserInfo(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (UpdateUserInfo)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(agent.ProfileService == null)
            {
                return;
            }

            var prefs = new ProfilePreferences()
            {
                User = agent.Owner,
                IMviaEmail = req.IMViaEmail,
                Visible = req.DirectoryVisibility != "hidden"
            };
            try
            {
                agent.ProfileService.Preferences[agent.Owner] = prefs;
            }
            catch
            {
                agent.SendAlertMessage("Error updating preferences", scene.ID);
            }
        }
        #endregion

        #region Avatar Properties
        private void HandleAvatarPropertiesRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (AvatarPropertiesRequest)m;
            if(req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            ProfileServiceData serviceData;
            UUI uui;
            try
            {
                serviceData = LookupProfileService(scene, req.AvatarID, out uui);
            }
            catch
            {
                return;
            }

            UserAgentServiceInterface.UserInfo userInfo;
            ProfileProperties props;

            try
            {
                userInfo = serviceData.UserAgentService.GetUserInfo(uui);
            }
            catch
#if DEBUG
 (Exception e)
#endif
            {
#if DEBUG
                m_Log.Debug("Exception at userinfo request", e);
#endif
                userInfo = new UserAgentServiceInterface.UserInfo()
                {
                    FirstName = uui.FirstName,
                    LastName = uui.LastName,
                    UserFlags = 0,
                    UserCreated = new Date(),
                    UserTitle = string.Empty
                };
            }

            try
            {
                props = serviceData.ProfileService.Properties[uui];
            }
            catch
#if DEBUG
                (Exception e)
#endif
            {
#if DEBUG
                m_Log.Debug("Exception at properties request", e);
#endif
                props = new ProfileProperties()
                {
                    ImageID = "5748decc-f629-461c-9a36-a35a221fe21f",
                    FirstLifeImageID = "5748decc-f629-461c-9a36-a35a221fe21f",
                    User = uui,
                    Partner = UUI.Unknown,
                    AboutText = string.Empty,
                    FirstLifeText = string.Empty,
                    Language = string.Empty,
                    WantToText = string.Empty,
                    SkillsText = string.Empty,
                    WebUrl = string.Empty
                };
            }

            var res = new AvatarPropertiesReply()
            {
                AgentID = req.AgentID,
                AvatarID = req.AvatarID,

                ImageID = props.ImageID,
                FLImageID = props.FirstLifeImageID,
                PartnerID = props.Partner.ID,
                AboutText = props.AboutText,
                FLAboutText = props.FirstLifeText,
                BornOn = userInfo.UserCreated.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                ProfileURL = props.WebUrl,
                CharterMember = new byte[] { 0 },
                Flags = 0
            };
            agent.SendMessageAlways(res, scene.ID);

            var res2 = new AvatarInterestsReply()
            {
                AgentID = req.AgentID,
                AvatarID = req.AvatarID
            };
            agent.SendMessageAlways(res2, scene.ID);

            var res3 = new AvatarGroupsReply()
            {
                AgentID = req.AgentID,
                AvatarID = req.AvatarID
            };
            /* when the scene has a groups service, we check which groups the avatar has */
            if (scene.GroupsService != null)
            {
                try
                {
                    foreach(var gmem in scene.GroupsService.Memberships[uui, uui])
                    {
                        res3.GroupData.Add(new AvatarGroupsReply.GroupDataEntry()
                        {
                            GroupPowers = gmem.GroupPowers,
                            AcceptNotices = gmem.IsAcceptNotices,
                            GroupTitle = gmem.GroupTitle,
                            GroupName = gmem.Group.GroupName,
                            GroupInsigniaID = gmem.GroupInsigniaID,
                            ListInProfile = gmem.IsListInProfile
                        });
                    }
                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
                    /* do not expose exceptions to caller */
#if DEBUG
                    m_Log.Debug("Exception at groups request", e);
#endif
                }
            }
            agent.SendMessageAlways(res3, scene.ID);
        }

        private void HandleAvatarPropertiesUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (AvatarPropertiesUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(agent.ProfileService == null)
            {
                return;
            }

            var props = new ProfileProperties()
            {
                ImageID = UUID.Zero,
                FirstLifeImageID = UUID.Zero,
                Partner = UUI.Unknown,
                User = agent.Owner,
                SkillsText = string.Empty,
                WantToText = string.Empty,
                Language = string.Empty,

                AboutText = req.AboutText,
                FirstLifeText = req.FLAboutText
            };
            props.ImageID = req.ImageID;
            props.PublishMature = req.MaturePublish;
            props.PublishProfile = req.AllowPublish;
            props.WebUrl = req.ProfileURL;

            try
            {
                agent.ProfileService.Properties[agent.Owner, ProfileServiceInterface.PropertiesUpdateFlags.Properties] = props;
            }
            catch
            {
                agent.SendAlertMessage("Error updating properties", scene.ID);
            }
        }

        private void HandleAvatarInterestsUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            var req = (AvatarInterestsUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(agent.ProfileService == null)
            {
                return;
            }

            var props = new ProfileProperties()
            {
                ImageID = UUID.Zero,
                FirstLifeImageID = UUID.Zero,
                FirstLifeText = string.Empty,
                AboutText = string.Empty,
                Partner = UUI.Unknown,
                User = agent.Owner,
                SkillsMask = req.SkillsMask,
                SkillsText = req.SkillsText,
                WantToMask = req.WantToMask,
                WantToText = req.WantToText,
                Language = req.LanguagesText
            };
            try
            {
                agent.ProfileService.Properties[agent.Owner, ProfileServiceInterface.PropertiesUpdateFlags.Interests] = props;
            }
            catch
            {
                agent.SendAlertMessage("Error updating interests", scene.ID);
            }
        }
        #endregion

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_CleanupTimer.Stop();
            m_CleanupTimer.Elapsed -= CleanupTimer;
            m_ShutdownProfile = true;
        }
    }
}
