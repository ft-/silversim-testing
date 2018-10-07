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
using System.Collections.Generic;

namespace SilverSim.Types
{
    public sealed class Map : Dictionary<string, IValue>, IValue
    {
        #region Properties
        public ValueType Type => ValueType.Map;

        public LSLValueType LSL_Type => LSLValueType.Invalid;
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
            foreach(var kvp in ival)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        #endregion Constructors
        #region Add methods
        public void Add(string key, bool val) => base.Add(key, new ABoolean(val));

        public void Add(string key, double val) => base.Add(key, new Real(val));

        public void Add(string key, string val) => base.Add(key, new AString(val));

        public void Add(string key, Uri val) => base.Add(key, new URI(val.ToString()));

        public void Add(string key, uint val) => base.Add(key, new Integer((int)val));

        public void Add(string key, int val) => base.Add(key, new Integer(val));

        public void Add(string key, long val) => base.Add(key, new LongInteger(val));

        public void Add(string key, ulong val) => base.Add(key, new LongInteger((long)val));
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
                return iv.TryConvertTo(out val);
            }
            val = (T)iv;
            return true;
        }

        #region Helpers
        public ABoolean AsBoolean => new ABoolean();
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
