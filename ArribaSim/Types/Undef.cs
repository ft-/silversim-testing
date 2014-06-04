/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using System;

namespace ArribaSim.Types
{
    public class Undef : IComparable<Undef>, IEquatable<Undef>, IValue
    {
        public Undef()
        {

        }

        public int CompareTo(Undef v)
        {
            return 0;
        }

        public bool Equals(Undef v)
        {
            return true;
        }

        #region Properties
        public ValueType Type
        {
            get
            {
                return ValueType.Undef;
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

        #region Operators
        public static implicit operator bool(Undef v)
        {
            return false;
        }
        #endregion Operators

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

        public override string ToString()
        {
            return "undef";
        }
    }
}
