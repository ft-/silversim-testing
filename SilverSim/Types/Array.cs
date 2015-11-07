// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types
{
    [SuppressMessage("Gendarme.Rules.Design", "EnsureSymmetryForOverloadedOperatorsRule")]
    public sealed class AnArray : List<IValue>, IValue
    {
        #region Constructors
        public AnArray()
        {

        }

        public AnArray(AnArray v)
            : base(v)
        {

        }

        public AnArray(List<IValue> ival)
        {
            foreach(IValue v in ival)
            {
                Add(v);
            }
        }
        #endregion Constructors

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Array;
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

        public override string ToString()
        {
            string s = string.Empty;
            foreach(IValue iv in this)
            {
                s += iv.ToString();
            }
            return s;
        }

        #region Add methods
        public void Add(bool val)
        {
            base.Add(new ABoolean(val));
        }

        public void Add(double val)
        {
            base.Add(new Real(val));
        }

        public void Add(string val)
        {
            base.Add(new AString(val));
        }

        public void Add(Uri val)
        {
            base.Add(new URI(val.ToString()));
        }

        public void Add(Int32 val)
        {
            base.Add(new Integer(val));
        }

        public static AnArray operator +(AnArray a, AnArray b)
        {
            AnArray n = new AnArray(a);
            n.AddRange(b);
            return n;
        }
        public static AnArray operator +(AnArray a, int b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        public static AnArray operator +(AnArray a, double b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        public static AnArray operator +(AnArray a, string b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        public static AnArray operator +(AnArray a, Vector3 b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        public static AnArray operator +(AnArray a, Quaternion b)
        {
            AnArray n = new AnArray(a);
            n.Add(b);
            return n;
        }
        #endregion Add methods

        #region Helper
        public ABoolean AsBoolean { get { return new ABoolean(); } }
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

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public Vector4 ElementsToVector4
        {
            get
            {
                return new Vector4(this[0].AsReal, this[1].AsReal, this[2].AsReal, this[3].AsReal);
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        public Vector3 ElementsToVector3
        {
            get
            {
                return new Vector3(this[0].AsReal, this[1].AsReal, this[2].AsReal);
            }
        }

        #region Stateful Enumerator
        public class MarkEnumerator : IEnumerator<IValue>, IEnumerator, IDisposable
        {
            private int m_CurrentIndex;
            private int m_MarkIndex;
            private AnArray m_Array;

            public MarkEnumerator(AnArray array)
            {
                m_Array = array;
                m_CurrentIndex = -1;
            }
            public IValue Current 
            { 
                get
                {
                    return m_Array[m_CurrentIndex];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Dispose()
            {
                m_Array = null;
            }

            public void Reset()
            {
                m_CurrentIndex = -1;
            }

            public bool MoveNext()
            {
                return ++m_CurrentIndex < m_Array.Count;
            }

            public void MarkPosition()
            {
                m_MarkIndex = m_CurrentIndex;
            }

            public void GoToMarkPosition()
            {
                m_CurrentIndex = m_MarkIndex;
            }
        }

        public MarkEnumerator GetMarkEnumerator()
        {
            return new MarkEnumerator(this);
        }
        #endregion
    }
}
