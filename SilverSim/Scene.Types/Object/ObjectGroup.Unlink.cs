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

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectGroup
    {
        private readonly object m_LinkUnlinkLock = new object();
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
            var rootPos = part.GlobalPosition;
            var rootRot = part.GlobalRotation;
            ObjectPart newRoot;
            if (!TryGetValue(2, out newRoot))
            {
                return false;
            }
            var offsetPos = newRoot.Position;
            var offsetRot = newRoot.Rotation;
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
                    var linkPart = this[i - 1];
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
