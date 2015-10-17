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

        public interface IExperienceInterface
        {
            ExperienceInfo this[UUID experienceID] { get; set; }
        }

        public abstract IExperienceInterface Experiences { get; }

        public interface IExperiencePermissionsInterface
        {
            ExperiencePermissionsInfo this[UUID experienceID, UUI agent] { get; set; }
        }

        public abstract IExperiencePermissionsInterface ExperiencePermissions { get; }

        public interface IExperienceKeyInterface
        {
            string this[UUID experienceID, string key] { get; set; }
        }

        public abstract IExperienceKeyInterface Keys { get; }
    }
}
