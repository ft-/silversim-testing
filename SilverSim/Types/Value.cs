// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
