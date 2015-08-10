// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Scene
{
    public interface ISceneListener
    {
        void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID);
    }
}
