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

using System;
using System.Collections.Generic;

namespace SilverSim.Types
{
    [Serializable]
    public sealed class ABoolean : IEquatable<ABoolean>, IValue
    {
        private bool m_Value;

        #region Properties
        public ValueType Type => ValueType.Boolean;

        public LSLValueType LSL_Type => LSLValueType.Integer;
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

        public override string ToString() => m_Value.ToString();

        public int CompareTo(ABoolean v) => m_Value.CompareTo(v.m_Value);

        public bool Equals(ABoolean v) => m_Value.Equals(v.m_Value);

        #region Operators
        public static explicit operator AString(ABoolean v) => new AString(v.m_Value ? "true" : "false");

        public static explicit operator string(ABoolean v) => v.m_Value ? "true" : "false";

        public static implicit operator Int32(ABoolean v) => v.m_Value ? 1 : 0;

        public static explicit operator Integer(ABoolean v) => new Integer(v.m_Value ? 1 : 0);

        public static implicit operator bool(ABoolean v) => v.m_Value;
        #endregion Operators

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos)
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
        public ABoolean AsBoolean => new ABoolean(m_Value);
        public Integer AsInteger => new Integer(m_Value ? 1 : 0);
        public Quaternion AsQuaternion => new Quaternion(0, 0, 0, m_Value ? 1 : 0);
        public Real AsReal => new Real(m_Value ? 1 : 0);
        public AString AsString => new AString(m_Value ? "1" : string.Empty);
        public UUID AsUUID => new UUID();
        public Vector3 AsVector3 => new Vector3(m_Value ? 1 : 0);
        public uint AsUInt => m_Value ? (uint)1 : 0;
        public int AsInt => m_Value ? 1 : 0;
        public ulong AsULong => m_Value ? (ulong)1 : 0;
        public long AsLong => m_Value ? 1 : 0;
        #endregion
    }
}
