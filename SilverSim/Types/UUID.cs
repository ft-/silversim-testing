// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types
{
    [Serializable]
    public struct UUID : IEquatable<UUID>, IValue
    {
        private Guid m_Guid;

        #region Constructors
        public UUID(string s)
        {
            if(string.IsNullOrEmpty(s))
            {
                m_Guid = new Guid();
            }
            else
            {
                m_Guid = new Guid(s);
            }
        }
        public UUID(Guid val)
        {
            m_Guid = val;
        }

        public UUID(UUID v)
        {
            m_Guid = v.m_Guid;
        }

        public UUID(byte[] source, int pos)
        {
            int a = (source[pos + 0] << 24) | (source[pos + 1] << 16) | (source[pos + 2] << 8) | source[pos + 3];
            short b = (short)((source[pos + 4] << 8) | source[pos + 5]);
            short c = (short)((source[pos + 6] << 8) | source[pos + 7]);

            m_Guid = new Guid(a, b, c, source[pos + 8], source[pos + 9], source[pos + 10], source[pos + 11],
                source[pos + 12], source[pos + 13], source[pos + 14], source[pos + 15]);
        }
        #endregion

        public void FromBytes(byte[] source, int pos)
        {
            int a = (source[pos + 0] << 24) | (source[pos + 1] << 16) | (source[pos + 2] << 8) | source[pos + 3];
            short b = (short)((source[pos + 4] << 8) | source[pos + 5]);
            short c = (short)((source[pos + 6] << 8) | source[pos + 7]);

            m_Guid = new Guid(a, b, c, source[pos + 8], source[pos + 9], source[pos + 10], source[pos + 11],
                source[pos + 12], source[pos + 13], source[pos + 14], source[pos + 15]);
        }

        public void ToBytes(byte[] dest, int pos)
        {
            byte[] bytes = m_Guid.ToByteArray();
            dest[pos + 0] = bytes[3];
            dest[pos + 1] = bytes[2];
            dest[pos + 2] = bytes[1];
            dest[pos + 3] = bytes[0];
            dest[pos + 4] = bytes[5];
            dest[pos + 5] = bytes[4];
            dest[pos + 6] = bytes[7];
            dest[pos + 7] = bytes[6];
            Buffer.BlockCopy(bytes, 8, dest, pos + 8, 8);
        }

        public byte[] GetBytes()
        {
            byte[] n = new byte[16];
            ToBytes(n, 0);
            return n;
        }

        public static UUID Parse(string val)
        {
            return new UUID(val);
        }

        public static bool TryParse(string val, out UUID result)
        {
            Guid o;
            result = null;

            if(Guid.TryParse(val, out o))
            {
                result = new UUID(o);
                return true;
            }
            return false;
        }

        public override bool Equals(object o)
        {
            if (!(o is UUID)) return false;

            return m_Guid == ((UUID)o).m_Guid;
        }

        public bool Equals(UUID uuid)
        {
            return m_Guid == uuid.m_Guid;
        }

        public override string ToString()
        {
            return m_Guid.ToString();
        }

        public override int GetHashCode()
        {
            return m_Guid.GetHashCode();
        }

        public int CompareTo(UUID id)
        {
            return m_Guid.CompareTo(id.m_Guid);
        }

        #region Operators
        public static bool operator ==(UUID l, UUID r)
        {
            return l.m_Guid == r.m_Guid;
        }

        public static bool operator !=(UUID l, UUID r)
        {
            return l.m_Guid != r.m_Guid;
        }

        public static UUID operator^(UUID a, UUID b)
        {
            byte[] ab = new byte[16];
            byte[] bb = new byte[16];
            a.ToBytes(ab, 0);
            b.ToBytes(bb, 0);

            for(int i = 0; i < 16; ++i)
            {
                ab[i] ^= bb[i];
            }

            return new UUID(ab, 0);
        }

        public static implicit operator UUID(string val)
        {
            return new UUID(val);
        }

        public static implicit operator UUID(Guid val)
        {
            return new UUID(val);
        }

        public static explicit operator string(UUID val)
        {
            return val.ToString();
        }
        #endregion

        #region Helpers
        public ABoolean AsBoolean { get { return new ABoolean(!this.Equals(Zero)); } }
        public Integer AsInteger { get { return new Integer(this.Equals(Zero) ? 0 : 1); } }
        public Quaternion AsQuaternion { get { return new Quaternion(0, 0, 0, this.Equals(Zero) ? 0 : 1); } }
        public Real AsReal { get { return new Real(this.Equals(Zero) ? 0 : 1); } }
        public AString AsString { get { return new AString(ToString()); } }
        public UUID AsUUID { get { return new UUID(m_Guid); } }
        public Vector3 AsVector3 { get { return new Vector3(this.Equals(Zero) ? 0 : 1); } }
        public uint AsUInt { get { return this.Equals(Zero) ? 0 : (uint) 1; } }
        public int AsInt { get { return this.Equals(Zero) ? 0 : 1; } }
        public ulong AsULong { get { return this.Equals(Zero) ? 0 : (ulong)1; } }
        #endregion

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.UUID;
            }
        }

        public LSLValueType LSL_Type
        {
            get
            {
                return LSLValueType.Invalid;
            }
        }

        public static UUID Random
        {
            get
            {
                return new UUID(Guid.NewGuid());
            }
        }

        public static UUID RandomFixedFirst(UInt32 val)
        {
            byte[] ub = Guid.NewGuid().ToByteArray();
            ub[0] = (byte)((val >> 24) & 0xFF);
            ub[1] = (byte)((val >> 16) & 0xFF);
            ub[2] = (byte)((val >> 8) & 0xFF);
            ub[3] = (byte)((val >> 0) & 0xFF);

            return new UUID(ub, 0);
        }

        public uint LLChecksum
        {
            get
            {
                uint retval = 0;
                byte[] bytes = m_Guid.ToByteArray();

                retval += (uint)((bytes[3] << 24) + (bytes[2] << 16) + (bytes[1] << 8) + bytes[0]);
                retval += (uint)((bytes[7] << 24) + (bytes[6] << 16) + (bytes[5] << 8) + bytes[4]);
                retval += (uint)((bytes[11] << 24) + (bytes[10] << 16) + (bytes[9] << 8) + bytes[8]);
                retval += (uint)((bytes[15] << 24) + (bytes[14] << 16) + (bytes[13] << 8) + bytes[12]);

                return retval;
            }
        }

        public static readonly UUID Zero = new UUID("00000000-0000-0000-0000-000000000000");
        #endregion
    }
}