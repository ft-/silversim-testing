// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public void UnlinkObjects(List<UUID> primids)
        {
            ObjectPart part;
            foreach (UUID primid in primids)
            {
                if (Primitives.TryGetValue(primid, out part))
                {
                    ObjectGroup grp = part.ObjectGroup;
                    if(null == grp)
                    {
                        continue;
                    }

                    ObjectGroup newGrp;
                    if(grp.TryUnlink(primid, out newGrp))
                    {
                        Add(newGrp);
                        newGrp.RootPart.Inventory.ResumeScripts();
                    }
                }
            }
        }

        public void LinkObjects(List<UUID> objectids)
        {
            List<ObjectGroup> groups = new List<ObjectGroup>();
            foreach(UUID objectid in objectids)
            {
                ObjectPart part;
                if(Primitives.TryGetValue(objectid, out part))
                {
                    if (!groups.Contains(part.ObjectGroup) &&
                        (groups.Count == 0 || groups[0].Owner.EqualsGrid(part.ObjectGroup.Owner)) &&
                        part.ObjectGroup.m_SittingAgents.Count == 0)
                    {
                        groups.Add(part.ObjectGroup);
                    }
                }
            }

            if(groups.Count < 2)
            {
                return;
            }

            if(groups[0].IsGroupOwned)
            {
                return;
            }

            ObjectGroup targetGrp = groups[0];
            Vector3 newRootPos = targetGrp.GlobalPosition;
            Quaternion newRootRot = targetGrp.GlobalRotation;
            
            for(int groupidx = 1; groupidx < groups.Count; ++groupidx)
            {
                ObjectGroup srcGrp = groups[groupidx];
                List<ObjectPart> parts = new List<ObjectPart>();
                foreach(ObjectPart part in srcGrp.Values)
                {
                    part.Inventory.SuspendScripts();
                    Vector3 newChildPos = part.GlobalPosition - newRootPos;
                    Quaternion newChildRot = part.GlobalRotation / newRootRot;
                    srcGrp.Remove(part.ID);
                    part.Position = newChildPos;
                    part.Rotation = newChildRot;
                    targetGrp.Add(targetGrp.Count + 1, part.ID, part);
                    part.Inventory.ResumeScripts();
                }
                Remove(srcGrp);
            }
        }
    }
}
