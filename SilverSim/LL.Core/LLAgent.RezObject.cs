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

using SilverSim.LL.Messages;
using SilverSim.Main.Common;
using SilverSim.Main.Common.Transfer;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Core
{
    public partial class LLAgent
    {
        class AgentRezObjectHandler  : RezObjectHandler
        {
            public AgentRezObjectHandler(SceneInterface scene, Vector3 targetpos, UUID assetid, AssetServiceInterface source, UUI rezzingagent, SceneInterface.RezObjectParams rezparams, InventoryPermissionsMask itemOwnerPermissions = InventoryPermissionsMask.Every)
                : base(scene, targetpos, assetid, source, rezzingagent, rezparams, itemOwnerPermissions)
            {

            }

            public override void PostProcessObjectGroups(List<ObjectGroup> grps)
            {
                foreach (ObjectGroup grp in grps)
                {
                    foreach (ObjectPart part in grp.Values)
                    {
                        UUID oldID = part.ID;
                        UUID newID = UUID.Random;
                        part.ID = newID;
                        grp.ChangeKey(oldID, newID);
                    }
                }
            }
        }

        void HandleRezObject(Message m)
        {
            Messages.Object.RezObject req = (Messages.Object.RezObject)m;
            if(req.AgentID != req.CircuitAgentID || req.SessionID != req.CircuitSessionID)
            {
                return;
            }
        }

        void HandleRezObjectFromNotecard(Message m)
        {
            Messages.Object.RezObjectFromNotecard req = (Messages.Object.RezObjectFromNotecard)m;
            if (req.AgentID != req.CircuitAgentID || req.SessionID != req.CircuitSessionID)
            {
                return;
            }
        }
    }
}
