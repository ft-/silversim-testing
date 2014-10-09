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

using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object
{
    public enum ClickActionType : int
    {
        None = 0,
        Touch = 0,
        Sit = 1,
        Buy = 2,
        Pay = 3,
        Open = 4,
        Play = 5,
        OpenMedia = 6,
        Zoom = 7
    }

    public enum PrimitiveParamsType : int
    {
        Name = 27,
        Desc = 28,
        Type = 9,
        Slice = 35,
        PhysicsShapeType = 30,
        Material = 2,
        Physics = 3,
        TempOnRez = 4,
        Phantom = 5,
        Position = 6,
        PosLocal = 33,
        Rotation = 8,
        RotLocal = 29,
        Size = 7,
        Texture = 17,
        Text = 26,
        Color = 18,
        BumpShiny = 19,
        PointLight = 23,
        FullBright = 20,
        Flexible = 21,
        TexGen = 22,
        Glow = 25,
        Omega = 32,
        LinkTarget = 34,
    }

    public enum PrimitivePhysicsShapeType : int
    {
        Prim = 0,
        None = 1,
        Convex = 2,
    }

    public enum PrimitiveMaterial : int
    {
        Stone = 0,
        Metal = 1,
        Glass = 2,
        Wood = 3,
        Flesh = 4,
        Plastic = 5,
        Rubber = 6
    }

    public enum PrimitiveShapeType : int
    {
        Box = 0,
        Cylinder = 1,
        Prism = 2,
        Sphere = 3,
        Torus = 4,
        Tube = 5,
        Ring = 6,
        Sculpt = 7
    }

    [Flags]
    public enum PrimitiveSculptType : byte
    {
        Sphere = 1,
        Torus = 2,
        Plane = 3,
        Cylinder = 4,
        Mesh = 5,
        Invert = 64,
        Mirror = 128
    }

    public enum PrimitiveHoleShape : int
    {
        Default = 0x00,
        Circle = 0x10,
        Square = 0x20,
        Triangle = 0x30
    }

    public enum ObjectDetailsType : int
    {
        Name = 1,
        Desc = 2,
        Pos = 3,
        Rot = 4,
        Velocity = 5,
        Owner = 6,
        Group = 7,
        Creator = 8,
        RunningScriptCount = 9,
        TotalScriptCount = 10,
        ScriptMemory = 11,
        ScriptTime = 12,
        PrimEquivalence = 13,
        ServerCost = 14,
        StreamingCost = 15,
        PhysicsCost = 16,
        CharacterTime = 17,
        Root = 18,
        AttachedPoint = 19,
        PathfindingType = 20,
        Physics = 21,
        Phantom = 22,
        TempOnRez = 23,
        RenderWeight = 24,
    }

    public interface IPrimitiveParamsInterface
    {
        void GetPrimitiveParams(PrimitiveParamsType type, ref AnArray paramList);
        void SetPrimitiveParams(PrimitiveParamsType type, AnArray.MarkEnumerator enumerator);
    }

    public interface IObject
    {
        event Action<IObject> OnPositionChange;

        UInt32 LocalID { get; set; }

        #region Properties
        UUID ID
        {
            get;
        }

        string Name
        {
            get;
            set;
        }

        UUI Owner
        {
            get;
            set;
        }

        string Description
        {
            get;
            set;
        }

        Vector3 Position
        {
            get;
            set;
        }

        Vector3 Velocity
        {
            get;
            set;
        }

        Vector3 AngularVelocity
        {
            get;
            set;
        }

        Vector3 GlobalPosition
        {
            get;
            set;
        }

        Vector3 LocalPosition
        {
            get;
            set;
        }

        Vector3 Acceleration
        {
            get;
            set;
        }

        Quaternion GlobalRotation
        {
            get;
            set;
        }

        Quaternion LocalRotation
        {
            get;
            set;
        }

        Quaternion Rotation
        {
            get;
            set;
        }

        Vector3 Size
        {
            get;
            set;
        }

        bool IsInScene(SceneInterface scene);

        byte[] TerseData
        {
            get;
        }
        #endregion

        #region Methods
        void GetPrimitiveParams(AnArray.Enumerator enumerator, ref AnArray paramList);
        void SetPrimitiveParams(AnArray.MarkEnumerator enumerator);
        void GetObjectDetails(AnArray.Enumerator enumerator, ref AnArray paramList);
        void PostEvent(IScriptEvent ev);
        #endregion
    }

    #region Params Helper
    public static class ParamsHelper
    {
        #region List Access Helpers
        public static PrimitiveParamsType GetPrimParamType(IEnumerator<IValue> enumerator)
        {
            if (enumerator.Current.LSL_Type != LSLValueType.Integer)
            {
                throw new ArgumentException("Expecting an integer parameter for parameter type: got " + enumerator.Current.LSL_Type.ToString());
            }
            return (PrimitiveParamsType)enumerator.Current.AsInt;
        }

        public static ObjectDetailsType GetObjectDetailsType(IEnumerator<IValue> enumerator)
        {
            if (enumerator.Current.LSL_Type != LSLValueType.Integer)
            {
                throw new ArgumentException("Expecting an integer parameter for object details type: got " + enumerator.Current.LSL_Type.ToString());
            }
            return (ObjectDetailsType)enumerator.Current.AsInt;
        }

        public static int GetInteger(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            if (enumerator.Current.LSL_Type != LSLValueType.Integer)
            {
                throw new ArgumentException("Expecting an integer parameter for " + paraName + ": got " + enumerator.Current.LSL_Type.ToString());
            }
            return enumerator.Current.AsInt;
        }

        public static bool GetBoolean(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            if (enumerator.Current.LSL_Type != LSLValueType.Integer)
            {
                throw new ArgumentException("Expecting an integer parameter for " + paraName + ": got " + enumerator.Current.LSL_Type.ToString());
            }
            return enumerator.Current.AsBoolean;
        }

        public static string GetString(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            if (enumerator.Current.LSL_Type != LSLValueType.String && enumerator.Current.LSL_Type != LSLValueType.Key)
            {
                throw new ArgumentException("Expecting a string parameter for " + paraName + ": got " + enumerator.Current.LSL_Type.ToString());
            }
            return enumerator.Current.ToString();
        }

        public static UUID GetKey(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            if (enumerator.Current.LSL_Type != LSLValueType.String && enumerator.Current.LSL_Type != LSLValueType.Key)
            {
                throw new ArgumentException("Expecting a key parameter for " + paraName + ": got " + enumerator.Current.LSL_Type.ToString());
            }
            return enumerator.Current.AsUUID;
        }

        public static double GetDouble(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            if (enumerator.Current.LSL_Type != LSLValueType.Float)
            {
                throw new ArgumentException("Expecting a float parameter for " + paraName + ": got " + enumerator.Current.LSL_Type.ToString());
            }
            return enumerator.Current.AsReal;
        }

        public static Quaternion GetRotation(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            if (enumerator.Current.LSL_Type != LSLValueType.Rotation)
            {
                throw new ArgumentException("Expecting a rotation parameter for " + paraName + ": got " + enumerator.Current.LSL_Type.ToString());
            }
            return enumerator.Current.AsQuaternion;
        }

        public static Vector3 GetVector(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            if (enumerator.Current.LSL_Type != LSLValueType.Vector)
            {
                throw new ArgumentException("Expecting a vector parameter for " + paraName + ": got " + enumerator.Current.LSL_Type.ToString());
            }
            return enumerator.Current.AsVector3;
        }
        #endregion
    }
    #endregion
}
