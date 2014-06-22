/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Types;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public class LSLState
    {
        public LSLState()
        {
        }

        public virtual void at_rot_target(int handle, Quaternion targetrot, Quaternion ourrot)
        {

        }

        public virtual void at_target(int tnum, Vector3 targetpos, Vector3 ourpos)
        {

        }

        public virtual void attach(UUID id)
        {

        }

        public virtual void changed(int change)
        {

        }

        public virtual void collision(int num_detected)
        {

        }

        public virtual void collision_end(int num_detected)
        {

        }

        public virtual void collision_start(int num_detected)
        {

        }

        public virtual void control(UUID id, int level, int edge)
        {

        }

        public virtual void dataserver(UUID queryid, string data)
        {

        }

        public virtual void email(string time, string address, string subject, string message, int num_left)
        {

        }

        public virtual void http_request(UUID request_id, string method, string body)
        {

        }

        public virtual void http_response(UUID request_id, int status, AnArray metadata, string body)
        {

        }

        public virtual void land_collision(Vector3 pos)
        {

        }

        public virtual void land_collision_end(Vector3 pos)
        {

        }

        public virtual void land_collision_start(Vector3 pos)
        {

        }

        public virtual void link_message(int sender_num, int num, string str, UUID id)
        {
        }

        public virtual void listen(int channel, string name, UUID id, string message)
        {

        }

        public virtual void money(UUID id, int amount)
        {

        }

        public virtual void moving_end()
        {
        }

        public virtual void moving_start()
        {

        }

        public virtual void no_sensor()
        {

        }

        public virtual void not_at_rot_target()
        {

        }

        public virtual void not_at_target()
        {
        }

        public virtual void object_rez(UUID id)
        {

        }

        public virtual void on_rez(int start_param)
        {

        }

        public virtual void path_update(int type, AnArray reserved)
        {

        }

        public virtual void remote_data(int event_type, UUID channel, UUID message_id, string sender, int idata, string sdata)
        {

        }

        public virtual void run_time_permissions(int perm)
        {

        }

        public virtual void sensor(int num_detected)
        {

        }

        public virtual void state_entry()
        {

        }

        public virtual void state_exit()
        {

        }

        public virtual void timer()
        {

        }

        public virtual void touch(int num_detected)
        {

        }

        public virtual void touch_end(int num_detected)
        {

        }

        public virtual void touch_start(int num_detected)
        {

        }

        public virtual void transaction_result(UUID id, int success, string data)
        {

        }
    }
}
