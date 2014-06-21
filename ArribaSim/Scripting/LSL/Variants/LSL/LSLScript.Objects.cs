using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

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
    }
}
