// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL
{
    public sealed class LSLKey : IComparable<LSLKey>, IEquatable<LSLKey>, IComparable<string>, IEquatable<string>, IValue
    {
        private string m_Value;

        #region Properties
        public SilverSim.Types.ValueType Type
        {
            get
            {
                return SilverSim.Types.ValueType.String;
            }
        }

        public LSLValueType LSL_Type
        {
            get
            {
                return LSLValueType.Key;
            }
        }
        #endregion Properties

        public LSLKey()
        {
            m_Value = string.Empty;
        }

        public LSLKey(UUID uuid)
        {
            m_Value = uuid.ToString();
        }

        public LSLKey(string val)
        {
            m_Value = (string)val.Clone();
        }

        public int CompareTo(LSLKey v)
        {
            return m_Value.CompareTo(v.m_Value);
        }

        public bool Equals(LSLKey v)
        {
            return m_Value.Equals(v.m_Value);
        }

        public int CompareTo(string v)
        {
            return m_Value.CompareTo(m_Value);
        }

        public bool Equals(string v)
        {
            return m_Value.Equals(m_Value);
        }

        public override string ToString()
        {
            return (string)m_Value.Clone();
        }

        public AString Substring(Int32 startIndex)
        {
            return new AString(m_Value.Substring(startIndex));
        }

        public AString Substring(Int32 startIndex, Int32 length)
        {
            return new AString(m_Value.Substring(startIndex, length));
        }

        public Integer Length
        {
            get
            {
                return new Integer(m_Value.Length);
            }
        }

        public int IsLSLTrue
        {
            get
            {
                UUID uuid;
                if (UUID.TryParse(m_Value, out uuid))
                {
                    return !uuid.Equals(UUID.Zero) ? 1 : 0;
                }
                return m_Value.Length != 0 ? 1 : 0;
            }
        }

        #region Operators
        public static implicit operator LSLKey(UUID u)
        {
            return new LSLKey(u);
        }

        public static implicit operator LSLKey(string v)
        {
            return new LSLKey(v);
        }

        public static implicit operator UUID(LSLKey v)
        {
            UUID uuid;
            if(UUID.TryParse(v.ToString(), out uuid))
            {
                return uuid;
            }
            return UUID.Zero;
        }

        public static implicit operator bool(LSLKey v)
        {
            UUID uuid;
            if(UUID.TryParse(v.m_Value, out uuid))
            {
                return !uuid.Equals(UUID.Zero);
            }
            return v.m_Value.Length != 0;
        }

        public static AnArray operator+(LSLKey k, AnArray a)
        {
            AnArray b = new AnArray();
            b.Add(k);
            b.AddRange(a);
            return b;
        }
        #endregion Operators

        #region Helpers
        public ABoolean AsBoolean { get { return new ABoolean(m_Value != ""); } }
        public Integer AsInteger { get { return new Integer(Int32.Parse(m_Value)); } }
        public Quaternion AsQuaternion { get { return Quaternion.Parse(m_Value); } }
        public Real AsReal { get { return Real.Parse(m_Value); } }
        public AString AsString { get { return new AString(m_Value); } }
        public UUID AsUUID { get { return new UUID(m_Value); } }
        public Vector3 AsVector3 { get { return Vector3.Parse(m_Value); } }
        public uint AsUInt { get { return uint.Parse(m_Value); } }
        public int AsInt { get { return int.Parse(m_Value); } }
        public ulong AsULong { get { return ulong.Parse(m_Value); } }
        #endregion
    }
}
