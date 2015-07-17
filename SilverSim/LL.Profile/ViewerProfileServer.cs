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
using SilverSim.LL.Messages.Profile;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Timers;
using ThreadedClasses;

namespace SilverSim.LL.Profile
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
        BlockingQueue<KeyValuePair<Circuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<Circuit, Message>>();
        bool m_ShutdownProfile = false;
        List<IUserAgentServicePlugin> m_UserAgentServices;
        List<IProfileServicePlugin> m_ProfileServices;
        System.Timers.Timer m_CleanupTimer = new System.Timers.Timer(10000);

        class ProfileServiceData
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

        public ViewerProfileServer()
        {

        }

        public void CleanupTimer(object sender, ElapsedEventArgs e)
        {
            List<string> removeList = new List<string>();
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

                        case MessageType.UserInfoRequest:
                            HandleUserInfoRequest(req.Key.Agent, scene, m);
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

        public void HandleUserInfoRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            UserInfoRequest req = (UserInfoRequest)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            ProfileServiceData serviceData;
            UUI uui;
            try
            {
                serviceData = LookupProfileService(scene, req.AgentID, out uui);
            }
            catch
            {
                return;
            }

            ProfilePreferences prefs;
            try
            {
                prefs = serviceData.ProfileService.Preferences[agent.Owner];
            }
            catch
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
            reply.EMail = "";
            reply.IMViaEmail = prefs.IMviaEmail;
        }

        public void HandleAvatarPropertiesRequest(LLAgent agent, SceneInterface scene, Message m)
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
            {
                userInfo = new UserAgentServiceInterface.UserInfo();
                userInfo.FirstName = uui.FirstName;
                userInfo.LastName = uui.LastName;
            }

            try
            {
                props = serviceData.ProfileService.Properties[uui];
            }
            catch
            {
                props = new ProfileProperties();
                props.ImageID = "5748decc-f629-461c-9a36-a35a221fe21f";
                props.FirstLifeImageID = "5748decc-f629-461c-9a36-a35a221fe21f";
                props.User = uui;
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
            agent.SendMessageAlways(res3, scene.ID);
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
            m_CleanupTimer.Stop();
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
