// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Object;
using SilverSim.Scripting.Common;

namespace SilverSim.Scripting.LSL.API.Base
{
    public partial class Base_API
    {
        [APILevel(APIFlags.LSL)]
        public void llDie(ScriptInstance Instance)
        {
            Instance.AbortBegin();
            Instance.Part.ObjectGroup.Scene.Remove(Instance.Part.ObjectGroup, Instance);
            throw new ScriptAbortException();
        }
    }
}
