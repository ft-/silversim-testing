// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types
{
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class BinaryData : IValue, IEquatable<BinaryData>
    {
        private byte[] m_Data;

        public BinaryData()
        {
            m_Data = new byte[0];
        }

        public BinaryData(byte[] data)
        {
            m_Data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, m_Data, 0, data.Length);
        }

        public BinaryData(byte[] data, int pos, int length)
        {
            m_Data = new byte[length];
            Buffer.BlockCopy(data, pos, m_Data, 0, length);
        }

        public BinaryData(int capacity)
        {
            m_Data = new byte[capacity];
        }

        #region Properties
        public byte this[ulong pos]
        {
            get
            {
                return m_Data[pos];
            }
            set
            {
                m_Data[pos] = value;
            }
        }

        public int Length
        {
            get
            {
                return m_Data.Length;
            }
        }

        public ValueType Type
        {
            get
            {
                return ValueType.BinaryData;
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

        public bool Equals(BinaryData a)
        {
            return m_Data.Equals(a.m_Data);
        }

        #region Operators
        public static implicit operator byte[](BinaryData a)
        {
            return a.m_Data;
        }
        #endregion Operators

        #region Helpers
        public ABoolean AsBoolean { get { return new ABoolean(m_Data.Length != 0); } }
        public Integer AsInteger { get { return new Integer(); } }
        public Quaternion AsQuaternion { get { return new Quaternion(); } }
        public Real AsReal { get { return new Real(); } }
        public AString AsString { get { return new AString(); } }
        public UUID AsUUID { get { return new UUID(); } }
        public Vector3 AsVector3 { get { return new Vector3(); } }
        public uint AsUInt { get { return 0; } }
        public int AsInt { get { return 0; } }
        public ulong AsULong { get { return 0; } }
        #endregion
    }
}
