// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Scene.Types.Agent
{
    public class AgentServiceList : List<object>
    {
        public AgentServiceList()
        {

        }

        public new void Add(object v)
        {
            if(v == null)
            {
                return;
            }
            foreach(object c in this)
            {
                if(c.GetType() == v.GetType())
                {
                    return;
                }
            }

            base.Add(v);
        }

        public T Get<T>()
        {
            foreach(object c in this)
            {
                if(typeof(T).IsAssignableFrom(c.GetType()))
                {
                    return (T)c;
                }
            }
            return default(T);
        }
    }
}
