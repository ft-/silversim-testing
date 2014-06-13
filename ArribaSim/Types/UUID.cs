/*

ArribaSim is distributed under the terms of the
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

namespace ArribaSim.Types
{
    [Serializable]
    public struct UUID : IComparable<UUID>, IEquatable<UUID>, IValue
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
            byte[] g = new byte[16];
            Buffer.BlockCopy(source, pos, g, 0, 16);
            m_Guid = new Guid(g);
        }
        #endregion

        public void FromBytes(byte[] source, int pos)
        {
            byte[] o = new byte[16];
            Buffer.BlockCopy(source, pos, o, 0, 16);
            m_Guid = new Guid(o);
        }

        public void ToBytes(byte[] dest, int pos)
        {
            byte[] o = m_Guid.ToByteArray();
            Buffer.BlockCopy(o, 0, dest, pos, 16);
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

        public static implicit operator UUID(string val)
        {
            return new UUID(val);
        }

        public static implicit operator string(UUID val)
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


        public static readonly UUID Zero = new UUID("00000000-0000-0000-0000-000000000000");
        #endregion
    }
}