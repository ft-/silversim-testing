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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using SilverSim.Scene.Types.Agent;

namespace SilverSim.Scripting.LSL.API.Controls
{
    [ScriptApiName("Controls")]
    [LSLImplementation]
    public partial class Controls_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Controls_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public const int PERMISSION_TAKE_CONTROLS = 0x4;

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void control(UUID id, int level, int edge);

        [APILevel(APIFlags.LSL)]
        public static void llTakeControls(ScriptInstance instance, int controls, int accept, int pass_on)
        {
            Script script = (Script)instance;
            if((script.m_ScriptPermissions & ScriptPermissions.TakeControls) == 0 ||
                script.m_ScriptPermissionsKey == UUID.Zero)
            {
                return;
            }
#if NOT_IMPLEMENTED
            IAgent agent;
            try
            {
                agent = instance.Part.ObjectGroup.Scene.Agents[script.m_ScriptPermissionsKey];
            }
            catch
            {
                instance.ShoutError("llTakeControls: permission granter not in region");
                return;
            }
#endif
        }

        [APILevel(APIFlags.LSL)]
        public static void llReleaseControls(ScriptInstance instance)
        {
            IAgent agent;
            Script script = (Script)instance;
            script.m_ScriptPermissions &= (~ScriptPermissions.TakeControls);
            try
            {
                agent = instance.Part.ObjectGroup.Scene.Agents[script.m_ScriptPermissionsKey];
            }
            catch
            {
                instance.ShoutError("llTakeControls: permission granter not in region");
                return;
            }
            agent.RevokePermissions(script.Part.ID, script.Item.ID, ScriptPermissions.TakeControls);
        }
    }
}
