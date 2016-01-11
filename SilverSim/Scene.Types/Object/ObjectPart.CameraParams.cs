// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        Vector3 m_CameraEyeOffset = Vector3.Zero;
        Vector3 m_CameraAtOffset = Vector3.Zero;
        bool m_ForceMouselook;

        public Vector3 CameraEyeOffset
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraEyeOffset;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraEyeOffset = value;
                }
                TriggerOnUpdate(0);
            }
        }

        public Vector3 CameraAtOffset
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CameraAtOffset;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_CameraAtOffset = value;
                }
                TriggerOnUpdate(0);
            }
        }

        public bool ForceMouselook
        {
            get
            {
                return m_ForceMouselook;
            }
            set
            {
                m_ForceMouselook = value;
                TriggerOnUpdate(0);
            }
        }
    }
}
