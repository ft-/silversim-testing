// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types
{
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public sealed class Map : Dictionary<string, IValue>, IValue
    {
        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Map;
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

        #region Constructors
        public Map()
        {
        }

        public Map(int capacity)
            : base(capacity)
        {
        }

        public Map(Map v)
            : base(v)
        {
        }

        public Map(Dictionary<string, IValue> ival)
        {
            foreach(KeyValuePair<string, IValue> kvp in ival)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        #endregion Constructors
        #region Add methods
        public void Add(string key, bool val)
        {
            base.Add(key, new ABoolean(val));
        }

        public void Add(string key, double val)
        {
            base.Add(key, new Real(val));
        }

        public void Add(string key, string val)
        {
            base.Add(key, new AString(val));
        }

        public void Add(string key, Uri val)
        {
            base.Add(key, new URI(val.ToString()));
        }

        public void Add(string key, Int32 val)
        {
            base.Add(key, new Integer(val));
        }
        #endregion Add methods

        public bool TryGetValue<T>(string key, out T val)
        {
            IValue iv;
            if(!base.TryGetValue(key, out iv))
            {
                val = default(T);
                return false;
            }
            if(!(iv is T))
            {
                val = default(T);
                return false;
            }
            val = (T)iv;
            return true;
        }

        #region Helpers
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
    }
}
