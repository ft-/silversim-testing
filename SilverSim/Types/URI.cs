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

namespace SilverSim.Types
{
    public sealed class URI : IEquatable<URI>, IEquatable<string>, IValue
    {
        private readonly Uri m_Value;

        #region Properties
        public ValueType Type => ValueType.URI;

        public LSLValueType LSL_Type => LSLValueType.String;
        #endregion

        #region Constructors
        public URI(string val)
        {
            m_Value = new Uri(val);
        }

        public URI(AString val)
        {
            m_Value = new Uri(val.ToString());
        }
        #endregion

        public override string ToString() => m_Value.ToString();

        public bool Equals(URI v) => m_Value.Equals(v.m_Value);

        public bool Equals(string v) => ToString() == v;

        #region Operators
        public static implicit operator string(URI v) => v.ToString();

        public static explicit operator Uri(URI v)
        {
            if(v.m_Value == null)
            {
                return null;
            }
            return new Uri(v.m_Value.ToString());
        }

        public override bool Equals(object o)
        {
            URI u = o as URI;
            if((object)u == null)
            {
                return false;
            }
            return m_Value == u.m_Value;
        }

        public override int GetHashCode() => m_Value.GetHashCode();

        public static bool operator ==(URI l, URI r)
        {
            /* get rid of type specifics */
            object lo = l;
            object ro = r;
            if (lo == null && ro == null)
            {
                return true;
            }
            else if (lo == null || ro == null)
            {
                return false;
            }
            return l.m_Value == r.m_Value;
        }

        public static bool operator !=(URI l, URI r)
        {
            /* get rid of type specifics */
            object lo = l;
            object ro = r;
            if (lo == null && ro == null)
            {
                return false;
            }
            else if (lo == null || ro == null)
            {
                return true;
            }
            return l.m_Value != r.m_Value;
        }

        public static bool IsSameService(string a, string b)
        {
            Uri uri_a;
            Uri uri_b;
            if(!Uri.TryCreate(a, UriKind.Absolute, out uri_a) ||
                !Uri.TryCreate(b, UriKind.Absolute, out uri_b))
            {
                return false;
            }
            return Uri.Compare(uri_a, uri_b, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        #endregion Operators

        #region LSL Helpers
        public ABoolean AsBoolean => new ABoolean(true);
        public Integer AsInteger => new Integer(1);
        public Quaternion AsQuaternion => new Quaternion(0, 0, 0, 1);
        public Real AsReal => new Real(1);
        public AString AsString => new AString(ToString());
        public UUID AsUUID => new UUID();
        public Vector3 AsVector3 => new Vector3(1);
        public uint AsUInt => 1;
        public int AsInt => 1;
        public ulong AsULong => 1;
        public long AsLong => 1;
        #endregion
    }
}
