// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;

namespace SilverSim.Types.Agent
{
    public class CircuitInfo
    {
        public uint CircuitCode;
        public string CapsPath = string.Empty;
        public bool IsChild;
        public Dictionary<UInt64, string> ChildrenCapSeeds = new Dictionary<UInt64, string>();
        public string MapServerURL = string.Empty;

        public CircuitInfo()
        {

        }
    }
}
