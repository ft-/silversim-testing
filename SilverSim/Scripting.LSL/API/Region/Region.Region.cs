// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Scene.Types.Script;
using System;

namespace SilverSim.Scripting.LSL.API.Region
{
    public partial class Region_API
    {
        [APILevel(APIFlags.LSL)]
        public string llGetRegionName(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.ObjectGroup.Scene.Name;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(10)]
        public string llGetSimulatorHostname(ScriptInstance Instance)
        {
#warning Implement llGetSimulatorHostname()
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetRegionCorner(ScriptInstance Instance)
        {
#warning Implement llGetRegionCorner()
            throw new NotImplementedException();
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(1)]
        public LSLKey llRequestSimulatorData(ScriptInstance Instance, string region, int data)
        {
#warning Implement llRequestSimulatorData()
            throw new NotImplementedException();
        }
    }
}
