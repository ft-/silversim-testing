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
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Experience.Common.Broker
{
    public sealed partial class ExperienceBrokerService : IExperienceKeyValueInterface
    {
        void IExperienceKeyValueInterface.Add(UEI experienceID, string key, string value) =>
            GetExperienceService(experienceID).KeyValueStore.Add(experienceID, key, value);

        bool IExperienceKeyValueInterface.GetDatasize(UEI experienceID, out int used, out int quota)
        {
            ExperienceServiceInterface experienceService;
            used = 0;
            quota = 0;
            return TryGetExperienceService(experienceID, out experienceService) && experienceService.KeyValueStore.GetDatasize(experienceID, out used, out quota);
        }

        List<string> IExperienceKeyValueInterface.GetKeys(UEI experienceID) =>
            GetExperienceService(experienceID).KeyValueStore.GetKeys(experienceID);

        bool IExperienceKeyValueInterface.Remove(UEI experienceID, string key)
        {
            ExperienceServiceInterface experienceService;
            return TryGetExperienceService(experienceID, out experienceService) && experienceService.KeyValueStore.Remove(experienceID, key);
        }

        void IExperienceKeyValueInterface.Store(UEI experienceID, string key, string value)
        {
            GetExperienceService(experienceID).KeyValueStore.Store(experienceID, key, value);
        }

        bool IExperienceKeyValueInterface.StoreOnlyIfEqualOrig(UEI experienceID, string key, string value, string orig_value)
        {
            ExperienceServiceInterface experienceService;
            return TryGetExperienceService(experienceID, out experienceService) && experienceService.KeyValueStore.StoreOnlyIfEqualOrig(experienceID, key, value, orig_value);
        }

        bool IExperienceKeyValueInterface.TryGetValue(UEI experienceID, string key, out string val)
        {
            ExperienceServiceInterface experienceService;
            val = default(string);
            return TryGetExperienceService(experienceID, out experienceService) && experienceService.KeyValueStore.TryGetValue(experienceID, key, out val);
        }
    }
}
