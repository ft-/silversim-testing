using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Primitive
{
    [Flags]
    public enum PrimitiveFlags : uint
    {
        None = 0,
        Physics = 1,
        CreateSelected = 2,
        ObjectModify = 4,
        ObjectCopy = 8,
        ObjectAnyOwner = 16,
        ObjectYouOwner = 32,
        Scripted = 64,
        Touch = 128,
        ObjectMove = 256,
        Money = 512,
        Phantom = 1024,
        InventoryEmpty = 2048,
        JointHinge = 4096,
        JointP2P = 8192,
        JointLP2P = 16384,
        JointWheel = 32768,
        AllowInventoryDrop = 65536,
        ObjectTransfer = 131072,
        ObjectGroupOwned = 262144,
        ObjectYouOfficer = 524288,
        CameraDecoupled = 1048576,
        AnimSource = 2097152,
        CameraSource = 4194304,
        CastShadows = 8388608,
        DieAtEdge = 16777216,
        ReturnAtEdge = 33554432,
        Sandbox = 67108864,
        Flying = 134217728,
        ObjectOwnerModify = 268435456,
        TemporaryOnRez = 536870912,
        Temporary = 1073741824,
        ZlibCompressed = 2147483648,
    }
}
