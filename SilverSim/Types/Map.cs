/*

SilverSim is distributed under the terms of the
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
using System.Collections.Generic;

namespace SilverSim.Types
{
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
            : base()
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
