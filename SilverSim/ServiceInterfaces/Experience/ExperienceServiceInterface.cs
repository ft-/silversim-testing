// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types.Experience;
using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.Experience
{
    public abstract class ExperienceServiceInterface
    {
        public ExperienceServiceInterface()
        {

        }

        public interface IExperienceInterface
        {
            ExperienceInfo this[UUID experienceID] { get; set; }
            bool TryGetValue(UUID experienceID, out ExperienceInfo experienceInfo);
        }

        public abstract IExperienceInterface Experiences { get; }

        public interface IExperiencePermissionsInterface
        {
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            ExperiencePermissionsInfo this[UUID experienceID, UUI agent] { get; set; }
            bool TryGetValue(UUID experienceID, UUI agent, out ExperiencePermissionsInfo expPermInfo);
        }

        public abstract IExperiencePermissionsInterface ExperiencePermissions { get; }

        public interface IExperienceKeyInterface
        {
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            string this[UUID experienceID, string key] { get; set; }
            bool TryGetValue(UUID experienceID, string key, out string val);
        }

        public abstract IExperienceKeyInterface Keys { get; }
    }
}
