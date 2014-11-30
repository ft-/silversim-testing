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

using SilverSim.ServiceInterfaces.IM;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using SilverSim.Types.IM;

namespace SilverSim.Scripting.LSL.APIs.IM
{
    public partial class IM_API
    {
        [APILevel(APIFlags.LSL)]
        public static void llInstantMessage(ScriptInstance Instance, UUID user, string message)
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
