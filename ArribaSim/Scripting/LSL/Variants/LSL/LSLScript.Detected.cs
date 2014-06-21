using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;
using ArribaSim.Scene.Types.Script.Events;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public Vector3 llDetectedGrab(Integer number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].GrabOffset;
            }
            return Vector3.Zero;
        }

        public Integer llDetectedGroup(Integer number)
        {
            return new Integer(0);
        }

        public UUID llDetectedKey(Integer number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.ID;
            }
            return UUID.Zero;
        }

        public Integer llDetectedLinkNumber(Integer number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return new Integer(m_Detected[number].LinkNumber);
            }
            return new Integer(-1);
        }

        public AString llDetectedName(Integer number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return new AString(m_Detected[number].Object.Name);
            }
            return new AString();
        }

        public UUID llDetectedOwner(Integer number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.Owner.ID;
            }
            return UUID.Zero;
        }

        public Vector3 llDetectedPos(Integer number)
        {
            if(m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.GlobalPosition;
            }
            return Vector3.Zero;
        }

        public Quaternion llDetectedRot(Integer number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.GlobalRotation;
            }
            return Quaternion.Identity;
        }

        public Vector3 llDetectedTouchBinormal(Integer number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchBinormal;
            }
            return Vector3.Zero;
        }
        
        public Integer llDetectedTouchFace(Integer number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return new Integer(m_Detected[number].TouchFace);
            }
            return new Integer(-1);
        }

        public Vector3 llDetectedTouchNormal(Integer number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchNormal;
            }
            return Vector3.Zero;
        }

        public Vector3 llDetectedTouchPos(Integer number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchPosition;
            }
            return Vector3.Zero;
        }

        public Vector3 llDetectedTouchST(Integer number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchST;
            }
            return Vector3.Zero;
        }

        public Vector3 llDetectedTouchUV(Integer number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].TouchUV;
            }
            return Vector3.Zero;
        }

        public Integer llDetectedType(Integer number)
        {
            return new Integer(0);
        }

        public Vector3 llDetectedVel(Integer number)
        {
            if (m_Detected.Count > number && number >= 0)
            {
                return m_Detected[number].Object.Velocity;
            }
            return Vector3.Zero;
        }
    }
}
