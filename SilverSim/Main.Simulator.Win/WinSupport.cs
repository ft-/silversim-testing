/*

SilverSim is distributed under the terms of the
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

using System.Runtime.InteropServices;
using System.Threading;
using SilverSim.Main.Simulator;

namespace SilverSim.Main.Simulator.Win
{
    public class WinSupport
    {
        // An enumerated type for the control messages
        // sent to the handler routine.
        enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        delegate bool HandlerRoutine(CtrlTypes CtrlType);
        [DllImport("Kernel32")]
        static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            if (ctrlType == CtrlTypes.CTRL_SHUTDOWN_EVENT || ctrlType == CtrlTypes.CTRL_LOGOFF_EVENT || ctrlType == CtrlTypes.CTRL_CLOSE_EVENT)
            {
                Application.m_ShutdownEvent.Set();
            }
            return true;
        }

        public WinSupport()
        {
            SetConsoleCtrlHandler(ConsoleCtrlCheck, true);
        }
    }
}
