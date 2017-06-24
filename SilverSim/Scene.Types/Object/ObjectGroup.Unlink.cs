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

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectGroup
    {
        public void AddLink(ObjectPart part)
        {
            lock(m_LinkUnlinkLock)
            {
                part.ObjectGroup = this;
                Add(Count + 1, part.ID, part);
            }
        }

        private readonly object m_LinkUnlinkLock = new object();

        public bool TryUnlink(List<UUID> uuids, List<ObjectGroup> unlinkedPrims)
        {
            lock(m_LinkUnlinkLock)
            {
                if(Count == 1)
                {
                    return false;
                }

                bool rootPrimRemoved = false;
                Dictionary<UUID, Vector3> newChildPos = new Dictionary<UUID, Vector3>();
                Dictionary<UUID, Quaternion> newChildRot = new Dictionary<UUID, Quaternion>();

                foreach(UUID id in uuids)
                {
                    ObjectPart part;
                    if(TryGetValue(id, out part))
                    {
                        /* is any of those our root prim? */
                        if(part == RootPart)
                        {
                            rootPrimRemoved = true;
                        }
                        newChildPos[part.ID] = part.GlobalPosition;
                        newChildRot[part.ID] = part.GlobalRotation;
                    }
                    else
                    {
                        return false;
                    }
                }

                SortedDictionary<int, int> reorderLinkSet = new SortedDictionary<int, int>();
                int newlink = LINK_ROOT;
                if (rootPrimRemoved)
                {
                    Vector3 rootPos = RootPart.GlobalPosition;
                    Quaternion rootRot = RootPart.GlobalRotation;
                    Vector3 offsetPos = Vector3.Zero; 
                    Quaternion offsetRot = Quaternion.Identity;
                    ObjectGroup newLinkSet = new ObjectGroup(this);

                    /* we are extracting the remaining linkset into new ObjectGroup */
                    for (int i = LINK_ROOT; i <= Count; ++i)
                    {
                        ObjectPart linkpart = this[i];
                        linkpart.Inventory.SuspendScripts();
                        if (!uuids.Contains(linkpart.ID))
                        {
                            if (newlink == 1)
                            {
                                Vector3 newRootPos = linkpart.GlobalPosition;
                                Quaternion newRootRot = linkpart.GlobalRotation;
                                offsetRot = linkpart.LocalRotation;
                                offsetPos = linkpart.LocalPosition / offsetRot;
                                linkpart.LocalPosition = newRootPos;
                                linkpart.LocalRotation = newRootRot;
                                linkpart.IsSandbox = false;
                                newLinkSet.Add(LINK_ROOT, linkpart.ID, linkpart);
                                linkpart.ObjectGroup = newLinkSet;
                            }
                            else
                            {
                                linkpart.LocalPosition += offsetPos;
                                linkpart.LocalRotation /= offsetRot;
                                newLinkSet.AddLink(linkpart);
                                linkpart.ObjectGroup = newLinkSet;
                            }
                            Remove(linkpart.ID);
                        }
                    }

                    /* root prim always remains in original group */
                    uuids.Remove(RootPart.ID);

                    if (newLinkSet.Count != 0)
                    {
                        newLinkSet.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
                        unlinkedPrims.Add(newLinkSet);
                    }
                }

                newlink = LINK_ROOT + 1;
                for (int i = LINK_ROOT + 1; i <= Count; ++i)
                {
                    ObjectPart linkpart = this[i];
                    if(!uuids.Contains(linkpart.ID))
                    {
                        if (i != newlink)
                        {
                            reorderLinkSet.Add(i, newlink);
                        }
                        ++newlink;
                    }
                }

                /* remove child prims */
                foreach(UUID id in uuids)
                {
                    ObjectPart part;
                    if(Remove(id, out part))
                    {
                        ObjectGroup newGrp = new ObjectGroup(this);
                        newGrp.Add(LINK_ROOT, part.ID, part);
                        part.ObjectGroup = newGrp;
                        part.LocalPosition = newChildPos[id];
                        part.LocalRotation = newChildRot[id];
                        part.IsSandbox = false;
                        newGrp.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
                        unlinkedPrims.Add(newGrp);
                    }
                }

                foreach(KeyValuePair<int, int> kvp in reorderLinkSet)
                {
                    ChangeKey(kvp.Value, kvp.Key);
                }

                foreach(ObjectPart part in ValuesByKey1)
                {
                    part.TriggerOnUpdate(UpdateChangedFlags.Link);
                    part.Inventory.ResumeScripts();
                }

                return true;
            }
        }

        private bool TryUnlink(UUID primID, out ObjectPart part)
        {
            part = null;
            lock(m_LinkUnlinkLock)
            {
                if (TryGetValue(primID, out part))
                {
                    if (Count == 1)
                    {
                        return false;
                    }

                    if (part.LinkNumber == LINK_ROOT)
                    {
#if DEBUG
                        m_Log.DebugFormat("Unlinking root prim {0} from object", primID);
#endif
                        return TryUnlinkRoot(out part);
                    }
                    else
                    {
                        /* separate single prim */
                        int linkno = part.LinkNumber;
                        Vector3 rootPos = part.GlobalPosition;
                        Quaternion rootRot = part.GlobalRotation;
#if DEBUG
                        m_Log.DebugFormat("Unlinking child prim {0} (link {3}) from object (New location {1} / {2})", primID, rootPos, rootRot, linkno);
#endif
                        part.Inventory.SuspendScripts();
                        if (!Remove(primID))
                        {
                            part.Inventory.ResumeScripts();
                            return false;
                        }
                        part.LocalPosition = rootPos;
                        part.LocalRotation = rootRot;

                        for(++linkno; linkno <= Count; ++linkno)
                        {
                            ChangeKey(linkno - 1, linkno);
                        }

                        PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryUnlink(UUID primID, out ObjectGroup newGrp)
        {
            ObjectPart part;
            newGrp = null;
            if(TryUnlink(primID, out part))
            {
                newGrp = new ObjectGroup(this);
                newGrp.Add(LINK_ROOT, part.ID, part);
                part.ObjectGroup = newGrp;
                newGrp.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
                return true;
            }
            return false;
        }

        private bool TryUnlinkRoot(out ObjectPart part, bool sendEvent = true)
        {
            part = null;
            int primCount = Count;
            part = this[LINK_ROOT];
            var rootPos = part.GlobalPosition;
            var rootRot = part.GlobalRotation;
            ObjectPart newRoot;
            if (!TryGetValue(2, out newRoot))
            {
                part = null;
                return false;
            }
            var offsetPos = newRoot.Position;
            var offsetRot = newRoot.Rotation;
            foreach(var linkpart in ValuesByKey1)
            {
                linkpart.Inventory.SuspendScripts();
            }
            part.Inventory.SuspendScripts();
            part.ObjectGroup.StopKeyframedMotion();
            if (!Remove(part.ID))
            {
                foreach (var linkpart in ValuesByKey1)
                {
                    linkpart.Inventory.ResumeScripts();
                }
                part = null;
                return false;
            }

            newRoot.LocalPosition += rootPos;
            newRoot.LocalRotation *= rootRot;
            ChangeKey(LINK_ROOT, 2);
            int i;
            for (i = 3; i < primCount; ++i)
            {
                ChangeKey(i - 1, i);
                var linkPart = this[i - 1];
                linkPart.Position -= offsetPos;
                linkPart.Rotation /= offsetRot;
            }

            foreach (var linkpart in ValuesByKey1)
            {
                linkpart.Inventory.ResumeScripts();
                linkpart.TriggerOnUpdate(UpdateChangedFlags.Link);
            }

            if (sendEvent)
            {
                PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
            }
            return true;
        }
    }
}
