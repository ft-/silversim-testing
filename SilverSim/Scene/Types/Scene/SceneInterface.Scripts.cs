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

using log4net;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Script;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.Types;
using SilverSim.Types.Economy;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using ThreadedClasses;
using SilverSim.Scene.Types.Script.Events;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public void HandleScriptAnswerYes(Message m)
        {
            ScriptAnswerYes req = (ScriptAnswerYes)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            Script.ScriptInstance instance;
            try
            {
                ObjectPart p = Primitives[req.TaskID];
                ObjectPartInventoryItem item = p.Inventory[req.ItemID];
                instance = item.ScriptInstance;
            }
            catch
            {
                return;
            }
            if(instance == null)
            {
                return;
            }

            RuntimePermissionsEvent e = new RuntimePermissionsEvent();
            e.PermissionsKey = req.AgentID;
            e.Permissions = req.Questions;

            instance.PostEvent(e);
        }

        public void HandleRevokePermissions(Message m)
        {
            RevokePermissions req = (RevokePermissions)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            try
            {
                IObject o = Objects[req.ObjectID];
                if (o is ObjectGroup)
                {
                    ((ObjectGroup)o).ForEach(delegate(ObjectPart p)
                    {
                        p.Inventory.ForEach(delegate(ObjectPartInventoryItem i)
                        {
                            Script.ScriptInstance instance = i.ScriptInstance;
                            if(instance != null)
                            {
                                instance.RevokePermissions(req.AgentID, req.ObjectPermissions);
                            }
                        });
                    });
                }
            }
            catch
            {
                return;
            }
        }
    }
}
