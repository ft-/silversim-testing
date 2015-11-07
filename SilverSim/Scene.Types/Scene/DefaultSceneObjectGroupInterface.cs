// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
                foreach(ObjectGroup g in this)
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
                ObjectGroup obj = m_Scene.Objects[id] as ObjectGroup;
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
        public IEnumerator<ObjectGroup> GetEnumerator()
        {
            return new ObjectGroupEnumerator(m_Scene.Objects.GetEnumerator());
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public sealed class ObjectGroupEnumerator : IEnumerator<ObjectGroup>
        {
            readonly IEnumerator m_Enum;

            internal ObjectGroupEnumerator(IEnumerator enumerator)
            {
                m_Enum = enumerator;
            }

            public void Dispose()
            {
            }

            public ObjectGroup Current
            {
                get
                {
                    return (ObjectGroup)m_Enum.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

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
