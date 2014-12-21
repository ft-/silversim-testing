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

namespace SilverSim.Scripting.LSL.API.AnimationOverride
{
    [ScriptApiName("AnimationOverride")]
    [LSLImplementation]
    public partial class AnimationOverride_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public AnimationOverride_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public const int PERMISSION_OVERRIDE_ANIMATIONS = 0x8000;

        [APILevel(APIFlags.LSL)]
        public void llSetAnimationOverride(ScriptInstance instance, string anim_state, string anim)
        {
            IAgent agent;
            Script script = (Script)instance;
            if((script.m_ScriptPermissions & Script.ScriptPermissions.OverrideAnimations) == 0 ||
                script.m_ScriptPermissionsKey == UUID.Zero)
            {
                return;
            }
            try
            {
                agent = instance.Part.ObjectGroup.Scene.Agents[script.m_ScriptPermissionsKey];
            }
            catch
            {
                instance.ShoutError("llSetAnimationOverride: permission granter not in region");
                return;
            }

            try
            {
                agent.SetAnimationOverride(anim_state, anim);
            }
            catch(Exception e)
            {
                instance.ShoutError(e.Message);
            }
        }

        [APILevel(APIFlags.LSL)]
        public string llGetAnimationOverride(ScriptInstance instance, string anim_state)
        {
            IAgent agent;
            Script script = (Script)instance;
            if (((script.m_ScriptPermissions & Script.ScriptPermissions.OverrideAnimations) == 0 &&
                (script.m_ScriptPermissions & Script.ScriptPermissions.TriggerAnimation) == 0) ||
                script.m_ScriptPermissionsKey == UUID.Zero)
            {
                return string.Empty;
            }
            try
            {
                agent = instance.Part.ObjectGroup.Scene.Agents[script.m_ScriptPermissionsKey];
            }
            catch
            {
                instance.ShoutError("llSetAnimationOverride: permission granter not in region");
                return string.Empty;
            }

            agent.ResetAnimationOverride(anim_state);
            return anim_state;
        }

        [APILevel(APIFlags.LSL)]
        public void llResetAnimationOverride(ScriptInstance instance, string anim_state)
        {
            IAgent agent;
            Script script = (Script)instance;
            if ((script.m_ScriptPermissions & Script.ScriptPermissions.OverrideAnimations) == 0 ||
                script.m_ScriptPermissionsKey == UUID.Zero)
            {
                return;
            }
            try
            {
                agent = instance.Part.ObjectGroup.Scene.Agents[script.m_ScriptPermissionsKey];
            }
            catch
            {
                instance.ShoutError("llSetAnimationOverride: permission granter not in region");
                return;
            }

            agent.ResetAnimationOverride(anim_state);
        }
    }
}
