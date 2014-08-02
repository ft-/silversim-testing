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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Agent;

namespace SilverSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public int llGetRegionAgentCount()
        {
            return Part.Group.Scene.Agents.Count;
        }

        public const int DATA_ONLINE = 1;
        public const int DATA_NAME = 2;
        public const int DATA_BORN = 3;
        public const int DATA_RATING = 4;
        public const int DATA_PAYINFO = 8;
        
        public const int PAYMENT_INFO_ON_FILE = 0x1;
        public const int PAYMENT_INFO_USED = 0x2;

        public UUID llRequestAgentData(UUID id, int data)
        {
            return UUID.Zero;
        }

        public UUID llRequestDisplayName(UUID id)
        {
            return UUID.Zero;
        }

        public UUID llRequestUsername(UUID id)
        {
            return UUID.Zero;
        }

        public string llGetDisplayName(UUID id)
        {
            return string.Empty;
        }

        public Vector3 llGetAgentSize(UUID id)
        {
            IAgent agent;
            try
            {
                agent = Part.Group.Scene.Agents[id];
            }
            catch
            {
                return Vector3.Zero;
            }

            if(agent.IsInScene(Part.Group.Scene))
            {
                return agent.Size;
            }
            return Vector3.Zero;
        }
    }
}
