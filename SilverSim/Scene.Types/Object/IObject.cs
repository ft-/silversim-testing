// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object
{
    public enum ObjectDetailsType
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
        HoverHeight = 25,
        BodyShapeType = 26,
        LastOwner = 27,
        ClickAction = 28,
        Omega = 29,
        PrimCount = 30,
        TotalInventoryCount = 31,
        RezzerKey = 32
    }

    public interface IPrimitiveParamsInterface
    {
        void GetPrimitiveParams(PrimitiveParamsType type, AnArray paramList);
        void SetPrimitiveParams(PrimitiveParamsType type, AnArray.MarkEnumerator enumerator);
    }

    public struct BoundingBox
    {
        public Vector3 CenterOffset;
        public Vector3 Size;
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

        UGI Group
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

        Vector3 AngularAcceleration 
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

        double PhysicsGravityMultiplier
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
        void GetBoundingBox(out BoundingBox box);
        void GetPrimitiveParams(AnArray.Enumerator enumerator, AnArray paramList);
        void SetPrimitiveParams(AnArray.MarkEnumerator enumerator);
        void GetObjectDetails(AnArray.Enumerator enumerator, AnArray paramList);
        DetectedTypeFlags DetectedType { get; }
        void PostEvent(IScriptEvent ev);
        #endregion
    }

    public interface IPhysicalObject : IObject
    {
        RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors
        {
            get;
        }

        IPhysicsObject PhysicsActor
        {
            get;
        }

        void PhysicsUpdate(PhysicsStateData data);
    }

    #region Params Helper
    public static class ParamsHelper
    {
        #region List Access Helpers
        public static PrimitiveParamsType GetPrimParamType(IEnumerator<IValue> enumerator)
        {
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Integer)
            {
                throw new ArgumentException("Expecting an integer parameter for parameter type: got " + current.LSL_Type.ToString());
            }
            return (PrimitiveParamsType)current.AsInt;
        }

        public static ObjectDetailsType GetObjectDetailsType(IEnumerator<IValue> enumerator)
        {
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Integer)
            {
                throw new ArgumentException("Expecting an integer parameter for object details type: got " + current.LSL_Type.ToString());
            }
            return (ObjectDetailsType)current.AsInt;
        }

        public static int GetInteger(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Integer)
            {
                throw new ArgumentException("Expecting an integer parameter for " + paraName + ": got " + current.LSL_Type.ToString());
            }
            return current.AsInt;
        }

        public static bool GetBoolean(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Integer)
            {
                throw new ArgumentException("Expecting an integer parameter for " + paraName + ": got " + current.LSL_Type.ToString());
            }
            return current.AsBoolean;
        }

        public static string GetString(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            IValue current = enumerator.Current;
            LSLValueType lslType = current.LSL_Type;
            if (lslType != LSLValueType.String && lslType != LSLValueType.Key)
            {
                throw new ArgumentException("Expecting a string parameter for " + paraName + ": got " + current.LSL_Type.ToString());
            }
            return current.ToString();
        }

        public static UUID GetKey(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            IValue current = enumerator.Current;
            LSLValueType lslType = current.LSL_Type;
            if (lslType != LSLValueType.String && lslType != LSLValueType.Key)
            {
                throw new ArgumentException("Expecting a key parameter for " + paraName + ": got " + current.LSL_Type.ToString());
            }
            return current.AsUUID;
        }

        public static double GetDouble(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Float && current.LSL_Type != LSLValueType.Integer)
            {
                throw new ArgumentException("Expecting a float parameter for " + paraName + ": got " + current.LSL_Type.ToString());
            }
            return current.AsReal;
        }

        public static Quaternion GetRotation(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Rotation)
            {
                throw new ArgumentException("Expecting a rotation parameter for " + paraName + ": got " + current.LSL_Type.ToString());
            }
            return current.AsQuaternion;
        }

        public static Vector3 GetVector(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("No parameter for " + paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Vector)
            {
                throw new ArgumentException("Expecting a vector parameter for " + paraName + ": got " + current.LSL_Type.ToString());
            }
            return current.AsVector3;
        }
        #endregion
    }
    #endregion
}
