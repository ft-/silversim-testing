/*

ArribaSim is distributed under the terms of the
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
using ArribaSim.Types;
using ArribaSim.Scene.Types.Object;

namespace ArribaSim.Scripting.LSL.Variants.LSL
{
    public partial class LSLScript
    {
        public Vector3 llGetCenterOfMass()
        {
            return Vector3.Zero;
        }

        public UUID llGetCreator()
        {
            return Part.Group.Creator.ID;
        }

        public AString llGetObjectDesc()
        {
            return new AString(Part.Group.Name);
        }

        public AnArray llGetObjectDetails(AnArray param)
        {
            AnArray parout = new AnArray();
            Part.Group.GetObjectDetails(param.GetEnumerator(), ref parout);
            return parout;
        }

        public string llGetObjectName()
        {
            return Part.Group.Description;
        }

        public void llSetObjectDesc(AString desc)
        {
            Part.Group.Description = desc.ToString();
        }

        public void llSetObjectName(AString name)
        {
            Part.Group.Name = name.ToString();
        }

        public Integer llSetRegionPos(Vector3 pos)
        {
            return new Integer(0);
        }

        public Vector3 llGetVel()
        {
            return Part.Group.Velocity;
        }

        public UUID llGetOwner()
        {
            return Part.Group.Owner.ID;
        }

        public UUID llGetOwnerKey(UUID id)
        {
            ObjectPart part;
            try
            {
                part = Part.Group.Scene.Primitives[id];
            }
            catch
            {
                return id;
            }
            return part.Owner.ID;
        }

        public Integer llGetNumberOfPrims()
        {
            return new Integer(Part.Group.Count);
        }

        public UUID llGetLinkKey(Integer link)
        {
            if(link == LINK_THIS)
            {
                return Part.ID;
            }
            else
            {
                return Part.Group[link].ID;
            }
        }

        public AString llGetLinkName(Integer link)
        {
            if (link == LINK_THIS)
            {
                return new AString(Part.Name);
            }
            else
            {
                return new AString(Part.Group[link].Name);
            }
        }

        public Integer llGetLinkNumber()
        {
            return new Integer(Part.LinkNumber);
        }
    }
}
