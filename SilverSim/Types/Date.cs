// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Globalization;

namespace SilverSim.Types
{
    [Serializable]
    public sealed class Date : IComparable<Date>, IEquatable<Date>, IValue
    {
        private DateTime m_Value;

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Date;
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

        public Date()
        {
            m_Value = DateTime.UtcNow;
        }

        public Date(DateTime v)
        {
            m_Value = v;
        }

        public Date(Date v)
        {
            m_Value = v.m_Value;
        }

        public Date(string v)
        {
            DateTime.ParseExact(v, DateFormats, EnUsCulture, DateTimeStyles.None);
        }

        public Date(byte[] data, int pos)
        {
            FromBytes(data, pos);
        }

        public int CompareTo(Date v)
        {
            return m_Value.CompareTo(v.m_Value);
        }

        public bool Equals(Date v)
        {
            return m_Value.Equals(v.m_Value);
        }

        public override bool Equals(object o)
        {
            Date v = o as Date;
            if(null == v)
            {
                return false;
            }
            return Equals(v);
        }

        public override int GetHashCode()
        {
            return m_Value.GetHashCode();
        }

        public override string ToString()
        {
            string format;
            if (m_Value.Millisecond > 0)
                format = "yyyy-MM-ddTHH:mm:ss.ffZ";
            else
                format = "yyyy-MM-ddTHH:mm:ssZ";
            return m_Value.ToUniversalTime().ToString(format);
        }

        public string ToString(string format, IFormatProvider culture)
        {
            return m_Value.ToString(format, culture);
        }

        public static implicit operator DateTime(Date v)
        {
            return v.m_Value;
        }

        public static explicit operator string(Date v)
        {
            return v.ToString();
        }

        public static explicit operator ulong(Date v)
        {
            return v.DateTimeToUnixTime();
        }

        /// <summary>
        /// Gets a unix timestamp for the current time
        /// </summary>
        /// <returns>An unsigned integer representing a unix timestamp for now</returns>
        public static ulong GetUnixTime()
        {
            return (ulong)(DateTime.UtcNow - Epoch).TotalSeconds;
        }

        public static Date UnixTimeToDateTime(ulong timestamp)
        {
            DateTime dateTime = Epoch;

            // Add the number of seconds in our UNIX timestamp
            dateTime = dateTime.AddSeconds(timestamp);

            return new Date(dateTime);
        }

        public ulong DateTimeToUnixTime()
        {
            TimeSpan ts = (m_Value - Epoch);
            return (ulong)ts.TotalSeconds;
        }

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos)
        {
            double val;
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                byte[] conversionBuffer = new byte[8];

                Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 8);

                System.Array.Reverse(conversionBuffer, 0, 8);

                val = BitConverter.ToDouble(conversionBuffer, 0);
            }
            else
            {
                // Little endian architecture
                val = BitConverter.ToDouble(byteArray, pos);
            }

            m_Value = Epoch + TimeSpan.FromTicks((long)(val * 10000000f));
        }

        public void ToBytes(byte[] dest, int pos)
        {
            double val;
            TimeSpan ts = (m_Value - Epoch);

            val = ts.Ticks / 10000000f;

            Buffer.BlockCopy(BitConverter.GetBytes(val), 0, dest, pos + 0, 8);

            if (!BitConverter.IsLittleEndian)
            {
                System.Array.Reverse(dest, pos + 0, 8);
            }
        }
        #endregion Serialization

        #region Helpers
        public ABoolean AsBoolean { get { return new ABoolean(true); } }
        public Integer AsInteger { get { return new Integer((int)DateTimeToUnixTime()); } }
        public Quaternion AsQuaternion { get { return new Quaternion(); } }
        public Real AsReal { get { return new Real(DateTimeToUnixTime()); } }
        public AString AsString { get { return new AString(ToString()); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(); } }
        public uint AsUInt { get { return (uint)DateTimeToUnixTime(); } }
        public int AsInt { get { return (int)DateTimeToUnixTime(); } }
        public ulong AsULong { get { return DateTimeToUnixTime(); } }
        #endregion

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private readonly static CultureInfo EnUsCulture = new CultureInfo("en-us");
        private static readonly string[] DateFormats = new string[2]
        {
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.ffZ"
        };
    }
}
