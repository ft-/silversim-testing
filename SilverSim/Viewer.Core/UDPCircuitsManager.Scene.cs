// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;

namespace SilverSim.Viewer.Core
{
    public partial class UDPCircuitsManager
    {
        public void ScheduleUpdate(ObjectUpdateInfo info)
        {
            m_Circuits.ForEach(delegate(Circuit circ)
            {
                if (circ is AgentCircuit)
                {
                    AgentCircuit acirc = (AgentCircuit)circ;
                    acirc.ScheduleUpdate(info);
                }
            });
        }
    }
}
