// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectGroup
    {
        readonly object m_LinkUnlinkLock = new object();
        public bool TryUnlink(UUID primID, out ObjectPart part)
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

                    }
                    else
                    {
                        /* separate single prim */
                        Vector3 rootPos = part.GlobalPosition;
                        Quaternion rootRot = part.GlobalRotation;
                        part.Inventory.SuspendScripts();
                        if (!Remove(primID))
                        {
                            part.Inventory.ResumeScripts();
                            return false;
                        }
                        part.Position = rootPos;
                        part.Rotation = rootRot;

                        PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryUnlink(UUID primID, out ObjectGroup newGrp)
        {
            ObjectPart part;
            newGrp = null;
            if(TryUnlink(primID, out part))
            {
                newGrp = new ObjectGroup();
                newGrp.Add(LINK_ROOT, part.ID, part);
                newGrp.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
                return true;
            }
            return false;
        }

        public bool TryUnlinkRoot(out ObjectPart part, bool sendEvent = true)
        {
            part = null;
            int primCount = Count;
            Vector3 rootPos = part.GlobalPosition;
            Quaternion rootRot = part.GlobalRotation;
            ObjectPart newRoot;
            if (!TryGetValue(2, out newRoot))
            {
                return false;
            }
            Vector3 offsetPos = newRoot.Position;
            Quaternion offsetRot = newRoot.Rotation;
            part.Inventory.SuspendScripts();
            part.ObjectGroup.StopKeyframedMotion();
            if (!Remove(LINK_ROOT))
            {
                part.Inventory.ResumeScripts();
                return false;
            }

            newRoot.Position += rootPos;
            newRoot.Rotation *= rootRot;
            int i;
            for (i = 2; i < primCount; ++i)
            {
                ChangeKey(i - 1, i);
                if (i > 2)
                {
                    ObjectPart linkPart = this[i - 1];
                    linkPart.Position -= offsetPos;
                    linkPart.Rotation /= offsetRot;
                }
            }

            if (sendEvent)
            {
                PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
            }
            return true;
        }
    }
}
