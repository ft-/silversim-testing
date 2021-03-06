﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Object.Localization;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public void UnlinkObjects(List<UUID> primids)
        {
            ObjectPart part;
            var unlinkPrimsets = new Dictionary<UUID, List<UUID>>();

            foreach(UUID primid in primids.ToArray()) /* make copy first */
            {
                if(!Primitives.TryGetValue(primid, out part))
                {
                    primids.Remove(primid);
                }
                else
                {
                    ObjectGroup grp = part.ObjectGroup;
                    if (grp != null && !grp.IsAttached) /* never unlink attachments */
                    {
                        List<UUID> unlinkPrims;
                        UUID grpID = grp.ID;
                        if(!unlinkPrimsets.TryGetValue(grpID, out unlinkPrims))
                        {
                            unlinkPrims = new List<UUID>();
                            unlinkPrimsets.Add(grpID, unlinkPrims);
                        }
                        unlinkPrims.Add(part.ID);
                    }
                }
            }

            foreach(KeyValuePair<UUID, List<UUID>> kvp in unlinkPrimsets)
            {
                var newGrps = new List<ObjectGroup>();
                ObjectGroup grp;
                if (Primitives.TryGetValue(kvp.Key, out part))
                {
                    grp = part.ObjectGroup;
                    if (grp != null && grp.TryUnlink(kvp.Value, newGrps))
                    {
                        foreach (ObjectGroup newGrp in newGrps)
                        {
                            AddObjectGroupOnly(newGrp);
                            foreach (ObjectPart newPart in newGrp.ValuesByKey1)
                            {
                                newPart.Inventory.ResumeScripts();
                                newPart.TriggerOnUpdate(UpdateChangedFlags.Link);
                                newGrp.Scene?.ScheduleUpdate(newPart.UpdateInfo);
                            }
                        }
                    }
                }
            }
        }

        public void LinkObjects(List<UUID> objectids, bool insertAtLink2 = false)
        {
            var groups = new List<ObjectGroup>();
            foreach(UUID objectid in objectids)
            {
                ObjectPart part;
                if(Primitives.TryGetValue(objectid, out part) &&
                    (!groups.Contains(part.ObjectGroup) &&
                        (groups.Count == 0 || groups[0].Owner.EqualsGrid(part.ObjectGroup.Owner)) &&
                        part.ObjectGroup.m_SittingAgents.Count == 0))
                {
                    groups.Add(part.ObjectGroup);
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

            var targetGrp = groups[0];
            var newRootPos = targetGrp.GlobalPosition;
            var newRootRot = targetGrp.GlobalRotation;

            for(int groupidx = 1; groupidx < groups.Count; ++groupidx)
            {
                var srcGrp = groups[groupidx];
                UUID formerObjGrpID = srcGrp.ID;
                RemoveObjectGroupOnly(formerObjGrpID);
                Dictionary<UUID, Vector3> newChildPos = new Dictionary<UUID, Vector3>();
                Dictionary<UUID, Quaternion> newChildRot = new Dictionary<UUID, Quaternion>();
                foreach(var part in srcGrp.Values)
                {
                    part.Inventory.SuspendScripts();
                    newChildPos.Add(part.ID, part.GlobalPosition - newRootPos);
                    newChildRot.Add(part.ID, part.GlobalRotation / newRootRot);
                }

                if (insertAtLink2)
                {
                    ObjectPart[] parts = srcGrp.Values.ToArray();
                    foreach (var part in parts)
                    {
                        srcGrp.Remove(part.ID);
                    }
                    targetGrp.InsertLinks(2, parts);
                    foreach (var part in parts)
                    {
                        part.LocalPosition = newChildPos[part.ID];
                        part.LocalRotation = newChildRot[part.ID];
                        part.UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.All);
                        part.Inventory.ResumeScripts();
                    }
                }
                else
                {
                    ObjectPart[] parts = srcGrp.Values.ToArray();
                    foreach (var part in parts)
                    {
                        srcGrp.Remove(part.ID);
                        targetGrp.AddLink(part);
                        part.LocalPosition = newChildPos[part.ID];
                        part.LocalRotation = newChildRot[part.ID];
                        part.UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.All);
                        part.Inventory.ResumeScripts();
                    }
                }
            }

            foreach(var part in targetGrp.Values)
            {
                part.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
            }
        }
    }
}
