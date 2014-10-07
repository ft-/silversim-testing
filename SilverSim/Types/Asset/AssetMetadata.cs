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

namespace SilverSim.Types.Asset
{
    [Serializable]
    public class AssetMetadata
    {
        public UUID ID = UUID.Zero;
        public bool Local = false;
        public bool Temporary = false;
        public AssetType Type = AssetType.Unknown;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public UUI Creator = UUI.Unknown;
        public uint Flags = 0;
        public Date CreateTime = new Date();
        public Date AccessTime = new Date();

        public AssetMetadata()
        {
        }

        public string ContentType
        {
            get
            {
                switch(Type)
                {
                    case AssetType.Texture: return "image/x-j2c";
                    case AssetType.TextureTGA: return "image/tga";
                    case AssetType.ImageTGA: return "image/tga";
                    case AssetType.ImageJPEG: return "image/jpeg";
                    case AssetType.Sound: return "audio/ogg";
                    case AssetType.SoundWAV: return "audio/x-wav";
                    case AssetType.CallingCard: return "application/vnd.ll.callingcard";
                    case AssetType.Landmark: return "application/vnd.ll.landmark";
                    case AssetType.Clothing: return "application/vnd.ll.clothing";
                    case AssetType.Object: return "application/vnd.ll.primitive";
                    case AssetType.Notecard: return "application/vnd.ll.notecard";
                    case AssetType.LSLText: return "application/vnd.ll.lsltext";
                    case AssetType.LSLBytecode: return "application/vnd.ll.lslbyte";
                    case AssetType.Bodypart: return "application/vnd.ll.bodypart";
                    case AssetType.Animation: return "application/vnd.ll.animation";
                    case AssetType.Gesture: return "application/vnd.ll.gesture";
                    case AssetType.Simstate: return "application/x-metaverse-simstate";
                    case AssetType.Mesh: return "application/vnd.ll.mesh";
                    default: return "application/octet-stream";
                }
            }
        }
    }
}
