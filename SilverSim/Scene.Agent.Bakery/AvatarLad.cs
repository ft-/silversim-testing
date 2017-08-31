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
using System.Xml;
using System.Linq;

namespace SilverSim.Scene.Agent.Bakery
{
    public sealed class AvatarLad
    {
        public Dictionary<uint, VisualParam> VisualParams { get; }
        private readonly List<DriverParam> m_DriverParams = new List<DriverParam>();
        private readonly List<VisualParam> m_VisualParamsTarget = new List<VisualParam>();

        private DriverParam[] m_DriverParamArray;
        private VisualParam[] m_VisualParamTargetArray;

        public DriverParam[] DriverParams => m_DriverParamArray != null ? m_DriverParamArray : m_DriverParamArray = m_DriverParams.ToArray();

        public VisualParam[] VisualParamsTarget => m_VisualParamTargetArray != null ? m_VisualParamTargetArray : m_VisualParamTargetArray = m_VisualParamsTarget.ToArray();

        public AvatarLad()
        {
            VisualParams = new Dictionary<uint, VisualParam>();
            var assembly = typeof(AvatarLad).Assembly;
            using (Stream resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources.avatar_lad.xml"))
            {
                using (var reader = new XmlTextReader(resource))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name != "linden_avatar" || reader.IsEmptyElement)
                            {
                                throw new InvalidDataException("not a linden_avatar.xml");
                            }

                            if (reader.MoveToFirstAttribute())
                            {
                                do
                                {
                                    switch (reader.Name)
                                    {
                                        case "version":
                                            break;

                                        case "wearable_definition_version":
                                            break;

                                        default:
                                            break;
                                    }
                                }
                                while (reader.MoveToNextAttribute());
                            }

                            ParseLindenAvatar(reader);
                        }
                    }
                }
            }

            foreach (VisualParam vp in VisualParams.Values.OrderBy(e => e.Id))
            {
                if(vp.Group == 0 || vp.Group == 3)
                {
                    m_VisualParamsTarget.Add(vp);
                }
            }
        }

        public void ReadToEndElementNoParamElement(XmlTextReader reader, string tagname = null)
        {
            if (string.IsNullOrEmpty(tagname))
            {
                tagname = reader.Name;
            }
            XmlNodeType nodeType = reader.NodeType;
            if ((nodeType == XmlNodeType.Element || nodeType == XmlNodeType.Attribute) && !reader.IsEmptyElement)
            {
                do
                {
                    nextelem:
                    if (!reader.Read())
                    {
                        throw new XmlException("Premature end of XML", null, reader.LineNumber, reader.LinePosition);
                    }
                    nodeType = reader.NodeType;
                    if (nodeType == XmlNodeType.Element)
                    {
                        if(reader.Name == "param")
                        {
                            throw new InvalidDataException("Unparsed param element found");
                        }
                        ReadToEndElementNoParamElement(reader);
                        goto nextelem;
                    }
                } while (nodeType != XmlNodeType.EndElement);
                if (tagname != reader.Name)
                {
                    throw new XmlException("Closing tag does not match", null, reader.LineNumber, reader.LinePosition);
                }
            }
        }

        private void ParseLindenAvatar(XmlTextReader reader)
        {
            var attrs = new Dictionary<string, string>();
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidDataException("invalid linden_avatar.xml");
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch(elementName)
                        {
                            case "param":
                                ParseLindenAvatarParam(reader, attrs);
                                break;

                            case "mesh":
                                ParseLindenAvatarMesh(reader, attrs, isEmptyElement);
                                break;

                            case "global_color":
                                ParseLindenAvatarGlobalColor(reader, attrs, isEmptyElement);
                                break;

                            case "skeleton":
                                ParseLindenAvatarSkeleton(reader, attrs, isEmptyElement);
                                break;

                            case "layer_set":
                                ParseLindenAvatarLayerSet(reader, attrs, isEmptyElement);
                                break;

                            case "driver_parameters":
                                ParseLindenAvatarDriverParameters(reader, attrs, isEmptyElement);
                                break;

                            default:
                                if (!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader, elementName);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "linden_avatar")
                        {
                            throw new InvalidDataException("invalid linden_avatar.xml");
                        }
                        return;
                }
            }
        }

        private void ParseLindenAvatarLayerSet(XmlTextReader reader, Dictionary<string, string> attrs, bool isEmptyElementOutside)
        {
            if (isEmptyElementOutside)
            {
                return;
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidDataException("Invalid linden_avatar.xml");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch (elementName)
                        {
                            case "param":
                                ParseLindenAvatarParam(reader, attrs);
                                break;

                            case "layer":
                                ParseLindenAvatarLayer(reader, attrs, isEmptyElement);
                                break;

                            default:
                                if (!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader, elementName);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "layer_set")
                        {
                            throw new InvalidDataException("Invalid linden_avatar.xml");
                        }
                        return;
                }
            }
        }

        private void ParseLindenAvatarLayer(XmlTextReader reader, Dictionary<string, string> attrs, bool isEmptyElementOutside)
        {
            if (isEmptyElementOutside)
            {
                return;
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidDataException("Invalid linden_avatar.xml");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch (elementName)
                        {
                            case "param":
                                ParseLindenAvatarParam(reader, attrs);
                                break;

                            default:
                                if (!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader, elementName);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "layer")
                        {
                            throw new InvalidDataException("Invalid linden_avatar.xml");
                        }
                        return;
                }
            }
        }

        private void ParseLindenAvatarSkeleton(XmlTextReader reader, Dictionary<string, string> attrs, bool isEmptyElementOutside)
        {
            if (isEmptyElementOutside)
            {
                return;
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidDataException("Invalid linden_avatar.xml");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch (elementName)
                        {
                            case "param":
                                ParseLindenAvatarParam(reader, attrs);
                                break;

                            default:
                                if (!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader, elementName);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "skeleton")
                        {
                            throw new InvalidDataException("Invalid linden_avatar.xml");
                        }
                        return;
                }
            }
        }

        private void ParseLindenAvatarDriverParameters(XmlTextReader reader, Dictionary<string, string> attrs, bool isEmptyElementOutside)
        {
            if (isEmptyElementOutside)
            {
                return;
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidDataException("Invalid linden_avatar.xml");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch (elementName)
                        {
                            case "param":
                                ParseLindenAvatarParam(reader, attrs);
                                break;

                            default:
                                if (!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader, elementName);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "driver_parameters")
                        {
                            throw new InvalidDataException("Invalid linden_avatar.xml");
                        }
                        return;
                }
            }
        }

        private void ParseLindenAvatarMesh(XmlTextReader reader, Dictionary<string, string> attrs, bool isEmptyElementOutside)
        {
            if(isEmptyElementOutside)
            {
                return;
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidDataException("Invalid linden_avatar.xml");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch (elementName)
                        {
                            case "param":
                                ParseLindenAvatarParam(reader, attrs);
                                break;

                            default:
                                if (!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader, elementName);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "mesh")
                        {
                            throw new InvalidDataException("Invalid linden_avatar.xml");
                        }
                        return;
                }
            }
        }

        private void ParseLindenAvatarGlobalColor(XmlTextReader reader, Dictionary<string, string> attrs, bool isEmptyElementOutside)
        {
            if (isEmptyElementOutside)
            {
                return;
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidDataException("Invalid linden_avatar.xml");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch (elementName)
                        {
                            case "param":
                                ParseLindenAvatarParam(reader, attrs);
                                break;

                            default:
                                if (!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader, elementName);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "global_color")
                        {
                            throw new InvalidDataException("Invalid linden_avatar.xml");
                        }
                        return;
                }
            }
        }

        private void ParseLindenAvatarParam(XmlTextReader reader, Dictionary<string, string> attrs)
        {
            uint vpId;
            string vpName;
            double minValue;
            double maxValue;
            double defValue;
            string defValueStr;
            uint group = uint.MaxValue;
            if(attrs.ContainsKey("group"))
            {
                group = uint.Parse(attrs["group"]);
            }
            vpId = uint.Parse(attrs["id"]);
            if(!attrs.TryGetValue("name", out vpName))
            {
                vpName = string.Empty;
            }

            try
            {
                minValue = double.Parse(attrs["value_min"], CultureInfo.InvariantCulture);
                maxValue = double.Parse(attrs["value_max"], CultureInfo.InvariantCulture);
                defValue = attrs.TryGetValue("value_default", out defValueStr) ?
                    double.Parse(defValueStr, CultureInfo.InvariantCulture) :
                    minValue;
            }
            catch
            {
                throw new InvalidDataException("Failed to parse parameter data");
            }
            bool isshared = attrs.ContainsKey("shared");

            var vp = new VisualParam(group, vpId, vpName, minValue, maxValue, defValue);

            for (; ;)
            {
                if(!reader.Read())
                {
                    throw new InvalidDataException("Invalid linden_avatar.xml");
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch(elementName)
                        {
                            case "param_skeleton":
                                if(!isEmptyElement)
                                {
                                    ParseLindenAvatarParamSkeleton(reader, vp, attrs);
                                }
                                break;

                            case "param_driver":
                                if(!isEmptyElement)
                                {
                                    ParseLindenAvatarParamDriver(reader, vp, attrs);
                                }
                                break;

                            default:
                                if(!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader, elementName);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "param")
                        {
                            throw new InvalidDataException("Invalid linden_avatar.xml");
                        }

                        if (!isshared)
                        {
                            VisualParams.Add(vp.Id, vp);
                        }
                        return;
                }
            }
        }

        private void ParseLindenAvatarParamDriver(XmlTextReader reader, VisualParam vp, Dictionary<string, string> attrs)
        {
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidDataException("invalid linden_avatar.xml");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch (elementName)
                        {
                            case "driven":
                                string drivVal;
                                bool haveweight = attrs.ContainsKey("min1") || attrs.ContainsKey("min2") || attrs.ContainsKey("max1") || attrs.ContainsKey("max2");
                                double min1 = attrs.TryGetValue("min1", out drivVal) ? double.Parse(drivVal, CultureInfo.InvariantCulture) : 0;
                                double min2 = attrs.TryGetValue("min2", out drivVal) ? double.Parse(drivVal, CultureInfo.InvariantCulture) : 0;
                                double max1 = attrs.TryGetValue("max1", out drivVal) ? double.Parse(drivVal, CultureInfo.InvariantCulture) : 0;
                                double max2 = attrs.TryGetValue("max2", out drivVal) ? double.Parse(drivVal, CultureInfo.InvariantCulture) : 0;
                                m_DriverParams.Add(new DriverParam(vp.Id,
                                    uint.Parse(attrs["id"]),
                                    min1, max1,
                                    min2, max2,
                                    haveweight));
                                goto default;

                            default:
                                if (!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "param_driver")
                        {
                            throw new InvalidDataException("invalid linden_avatar.xml");
                        }
                        return;
                }
            }
        }

        private Vector3 FromAttrToVector(string val)
        {
            string[] parts = val.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length != 3)
            {
                throw new InvalidDataException("Invalid vector parameter");
            }
            return new Vector3(double.Parse(parts[0], CultureInfo.InvariantCulture),
                double.Parse(parts[1], CultureInfo.InvariantCulture),
                double.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        private void ParseLindenAvatarParamSkeleton(XmlTextReader reader, VisualParam vp, Dictionary<string, string> attrs)
        {
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidDataException("invalid linden_avatar.xml");
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        string elementName = reader.Name;
                        attrs.Clear();
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                attrs.Add(reader.Name, reader.Value);
                            }
                            while (reader.MoveToNextAttribute());
                        }

                        switch (elementName)
                        {
                            case "bone":
                                string val;
                                Vector3 scale = attrs.TryGetValue("scale", out val) ? FromAttrToVector(val) : Vector3.Zero;
                                Vector3 offset = attrs.TryGetValue("offset", out val) ? FromAttrToVector(val) : Vector3.Zero;
                                var bone = new BoneParam(attrs["name"], scale, offset);
                                vp.Bones.Add(bone);
                                goto default;

                            default:
                                if (!isEmptyElement)
                                {
                                    ReadToEndElementNoParamElement(reader);
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "param_skeleton")
                        {
                            throw new InvalidDataException("invalid linden_avatar.xml");
                        }
                        return;
                }
            }
        }

        public sealed class DriverParam
        {
            public uint FromId { get; }
            public uint ToId { get; }
            public double Min1 { get; }
            public double Min2 { get; }
            public double Max1 { get; }
            public double Max2 { get; }
            public bool HaveWeight { get; }

            public DriverParam(uint fromid, uint toid, double min1, double max1, double min2, double max2, bool haveweight)
            {
                FromId = fromid;
                ToId = toid;
                Min1 = min1;
                Max1 = max1;
                Min2 = min2;
                Max2 = max2;
                HaveWeight = haveweight;
            }
        }

        public sealed class BoneParam
        {
            public string Name { get; }
            public Vector3 Scale { get; }
            public Vector3 Offset { get; }

            public BoneParam(string name, Vector3 scale, Vector3 offset)
            {
                Name = name;
                Scale = scale;
                Offset = offset;
            }
        }

        public sealed class VisualParam
        {
            public uint Group { get; }
            public uint Id { get; }
            public string Name { get; }
            public double MinimumValue { get; }
            public double MaximumValue { get; }
            public double DefaultValue { get; }
            public readonly List<BoneParam> Bones = new List<BoneParam>();

            public VisualParam(uint group, uint id, string name, double min, double max, double def)
            {
                Group = group;
                Id = id;
                Name = name;
                MinimumValue = min;
                MaximumValue = max;
                DefaultValue = def;
            }
        }
    }
}
