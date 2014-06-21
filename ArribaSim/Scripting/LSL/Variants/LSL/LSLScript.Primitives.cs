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

        public AnArray llGetLinkPrimitiveParams(Integer link, AnArray param)
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

        public void llPassCollisions(Integer pass)
        {
            Part.IsPassCollisions = pass;
        }

        public void llPassTouches(Integer pass)
        {
            Part.IsPassTouches = pass != 0;
        }

        public void llSetClickAction(Integer action)
        {
            Part.ClickAction = (ClickActionType)action.AsInt;
        }

        public void llSetPayPrice(Integer price, AnArray quick_pay_buttons)
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

        public void llSetLinkPrimitiveParams(Integer linkTarget, AnArray rules)
        {
            Part.Group.SetPrimitiveParams(Part.LinkNumber, linkTarget, rules.GetMarkEnumerator());
        }

        public void llSetScale(Vector3 size)
        {
            Part.Size = size;
        }

        public void llSetSitText(AString text)
        {
            Part.Group.RootPart.SitText = text.ToString();
        }

        public void llSetSoundQueueing(Integer queue)
        {
            Part.IsSoundQueueing = queue != 0;
        }

        public void llSetText(AString text, Color color, Real alpha)
        {
            ObjectPart.TextParam tp = new ObjectPart.TextParam();
            tp.Text = text.ToString();
            tp.TextColor = new ColorAlpha(color, alpha);
            Part.Text = tp;
        }

        public void llSetTouchText(AString text)
        {
            Part.Group.RootPart.TouchText = text.ToString();
        }

        public void llTargetOmega(Vector3 axis, Real spinrate, Real gain)
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
