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

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Primitive;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        #region Primitives

        [APILevel(APIFlags.LSL)]
        public UUID llGetKey()
        {
            lock (this)
            {
                return Part.ID;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llAllowInventoryDrop(int add)
        {
            lock (this)
            {
                Part.IsAllowedDrop = add != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llGetLinkPrimitiveParams(int link, AnArray param)
        {
            AnArray parout = new AnArray();
            lock (this)
            {
                Part.ObjectGroup.GetPrimitiveParams(Part.LinkNumber, link, param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llGetPrimitiveParams(AnArray param)
        {
            AnArray parout = new AnArray();
            lock (this)
            {
                Part.ObjectGroup.GetPrimitiveParams(Part.LinkNumber, LINK_THIS, param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetLocalPos()
        {
            lock (this)
            {
                return Part.LocalPosition;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetPos()
        {
            lock (this)
            {
                return Part.Position;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetRootPosition()
        {
            lock (this)
            {
                return Part.ObjectGroup.Position;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Quaternion llGetRootRotation()
        {
            lock (this)
            {
                return Part.ObjectGroup.RootPart.Rotation;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Quaternion llGetRot()
        {
            lock (this)
            {
                return Part.Rotation;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetScale()
        {
            lock (this)
            {
                return Part.Size;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llPassCollisions(int pass)
        {
            lock (this)
            {
                Part.IsPassCollisions = pass != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llPassTouches(int pass)
        {
            lock (this)
            {
                Part.IsPassTouches = pass != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetClickAction(int action)
        {
            lock (this)
            {
                Part.ClickAction = (ClickActionType)action;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetPayPrice(int price, AnArray quick_pay_buttons)
        {
#warning Implement llSetPayPrice(int, AnArray)
        }

        [APILevel(APIFlags.LSL)]
        public void llSetPos(Vector3 pos)
        {
            lock (this)
            {
                Part.Position = pos;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetPrimitiveParams(AnArray rules)
        {
            lock (this)
            {
                Part.ObjectGroup.SetPrimitiveParams(Part.LinkNumber, LINK_THIS, rules.GetMarkEnumerator());
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetLinkPrimitiveParams(int linkTarget, AnArray rules)
        {
            lock (this)
            {
                Part.ObjectGroup.SetPrimitiveParams(Part.LinkNumber, linkTarget, rules.GetMarkEnumerator());
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetScale(Vector3 size)
        {
            lock (this)
            {
                Part.Size = size;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetSitText(string text)
        {
            lock (this)
            {
                Part.ObjectGroup.RootPart.SitText = text;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetText(string text, Color color, double alpha)
        {
            ObjectPart.TextParam tp = new ObjectPart.TextParam();
            tp.Text = text;
            tp.TextColor = new ColorAlpha(color, alpha);
            lock (this)
            {
                Part.Text = tp;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetTouchText(string text)
        {
            lock (this)
            {
                Part.ObjectGroup.RootPart.TouchText = text;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llTargetOmega(Vector3 axis, double spinrate, double gain)
        {
            ObjectPart.OmegaParam op = new ObjectPart.OmegaParam();
            op.Axis = axis;
            op.Spinrate = spinrate;
            op.Gain = gain;
            lock (this)
            {
                Part.Omega = op;
            }
        }
        #endregion
    }
}
