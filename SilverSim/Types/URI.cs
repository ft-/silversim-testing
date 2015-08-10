// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types
{
    public sealed class URI : IEquatable<URI>, IEquatable<string>, IValue
    {
        private Uri m_Value;

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.URI;
            }
        }

        public LSLValueType LSL_Type
        {
            get
            {
                return LSLValueType.String;
            }
        }
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

        public override string ToString()
        {
            return m_Value.ToString();
       }

        public bool Equals(URI v)
        {
            return m_Value.Equals(v.m_Value);
        }

        public bool Equals(string v)
        {
            return ToString() == v;
        }

        #region Operators
        public static implicit operator string(URI v)
        {
            return v.ToString();
        }

        public static implicit operator Uri(URI v)
        {
            if(v.m_Value == null)
            {
                throw new ArgumentNullException();
            }
            return new Uri(v.m_Value.ToString());
        }
        #endregion Operators

        #region LSL Helpers
        public ABoolean AsBoolean { get { return new ABoolean(true); } }
        public Integer AsInteger { get { return new Integer(1); } }
        public Quaternion AsQuaternion { get { return new Quaternion(0, 0, 0, 1); } }
        public Real AsReal { get { return new Real(1); } }
        public AString AsString { get { return new AString(ToString()); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(1); } }
        public uint AsUInt { get { return (uint)1; } }
        public int AsInt { get { return 1; } }
        public ulong AsULong { get { return (ulong)1; } }
        #endregion

    }
}
