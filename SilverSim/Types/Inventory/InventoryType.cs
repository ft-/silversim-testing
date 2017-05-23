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

namespace SilverSim.Types.Inventory
{
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

    public static class InventoryTypeExtensionMethods
    {
        public static string AssetTypeToString(sbyte v)
        {
            switch (v)
            {
                default: return "unknown";
            }
        }

        public static string InventoryTypeToString(this InventoryType v)
        {
            switch (v)
            {
                case InventoryType.Snapshot: return "snapshot";
                case InventoryType.Attachable: return "attach";
                case InventoryType.Wearable: return "wearable";
                case InventoryType.Texture: return "texture";
                case InventoryType.Sound: return "sound";
                case InventoryType.CallingCard: return "callcard";
                case InventoryType.Landmark: return "Landmark";
                case InventoryType.Clothing: return "clothing";
                case InventoryType.Object: return "object";
                case InventoryType.Notecard: return "notecard";
                case InventoryType.LSLText: return "lsltext";
                case InventoryType.LSLBytecode: return "lslbyte";
                case InventoryType.TextureTGA: return "txtr_tga";
                case InventoryType.Bodypart: return "bodypart";
                case InventoryType.Animation: return "animatn";
                case InventoryType.Gesture: return "gesture";
                case InventoryType.Simstate: return "simstate";
                case InventoryType.Mesh: return "mesh";
                default: return "unknown";
            }
        }

        public static InventoryType StringToInventoryType(this string v)
        {
            switch (v)
            {
                case "texture":
                    return InventoryType.Texture;
                case "sound":
                    return InventoryType.Sound;
                case "callcard":
                    return InventoryType.CallingCard;
                case "landmark":
                    return InventoryType.Landmark;
                case "clothing":
                    return InventoryType.Clothing;
                case "object":
                    return InventoryType.Object;
                case "notecard":
                    return InventoryType.Notecard;
                case "lsltext":
                    return InventoryType.LSLText;
                case "lslbyte":
                    return InventoryType.LSLBytecode;
                case "txtr_tga":
                    return InventoryType.TextureTGA;
                case "bodypart":
                    return InventoryType.Bodypart;
                case "animatn":
                    return InventoryType.Animation;
                case "gesture":
                    return InventoryType.Gesture;
                case "simstate":
                    return InventoryType.Simstate;
                case "snapshot":
                    return InventoryType.Snapshot;
                case "attach":
                    return InventoryType.Attachable;
                case "wearable":
                    return InventoryType.Wearable;
                case "mesh":
                    return InventoryType.Mesh;
                default:
                    return InventoryType.Unknown;
            }
        }
    }
}
