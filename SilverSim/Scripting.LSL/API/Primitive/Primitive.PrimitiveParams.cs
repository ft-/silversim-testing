// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using SilverSim.Scene.Types.Script;

namespace SilverSim.Scripting.LSL.API.Primitive
{
    public partial class Primitive_API
    {
        #region Primitives

        [APILevel(APIFlags.LSL)]
        public LSLKey llGetKey(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.ID;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llAllowInventoryDrop(ScriptInstance Instance, int add)
        {
            lock (Instance)
            {
                Instance.Part.IsAllowedDrop = add != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        public AnArray llGetLinkPrimitiveParams(ScriptInstance Instance, int link, AnArray param)
        {
            AnArray parout = new AnArray();
            lock (Instance)
            {
                Instance.Part.ObjectGroup.GetPrimitiveParams(Instance.Part.LinkNumber, link, param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.2)]
        public AnArray llGetPrimitiveParams(ScriptInstance Instance, AnArray param)
        {
            AnArray parout = new AnArray();
            lock (Instance)
            {
                Instance.Part.ObjectGroup.GetPrimitiveParams(Instance.Part.LinkNumber, LINK_THIS, param.GetEnumerator(), ref parout);
            }
            return parout;
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetLocalPos(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.LocalPosition;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetPos(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.Position;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetRootPosition(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.ObjectGroup.Position;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Quaternion llGetRootRotation(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.ObjectGroup.RootPart.Rotation;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Quaternion llGetRot(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.Rotation;
            }
        }

        [APILevel(APIFlags.LSL)]
        public Vector3 llGetScale(ScriptInstance Instance)
        {
            lock (Instance)
            {
                return Instance.Part.Size;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llPassCollisions(ScriptInstance Instance, int pass)
        {
            lock (Instance)
            {
                Instance.Part.IsPassCollisions = pass != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llPassTouches(ScriptInstance Instance, int pass)
        {
            lock (Instance)
            {
                Instance.Part.IsPassTouches = pass != 0;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetClickAction(ScriptInstance Instance, int action)
        {
            lock (Instance)
            {
                Instance.Part.ClickAction = (ClickActionType)action;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetPayPrice(ScriptInstance Instance, int price, AnArray quick_pay_buttons)
        {
#warning Implement llSetPayPrice(int, AnArray)
        }

        [APILevel(APIFlags.LSL)]
        public void llSetPos(ScriptInstance Instance, Vector3 pos)
        {
            lock (Instance)
            {
                Instance.Part.Position = pos;
            }
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.2)]
        public void llSetPrimitiveParams(ScriptInstance Instance, AnArray rules)
        {
            lock (Instance)
            {
                Instance.Part.ObjectGroup.SetPrimitiveParams(Instance.Part.LinkNumber, LINK_THIS, rules.GetMarkEnumerator());
            }
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.2)]
        public void llSetLinkPrimitiveParams(ScriptInstance Instance, int linkTarget, AnArray rules)
        {
            llSetLinkPrimitiveParamsFast(Instance, linkTarget, rules);
        }

        [APILevel(APIFlags.LSL)]
        public void llSetLinkPrimitiveParamsFast(ScriptInstance Instance, int linkTarget, AnArray rules)
        {
            lock (Instance)
            {
                Instance.Part.ObjectGroup.SetPrimitiveParams(Instance.Part.LinkNumber, linkTarget, rules.GetMarkEnumerator());
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetScale(ScriptInstance Instance, Vector3 size)
        {
            lock (Instance)
            {
                Instance.Part.Size = size;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetSitText(ScriptInstance Instance, string text)
        {
            lock (Instance)
            {
                Instance.Part.ObjectGroup.RootPart.SitText = text;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetText(ScriptInstance Instance, string text, Vector3 color, double alpha)
        {
            ObjectPart.TextParam tp = new ObjectPart.TextParam();
            tp.Text = text;
            tp.TextColor = new ColorAlpha(color, alpha);
            lock (Instance)
            {
                Instance.Part.Text = tp;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llSetTouchText(ScriptInstance Instance, string text)
        {
            lock (Instance)
            {
                Instance.Part.ObjectGroup.RootPart.TouchText = text;
            }
        }

        [APILevel(APIFlags.LSL)]
        public void llTargetOmega(ScriptInstance Instance, Vector3 axis, double spinrate, double gain)
        {
            ObjectPart.OmegaParam op = new ObjectPart.OmegaParam();
            op.Axis = axis;
            op.Spinrate = spinrate;
            op.Gain = gain;
            lock (Instance)
            {
                Instance.Part.Omega = op;
            }
        }
        #endregion
    }
}
