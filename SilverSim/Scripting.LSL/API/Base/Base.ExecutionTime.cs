// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public void llResetTime(ScriptInstance Instance)
        {
            lock(Instance)
            {
                Instance.ExecutionTime = 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        public double llGetTime(ScriptInstance Instance)
        {
            double v;
            lock (Instance)
            {
                v = Instance.ExecutionTime;
            }
            return v;
        }

        [APILevel(APIFlags.LSL)]
        public double llGetAndResetTime(ScriptInstance Instance)
        {
            double old;
            lock(Instance)
            {
                old = Instance.ExecutionTime;
                Instance.ExecutionTime = 0;
            }
            return old;
        }
    }
}
