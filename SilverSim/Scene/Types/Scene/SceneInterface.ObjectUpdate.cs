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
using SilverSim.Scene.Types.Agent;
using SilverSim.Types.Primitive;
using SilverSim.LL.Messages;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        private SilverSim.LL.Messages.Object.ObjectUpdate AgentToObjectUpdate(IAgent agent)
        {
            SilverSim.LL.Messages.Object.ObjectUpdate m = new LL.Messages.Object.ObjectUpdate();
            SilverSim.LL.Messages.Object.ObjectUpdate.ObjData d = new LL.Messages.Object.ObjectUpdate.ObjData();
            d.Data = new byte[0];
            d.ExtraParams = new byte[1];
            d.FullID = agent.ID;
            d.LocalID = agent.LocalID;
            d.Material = (byte)PrimitiveMaterial.Flesh;
            d.MediaURL = "";
            d.NameValue = string.Format("FirstName STRING RW SV {0}\nLastName STRING RW SV {1}\nTitle STRING RW SV {2}", agent.FirstName, agent.LastName, "");
            d.ObjectData = new byte[76];
            agent.Position.ToBytes(d.ObjectData, 16);
            /*
            data.CollisionPlane.ToBytes(objectData, 0);
            data.OffsetPosition.ToBytes(objectData, 16);
//            data.Velocity.ToBytes(objectData, 28);
//            data.Acceleration.ToBytes(objectData, 40);
            //data.AngularVelocity.ToBytes(objectData, 64);
             */
            Quaternion rot = agent.Rotation;
            //if(!agent.SittingOnObject)
            {
                rot.X = 0;
                rot.Y = 0;
            }
            rot.ToBytes(d.ObjectData, 52);
            
            d.ParentID = 0;
            d.PathCurve = 16;
            d.PathScaleX = 100;
            d.PathScaleY = 100;
            d.PCode = PrimitiveCode.Avatar;
            d.ProfileCurve = 1;
            d.PSBlock = new byte[0];
            d.Scale = new SilverSim.Types.Vector3(0.45f, 0.6f, 1.9f);
            d.Text = "";
            d.TextColor = new SilverSim.Types.ColorAlpha(0, 0, 0, 0);
            d.TextureAnim = new byte[0];
            d.TextureEntry = new byte[0];
            d.UpdateFlags = PrimitiveFlags.Physics | PrimitiveFlags.ObjectModify | PrimitiveFlags.ObjectCopy | PrimitiveFlags.ObjectAnyOwner |
                            PrimitiveFlags.ObjectYouOwner | PrimitiveFlags.ObjectMove | PrimitiveFlags.InventoryEmpty | PrimitiveFlags.ObjectTransfer |
                            PrimitiveFlags.ObjectOwnerModify;
            m.ObjectData.Add(d);
            m.GridPosition = RegionData.Location;
            return m;
        }

        public void SendAgentObjectToAgent(IAgent agent, IAgent targetAgent)
        {
            SilverSim.LL.Messages.Object.ObjectUpdate m = AgentToObjectUpdate(agent);
            targetAgent.SendMessageAlways(m, ID);
        }

        public void SendAgentObjectToAllAgents(IAgent agent)
        {
            SilverSim.LL.Messages.Object.ObjectUpdate m = AgentToObjectUpdate(agent);
            foreach (IAgent a in Agents)
            {
                m_Log.DebugFormat("Sending Agent ObjectUpdate to {0} for {1}", a.ID, agent.ID);
                a.SendMessageAlways(m, ID);
            }
        }
    }
}
