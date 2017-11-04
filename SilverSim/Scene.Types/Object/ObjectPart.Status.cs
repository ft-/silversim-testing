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
        private bool m_IsSandbox;
        private bool m_IsBlockGrab;
        private bool m_IsDieAtEdge;
        private bool m_IsReturnAtEdge;
        private bool m_IsBlockGrabObject;

        public bool IsSandbox
        {
            get { return m_IsSandbox; }

            set
            {
                lock (m_DataLock)
                {
                    m_IsSandbox = value;
                    m_SandboxOrigin = LocalPosition;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.None);
            }
        }

        public Vector3 SandboxOrigin
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_SandboxOrigin;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_SandboxOrigin = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.None);
            }
        }

        public bool IsBlockGrab
        {
            get { return m_IsBlockGrab; }

            set
            {
                m_IsBlockGrab = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.None);
            }
        }

        public bool IsDieAtEdge
        {
            get { return m_IsDieAtEdge; }

            set
            {
                m_IsDieAtEdge = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.None);
            }
        }

        public bool IsReturnAtEdge
        {
            get { return m_IsReturnAtEdge; }

            set
            {
                m_IsReturnAtEdge = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.None);
            }
        }

        public bool IsBlockGrabObject
        {
            get { return m_IsBlockGrabObject; }

            set
            {
                m_IsBlockGrabObject = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.None);
            }
        }

    }
}
