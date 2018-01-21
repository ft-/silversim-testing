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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Globalization;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectUpdateInfo : IObjUpdateInfo
    {
        private bool m_Killed;
        public uint LocalID { get; internal set; }
        public ObjectPart Part { get; }
        public UUID ID { get; internal set; }
        public UUID SceneID { get; internal set; }

        internal ObjectUpdateInfo(ObjectPart part)
        {
            Part = part;
            ID = part.ID;
        }

        public void KillObject()
        {
            m_Killed = true;
        }

        public UUI Owner => Part.ObjectGroup.Owner;

        public virtual bool IsAlwaysFull => false;

        public bool IsKilled => m_Killed;

        public bool IsMoving => Part.IsMoving && Part.ObjectGroup.RootPart == Part;

        public bool IsAttached => Part.ObjectGroup.IsAttached;

        public bool IsAttachedToPrivate => Part.ObjectGroup.IsAttachedToPrivate;

        public bool IsPhysics
        {
            get
            {
                if(Part != null && !m_Killed)
                {
                    return Part.ObjectGroup.IsPhysics;
                }
                return false;
            }
        }

        public uint ParentID
        {
            get
            {
                uint parentID = 0;
                ObjectGroup objectGroup = Part.ObjectGroup;
                ObjectPart rootPart = objectGroup.RootPart;

                if (rootPart != Part)
                {
                    parentID = rootPart.LocalID[SceneID];
                }
                else if (objectGroup.IsAttached)
                {
                    IAgent agent;
                    SceneInterface scene = objectGroup.Scene;
                    if (scene != null && scene.Agents.TryGetValue(Owner.ID, out agent))
                    {
                        parentID = agent.LocalID[SceneID];
                    }
                }

                return parentID;
            }
        }

        public byte[] GetFullUpdate(CultureInfo cultureInfo)
        {
            if(Part != null && !m_Killed)
            {
                return Part.GetFullUpdateData(cultureInfo);
            }
            return null;
        }

        public byte[] GetTerseUpdate(CultureInfo cultureInfo)
        {
            if (Part != null && !m_Killed)
            {
                return Part.GetTerseUpdateData(cultureInfo);
            }
            return null;
        }

        public byte[] GetPropertiesUpdate(CultureInfo cultureInfo)
        {
            if (Part != null && !m_Killed)
            {
                return Part.GetPropertiesUpdateData(cultureInfo);
            }
            return null;
        }

        public int SerialNumber => Part.SerialNumber;
    }
}
