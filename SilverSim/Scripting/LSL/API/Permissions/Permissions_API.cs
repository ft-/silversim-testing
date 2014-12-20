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
using SilverSim.LL.Messages.Script;
using SilverSim.Scene.Types.Agent;

namespace SilverSim.Scripting.LSL.API.Permissions
{
    [ScriptApiName("Permissions")]
    [LSLImplementation]
    public partial class Permissions_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Permissions_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void run_time_permissions(int perm);

        [APILevel(APIFlags.LSL)]
        public int llGetPermissions(ScriptInstance Instance)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                return (int)script.Permissions;
            }
        }

        [APILevel(APIFlags.LSL)]
        public UUID llGetPermissionsKey(ScriptInstance Instance)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                return script.PermissionsKey;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llRequestPermissions(ScriptInstance Instance, UUID agentID, int permissions)
        {
            if (agentID == UUID.Zero || permissions == 0)
            {
                Instance.RevokePermissions(agentID, (UInt32)permissions);
            }
            else
            {
                ScriptQuestion m = new ScriptQuestion();
                m.ExperienceID = UUID.Zero;
                m.ItemID = Instance.Item.ID;
                m.ObjectOwner = Instance.Part.Owner.ID;
                m.Questions = (UInt32)permissions;
                m.TaskID = Instance.Part.ID;
                IAgent a;
                try
                {
                    a = Instance.Part.ObjectGroup.Scene.Agents[agentID];
                }
                catch
                {
                    return;
                }
                a.SendMessageAlways(m, Instance.Part.ObjectGroup.Scene.ID);
            }
        }

        [ExecutedOnScriptReset]
        public static void ResetPermissions(ScriptInstance Instance)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                script.Permissions = 0;
                script.PermissionsKey = UUID.Zero;
            }
        }
    }
}
