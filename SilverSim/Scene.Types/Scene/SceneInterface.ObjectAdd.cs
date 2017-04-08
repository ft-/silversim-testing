// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Types.Primitive;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        [PacketHandler(MessageType.ObjectAdd)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectAdd(Message m)
        {
            ObjectAdd p = (ObjectAdd)m;
            if(p.CircuitAgentID != p.AgentID ||
                p.CircuitSessionID != p.SessionID)
            {
                return;
            }

            ObjectAdd(p);
        }

        public UInt32 ObjectAdd(ObjectAdd p)
        {
            RezObjectParams rezparams = new RezObjectParams();
            Object.ObjectGroup group = new Object.ObjectGroup();
            ObjectPart part = new ObjectPart();
            part.ID = UUID.Random;
            group.Add(1, part.ID, part);
            group.Name = "Primitive";
            IAgent agent;
            agent = Agents[p.AgentID];
            UUI agentOwner = agent.Owner;
            group.Owner = agentOwner;
            group.LastOwner = agentOwner;
            part.Creator = agentOwner;
            ObjectPart.PrimitiveShape pshape;
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
            part.ObjectGroup = group;
            TextureEntry te = new TextureEntry();
            te.DefaultTexture = new TextureEntryFace(null);
            part.TextureEntry = te;
            
            return RezObject(group, rezparams);
        }
    }
}
