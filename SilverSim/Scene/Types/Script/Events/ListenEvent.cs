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
using SilverSim.Scene.Types.Object;

namespace SilverSim.Scene.Types.Script.Events
{
    public struct ListenEvent : IScriptEvent
    {
        public enum ChatType
        {
            Whisper = 0,
            Say = 1,
            Shout = 2,
            StartTyping = 4,
            StopTyping = 5,
            DebugChannel = 6,
            Region = 7,
            OwnerSay = 8,
            Broadcast = 0xFF
        }

        #region Extension Fields for Chat Router
        public ChatType Type;
        public Vector3 GlobalPosition;
        public UUID TargetID; /* SayTo when not UUID.Zero */
        #endregion

        public int Channel;
        public string Name;
        public UUID ID;
        public string Message;
        public int ButtonIndex;
    }
}
