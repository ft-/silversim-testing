// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.API.Detected
{
    public partial class Detected_API
    {
        /* REMARKS: The internal attribute for the LSLScript has been done deliberately here.
         * The other option of implementing this would have been to make it a namespace class of the Script class.
         */
        [APILevel(APIFlags.LSL)]
        public Vector3 llDetectedGrab(ScriptInstance Instance, int number)
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
        public int llDetectedGroup(ScriptInstance Instance, int number)
        {
#warning Implement llDetectedGroup(int)
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llDetectedKey(ScriptInstance Instance, int number)
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
        public int llDetectedLinkNumber(ScriptInstance Instance, int number)
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
        public string llDetectedName(ScriptInstance Instance, int number)
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
        public LSLKey llDetectedOwner(ScriptInstance Instance, int number)
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
        public Vector3 llDetectedPos(ScriptInstance Instance, int number)
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
        public Quaternion llDetectedRot(ScriptInstance Instance, int number)
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
        public Vector3 llDetectedTouchBinormal(ScriptInstance Instance, int number)
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
        public int llDetectedTouchFace(ScriptInstance Instance, int number)
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
        public Vector3 llDetectedTouchNormal(ScriptInstance Instance, int number)
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
        public Vector3 llDetectedTouchPos(ScriptInstance Instance, int number)
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
        public Vector3 llDetectedTouchST(ScriptInstance Instance, int number)
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
        public Vector3 llDetectedTouchUV(ScriptInstance Instance, int number)
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
        public int llDetectedType(ScriptInstance Instance, int number)
        {
#warning Implement llDetectedType(int)
            throw new NotImplementedException();
        }

        public Vector3 llDetectedVel(ScriptInstance Instance, int number)
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
