// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;

namespace SilverSim.Types
{
    [Serializable]
    public sealed class ABoolean : IEquatable<ABoolean>, IValue
    {
        private bool m_Value;

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Boolean;
            }
        }

        public LSLValueType LSL_Type
        {
            get
            {
                return LSLValueType.Integer;
            }
        }
        #endregion Properties

        #region Constructors
        public ABoolean()
        {
        }

        public ABoolean(bool val)
        {
            m_Value = val;
        }
        #endregion Constructors

        public override string ToString()
        {
            return m_Value.ToString();
        }

        public int CompareTo(ABoolean v)
        {
            return m_Value.CompareTo(v.m_Value);
        }

        public bool Equals(ABoolean v)
        {
            return m_Value.Equals(v.m_Value);
        }

        #region Operators
        public static explicit operator AString(ABoolean v)
        {
            return new AString(v.m_Value ? "true" : "false");
        }

        public static explicit operator string(ABoolean v)
        {
            return v.m_Value ? "true" : "false";
        }

        public static implicit operator Int32(ABoolean v)
        {
            return v.m_Value ? 1 : 0;
        }

        public static explicit operator Integer(ABoolean v)
        {
            return new Integer(v.m_Value ? 1 : 0);
        }

        public static implicit operator bool(ABoolean v)
        {
            return v.m_Value;
        }
        #endregion Operators

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos, bool normalized)
        {
            // Little endian architecture
            m_Value = BitConverter.ToBoolean(byteArray, pos);
        }

        public void ToBytes(byte[] dest, int pos)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(m_Value), 0, dest, pos + 0, 1);
        }
        #endregion Serialization

        #region LSL Helpers
        public ABoolean AsBoolean { get { return new ABoolean(m_Value); } }
        public Integer AsInteger { get { return new Integer(m_Value ? 1 : 0); } }
        public Quaternion AsQuaternion { get { return new Quaternion(0, 0, 0, m_Value ? 1 : 0); } }
        public Real AsReal { get { return new Real(m_Value ? 1 : 0); } }
        public AString AsString { get { return new AString(m_Value ? "1" : string.Empty); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(m_Value ? 1 : 0); } }
        public uint AsUInt { get { return m_Value ? (uint)1 : 0; } }
        public int AsInt { get { return m_Value ? 1 : 0; } }
        public ulong AsULong { get { return m_Value ? (ulong)1 : 0; } }
        #endregion
    }
}
