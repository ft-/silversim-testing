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
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;

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
            lock (Instance)
            {
                return (int)Instance.Item.PermsGranter.PermsMask;
            }
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llGetPermissionsKey(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Item.PermsGranter.PermsGranter.ID;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llRequestPermissions(ScriptInstance Instance, LSLKey agentID, int permissions)
        {
            lock(Instance)
            {
                if (agentID == UUID.Zero || permissions == 0)
                {
                    Instance.RevokePermissions(agentID, (ScriptPermissions)permissions);
                }
                else
                {
                    IAgent a;
                    try
                    {
                        a = Instance.Part.ObjectGroup.Scene.Agents[agentID];
                    }
                    catch
                    {
                        Instance.Item.PermsGranter = null;
                        return;
                    }
                    ScriptPermissions perms = a.RequestPermissions(Instance.Part, Instance.Item.ID, (ScriptPermissions)permissions);
                    if (perms != ScriptPermissions.None)
                    {
                        RuntimePermissionsEvent e = new RuntimePermissionsEvent();
                        e.Permissions = perms;
                        e.PermissionsKey = a.Owner;
                        Instance.PostEvent(e);
                    }
                }
            }
        }

        [ExecutedOnScriptReset]
        public static void ResetPermissions(ScriptInstance Instance)
        {
            lock (Instance)
            {
                Instance.Item.PermsGranter = null;
            }
        }
    }
}
