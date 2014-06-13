/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Scene.Types.Object;
using ArribaSim.Types;
using System.Collections;
using System.Collections.Generic;

namespace ArribaSim.Scene.Types.Scene
{
    public class DefaultSceneObjectGroupInterface : ISceneObjectGroups
    {
        private SceneInterface m_Scene;

        public DefaultSceneObjectGroupInterface(SceneInterface scene)
        {
            m_Scene = scene;
        }

        public ObjectGroup this[UUID id]
        {
            get
            {
                IObject obj = m_Scene.Objects[id];
                if(obj is ObjectGroup)
                {
                    return (ObjectGroup)obj;
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

        class ObjectGroupEnumerator : IEnumerator<ObjectGroup>
        {
            IEnumerator m_Enum;

            public ObjectGroupEnumerator(IEnumerator enumerator)
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
                if (m_Enum.MoveNext())
                {
                    return true;
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
