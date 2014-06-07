/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.Scene.Types.Agent;
using ArribaSim.Scene.Types.Object;
using ArribaSim.Types;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ArribaSim.Scene.Types.Scene
{
    public class DefaultSceneAgentInterface : ISceneAgents
    {
        private SceneInterface m_Scene;

        public DefaultSceneAgentInterface(SceneInterface scene)
        {
            m_Scene = scene;
        }

        public IAgent this[UUID id]
        {
            get
            {
                IObject obj = m_Scene.Objects[id];
                if(obj.GetType().GetInterfaces().Contains(typeof(IAgent)) && obj.IsInScene(m_Scene))
                {
                    return (IAgent)obj;
                }
                throw new KeyNotFoundException();
            }
        }

        public IEnumerator<IAgent> GetEnumerator()
        {
            return new AgentEnumerator(m_Scene.Objects.GetEnumerator(), m_Scene);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class AgentEnumerator : IEnumerator<IAgent>
        {
            IEnumerator m_Enum;
            SceneInterface m_Scene;

            public AgentEnumerator(IEnumerator enumerator, SceneInterface scene)
            {
                m_Enum = enumerator;
                m_Scene = scene;
            }

            public void Dispose()
            {
                m_Enum = null;
            }

            public IAgent Current
            {
                get
                {
                    return (IAgent)m_Enum.Current;
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
                while (!(m_Enum.Current.GetType().GetInterfaces().Contains(typeof(IAgent))) || !((IObject)m_Enum.Current).IsInScene(m_Scene))
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
