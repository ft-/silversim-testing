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

using System;
using System.Collections.Generic;

namespace SilverSim.Types
{
    [Serializable]
    public sealed class ABoolean : IComparable<ABoolean>, IEquatable<ABoolean>, IValue
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
            m_Value = false;
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

        public static implicit operator Integer(ABoolean v)
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
        public AString AsString { get { return new AString(m_Value ? "1" : ""); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(m_Value ? 1 : 0); } }
        public uint AsUInt { get { return m_Value ? (uint)1 : 0; } }
        public int AsInt { get { return m_Value ? 1 : 0; } }
        public ulong AsULong { get { return m_Value ? (ulong)1 : 0; } }
        #endregion
    }
}
