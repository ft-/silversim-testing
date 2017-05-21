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

using SilverSim.Types.IM;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.IM;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        [IMMessageHandler(GridInstantMessageDialog.MessageFromAgent)]
        [IMMessageHandler(GridInstantMessageDialog.StartTyping)]
        [IMMessageHandler(GridInstantMessageDialog.StopTyping)]
        [IMMessageHandler(GridInstantMessageDialog.BusyAutoResponse)]
        public void HandleIM(ViewerAgent nop, AgentCircuit circuit, Message m)
        {
            var im = (GridInstantMessage)(ImprovedInstantMessage)m;
            im.IsFromGroup = false;
            im.FromAgent.ID = ID;

            im.OnResult = circuit.OnIMResult;

            var server = circuit.Server;
            server?.RouteIM(im);
        }
    }
}
