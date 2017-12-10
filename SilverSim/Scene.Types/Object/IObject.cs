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

using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
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
        RezzerKey = 32,
        GroupTag = 33,
        TempAttached = 34,
        AttachedSlotsAvailable = 35,
        CreationTime = 36,
        SelectCount = 37,
        SitCount = 38
    }

    public enum PathfindingType
    {
        Other = -1,
        LegacyLinkset = 0,
        Avatar = 1,
        Character = 2,
        Walkable = 3,
        StaticObstacle = 4,
        MaterialVolume = 5,
        ExclusionVolume = 6
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

    public interface ILocalIDAccessor
    {
        UInt32 this[UUID sceneID] { get; set; }
    }

    public interface IObject
    {
        event Action<IObject> OnPositionChange;

        ILocalIDAccessor LocalID { get; }

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

        PathfindingType PathfindingType
        {
            get;
            set;
        }

        double WalkableCoefficientAvatar
        {
            get;
            set;
        }

        double WalkableCoefficientA
        {
            get;
            set;
        }

        double WalkableCoefficientB
        {
            get;
            set;
        }

        double WalkableCoefficientC
        {
            get;
            set;
        }

        double WalkableCoefficientD
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

        void MoveToTarget(Vector3 target, double tau, UUID notifyPrimId, UUID notifyItemId);
        void StopMoveToTarget();
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
        private sealed class NlsAnchor
        {
        }
        static readonly NlsAnchor m_NlsAnchor = new NlsAnchor();

        #region List Access Helpers
        public static PrimitiveParamsType GetPrimParamType(IEnumerator<IValue> enumerator)
        {
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Integer)
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "ExpectingAnIntegerParameterForParameterTypeGot0", "Expecting an integer parameter for parameter type: got {0}", current.LSL_Type.ToString());
            }
            return (PrimitiveParamsType)current.AsInt;
        }

        public static ObjectDetailsType GetObjectDetailsType(IEnumerator<IValue> enumerator)
        {
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Integer)
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "ExpectingAnIntegerParameterForObjectDetailsTypeGot0", "Expecting an integer parameter for object details type: got {0}", current.LSL_Type.ToString());
            }
            return (ObjectDetailsType)current.AsInt;
        }

        public static int GetInteger(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "NoParameterFor0", "No parameter for {0}", paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Integer)
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "ExpectingAnIntegerParameterFor0Got1", "Expecting an integer parameter for {0}: got {1}", paraName, current.LSL_Type.ToString());
            }
            return current.AsInt;
        }

        public static bool GetBoolean(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "NoParameterFor0", "No parameter for {0}", paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Integer)
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "ExpectingAnIntegerParameterFor0Got1", "Expecting an integer parameter for {0}: got {1}", paraName, current.LSL_Type.ToString());
            }
            return current.AsBoolean;
        }

        public static string GetString(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "NoParameterFor0", "No parameter for {0}", paraName);
            }
            IValue current = enumerator.Current;
            LSLValueType lslType = current.LSL_Type;
            if (lslType != LSLValueType.String && lslType != LSLValueType.Key)
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "ExpectingAStringParameterFor0Got1", "Expecting a string parameter for {0}: got {1}", paraName, current.LSL_Type.ToString());
            }
            return current.ToString();
        }

        public static UUID GetKey(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "NoParameterFor0", "No parameter for {0}", paraName);
            }
            IValue current = enumerator.Current;
            LSLValueType lslType = current.LSL_Type;
            if (lslType != LSLValueType.String && lslType != LSLValueType.Key)
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "ExpectingAKeyParameterFor0Got1", "Expecting a key parameter for {0}: got {1}", paraName, current.LSL_Type.ToString());
            }
            return current.AsUUID;
        }

        public static double GetDouble(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "NoParameterFor0", "No parameter for {0}", paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Float && current.LSL_Type != LSLValueType.Integer)
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "ExpectingAFloatParameterFor0Got1", "Expecting a float parameter for {0}: got {1}", paraName, current.LSL_Type.ToString());
            }
            return current.AsReal;
        }

        public static Quaternion GetRotation(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "NoParameterFor0", "No parameter for {0}", paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Rotation)
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "ExpectingARotationParameterFor0Got1", "Expecting a rotation parameter for {0}: got {1}", paraName, current.LSL_Type.ToString());
            }
            return current.AsQuaternion;
        }

        public static Vector3 GetVector(IEnumerator<IValue> enumerator, string paraName)
        {
            if (!enumerator.MoveNext())
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "NoParameterFor0", "No parameter for {0}", paraName);
            }
            IValue current = enumerator.Current;
            if (current.LSL_Type != LSLValueType.Vector)
            {
                throw new LocalizedScriptErrorException(m_NlsAnchor, "ExpectingAVectorParameterFor0Got1", "Expecting a vector parameter for {0}: got {1}", paraName, current.LSL_Type.ToString());
            }
            return current.AsVector3;
        }
        #endregion
    }
    #endregion
}
