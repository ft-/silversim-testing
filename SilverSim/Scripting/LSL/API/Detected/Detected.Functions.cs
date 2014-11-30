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

namespace SilverSim.Scripting.LSL.API.Detected
{
    public partial class Detected_API
    {
        /* REMARKS: The internal attribute for the LSLScript has been done deliberately here.
         * The other option of implementing this would have been to make it a namespace class of the Script class.
         */
        [APILevel(APIFlags.LSL)]
        public static Vector3 llDetectedGrab(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].GrabOffset;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static int llDetectedGroup(ScriptInstance Instance, int number)
        {
#warning Implement llDetectedGroup(int)
            return 0;
        }

        [APILevel(APIFlags.LSL)]
        public static UUID llDetectedKey(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.ID;
                }
                return UUID.Zero;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static int llDetectedLinkNumber(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].LinkNumber;
                }
                return -1;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static string llDetectedName(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.Name;
                }
                return string.Empty;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static UUID llDetectedOwner(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.Owner.ID;
                }
                return UUID.Zero;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llDetectedPos(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.GlobalPosition;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static Quaternion llDetectedRot(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.GlobalRotation;
                }
                return Quaternion.Identity;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llDetectedTouchBinormal(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchBinormal;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static int llDetectedTouchFace(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchFace;
                }
                return -1;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llDetectedTouchNormal(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchNormal;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llDetectedTouchPos(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchPosition;
                }
            }
            return Vector3.Zero;
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llDetectedTouchST(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchST;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static Vector3 llDetectedTouchUV(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].TouchUV;
                }
                return Vector3.Zero;
            }
        }

        [APILevel(APIFlags.LSL)]
        public static int llDetectedType(ScriptInstance Instance, int number)
        {
#warning Implement llDetectedType(int)
            return 0;
        }

        public static Vector3 llDetectedVel(ScriptInstance Instance, int number)
        {
            Script script = (Script)Instance;
            lock (script)
            {
                if (script.m_Detected.Count > number && number >= 0)
                {
                    return script.m_Detected[number].Object.Velocity;
                }
                return Vector3.Zero;
            }
        }
    }
}
