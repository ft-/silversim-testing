// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types
{
    public sealed class Undef : IEquatable<Undef>, IValue
    {
        public Undef()
        {

        }

        public int CompareTo(Undef v)
        {
            return 0;
        }

        public bool Equals(Undef v)
        {
            return true;
        }

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Undef;
            }
        }

        public LSLValueType LSL_Type
        {
            get
            {
                return LSLValueType.Invalid;
            }
        }
        #endregion Properties

        #region Operators
        public static implicit operator bool(Undef v)
        {
            return false;
        }
        #endregion Operators

        #region Helpers
        public ABoolean AsBoolean { get { return new ABoolean(); } }
        public Integer AsInteger { get { return new Integer(); } }
        public Quaternion AsQuaternion { get { return new Quaternion(); } }
        public Real AsReal { get { return new Real(); } }
        public AString AsString { get { return new AString(); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(); } }
        public uint AsUInt { get { return 0; } }
        public int AsInt { get { return 0; } }
        public ulong AsULong { get { return 0; } }
        #endregion

        public override string ToString()
        {
            return "undef";
        }
    }
}
