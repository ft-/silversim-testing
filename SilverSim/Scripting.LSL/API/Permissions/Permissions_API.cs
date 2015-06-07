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
        [LSLTooltip("permission to take money from agent's account")]
        public const int PERMISSION_DEBIT = 0x2;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to take agent's controls")]
        public const int PERMISSION_TAKE_CONTROLS = 0x4;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to start or stop animations on agent")]
        public const int PERMISSION_TRIGGER_ANIMATION = 0x10;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to attach/detach from agent")]
        public const int PERMISSION_ATTACH = 0x20;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to change links")]
        public const int PERMISSION_CHANGE_LINKS = 0x80;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to track agent's camera position and rotation")]
        public const int PERMISSION_TRACK_CAMERA = 0x400;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to control the agent's camera\n(must be sat on or attached; automatically revoked on stand or detach)")]
        public const int PERMISSION_CONTROL_CAMERA = 0x800;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to teleport the agent")]
        public const int PERMISSION_TELEPORT = 0x1000;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to manage estate access without notifying the owner of changes")]
        public const int PERMISSION_SILENT_ESTATE_MANAGEMENT = 0x4000;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to configure overriding of default animations")]
        public const int PERMISSION_OVERRIDE_ANIMATIONS = 0x8000;
        [APILevel(APIFlags.LSL)]
        [LSLTooltip("permission to return object from parcels by llReturnObjectsByOwner and llReturnObjectsByID")]
        public const int PERMISSION_RETURN_OBJECTS = 0x10000;



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
