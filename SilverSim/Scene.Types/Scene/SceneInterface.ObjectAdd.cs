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
using SilverSim.LL.Messages.Object;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        void HandleObjectAdd(Message m)
        {
            ObjectAdd p = (ObjectAdd)m;
            if(p.CircuitAgentID != p.AgentID ||
                p.CircuitSessionID != p.SessionID)
            {
                return;
            }
        }

        public UInt32 ObjectAdd(ObjectAdd p)
        {
            RezObjectParams rezparams = new RezObjectParams();
            SilverSim.Scene.Types.Object.ObjectGroup group = new SilverSim.Scene.Types.Object.ObjectGroup();
            ObjectPart part = new ObjectPart();
            part.ID = UUID.Random;
            group.Add(1, part.ID, part);
            group.Name = "Primitive";
            IAgent agent;
            agent = Agents[p.AgentID];
            group.Owner = agent.Owner;
            group.LastOwner = agent.Owner;
            part.Creator = agent.Owner;
            SilverSim.Scene.Types.Object.ObjectPart.PrimitiveShape pshape;
            pshape = part.Shape;
            pshape.PCode = p.PCode;
            part.Material = p.Material;
            pshape.PathCurve = p.PathCurve;
            pshape.ProfileCurve = p.ProfileCurve;
            pshape.PathBegin = p.PathBegin;
            pshape.PathEnd = p.PathEnd;
            pshape.PathScaleX = p.PathScaleX;
            pshape.PathScaleY = p.PathScaleY;
            pshape.PathShearX = p.PathShearX;
            pshape.PathShearY = p.PathShearY;
            pshape.PathTwist = p.PathTwist;
            pshape.PathTwistBegin = p.PathTwistBegin;
            pshape.PathRadiusOffset = p.PathRadiusOffset;
            pshape.PathTaperX = p.PathTaperX;
            pshape.PathTaperY = p.PathTaperY;
            pshape.PathRevolutions = p.PathRevolutions;
            pshape.PathSkew = p.PathSkew;
            pshape.ProfileBegin = p.ProfileBegin;
            pshape.ProfileEnd = p.ProfileEnd;
            pshape.ProfileHollow = p.ProfileHollow;

            rezparams.RayStart = p.RayStart;
            rezparams.RayEnd = p.RayEnd;
            rezparams.RayTargetID = p.RayTargetID;
            rezparams.RayEndIsIntersection = p.RayEndIsIntersection;
            rezparams.Scale = p.Scale;
            rezparams.Rotation = p.Rotation;
            pshape.State = p.State;
            group.AttachPoint = p.LastAttachPoint;

            part.BaseMask = p.BasePermissions;
            part.EveryoneMask = p.EveryOnePermissions;
            part.OwnerMask = p.CurrentPermissions;
            part.NextOwnerMask = p.NextOwnerPermissions;
            part.GroupMask = p.GroupPermissions;
            group.Group.ID = p.GroupID;
            
            return RezObject(group, rezparams);
        }
    }
}
