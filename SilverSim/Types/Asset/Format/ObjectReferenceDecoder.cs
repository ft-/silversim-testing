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
            var reflist = new List<UUID>();
            GetReferences(data.InputStream, string.Empty, reflist);
            reflist.Remove(UUID.Zero);
            reflist.Remove(data.ID);
            return reflist;
        }

        public static void GetReferences(Stream xmlstream, string parentNodeName, List<UUID> reflist)
        {
            using (var reader = new XmlTextReader(new ObjectXmlStreamFilter(xmlstream)))
            {
                GetReferences(reader, parentNodeName, reflist);
            }
        }

        private static void GetReferences(XmlTextReader data, string parentNodeName, List<UUID> reflist)
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
                            foreach(var tex in new TextureEntry(data.ReadContentAsBase64()).References)
                            {
                                if(!reflist.Contains(tex))
                                {
                                    reflist.Add(tex);
                                }
                            }
                        }
                        else if(nodeName == "ParticleSystem")
                        {
                            foreach(var tex in new ParticleSystem(data.ReadContentAsBase64(), 0).References)
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
                                    var type = (ushort)(extraparams[pos] | (extraparams[pos + 1] << 8));
                                    var len = (UInt32)(extraparams[pos + 2] | (extraparams[pos + 3] << 8) | (extraparams[pos + 4] << 16) | (extraparams[pos + 5] << 24));
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
                                            var sculptTexture = new UUID(extraparams, pos);
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
                                            var projectionTextureID = new UUID(extraparams, pos);
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
