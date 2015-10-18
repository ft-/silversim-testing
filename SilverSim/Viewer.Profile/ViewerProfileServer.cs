// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.Profile;
using SilverSim.Viewer.Messages.Search;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Groups;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Timers;
using ThreadedClasses;

namespace SilverSim.Viewer.Profile
{
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
        BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();
        bool m_ShutdownProfile;
        List<IUserAgentServicePlugin> m_UserAgentServices;
        List<IProfileServicePlugin> m_ProfileServices;
        System.Timers.Timer m_CleanupTimer = new System.Timers.Timer(10000);

        sealed class ProfileServiceData
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

        readonly RwLockedDictionary<string, ProfileServiceData> m_LastKnownProfileServices = new RwLockedDictionary<string, ProfileServiceData>();
        readonly RwLockedDictionary<UUID, KeyValuePair<UUI, int>> m_ClassifiedQueryCache = new RwLockedDictionary<UUID, KeyValuePair<UUI, int>>();

        public ViewerProfileServer()
        {

        }

        public void CleanupTimer(object sender, ElapsedEventArgs e)
        {
            List<string> removeList = new List<string>();
            List<UUID> removeClassifiedList = new List<UUID>();
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
            foreach (KeyValuePair<UUID, KeyValuePair<UUI, int>> kvp in m_ClassifiedQueryCache)
            {
                if(Environment.TickCount - kvp.Value.Value > 1800000)
                {
                    removeClassifiedList.Add(kvp.Key);
                }
            }
            foreach(UUID classifiedid in removeClassifiedList)
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
            new Thread(HandlerThread).Start();
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
        ProfileServiceData LookupProfileService(SceneInterface scene, UUID agentID, out UUI agentUUI)
        {
            ProfileServiceData serviceData = null;
            ProfileServiceInterface profileService = null;
            UserAgentServiceInterface userAgentService = null;
            agentUUI = UUI.Unknown;

            if(null == profileService)
            {
                try
                {
                    IAgent agent = scene.Agents[agentID];
                    agentUUI = agent.Owner;
                    profileService = agent.ProfileService;
                    userAgentService = agent.UserAgentService;
                    if(null == profileService)
                    {
                        profileService = new DummyProfileService();
                    }
                    if(null == userAgentService)
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

            if(null == profileService && null == userAgentService)
            {
                UUI uui;
                try
                {
                    uui = scene.AvatarNameService[agentID];
                    agentUUI = uui;

                    if (m_LastKnownProfileServices.TryGetValue(uui.HomeURI.ToString(), out serviceData))
                    {

                    }
                    else
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
                            if (null == profileService)
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
        void HandleDirClassifiedQuery(ViewerAgent agent, SceneInterface scene, Message m)
        {
            DirClassifiedQuery req = (DirClassifiedQuery)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

        }

        void HandleAvatarClassifiedsRequest(ViewerAgent agent, SceneInterface scene, GenericMessage m)
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
                reply = new AvatarClassifiedReply();
                reply.AgentID = m.AgentID;
                reply.TargetID = targetuuid;
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
                if (null == reply)
                {
                    reply = new AvatarClassifiedReply();
                    reply.AgentID = m.AgentID;
                    reply.TargetID = targetuuid;
                    messageFill = 0;
                }

                AvatarClassifiedReply.ClassifiedData d = new AvatarClassifiedReply.ClassifiedData();
                d.ClassifiedID = classified.Key;
                d.Name = classified.Value;
                reply.Data.Add(d);
                m_ClassifiedQueryCache[classified.Key] = new KeyValuePair<UUI, int>(uui, Environment.TickCount);
                messageFill += entryLen;
            }

            if (null != reply)
            {
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        void HandleClassifiedInfoRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            ClassifiedInfoRequest req = (ClassifiedInfoRequest)m;
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
                ClassifiedInfoReply reply = new ClassifiedInfoReply();
                reply.AgentID = req.AgentID;

                reply.ClassifiedID = cls.ClassifiedID;
                reply.CreatorID = cls.Creator.ID;
                reply.CreationDate = cls.CreationDate;
                reply.ExpirationDate = cls.ExpirationDate;
                reply.Category = cls.Category;
                reply.Name = cls.Name;
                reply.Description = cls.Description;
                reply.ParcelID = cls.ParcelID;
                reply.ParentEstate = cls.ParentEstate;
                reply.SnapshotID = cls.SnapshotID;
                reply.SimName = cls.SimName;
                reply.PosGlobal = cls.GlobalPos;
                reply.ParcelName = cls.ParcelName;
                reply.ClassifiedFlags = cls.Flags;
                reply.PriceForListing = cls.Price;
                agent.SendMessageAlways(reply, scene.ID);
            }
            catch
            {

            }
        }

        void HandleClassifiedInfoUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            ClassifiedInfoUpdate req = (ClassifiedInfoUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(null == agent.ProfileService)
            {
                return;
            }

            try
            {
                ProfileClassified classified = agent.ProfileService.Classifieds[agent.Owner, req.ClassifiedID];
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

        void HandleClassifiedDelete(ViewerAgent agent, SceneInterface scene, Message m)
        {
            ClassifiedDelete req = (ClassifiedDelete)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(null == agent.ProfileService)
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

        void HandleClassifiedGodDelete(ViewerAgent agent, SceneInterface scene, Message m)
        {
            ClassifiedGodDelete req = (ClassifiedGodDelete)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

        }
        #endregion

        #region Notes
        void HandleAvatarNotesRequest(ViewerAgent agent, SceneInterface scene, GenericMessage m)
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


            AvatarNotesReply reply = new AvatarNotesReply();
            reply.AgentID = m.AgentID;
            reply.TargetID = targetuui.ID;
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

        void HandleAvatarNotesUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            AvatarNotesUpdate req = (AvatarNotesUpdate)m;
            if(req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if (null == agent.ProfileService)
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
        void HandleAvatarPicksRequest(ViewerAgent agent, SceneInterface scene, GenericMessage m)
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
                reply = new AvatarPicksReply();
                reply.AgentID = m.AgentID;
                reply.TargetID = targetuuid;
                agent.SendMessageAlways(reply, scene.ID);
                return;
            }
            foreach(KeyValuePair<UUID, string> pick in picks)
            {
                int entryLen = pick.Value.Length + 18;
                if((entryLen + messageFill > 1400 || reply.Data.Count == 255) && reply != null)
                {
                    agent.SendMessageAlways(reply, scene.ID);
                    reply = null;
                }
                if(null == reply)
                {
                    reply = new AvatarPicksReply();
                    reply.AgentID = m.AgentID;
                    reply.TargetID = targetuuid;
                    messageFill = 0;
                }

                AvatarPicksReply.PickData d = new AvatarPicksReply.PickData();
                d.PickID = pick.Key;
                d.Name = pick.Value;
                reply.Data.Add(d);
                messageFill += entryLen;
            }

            if(null != reply)
            {
                agent.SendMessageAlways(reply, scene.ID);
            }
        }

        void HandlePickInfoRequest(ViewerAgent agent, SceneInterface scene, GenericMessage m)
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
                ProfilePick pick = serviceData.ProfileService.Picks[uui, pickid];
                PickInfoReply reply = new PickInfoReply();
                reply.AgentID = m.AgentID;
                reply.CreatorID = pick.Creator.ID;
                reply.Description = pick.Description;
                reply.IsEnabled = pick.Enabled;
                reply.Name = pick.Name;
                reply.OriginalName = pick.OriginalName;
                reply.ParcelID = pick.ParcelID;
                reply.PickID = pick.PickID;
                reply.PosGlobal = pick.GlobalPosition;
                reply.SnapshotID = pick.SnapshotID;
                reply.SortOrder = pick.SortOrder;
                reply.TopPick = pick.TopPick;
                reply.User = string.Empty;
                agent.SendMessageAlways(reply, scene.ID);
            }
            catch
            {

            }
        }

        void HandlePickInfoUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            PickInfoUpdate req = (PickInfoUpdate)m;
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
                ProfilePick pick = agent.ProfileService.Picks[agent.Owner, req.PickID];
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

        void HandlePickDelete(ViewerAgent agent, SceneInterface scene, Message m)
        {
            PickDelete req = (PickDelete)m;
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

        void HandlePickGodDelete(ViewerAgent agent, SceneInterface scene, Message m)
        {
            PickGodDelete req = (PickGodDelete)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

        }
        #endregion

        #region User Info
        void HandleUserInfoRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            UserInfoRequest req = (UserInfoRequest)m;
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
                prefs = new ProfilePreferences();
                prefs.IMviaEmail = false;
                prefs.Visible = false;
                prefs.User = agent.Owner;
            }

            UserInfoReply reply = new UserInfoReply();
            reply.AgentID = req.AgentID;
            if (prefs.Visible)
            {
                reply.DirectoryVisibility = "default";
            }
            else
            {
                reply.DirectoryVisibility = "hidden";
            }
            reply.EMail = string.Empty;
            reply.IMViaEmail = prefs.IMviaEmail;
            agent.SendMessageAlways(reply, scene.ID);
        }

        void HandleUpdateUserInfo(ViewerAgent agent, SceneInterface scene, Message m)
        {
            UpdateUserInfo req = (UpdateUserInfo)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(null == agent.ProfileService)
            {
                return;
            }

            ProfilePreferences prefs = new ProfilePreferences();
            prefs.User = agent.Owner;
            prefs.IMviaEmail = req.IMViaEmail;
            prefs.Visible = req.DirectoryVisibility != "hidden";
            
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
        void HandleAvatarPropertiesRequest(ViewerAgent agent, SceneInterface scene, Message m)
        {
            AvatarPropertiesRequest req = (AvatarPropertiesRequest)m;
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
                userInfo = new UserAgentServiceInterface.UserInfo();
                userInfo.FirstName = uui.FirstName;
                userInfo.LastName = uui.LastName;
                userInfo.UserFlags = 0;
                userInfo.UserCreated = new Date();
                userInfo.UserTitle = string.Empty;
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
                props = new ProfileProperties();
                props.ImageID = "5748decc-f629-461c-9a36-a35a221fe21f";
                props.FirstLifeImageID = "5748decc-f629-461c-9a36-a35a221fe21f";
                props.User = uui;
                props.Partner = UUI.Unknown;
                props.AboutText = string.Empty;
                props.FirstLifeText = string.Empty;
                props.Language = string.Empty;
                props.WantToText = string.Empty;
                props.SkillsText = string.Empty;
                props.WebUrl = string.Empty;
            }

            AvatarPropertiesReply res = new AvatarPropertiesReply();
            res.AgentID = req.AgentID;
            res.AvatarID = req.AvatarID;

            res.ImageID = props.ImageID;
            res.FLImageID = props.FirstLifeImageID;
            res.PartnerID = props.Partner.ID;
            res.AboutText = props.AboutText;
            res.FLAboutText = props.FirstLifeText;
            res.BornOn = userInfo.UserCreated.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
            res.ProfileURL = props.WebUrl;
            res.CharterMember = new byte[] { 0 };
            res.Flags = 0;

            agent.SendMessageAlways(res, scene.ID);

            AvatarInterestsReply res2 = new AvatarInterestsReply();
            res2.AgentID = req.AgentID;
            res2.AvatarID = req.AvatarID;
            agent.SendMessageAlways(res2, scene.ID);


            AvatarGroupsReply res3 = new AvatarGroupsReply();
            res3.AgentID = req.AgentID;
            res3.AvatarID = req.AvatarID;
            /* when the scene has a groups service, we check which groups the avatar has */
            if(null != scene.GroupsService)
            {
                try
                {
                    List<GroupMembership> gmems = scene.GroupsService.Memberships[uui, uui];

                    foreach(GroupMembership gmem in gmems)
                    {
                        AvatarGroupsReply.GroupDataEntry d = new AvatarGroupsReply.GroupDataEntry();
                        d.GroupPowers = gmem.GroupPowers;
                        d.AcceptNotices = gmem.AcceptNotices;
                        d.GroupTitle = gmem.GroupTitle;
                        d.GroupName = gmem.Group.GroupName;
                        d.GroupInsigniaID = gmem.GroupInsigniaID;
                        d.ListInProfile = gmem.ListInProfile;
                        res3.GroupData.Add(d);
                    }

                }
                catch
#if DEBUG
                    (Exception e)
#endif
                {
#if DEBUG
                    m_Log.Debug("Exception at groups request", e);
#endif
                }
            }
            agent.SendMessageAlways(res3, scene.ID);
        }

        void HandleAvatarPropertiesUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            AvatarPropertiesUpdate req = (AvatarPropertiesUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(agent.ProfileService == null)
            {
                return;
            }

            ProfileProperties props = new ProfileProperties();
            props.ImageID = UUID.Zero;
            props.FirstLifeImageID = UUID.Zero;
            props.Partner = UUI.Unknown;
            props.User = agent.Owner;
            props.SkillsText = string.Empty;
            props.WantToText = string.Empty;
            props.Language = string.Empty;

            props.AboutText = req.AboutText;
            props.FirstLifeText = req.FLAboutText;
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

        void HandleAvatarInterestsUpdate(ViewerAgent agent, SceneInterface scene, Message m)
        {
            AvatarInterestsUpdate req = (AvatarInterestsUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            if(agent.ProfileService == null)
            {
                return;
            }

            ProfileProperties props = new ProfileProperties();
            props.ImageID = UUID.Zero;
            props.FirstLifeImageID = UUID.Zero;
            props.FirstLifeText = string.Empty;
            props.AboutText = string.Empty;
            props.Partner = UUI.Unknown;
            props.User = agent.Owner;
            props.SkillsMask = req.SkillsMask;
            props.SkillsText = req.SkillsText;
            props.WantToMask = req.WantToMask;
            props.WantToText = req.WantToText;
            props.Language = req.LanguagesText;
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

        public ShutdownOrder ShutdownOrder
        {
            get 
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_CleanupTimer.Stop();
            m_CleanupTimer.Elapsed -= CleanupTimer;
            m_ShutdownProfile = true;
        }
    }

    [PluginName("ViewerProfileServer")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerProfileServer();
        }
    }
}
