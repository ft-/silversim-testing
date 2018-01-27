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

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace SilverSim.Scene.Agent.Bakery
{
    public sealed class AvatarSkeleton
    {
        private static Vector3 GetVectorAttribute(XmlElement elem, string name)
        {
            string val = elem.GetAttribute(name);
            if (string.IsNullOrEmpty(val))
            {
                throw new InvalidDataException();
            }
            string[] vals = val.Split(' ');
            if (vals.Length != 3)
            {
                throw new InvalidDataException();
            }
            return new Vector3(
                double.Parse(vals[0], CultureInfo.InvariantCulture),
                double.Parse(vals[1], CultureInfo.InvariantCulture),
                double.Parse(vals[2], CultureInfo.InvariantCulture));
        }

        public sealed class CollisionVolume
        {
            public string Name;
            public string Support;
            public Vector3 End { get; }
            public Vector3 Position { get; set; }
            public Vector3 Rot { get; }
            public Vector3 Scale { get; set; }

            public CollisionVolume(XmlElement elem)
            {
                Name = elem.GetAttribute("name");
                Support = elem.GetAttribute("support");
                Position = GetVectorAttribute(elem, "pos");
                Rot = GetVectorAttribute(elem, "rot");
                Scale = GetVectorAttribute(elem, "scale");
                End = GetVectorAttribute(elem, "end");
            }

            public CollisionVolume(CollisionVolume src)
            {
                Name = src.Name;
                Support = src.Support;
                End = src.End;
                Position = src.Position;
                Rot = src.Rot;
                Scale = src.Scale;
            }
        }

        public sealed class BoneInfo
        {
            public string Name { get; }
            public string Support { get; }
            public readonly string[] Aliases = new string[0];
            public bool IsJoint { get; }
            public Vector3 Position { get; set; }
            public Vector3 End { get; }
            public Vector3 Rot { get; }
            public Vector3 Scale { get; set; }
            public Vector3 Pivot { get; }
            public readonly List<BoneInfo> Children = new List<BoneInfo>();
            public readonly List<CollisionVolume> CollisionVolumes = new List<CollisionVolume>();

            public BoneInfo(XmlElement elem)
            {
                string val = elem.GetAttribute("aliases");
                if(!string.IsNullOrEmpty(val))
                {
                    Aliases = val.Split(' ');
                }
                Name = elem.GetAttribute("name");
                End = GetVectorAttribute(elem, "end");
                Pivot = GetVectorAttribute(elem, "pivot");
                Position = GetVectorAttribute(elem, "pos");
                Rot = GetVectorAttribute(elem, "rot");
                Scale = GetVectorAttribute(elem, "scale");
                Support = elem.GetAttribute("support");
                IsJoint = bool.Parse(elem.GetAttribute("connected"));
            }

            public BoneInfo(BoneInfo src)
            {
                Name = src.Name;
                End = src.End;
                Pivot = src.Pivot;
                Position = src.Position;
                Rot = src.Rot;
                Scale = src.Scale;
                Support = src.Support;
                IsJoint = src.IsJoint;

                foreach(BoneInfo child in src.Children)
                {
                    Children.Add(new BoneInfo(child));
                }

                foreach(CollisionVolume colvol in src.CollisionVolumes)
                {
                    CollisionVolumes.Add(new CollisionVolume(colvol));
                }
            }
        }

        public readonly List<BoneInfo> Bones = new List<BoneInfo>();
        public readonly Dictionary<string, BoneInfo> BonesRef = new Dictionary<string, BoneInfo>();

        private void Load(Stream s)
        {
            var doc = new XmlDocument();
            doc.Load(s);
            var rootelem = doc.GetElementsByTagName("linden_skeleton")[0] as XmlElement;
            if(rootelem == null)
            {
                throw new InvalidDataException();
            }
            var stack = new List<KeyValuePair<XmlElement, BoneInfo>>();
            foreach(XmlElement elem in rootelem.ChildNodes.OfType<XmlElement>())
            {
                if(elem.Name == "bone")
                {
                    var info = new BoneInfo(elem);
                    BonesRef.Add(info.Name, info);
                    foreach (string alias in info.Aliases)
                    {
                        BonesRef.Add(alias, info);
                    }
                    Bones.Add(info);
                    stack.Add(new KeyValuePair<XmlElement, BoneInfo>(elem, info));
                }
            }

            while(stack.Count != 0)
            {
                KeyValuePair<XmlElement, BoneInfo> kvp = stack[0];
                stack.RemoveAt(0);

                foreach (XmlElement elem in kvp.Key.ChildNodes.OfType<XmlElement>())
                {
                    if (elem.Name == "bone")
                    {
                        var info = new BoneInfo(elem);
                        kvp.Value.Children.Add(info);
                        BonesRef.Add(info.Name, info);
                        foreach(string alias in info.Aliases)
                        {
                            BonesRef.Add(alias, info);
                        }
                        stack.Add(new KeyValuePair<XmlElement, BoneInfo>(elem, info));
                    }
                    else if(elem.Name == "collision_volume")
                    {
                        var colvol = new CollisionVolume(elem);
                        kvp.Value.CollisionVolumes.Add(colvol);
                    }
                }
            }
        }

        private AvatarSkeleton()
        {
            var assembly = typeof(AvatarLad).Assembly;
            using (Stream resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources.avatar_skeleton.xml"))
            {
                Load(resource);
            }
        }

        public static readonly AvatarSkeleton DefaultSkeleton = new AvatarSkeleton();

        private void BuildBonesRef(BoneInfo bone)
        {
            BonesRef.Add(bone.Name, bone);
            foreach (string alias in bone.Aliases)
            {
                BonesRef.Add(alias, bone);
            }
            foreach (BoneInfo children in bone.Children)
            {
                BuildBonesRef(children);
            }
        }

        public AvatarSkeleton(AvatarLad lad, Dictionary<uint, double> visualParamInputs)
        {
            foreach (BoneInfo bone in DefaultSkeleton.Bones)
            {
                var newBone = new BoneInfo(bone);
                Bones.Add(newBone);
                BuildBonesRef(newBone);
            }
            foreach (KeyValuePair<uint, AvatarLad.VisualParam> kvp in lad.VisualParams)
            {
                double val;
                if (visualParamInputs.TryGetValue(kvp.Key, out val))
                {
                    foreach (AvatarLad.BoneParam bp in kvp.Value.Bones)
                    {
                        Vector3 scale = bp.Scale * val;
                        Vector3 offset = bp.Offset * val;
                        BoneInfo bone;
                        if (BonesRef.TryGetValue(bp.Name, out bone))
                        {
                            bone.Scale = bone.Scale.ElementMultiply(scale);
                            bone.Position += offset;
                        }
                        foreach (CollisionVolume colvol in bone.CollisionVolumes)
                        {
                            colvol.Scale = colvol.Scale.ElementMultiply(scale);
                        }
                    }
                }
            }
        }

        public Vector3 BodySize
        {
            get
            {
                var size = new Vector3(0.45, 0.6, 0);
                BoneInfo pelvis = BonesRef["mPelvis"];
                BoneInfo skull = BonesRef["mSkull"];
                BoneInfo neck = BonesRef["mNeck"];
                BoneInfo chest = BonesRef["mChest"];
                BoneInfo head = BonesRef["mHead"];
                BoneInfo torso = BonesRef["mTorso"];
                BoneInfo hipleft = BonesRef["mHipLeft"];
                BoneInfo kneeleft = BonesRef["mKneeLeft"];
                BoneInfo ankleleft = BonesRef["mAnkleLeft"];
                BoneInfo footleft = BonesRef["mFootLeft"];

                double pelvisToFoot = (hipleft.Position.Z * pelvis.Scale.Z) -
                    (kneeleft.Position.Z * hipleft.Scale.Z) -
                    (ankleleft.Position.Z * kneeleft.Scale.Z) -
                    (footleft.Position.Z * ankleleft.Scale.Z);

                size.Z = pelvisToFoot + (Math.Sqrt(2.0) * (skull.Position.Z * head.Scale.Z)) +
                    (head.Position.Z * neck.Scale.Z) +
                    (neck.Position.Z * chest.Scale.Z) +
                    (chest.Position.Z * torso.Scale.Z) +
                    (torso.Position.Z * pelvis.Scale.Z);
                return size;
            }
        }
    }
}
