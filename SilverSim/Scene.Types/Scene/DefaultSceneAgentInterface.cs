// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Scene.Types.Scene
{
    public class DefaultSceneAgentInterface : ISceneAgents
    {
        private SceneInterface m_Scene;

        public DefaultSceneAgentInterface(SceneInterface scene)
        {
            m_Scene = scene;
        }

        public virtual int Count
        {
            get
            {
                int c = 0;
                foreach(IAgent n in this)
                {
                    ++c;
                }
                return c;
            }
        }

        public IAgent this[UUID id]
        {
            get
            {
                IObject obj = m_Scene.Objects[id];
                if(obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
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
                if(!m_Enum.MoveNext())
                {
                    return false;
                }
                while (!(m_Enum.Current.GetType().GetInterfaces().Contains(typeof(IAgent))))
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
