/*

ArribaSim is distributed under the terms of the
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

namespace ArribaSim.Types.Asset
{
    public enum AssetType : int
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
        Folder = 8,
        RootFolder = 9,
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
        BasicRoot = 51,
    }
}
