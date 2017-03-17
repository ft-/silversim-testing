// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;

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
