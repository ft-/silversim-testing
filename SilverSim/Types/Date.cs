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
using System.Globalization;

namespace SilverSim.Types
{
    [Serializable]
    public sealed class Date : IComparable<Date>, IEquatable<Date>, IValue
    {
        private DateTime m_Value;

        #region Properties
        public static Date Now => new Date();

        public ValueType Type => ValueType.Date;

        public LSLValueType LSL_Type => LSLValueType.Invalid;
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
            DateTime.ParseExact(v, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public Date(byte[] data, int pos)
        {
            FromBytes(data, pos);
        }

        public int CompareTo(Date v) => m_Value.CompareTo(v.m_Value);

        public bool Equals(Date v) => m_Value.Equals(v.m_Value);

        public override bool Equals(object o)
        {
            var v = o as Date;
            return v?.Equals(v) == true;
        }

        public override int GetHashCode() => m_Value.GetHashCode();

        public override string ToString()
        {
            var format = (m_Value.Millisecond > 0) ?
                "yyyy-MM-ddTHH:mm:ss.ffZ" :
                "yyyy-MM-ddTHH:mm:ssZ";
            return m_Value.ToUniversalTime().ToString(format);
        }

        public string Iso8601 => m_Value.ToString("yyyyMMdd'T'HH':'mm':'ss", DateTimeFormatInfo.InvariantInfo);

        public string ToString(string format, IFormatProvider culture) => m_Value.ToString(format, culture);

        public static implicit operator DateTime(Date v) => v.m_Value;

        public static explicit operator string(Date v) => v.ToString();

        public static explicit operator ulong(Date v) => v.DateTimeToUnixTime();

        /// <summary>
        /// Gets a unix timestamp for the current time
        /// </summary>
        /// <returns>An unsigned integer representing a unix timestamp for now</returns>
        public static ulong GetUnixTime() => (ulong)(DateTime.UtcNow - Epoch).TotalSeconds;

        public static Date UnixTimeToDateTime(ulong timestamp) => new Date(Epoch.AddSeconds(timestamp));

        public ulong DateTimeToUnixTime() => (ulong)((m_Value - Epoch).TotalSeconds);

        public Date Add(TimeSpan value) => new Date(m_Value.Add(value));
        public Date AddDays(double value) => new Date(m_Value.AddDays(value));
        public Date AddHours(double value) => new Date(m_Value.AddHours(value));
        public Date AddMilliseconds(double value) => new Date(m_Value.AddMilliseconds(value));
        public Date AddMinutes(double value) => new Date(m_Value.AddMinutes(value));
        public Date AddMonths(int months) => new Date(m_Value.AddMonths(months));
        public Date AddSeconds(double value) => new Date(m_Value.AddSeconds(value));
        public Date AddTicks(long value) => new Date(m_Value.AddTicks(value));
        public Date AddYears(int value) => new Date(m_Value.AddYears(value));

        #region Serialization
        public void FromBytes(byte[] byteArray, int pos)
        {
            double val;
            if (!BitConverter.IsLittleEndian)
            {
                // Big endian architecture
                var conversionBuffer = new byte[8];

                Buffer.BlockCopy(byteArray, pos, conversionBuffer, 0, 8);

                Array.Reverse(conversionBuffer, 0, 8);

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
            var ts = m_Value - Epoch;

            val = ts.Ticks / 10000000f;

            Buffer.BlockCopy(BitConverter.GetBytes(val), 0, dest, pos + 0, 8);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(dest, pos + 0, 8);
            }
        }
        #endregion Serialization

        #region Helpers
        public ABoolean AsBoolean => new ABoolean(true);
        public Integer AsInteger => new Integer((int)DateTimeToUnixTime());
        public Quaternion AsQuaternion => new Quaternion();
        public Real AsReal => new Real(DateTimeToUnixTime());
        public AString AsString => new AString(ToString());
        public UUID AsUUID => new UUID();
        public Vector3 AsVector3 => new Vector3();
        public uint AsUInt => (uint)DateTimeToUnixTime();
        public int AsInt => (int)DateTimeToUnixTime();
        public ulong AsULong => DateTimeToUnixTime();
        public long AsLong => (long)DateTimeToUnixTime();
        #endregion

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static readonly string[] DateFormats = new string[2]
        {
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.ffZ"
        };
    }
}
