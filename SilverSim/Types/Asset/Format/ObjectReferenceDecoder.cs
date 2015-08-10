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
            GetReferences(data.InputStream, "", reflist);
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
                        if((parentNodeName == "SculptTexture" && data.Name == "UUID") ||
                            (parentNodeName == "CollisionSound" && data.Name == "UUID") ||
                            (parentNodeName == "AssetID" && data.Name == "UUID"))
                        {
                            UUID id = UUID.Parse(data.ReadElementValueAsString());
                            if(id != UUID.Zero)
                            {
                                if(!reflist.Contains(id))
                                {
                                    reflist.Add(id);
                                }
                            }
                        }
                        else if(data.Name == "TextureEntry")
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
                        else if(data.Name == "ParticleSystem")
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
                        else if(data.Name == "ExtraParams")
                        {
                            byte[] extraparams = data.ReadContentAsBase64();

                            if (extraparams.Length < 1)
                            {
                            }
                            else
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
                                            if (len < 16)
                                            {
                                                break;
                                            }
                                            pos += 16;
                                            break;

                                        case LightEP:
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
