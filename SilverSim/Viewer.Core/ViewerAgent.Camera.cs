// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        public override Quaternion CameraRotation
        {
            get
            {
                return Quaternion.Axes2Rot(CameraAtAxis, CameraLeftAxis, CameraUpAxis);
            }

            set
            {
                CameraAtAxis = value.FwdAxis;
                CameraLeftAxis = value.LeftAxis;
                CameraUpAxis = value.UpAxis;
            }
        }

        Vector3 m_CameraAtAxis;
        Vector3 m_CameraLeftAxis;
        Vector3 m_CameraUpAxis;
        Vector3 m_CameraPosition;

        public override Vector3 CameraPosition
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraPosition;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraPosition = value;
                }
            }
        }

        public Vector3 CameraLookAt
        {
            get
            {
                Vector3 atAxis = CameraAtAxis;
                if(atAxis == Vector3.Zero)
                {
                    return atAxis;
                }
                return new Vector3(atAxis.X, atAxis.Y, 0).Normalize();
            }
        }

        public override Vector3 CameraAtAxis
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraAtAxis;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraAtAxis = value;
                }
            }
        }

        public override Vector3 CameraLeftAxis
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraLeftAxis;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraLeftAxis = value;
                }
            }
        }

        public override Vector3 CameraUpAxis
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraUpAxis;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraUpAxis = value;
                }
            }
        }
    }
}
