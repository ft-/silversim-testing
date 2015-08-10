// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Globalization;

namespace SilverSim.Types
{
    public sealed class AString : IComparable<AString>, IEquatable<AString>, IComparable<string>, IEquatable<string>, IValue
    {
        private string m_Value;

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.String;
            }
        }

        public LSLValueType LSL_Type
        {
            get
            {
                return LSLValueType.String;
            }
        }
        #endregion Properties

        public AString()
        {
            m_Value = string.Empty;
        }

        public AString(string val)
        {
            m_Value = (string)val.Clone();
        }

        public int CompareTo(AString v)
        {
            return m_Value.CompareTo(v.m_Value);
        }

        public bool Equals(AString v)
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

        #region Operators
        public static AString operator+(AString a, AString b)
        {
            return new AString(a.m_Value + b.m_Value);
        }

        public static implicit operator bool(AString v)
        {
            bool result;
            if(Boolean.TryParse(v.ToString(), out result))
            {
                return result;
            }
            Int32 res;
            if(Int32.TryParse(v.ToString(), out res))
            {
                return res != 0;
            }
            return false;
        }

        public static explicit operator Integer(AString v)
        {
            return Integer.Parse(v.m_Value);
        }

        public static explicit operator Real(AString v)
        {
            return Real.Parse(v.m_Value);
        }

        public static explicit operator double(AString v)
        {
            return Double.Parse(v.m_Value.Trim(), EnUsCulture);
        }

        public static explicit operator Vector3(AString v)
        {
            return new Vector3((double)v);
        }
        #endregion Operators

        public static AString Format(string format, object arg0)
        {
            return new AString(string.Format(format, arg0));
        }

        public static AString Format(string format, params object[] args)
        {
            return new AString(string.Format(format, args));
        }

        public static AString Format(string format, object arg0, object arg1)
        {
            return new AString(string.Format(format, arg0, arg1));
        }

        public static AString Format(string format, object arg0, object arg1, object arg2)
        {
            return new AString(string.Format(format, arg0, arg1, arg2));
        }

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

        private readonly static CultureInfo EnUsCulture = new CultureInfo("en-us");
    }
}
