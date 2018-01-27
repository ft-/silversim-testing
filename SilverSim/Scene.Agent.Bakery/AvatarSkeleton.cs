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
            public Vector3 Position { get; }
            public Vector3 Rot { get; }
            public Vector3 Scale { get; }

            public CollisionVolume(XmlElement elem)
            {
                Name = elem.GetAttribute("name");
                Support = elem.GetAttribute("support");
                Position = GetVectorAttribute(elem, "pos");
                Rot = GetVectorAttribute(elem, "rot");
                Scale = GetVectorAttribute(elem, "scale");
                End = GetVectorAttribute(elem, "end");
            }
        }

        public sealed class BoneInfo
        {
            public string Name { get; }
            public string Support { get; }
            public readonly List<string> Aliases = new List<string>();
            public bool IsJoint { get; }
            public Vector3 Position { get; }
            public Vector3 End { get; }
            public Vector3 Rot { get; }
            public Vector3 Scale { get; }
            public Vector3 Pivot { get; }
            public readonly List<BoneInfo> Children = new List<BoneInfo>();
            public readonly List<CollisionVolume> CollisionVolumes = new List<CollisionVolume>();

            public BoneInfo(XmlElement elem)
            {
                string val = elem.GetAttribute("aliases");
                if(!string.IsNullOrEmpty(val))
                {
                    Aliases.AddRange(val.Split(' '));
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
        }

        public readonly List<BoneInfo> Bones = new List<BoneInfo>();

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
    }
}
