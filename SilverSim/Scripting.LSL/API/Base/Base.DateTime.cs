// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public string llGetTimestamp(ScriptInstance Instance)
        {
            return DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }
    }
}
