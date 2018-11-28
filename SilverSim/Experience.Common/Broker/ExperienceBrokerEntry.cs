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

using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Experience;
using System.Collections.Generic;

namespace SilverSim.Experience.Common.Broker
{
    public sealed partial class ExperienceBrokerEntry : ExperienceServiceInterface
    {
        private const int ExperienceInfoTimeout = 30;

        private static TimeProvider m_ClockSource = TimeProvider.StopWatch;

        private ExperienceServiceInterface InnerExperienceService { get; }

        public long ExpiryTickCount;

        public override IExperiencePermissionsInterface Permissions => this;

        public override IExperienceAdminInterface Admins => this;

        public override IExperienceKeyValueInterface KeyValueStore => this;

        private class ExperienceInfoCache
        {
            public ExperienceInfo Info;
            public long ExpiryTickCount;
        }

        private readonly RwLockedDictionary<UUID, ExperienceInfoCache> m_ExperienceInfoCache = new RwLockedDictionary<UUID, ExperienceInfoCache>();

        public ExperienceBrokerEntry(ExperienceServiceInterface innerExperienceService, long expiryTickCount)
        {
            InnerExperienceService = innerExperienceService;
            ExpiryTickCount = expiryTickCount;
        }

        internal void ExpireHandler()
        {
            foreach (UUID id in m_ExperienceInfoCache.Keys)
            {
                m_ExperienceInfoCache.RemoveIf(id, (entry) => m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) > m_ClockSource.SecsToTicks(ExperienceInfoTimeout));
            }
        }

        public override bool TryGetValue(UUID experienceID, out UEI uei)
        {
            ExperienceInfo info;
            if(TryGetValue(new UEI(experienceID), out info))
            {
                uei = info.ID;
                return true;
            }
            uei = default(UEI);
            return false;
        }

        public override bool TryGetValue(UEI experienceID, out ExperienceInfo experienceInfo)
        {
            ExperienceInfoCache info;
            if (m_ExperienceInfoCache.TryGetValue(experienceID.ID, out info) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, info.ExpiryTickCount) < m_ClockSource.SecsToTicks(ExperienceInfoTimeout))
            {
                experienceInfo = new ExperienceInfo(info.Info);
                return true;
            }

            ExperienceInfo expInfo;
            if(InnerExperienceService.TryGetValue(experienceID, out expInfo))
            {
                info = new ExperienceInfoCache
                {
                    Info = expInfo,
                    ExpiryTickCount = m_ClockSource.TickCount
                };
                m_ExperienceInfoCache[experienceID.ID] = info;
                experienceInfo = new ExperienceInfo(expInfo);
                return true;
            }

            experienceInfo = default(ExperienceInfo);
            return false;
        }

        public override void Add(ExperienceInfo info) => InnerExperienceService.Add(info);

        public override void Update(UGUI requestingAgent, ExperienceInfo info) => InnerExperienceService.Update(requestingAgent, info);

        public override bool Remove(UGUI requestingAgent, UEI id) => InnerExperienceService.Remove(requestingAgent, id);

        public override List<UEI> GetGroupExperiences(UGI group) => InnerExperienceService.GetGroupExperiences(group);

        public override List<UEI> GetCreatorExperiences(UGUI creator) => InnerExperienceService.GetCreatorExperiences(creator);

        public override List<UEI> GetOwnerExperiences(UGUI owner) => InnerExperienceService.GetOwnerExperiences(owner);

        public override List<UEI> FindExperienceByName(string query) => InnerExperienceService.FindExperienceByName(query);

        public override List<ExperienceInfo> FindExperienceInfoByName(string query) => InnerExperienceService.FindExperienceInfoByName(query);
    }
}
