// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.IM;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.IM;

namespace SilverSim.Scripting.LSL.APIs.IM
{
    public partial class IM_API
    {
        [APILevel(APIFlags.LSL)]
        [ForcedSleep(2)]
        public void llInstantMessage(ScriptInstance Instance, LSLKey user, string message)
        {
            lock(Instance)
            {
                IMServiceInterface imservice = Instance.Part.ObjectGroup.Scene.GetService<IMServiceInterface>();
                GridInstantMessage im = new GridInstantMessage();
                im.FromAgent.ID = Instance.Part.ObjectGroup.ID;
                im.FromAgent.FullName = Instance.Part.ObjectGroup.Name;
                im.ToAgent.ID = user;
                im.Position = Instance.Part.ObjectGroup.GlobalPosition;
                im.RegionID = Instance.Part.ObjectGroup.Scene.ID;
                im.Message = message;
                im.OnResult = delegate(GridInstantMessage imret, bool success) { };

                imservice.Send(im);
            }
        }
    }
}
