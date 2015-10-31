// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Asset
{
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum AssetType : sbyte
    {
        Material = -2,
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
        RootFolder = 8,
        LSLText = 10,
        LSLBytecode = 11,
        TextureTGA = 12,
        Bodypart = 13,
        TrashFolder = 14,
        SnapshotFolder = 15,
        LostAndFoundFolder = 16,
        SoundWAV = 17,
        ImageTGA = 18,
        ImageJPEG = 19,
        Animation = 20,
        Gesture = 21,
        Simstate = 22,
        FavoriteFolder = 23,
        Link = 24,
        LinkFolder = 25,
        EnsembleStart = 26,
        EnsembleEnd = 45,
        CurrentOutfitFolder = 46,
        OutfitFolder = 47,
        MyOutfitsFolder = 48,
        Mesh = 49,
        Inbox = 50,
        Outbox = 51,
        BasicRoot = 52,
        MarketplaceListings = 53,
        MarketplaceStock = 54
    }
}
