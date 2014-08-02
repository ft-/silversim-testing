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

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public Vector3 llDetectedGrab(int number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].GrabOffset;
            }
            return Vector3.Zero;
        }

        public int llDetectedGroup(int number)
        {
            return 0;
        }

        public UUID llDetectedKey(int number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.ID;
            }
            return UUID.Zero;
        }

        public int llDetectedLinkNumber(int number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].LinkNumber;
            }
            return -1;
        }

        public string llDetectedName(int number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.Name;
            }
            return string.Empty;
        }

        public UUID llDetectedOwner(int number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.Owner.ID;
            }
            return UUID.Zero;
        }

        public Vector3 llDetectedPos(int number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.GlobalPosition;
            }
            return Vector3.Zero;
        }

        public Quaternion llDetectedRot(int number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.GlobalRotation;
            }
            return Quaternion.Identity;
        }

        public Vector3 llDetectedTouchBinormal(int number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchBinormal;
            }
            return Vector3.Zero;
        }

        public int llDetectedTouchFace(int number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchFace;
            }
            return -1;
        }

        public Vector3 llDetectedTouchNormal(int number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchNormal;
            }
            return Vector3.Zero;
        }

        public Vector3 llDetectedTouchPos(int number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchPosition;
            }
            return Vector3.Zero;
        }

        public Vector3 llDetectedTouchST(int number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchST;
            }
            return Vector3.Zero;
        }

        public Vector3 llDetectedTouchUV(int number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchUV;
            }
            return Vector3.Zero;
        }

        public int llDetectedType(int number)
        {
            return 0;
        }

        public Vector3 llDetectedVel(int number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.Velocity;
            }
            return Vector3.Zero;
        }
    }
}
