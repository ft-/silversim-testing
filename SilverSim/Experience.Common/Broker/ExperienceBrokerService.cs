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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Experience;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;

namespace SilverSim.Experience.Common.Broker
{
    [Description("Experience brokering service")]
    [PluginName("ExperienceBroker")]
    public sealed partial class ExperienceBrokerService : ExperienceServiceInterface, IPlugin, IPluginShutdown
    {
        private readonly Timer m_Timer = new Timer(60000);

        private TimeProvider m_ClockSource;

        private ExperienceServiceInterface m_ExperienceService;
        private readonly string m_ExperienceServiceName;
        private string m_ExperienceHomeURI;

        private ExperienceNameServiceInterface m_ExperienceNameService;
        private readonly string m_ExperienceNameServiceName;
        private List<IExperienceServicePlugin> m_ExperienceServicePlugins;

        private SceneList m_Scenes;

        public ExperienceBrokerService(IConfig config)
        {
            m_ExperienceNameServiceName = config.GetString("ExperienceNameService", "ExperienceNameStorage");
            m_ExperienceServiceName = config.GetString("ExperienceService", string.Empty);
        }

        public void Startup(ConfigurationLoader loader)
        {
            if(m_ExperienceServiceName.Length != 0)
            {
                loader.GetService(m_ExperienceServiceName, out m_ExperienceService);
            }
            m_ExperienceServicePlugins = loader.GetServicesByValue<IExperienceServicePlugin>();
            m_ExperienceHomeURI = loader.HomeURI;
            m_Scenes = loader.Scenes;
            loader.GetService(m_ExperienceNameServiceName, out m_ExperienceNameService);
            m_Timer.Elapsed += ExpireHandler;
            m_Timer.Start();
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

        public void Shutdown()
        {
            m_Timer.Stop();
            m_Timer.Elapsed -= ExpireHandler;
        }

        public override IExperiencePermissionsInterface Permissions => this;

        public override IExperienceAdminInterface Admins => this;

        public override IExperienceKeyValueInterface KeyValueStore => this;

        public override void Add(ExperienceInfo info)
        {
            throw new NotSupportedException();
        }

        public override List<UEI> FindExperienceByName(string query)
        {
            throw new NotImplementedException();
        }

        public override List<ExperienceInfo> FindExperienceInfoByName(string query)
        {
            throw new NotImplementedException();
        }

        public override List<UEI> GetCreatorExperiences(UGUI creator)
        {
            return new List<UEI>();
        }

        public override List<UEI> GetGroupExperiences(UGI group)
        {
            return new List<UEI>();
        }

        public override List<UEI> GetOwnerExperiences(UGUI owner)
        {
            return new List<UEI>();
        }

        public override bool Remove(UGUI requestingAgent, UEI id)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetValue(UUID experienceID, out UEI uei)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetValue(UEI experienceID, out ExperienceInfo experienceInfo)
        {
            throw new NotImplementedException();
        }

        public override void Update(UGUI requestingAgent, ExperienceInfo info)
        {
            throw new NotImplementedException();
        }

        private readonly RwLockedDictionary<UUID, ExperienceBrokerEntry> m_NameCache = new RwLockedDictionary<UUID, ExperienceBrokerEntry>();

        [Serializable]
        public class ExperienceServiceNotFoundException : KeyNotFoundException
        {
            public ExperienceServiceNotFoundException()
            {
            }

            public ExperienceServiceNotFoundException(string message) : base(message)
            {
            }

            public ExperienceServiceNotFoundException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        private ExperienceServiceInterface GetExperienceService(UGUI principal)
        {
            ExperienceServiceInterface experienceService;
            if (!TryGetExperienceService(principal, out experienceService))
            {
                throw new ExperienceServiceNotFoundException();
            }
            return experienceService;
        }

        private ExperienceServiceInterface GetExperienceService(UUID experienceid)
        {
            ExperienceServiceInterface experienceService;
            if (!TryGetExperienceService(experienceid, out experienceService))
            {
                throw new ExperienceServiceNotFoundException();
            }
            return experienceService;
        }

        private ExperienceServiceInterface GetExperienceService(UEI experience)
        {
            ExperienceServiceInterface experienceService;
            if (!TryGetExperienceService(experience, out experienceService))
            {
                throw new ExperienceServiceNotFoundException();
            }
            return experienceService;
        }

        private ExperienceServiceInterface GetExperienceService(string experienceServerURI)
        {
            ExperienceServiceInterface experienceService;
            if (!TryGetExperienceService(experienceServerURI, out experienceService))
            {
                throw new ExperienceServiceNotFoundException();
            }
            return experienceService;
        }

        private bool TryGetExperienceService(UGUI principal, out ExperienceServiceInterface experienceService)
        {
            experienceService = null;
            IAgent agent;
            UUID regionID;
            if (m_Scenes.TryFindRootAgent(principal.ID, out agent, out regionID))
            {
                experienceService = agent.ExperienceService;
            }
            return experienceService != null;
        }

        private bool TryGetExperienceService(UUID experienceID, out ExperienceServiceInterface experienceService)
        {
            UEI experience;
            ExperienceBrokerEntry entry;
            if (m_NameCache.TryGetValue(experienceID, out entry) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) < m_ClockSource.SecsToTicks(120))
            {
                experienceService = entry;
                return true;
            }
            if (!m_ExperienceNameService.TryGetValue(experienceID, out experience))
            {
                experienceService = default(ExperienceServiceInterface);
                return false;
            }
            return TryGetExperienceService(experience, out experienceService);
        }

        private bool TryGetExperienceService(UEI uei, out ExperienceServiceInterface experienceService)
        {
            experienceService = default(ExperienceServiceInterface);
            Dictionary<string, string> cachedheaders;
            ExperienceBrokerEntry entry;
            if (uei.HomeURI == null)
            {
                return false;
            }

            if (uei.AuthorizationToken == null)
            {
                /* if unset, try fetching the authorization token */
                UEI uei2;
                if (m_ExperienceNameService.TryGetValue(uei.ID, out uei2))
                {
                    uei = uei2;
                    if (uei.HomeURI == null)
                    {
                        return false;
                    }
                }
            }

            string experienceServerURI = uei.HomeURI.ToString();

            if (m_ExperienceService != null && m_ExperienceHomeURI == experienceServerURI)
            {
                entry = new ExperienceBrokerEntry(m_ExperienceService, m_ClockSource.TickCount);
                m_NameCache[uei.ID] = entry;
                experienceService = entry;
                return true;
            }

            cachedheaders = ServicePluginHelo.HeloRequest(experienceServerURI);
            foreach (IExperienceServicePlugin plugin in m_ExperienceServicePlugins)
            {
                if (plugin.IsProtocolSupported(experienceServerURI, cachedheaders))
                {
                    ExperienceServiceInterface service = plugin.Instantiate(uei);
                    entry = new ExperienceBrokerEntry(service, m_ClockSource.TickCount);
                    m_NameCache[uei.ID] = entry;
                    experienceService = entry;
                    return true;
                }
            }
            return false;
        }

        public override bool TryRequestAuthorization(UGUI requestingAgent, UEI uei)
        {
            ExperienceServiceInterface experienceService;
            if (TryGetExperienceService(requestingAgent, out experienceService) && experienceService.TryRequestAuthorization(requestingAgent, uei))
            {
                m_ExperienceNameService.Store(uei);
                return true;
            }
            return false;
        }

        private void ExpireHandler(object o, ElapsedEventArgs args)
        {
            try
            {
                foreach (UUID id in m_NameCache.Keys)
                {
                    ExperienceBrokerEntry entry;
                    if (m_NameCache.TryGetValue(id, out entry))
                    {
                        if (m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) > m_ClockSource.SecsToTicks(120))
                        {
                            m_NameCache.Remove(id);
                        }
                        else
                        {
                            entry.ExpireHandler();
                        }
                    }
                }
            }
            catch
            {
                /* do not pass these to timer handler */
            }
        }
    }
}
