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
        #region Primitives

        public UUID llGetKey()
        {
            return Part.ID;
        }

        public void llAllowInventoryDrop(int add)
        {
            Part.IsAllowedDrop = add != 0;
        }

        public AnArray llGetLinkPrimitiveParams(int link, AnArray param)
        {
            AnArray parout = new AnArray();
            Part.Group.GetPrimitiveParams(Part.LinkNumber, link, param.GetEnumerator(), ref parout);
            return parout;
        }

        public AnArray llGetPrimitiveParams(AnArray param)
        {
            AnArray parout = new AnArray();
            Part.Group.GetPrimitiveParams(Part.LinkNumber, LINK_THIS, param.GetEnumerator(), ref parout);
            return parout;
        }

        public Vector3 llGetLocalPos()
        {
            return Part.LocalPosition;
        }

        public Vector3 llGetPos()
        {
            return Part.Position;
        }

        public Vector3 llGetRootPosition()
        {
            return Part.Group.Position;
        }

        public Quaternion llGetRootRotation()
        {
            return Part.Group.RootPart.Rotation;
        }

        public Quaternion llGetRot()
        {
            return Part.Rotation;
        }

        public Vector3 llGetScale()
        {
            return Part.Size;
        }

        public void llPassCollisions(int pass)
        {
            Part.IsPassCollisions = pass != 0;
        }

        public void llPassTouches(int pass)
        {
            Part.IsPassTouches = pass != 0;
        }

        public void llSetClickAction(int action)
        {
            Part.ClickAction = (ClickActionType)action;
        }

        public void llSetPayPrice(int price, AnArray quick_pay_buttons)
        {

        }

        public void llSetPos(Vector3 pos)
        {
            Part.Position = pos;
        }

        public void llSetPrimitiveParams(AnArray rules)
        {
            Part.Group.SetPrimitiveParams(Part.LinkNumber, LINK_THIS, rules.GetMarkEnumerator());
        }

        public void llSetLinkPrimitiveParams(int linkTarget, AnArray rules)
        {
            Part.Group.SetPrimitiveParams(Part.LinkNumber, linkTarget, rules.GetMarkEnumerator());
        }

        public void llSetScale(Vector3 size)
        {
            Part.Size = size;
        }

        public void llSetSitText(string text)
        {
            Part.Group.RootPart.SitText = text;
        }

        public void llSetText(string text, Color color, double alpha)
        {
            ObjectPart.TextParam tp = new ObjectPart.TextParam();
            tp.Text = text;
            tp.TextColor = new ColorAlpha(color, alpha);
            Part.Text = tp;
        }

        public void llSetTouchText(string text)
        {
            Part.Group.RootPart.TouchText = text;
        }

        public void llTargetOmega(Vector3 axis, double spinrate, double gain)
        {
            ObjectPart.OmegaParam op = new ObjectPart.OmegaParam();
            op.Axis = axis;
            op.Spinrate = spinrate;
            op.Gain = gain;
            Part.Omega = op;
        }
        #endregion
    }
}
