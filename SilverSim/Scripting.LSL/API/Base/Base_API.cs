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

namespace SilverSim.Scripting.LSL.API.Base
{
    [ScriptApiName("Base")]
    [LSLImplementation]
    public partial class Base_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void at_rot_target(int handle, Quaternion targetrot, Quaternion ourrot);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void at_target(int tnum, Vector3 targetpos, Vector3 ourpos);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void attach(LSLKey id);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void changed(int change);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void collision(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void collision_end(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void collision_start(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void dataserver(LSLKey queryid, string data);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void email(string time, string address, string subject, string message, int num_left);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void http_request(LSLKey request_id, string method, string body);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void http_response(LSLKey request_id, int status, AnArray metadata, string body);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void land_collision(Vector3 pos);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void land_collision_end(Vector3 pos);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void land_collision_start(Vector3 pos);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void link_message(int sender_num, int num, string str, LSLKey id);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void listen(int channel, string name, LSLKey id, string message);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void money(LSLKey id, int amount);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void moving_end();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void moving_start();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void no_sensor();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void not_at_rot_target();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void not_at_target();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void object_rez(LSLKey id);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void on_rez(int start_param);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void path_update(int type, AnArray reserved);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void remote_data(int event_type, LSLKey channel, LSLKey message_id, string sender, int idata, string sdata);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void sensor(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void state_entry();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void state_exit();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void timer();

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void touch(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void touch_end(int num_detected);

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void touch_start(int num_detected);

        public Base_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        public void llSleep(ScriptInstance Instance, double secs)
        {
            Instance.Sleep(secs);
        }

        [APILevel(APIFlags.ASSL)]
        public void asSetForcedSleep(ScriptInstance Instance, int flag, double factor)
        {
            if(factor > 1)
            {
                factor = 1;
            }
            if(factor <= 0)
            {
                flag = 0;
            }
            lock(Instance)
            {
                Script script = (Script)Instance;
                script.ForcedSleepFactor = factor;
                script.UseForcedSleep = flag != 0;
            }
        }

        [APILevel(APIFlags.ASSL)]
        public void asSetForcedSleepEnable(ScriptInstance Instance, int flag)
        {
            lock(Instance)
            {
                Script script = (Script)Instance;
                script.UseForcedSleep = flag != 0;
            }
        }
    }
}
