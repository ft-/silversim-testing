// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System.Collections;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public class DefaultSceneObjectGroupInterface : ISceneObjectGroups
    {
        private SceneInterface m_Scene;

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

        public IEnumerator<ObjectGroup> GetEnumerator()
        {
            return new ObjectGroupEnumerator(m_Scene.Objects.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class ObjectGroupEnumerator : IEnumerator<ObjectGroup>
        {
            IEnumerator m_Enum;

            internal ObjectGroupEnumerator(IEnumerator enumerator)
            {
                m_Enum = enumerator;
            }

            public void Dispose()
            {
                m_Enum = null;
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
