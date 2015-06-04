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

using SilverSim.Types;
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.LSL.API.Region
{
    public partial class Region_API
    {
        [APILevel(APIFlags.LSL)]
        public string llGetRegionName(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.ObjectGroup.Scene.Name;
            }
        }

        [APILevel(APIFlags.LSL)]
        public string llGetSimulatorHostname(ScriptInstance Instance)
        {
#warning Implement llGetSimulatorHostname()
            return string.Empty;
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetRegionCorner(ScriptInstance Instance)
        {
#warning Implement llGetRegionCorner()
            return Vector3.Zero;
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llRequestSimulatorData(ScriptInstance Instance, string region, int data)
        {
#warning Implement llRequestSimulatorData()
            return UUID.Zero;
        }
    }
}
