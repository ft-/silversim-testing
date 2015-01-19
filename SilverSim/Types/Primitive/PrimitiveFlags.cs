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
