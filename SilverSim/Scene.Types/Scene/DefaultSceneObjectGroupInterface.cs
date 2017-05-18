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

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Scene
{
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public class DefaultSceneObjectGroupInterface : ISceneObjectGroups
    {
        readonly SceneInterface m_Scene;

        public DefaultSceneObjectGroupInterface(SceneInterface scene)
        {
            m_Scene = scene;
        }

        public int Count
        {
            get
            {
                int n = 0;
                foreach(var g in this)
                {
                    ++n;
                }
                return n;
            }
        }

        public ObjectGroup this[UUID id]
        {
            get
            {
                var obj = m_Scene.Objects[id] as ObjectGroup;
                if(null != obj)
                {
                    return obj;
                }
                throw new KeyNotFoundException();
            }
        }

        public bool TryGetValue(UUID id, out ObjectGroup grp)
        {
            IObject obj;
            grp = null;
            if(!m_Scene.Objects.TryGetValue(id, out obj))
            {
                return false;
            }
            grp = obj as ObjectGroup;
            return grp != null;
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        public IEnumerator<ObjectGroup> GetEnumerator() => new ObjectGroupEnumerator(m_Scene.Objects.GetEnumerator());

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public sealed class ObjectGroupEnumerator : IEnumerator<ObjectGroup>
        {
            readonly IEnumerator m_Enum;

            internal ObjectGroupEnumerator(IEnumerator enumerator)
            {
                m_Enum = enumerator;
            }

            public void Dispose()
            {
                /* intentionally left empty */
            }

            public ObjectGroup Current => (ObjectGroup)m_Enum.Current;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (!m_Enum.MoveNext())
                {
                    return false;
                }
                while (!(m_Enum.Current is ObjectGroup))
                {
                    if (!m_Enum.MoveNext())
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Reset()
            {
                m_Enum.Reset();
            }
        }
    }
}
