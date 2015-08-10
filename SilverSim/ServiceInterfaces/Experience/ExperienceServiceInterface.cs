// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types.Experience;
using SilverSim.Types;

namespace SilverSim.ServiceInterfaces.Experience
{
    public abstract class ExperienceServiceInterface
    {
        public ExperienceServiceInterface()
        {

        }

        public interface ExperienceInterface
        {
            ExperienceInfo this[UUID experienceID] { get; set; }
        }

        public abstract ExperienceInterface Experiences { get; }

        public interface ExperiencePermissionsInterface
        {
            ExperiencePermissionsInfo this[UUID experienceID, UUI agent] { get; set; }
        }

        public abstract ExperiencePermissionsInterface ExperiencePermissions { get; }

        public interface ExperienceKeyInterface
        {
            string this[UUID experienceID, string key] { get; set; }
        }

        public abstract ExperienceKeyInterface Keys { get; }
    }
}
