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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SilverSim.Types
{
    [SuppressMessage("Gendarme.Rules.Design", "EnsureSymmetryForOverloadedOperatorsRule")]
    public sealed class AString : IEquatable<AString>, IEquatable<string>, IValue
    {
        readonly string m_Value;

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
            m_Value = val;
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
            return m_Value;
        }

        public override bool Equals(object obj)
        {
            AString a;
            string s;
            s = obj as string;
            if(null != s)
            {
                return m_Value == s;
            }

            a = obj as AString;
            if(null != a)
            {
                return m_Value == a.m_Value;
            }

            return false;
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

        public override int GetHashCode()
        {
            return m_Value.GetHashCode();
        }

        #region Operators
        public static AString operator+(AString a, AString b)
        {
            return new AString(a.m_Value + b.m_Value);
        }

        public static implicit operator bool(AString v)
        {
            bool result;
            if(bool.TryParse(v.ToString(), out result))
            {
                return result;
            }
            int res;
            if(int.TryParse(v.ToString(), out res))
            {
                return res != 0;
            }
            return false;
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static explicit operator Integer(AString v)
        {
            return Integer.Parse(v.m_Value);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static explicit operator Real(AString v)
        {
            return Real.Parse(v.m_Value);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public static explicit operator double(AString v)
        {
            return double.Parse(v.m_Value.Trim(), CultureInfo.InvariantCulture);
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
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
        public ABoolean AsBoolean { get { return new ABoolean(!string.IsNullOrEmpty(m_Value)); } }
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Integer AsInteger { get { return new Integer(Int32.Parse(m_Value)); } }
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Quaternion AsQuaternion { get { return Quaternion.Parse(m_Value); } }
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Real AsReal { get { return Real.Parse(m_Value); } }
        public AString AsString { get { return new AString(m_Value); } }
        public UUID AsUUID { get { return new UUID(m_Value); } }
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public Vector3 AsVector3 { get { return Vector3.Parse(m_Value); } }
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public uint AsUInt { get { return uint.Parse(m_Value); } }
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public int AsInt { get { return int.Parse(m_Value); } }
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public ulong AsULong { get { return ulong.Parse(m_Value); } }
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        public long AsLong { get { return long.Parse(m_Value); } }
        #endregion
    }
}
