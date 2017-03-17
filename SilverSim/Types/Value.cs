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

namespace SilverSim.Types
{
    /**************************************************************************/
    public enum ValueType
    {
        Unknown,
        Undef,
        Boolean,
        Integer,
        Real,
        String,
        UUID,
        Date,
        URI,
        Vector,
        Rotation,
        Map,
        Array,
        BinaryData
    }

    /**************************************************************************/
    public enum LSLValueType
    {
        Invalid = 0,
        Integer = 1,
        Float = 2,
        String = 3,
        Key = 4,
        Vector = 5,
        Rotation = 6
    }

    /**************************************************************************/
    public interface IValue
    {
        ValueType Type { get; }
        LSLValueType LSL_Type { get; }

        ABoolean AsBoolean { get; }
        Integer AsInteger { get; }
        int AsInt { get; }
        uint AsUInt { get; }
        ulong AsULong { get; }
        Quaternion AsQuaternion { get; }
        Real AsReal { get; }
        AString AsString { get; }
        UUID AsUUID { get; }
        Vector3 AsVector3 { get; }
    }
}
