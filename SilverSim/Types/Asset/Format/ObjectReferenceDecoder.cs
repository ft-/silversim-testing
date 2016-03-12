// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SilverSim.Types.Asset.Format
{
    public static class ObjectReferenceDecoder
    {
        public static List<UUID> GetReferences(AssetData data)
        {
            List<UUID> reflist = new List<UUID>();
            GetReferences(data.InputStream, string.Empty, reflist);
            return reflist;
        }

        public static void GetReferences(Stream xmlstream, string parentNodeName, List<UUID> reflist)
        {
            using (XmlTextReader reader = new XmlTextReader(new ObjectXmlStreamFilter(xmlstream)))
            {
                GetReferences(reader, parentNodeName, reflist);
            }
        }

        static void GetReferences(XmlTextReader data, string parentNodeName, List<UUID> reflist)
        {
            while(data.Read())
            {
                switch(data.NodeType)
                {
                    case XmlNodeType.Element:
                        if(data.IsEmptyElement)
                        {
                            break;
                        }
                        string nodeName = data.Name;
                        if((parentNodeName == "SculptTexture" && nodeName == "UUID") ||
                            (parentNodeName == "CollisionSound" && nodeName == "UUID") ||
                            (parentNodeName == "AssetID" && nodeName == "UUID"))
                        {
                            UUID id;
                            if(!UUID.TryParse(data.ReadElementValueAsString(), out id))
                            {
                                throw new InvalidDataException("Invalid UUID in object asset");
                            }
                            if(id != UUID.Zero &&
                                !reflist.Contains(id))
                            {
                                reflist.Add(id);
                            }
                        }
                        else if(nodeName == "TextureEntry")
                        {
                            List<UUID> texlist = new TextureEntry(data.ReadContentAsBase64()).References;
                            foreach(UUID tex in texlist)
                            {
                                if(!reflist.Contains(tex))
                                {
                                    reflist.Add(tex);
                                }
                            }
                        }
                        else if(nodeName == "ParticleSystem")
                        {
                            List<UUID> texlist = new ParticleSystem(data.ReadContentAsBase64(), 0).References;
                            foreach(UUID tex in texlist)
                            {
                                if(!reflist.Contains(tex))
                                {
                                    reflist.Add(tex);
                                }
                            }
                        }
                        else if(nodeName == "ExtraParams")
                        {
                            byte[] extraparams = data.ReadContentAsBase64();

                            if (extraparams.Length >= 1)
                            {
                                const ushort FlexiEP = 0x10;
                                const ushort LightEP = 0x20;
                                const ushort SculptEP = 0x30;
                                const ushort ProjectionEP = 0x40;

                                int paramCount = extraparams[0];
                                int pos = 0;
                                for (int paramIdx = 0; paramIdx < paramCount; ++paramIdx)
                                {
                                    if (pos + 6 > extraparams.Length)
                                    {
                                        break;
                                    }
                                    ushort type = (ushort)(extraparams[pos] | (extraparams[pos + 1] << 8));
                                    UInt32 len = (UInt32)(extraparams[pos + 2] | (extraparams[pos + 3] << 8) | (extraparams[pos + 4] << 16) | (extraparams[pos + 5] << 24));
                                    pos += 6;

                                    if (pos + len > extraparams.Length)
                                    {
                                        break;
                                    }

                                    switch (type)
                                    {
                                        case FlexiEP:
                                        case LightEP:
                                            /* in this decoder, both types are equal */
                                            if (len < 16)
                                            {
                                                break;
                                            }
                                            pos += 16;
                                            break;

                                        case SculptEP:
                                            if (len < 17)
                                            {
                                                break;
                                            }
                                            UUID sculptTexture = new UUID(extraparams, pos);
                                            if(!reflist.Contains(sculptTexture))
                                            {
                                                reflist.Add(sculptTexture);
                                            }
                                            pos += 17;
                                            break;

                                        case ProjectionEP:
                                            if (len < 28)
                                            {
                                                break;
                                            }
                                            UUID projectionTextureID = new UUID(extraparams, pos);
                                            if(projectionTextureID != UUID.Zero && !reflist.Contains(projectionTextureID))
                                            {
                                                reflist.Add(projectionTextureID);
                                            }
                                            pos += 28;
                                            break;

                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            GetReferences(data, data.Name, reflist);
                        }
                        break;

                    case XmlNodeType.EndElement:
                        return;

                    default:
                        break;
                }
            }
        }
    }
}
