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
        public bool Local = false; /* not for serialization */
        public bool Temporary = false;
        public AssetType Type = AssetType.Unknown;
        public string Name = string.Empty;
        public UUI Creator = UUI.Unknown;
        public AssetFlags Flags = AssetFlags.Normal;
        public Date CreateTime = new Date();
        public Date AccessTime = new Date();

        public AssetMetadata()
        {
        }

        public string FileExtension
        {
            get
            {
                switch (Type)
                {
                    case AssetType.Texture: return ".texture.jp2";
                    case AssetType.TextureTGA: return ".texture.tga";
                    case AssetType.ImageTGA: return ".image.tga";
                    case AssetType.ImageJPEG: return ".image.jpg";
                    case AssetType.Sound: return ".sound.ogg";
                    case AssetType.SoundWAV: return ".sound.wav";
                    case AssetType.CallingCard: return ".callingcard.txt";
                    case AssetType.Landmark: return ".landmark.txt";
                    case AssetType.Clothing: return ".clothing.txt";
                    case AssetType.Object: return ".object.xml";
                    case AssetType.Notecard: return ".notecard.txt";
                    case AssetType.LSLText: return ".script.lsl";
                    case AssetType.LSLBytecode: return ".bytecode.lso";
                    case AssetType.Bodypart: return ".bodypart.txt";
                    case AssetType.Animation: return ".animation.bvh";
                    case AssetType.Gesture: return ".gesture.txt";
                    case AssetType.Simstate: return ".simstate.bin";
                    case AssetType.Mesh: return ".mesh.llmesh";
                    case AssetType.Material: return ".material.xml";
                    default: throw new Exception("Unmapped asset type " + Type.ToString());
                }
            }
        }

        public string FileName
        {
            get
            {
                return "assets/" + ID + FileExtension;
            }
            set
            {
                AssetType type;
                if(value.EndsWith(".texture.jp2"))
                {
                    type = AssetType.Texture;
                }
                else if(value.EndsWith(".texture.tga"))
                {
                    type = AssetType.TextureTGA;
                }
                else if(value.EndsWith(".image.tga"))
                {
                    type = AssetType.ImageTGA;
                }
                else if(value.EndsWith(".image.jpg"))
                {
                    type = AssetType.ImageJPEG;
                }
                else if(value.EndsWith(".sound.ogg"))
                {
                    type = AssetType.Sound;
                }
                else if(value.EndsWith(".sound.wav"))
                {
                    type = AssetType.SoundWAV;
                }
                else if(value.EndsWith(".callingcard.txt"))
                {
                    type = AssetType.CallingCard;
                }
                else if(value.EndsWith(".landmark.txt"))
                {
                    type = AssetType.Landmark;
                }
                else if(value.EndsWith(".clothing.txt"))
                {
                    type = AssetType.Clothing;
                }
                else if(value.EndsWith(".object.xml"))
                {
                    type = AssetType.Object;
                }
                else if(value.EndsWith(".notecard.txt"))
                {
                    type = AssetType.Notecard;
                }
                else if(value.EndsWith(".script.lsl"))
                {
                    type = AssetType.LSLText;
                }
                else if(value.EndsWith(".bytecode.lso"))
                {
                    type = AssetType.LSLBytecode;
                }
                else if(value.EndsWith(".bodypart.txt"))
                {
                    type = AssetType.Bodypart;
                }
                else if(value.EndsWith("animation.bvh"))
                {
                    type = AssetType.Animation;
                }
                else if(value.EndsWith(".gesture.txt"))
                {
                    type = AssetType.Gesture;
                }
                else if(value.EndsWith(".simstate.bin"))
                {
                    type = AssetType.Simstate;
                }
                else if(value.EndsWith(".mesh.llmesh"))
                {
                    type = AssetType.Mesh;
                }
                else if(value.EndsWith(".material.xml"))
                {
                    type = AssetType.Material;
                }
                else
                {
                    throw new Exception("Unknown extension " + value);
                }

                int lastOf = value.LastIndexOf('/');
                string fname;
                if (lastOf >= 0)
                {
                    fname = value.Substring(lastOf + 1);
                }
                else
                {
                    fname = value;
                }
                fname = fname.Substring(fname.IndexOf('.'));
                ID = fname;
                Type = type;
            }
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
                    case AssetType.Material: return "application/llsd+xml";
                    default: return "application/octet-stream";
                }
            }
            set
            {
                switch(value)
                {
                    case "image/x-j2c": Type = AssetType.Texture; break;
                    case "image/tga": Type = AssetType.TextureTGA; break;
                    case "image/jpeg": Type = AssetType.ImageJPEG; break;
                    case "audio/ogg": Type = AssetType.Sound; break;
                    case "audio/x-wav": Type = AssetType.SoundWAV; break;
                    case "application/vnd.ll.callingcard": Type = AssetType.CallingCard; break;
                    case "application/vnd.ll.landmark": Type = AssetType.Landmark; break;
                    case "application/vnd.ll.clothing": Type = AssetType.Clothing; break;
                    case "application/vnd.ll.primitive": Type = AssetType.Object; break;
                    case "application/vnd.ll.notecard": Type = AssetType.Notecard; break;
                    case "application/vnd.ll.lsltext": Type = AssetType.LSLText; break;
                    case "application/vnd.ll.lslbyte": Type = AssetType.LSLBytecode; break;
                    case "application/vnd.ll.bodypart": Type = AssetType.Bodypart; break;
                    case "application/vnd.ll.animation": Type = AssetType.Animation; break;
                    case "application/vnd.ll.gesture": Type = AssetType.Gesture; break;
                    case "application/x-metaverse-simstate": Type = AssetType.Simstate; break;
                    case "application/vnd.ll.mesh": Type = AssetType.Mesh; break;
                    case "application/llsd+xml": Type = AssetType.Material; break;
                    default: throw new InvalidOperationException();
                }
            }
        }
    }
}
