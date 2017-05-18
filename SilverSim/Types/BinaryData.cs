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

namespace SilverSim.Types
{
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class BinaryData : IValue, IEquatable<BinaryData>
    {
        readonly byte[] m_Data;

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

        public int Length => m_Data.Length;

        public ValueType Type => ValueType.BinaryData;

        public LSLValueType LSL_Type => LSLValueType.Invalid;

        #endregion Properties

        public bool Equals(BinaryData a) => m_Data.Equals(a.m_Data);

        #region Operators
        public static implicit operator byte[] (BinaryData a) => a.m_Data;
        #endregion Operators

        #region Helpers
        public ABoolean AsBoolean => new ABoolean(m_Data.Length != 0);
        public Integer AsInteger => new Integer();
        public Quaternion AsQuaternion => new Quaternion();
        public Real AsReal => new Real();
        public AString AsString => new AString();
        public UUID AsUUID => new UUID();
        public Vector3 AsVector3 => new Vector3();
        public uint AsUInt => 0;
        public int AsInt => 0;
        public ulong AsULong => 0;
        public long AsLong => 0;
        #endregion
    }
}
