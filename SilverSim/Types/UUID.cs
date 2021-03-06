﻿// SilverSim is distributed under the terms of the
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

namespace SilverSim.Types
{
    [Serializable]
    public struct UUID : IComparable<UUID>, IEquatable<UUID>, IValue
    {
        private Guid m_Guid;

        #region Constructors
        public UUID(string s)
        {
            m_Guid = string.IsNullOrEmpty(s) ? new Guid() : new Guid(s);
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
            var b = (short)((source[pos + 4] << 8) | source[pos + 5]);
            var c = (short)((source[pos + 6] << 8) | source[pos + 7]);

            m_Guid = new Guid(a, b, c, source[pos + 8], source[pos + 9], source[pos + 10], source[pos + 11],
                source[pos + 12], source[pos + 13], source[pos + 14], source[pos + 15]);
        }
        #endregion

        public void FromBytes(byte[] source, int pos)
        {
            int a = (source[pos + 0] << 24) | (source[pos + 1] << 16) | (source[pos + 2] << 8) | source[pos + 3];
            var b = (short)((source[pos + 4] << 8) | source[pos + 5]);
            var c = (short)((source[pos + 6] << 8) | source[pos + 7]);

            m_Guid = new Guid(a, b, c, source[pos + 8], source[pos + 9], source[pos + 10], source[pos + 11],
                source[pos + 12], source[pos + 13], source[pos + 14], source[pos + 15]);
        }

        public void ToBytes(byte[] dest, int pos)
        {
            var bytes = m_Guid.ToByteArray();
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
            var n = new byte[16];
            ToBytes(n, 0);
            return n;
        }

        public static UUID Parse(string val) => new UUID(val);

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
            if (!(o is UUID))
            {
                return false;
            }

            return m_Guid == ((UUID)o).m_Guid;
        }

        public bool Equals(UUID uuid) => m_Guid == uuid.m_Guid;

        public override string ToString() => m_Guid.ToString();

        public override int GetHashCode() => m_Guid.GetHashCode();

        public int CompareTo(UUID id) => m_Guid.CompareTo(id.m_Guid);

        #region Operators
        public static bool operator ==(UUID l, UUID r) => l.m_Guid == r.m_Guid;

        public static bool operator !=(UUID l, UUID r) => l.m_Guid != r.m_Guid;

        public static UUID operator^(UUID a, UUID b)
        {
            var ab = new byte[16];
            var bb = new byte[16];
            a.ToBytes(ab, 0);
            b.ToBytes(bb, 0);

            for(int i = 0; i < 16; ++i)
            {
                ab[i] ^= bb[i];
            }

            return new UUID(ab, 0);
        }

        public static implicit operator UUID(string val) => new UUID(val);

        public static implicit operator UUID(Guid val) => new UUID(val);

        public static explicit operator string(UUID val) => val.ToString();

        public static explicit operator Guid(UUID val) => val.m_Guid;

        public static explicit operator UUID(AString val) => new UUID(val.ToString());
        #endregion

        #region Helpers
        public ABoolean AsBoolean => new ABoolean(!Equals(Zero));
        public Integer AsInteger => new Integer(Equals(Zero) ? 0 : 1);
        public Quaternion AsQuaternion => new Quaternion(0, 0, 0, Equals(Zero) ? 0 : 1);
        public Real AsReal => new Real(Equals(Zero) ? 0 : 1);
        public AString AsString => new AString(ToString());
        public UUID AsUUID => new UUID(m_Guid);
        public Vector3 AsVector3 => new Vector3(Equals(Zero) ? 0 : 1);
        public uint AsUInt => Equals(Zero) ? 0 : (uint)1;
        public int AsInt => Equals(Zero) ? 0 : 1;
        public ulong AsULong => Equals(Zero) ? 0 : (ulong)1;
        public long AsLong => Equals(Zero) ? 0 : 1;
        #endregion

        #region Properties
        public ValueType Type => ValueType.UUID;

        public LSLValueType LSL_Type => LSLValueType.Invalid;

        public static UUID Random => new UUID(Guid.NewGuid());

        public static UUID RandomFixedFirst(UInt32 val)
        {
            var ub = Guid.NewGuid().ToByteArray();
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
                var bytes = m_Guid.ToByteArray();

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

    public static class TextureConstant
    {
        public static readonly UUID DefaultTerrainTexture1 = new UUID("0bc58228-74a0-7e83-89bc-5c23464bcec5");
        public static readonly UUID DefaultTerrainTexture2 = new UUID("63338ede-0037-c4fd-855b-015d77112fc8");
        public static readonly UUID DefaultTerrainTexture3 = new UUID("303cd381-8560-7579-23f1-f0a880799740");
        public static readonly UUID DefaultTerrainTexture4 = new UUID("53a2f406-4895-1d13-d541-d2e3b86bc19c");
        public static readonly UUID Blank = new UUID("5748decc-f629-461c-9a36-a35a221fe21f");
        public static readonly UUID Default = new UUID("89556747-24cb-43ed-920b-47caed15465f");
    }
}