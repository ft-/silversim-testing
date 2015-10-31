// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Inventory
{
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum InventoryType : sbyte
    {
        Unknown = -1,
        Texture = 0,
        Sound = 1,
        CallingCard = 2,
        Landmark = 3,
        //[Obsolete]
        //Script = 4,
        Clothing = 5,
        Object = 6,
        Notecard = 7,
        Folder = 8,
        RootFolder = 9,
        LSLText = 10,
        LSLBytecode = 11,
        TextureTGA = 12,
        Bodypart = 13,
        TrashFolder = 14,
        SnapshotFolder = 15,
        Snapshot = 15,
        LostAndFoundFolder = 16,
        Attachable = 17,
        Wearable = 18,
        Animation = 20,
        Gesture = 21,
        Simstate = 22,
        FavoriteFolder = 23,
        CurrentOutfitFolder = 46,
        OutfitFolder = 47,
        MyOutfitsFolder = 48,
        Mesh = 49,
        Inbox = 50,
        Outbox = 51,
        BasicRoot = 51
    }
}
