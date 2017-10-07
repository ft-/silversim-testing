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

namespace SilverSim.Types.Asset
{
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
        MarketplaceStock = 54,
        Settings = 55
    }

    public static class AssetTypeExtensionMethods
    {
        public static string AssetTypeToString(this AssetType v)
        {
            switch (v)
            {
                case AssetType.Texture: return "texture";
                case AssetType.Sound: return "sound";
                case AssetType.CallingCard: return "callcard";
                case AssetType.Landmark: return "Landmark";
                case AssetType.Clothing: return "clothing";
                case AssetType.Object: return "object";
                case AssetType.Notecard: return "notecard";
                case AssetType.LSLText: return "lsltext";
                case AssetType.LSLBytecode: return "lslbyte";
                case AssetType.TextureTGA: return "txtr_tga";
                case AssetType.Bodypart: return "bodypart";
                case AssetType.SoundWAV: return "snd_wav";
                case AssetType.ImageTGA: return "img_tga";
                case AssetType.ImageJPEG: return "jpeg";
                case AssetType.Animation: return "animatn";
                case AssetType.Gesture: return "gesture";
                case AssetType.Simstate: return "simstate";
                case AssetType.Link: return "link";
                case AssetType.LinkFolder: return "link_f";
                case AssetType.Mesh: return "mesh";
                case AssetType.Settings: return "settings";
                default: return "unknown";
            }
        }

        public static AssetType StringToAssetType(this string s)
        {
            switch (s)
            {
                case "texture":
                    return AssetType.Texture;
                case "sound":
                    return AssetType.Sound;
                case "callcard":
                    return AssetType.CallingCard;
                case "landmark":
                    return AssetType.Landmark;
                case "clothing":
                    return AssetType.Clothing;
                case "object":
                    return AssetType.Object;
                case "notecard":
                    return AssetType.Notecard;
                case "lsltext":
                    return AssetType.LSLText;
                case "lslbyte":
                    return AssetType.LSLBytecode;
                case "txtr_tga":
                    return AssetType.TextureTGA;
                case "bodypart":
                    return AssetType.Bodypart;
                case "snd_wav":
                    return AssetType.SoundWAV;
                case "img_tga":
                    return AssetType.ImageTGA;
                case "jpeg":
                    return AssetType.ImageJPEG;
                case "animatn":
                    return AssetType.Animation;
                case "gesture":
                    return AssetType.Gesture;
                case "simstate":
                    return AssetType.Simstate;
                case "link":
                    return AssetType.Link;
                case "link_f":
                    return AssetType.LinkFolder;
                case "mesh":
                    return AssetType.Mesh;
                case "settings":
                    return AssetType.Settings;
                default:
                    return AssetType.Unknown;
            }
        }
    }
}
