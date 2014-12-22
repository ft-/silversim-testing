using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
