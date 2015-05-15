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
        Physics = 1, /* 0x00000001 */
        CreateSelected = 2, /* 0x00000002 */
        ObjectModify = 4, /* 0x00000004 */
        ObjectCopy = 8, /* 0x00000008 */
        ObjectAnyOwner = 16, /* 0x00000010 */
        ObjectYouOwner = 32, /* 0x00000020 */
        Scripted = 64, /* 0x00000040 */
        Touch = 128, /* 0x00000080 */
        ObjectMove = 256, /* 0x00000100 */
        Money = 512, /* 0x00000200 */
        Phantom = 1024, /* 0x00000400 */
        InventoryEmpty = 2048, /* 0x00000800 */
        JointHinge = 4096, /* 0x00001000 */
        JointP2P = 8192, /* 0x000002000 */
        JointLP2P = 16384, /* 0x00004000 */
        JointWheel = 32768, /* 0x00008000 */
        AllowInventoryDrop = 65536, /* 0x00010000 */
        ObjectTransfer = 131072, /* 0x00020000 */
        ObjectGroupOwned = 262144, /* 0x00040000 */
        ObjectYouOfficer = 524288, /* 0x00080000 */
        CameraDecoupled = 1048576, /* 0x00100000 */
        AnimSource = 2097152, /* 0x00200000 */
        CameraSource = 4194304, /* 0x00400000 */
        CastShadows = 8388608, /* 0x00800000 */
        DieAtEdge = 16777216, /* 0x01000000 */
        ReturnAtEdge = 33554432, /* 0x02000000 */
        Sandbox = 67108864, /* 0x04000000 */
        Flying = 134217728, /* 0x08000000 */
        ObjectOwnerModify = 268435456, /* 0x10000000 */
        TemporaryOnRez = 536870912, /* 0x20000000 */
        Temporary = 1073741824, /* 0x40000000 */
        ZlibCompressed = 2147483648, /* 0x80000000 */
    }
}
