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

using SilverSim.Types.Primitive;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SilverSim.Types.Asset.Format
{
    public static class ObjectReferenceDecoder
    {
        public static List<UUID> GetReferences(AssetData data)
        {
            using(MemoryStream ms = new MemoryStream(data.Data))
            {
                using(XmlTextReader reader = new XmlTextReader(ms))
                {
                    List<UUID> reflist = new List<UUID>();
                    GetReferences(reader, "", reflist);
                    return reflist;
                }
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
                        if((parentNodeName == "SculptTexture" && data.Name == "UUID") ||
                            (parentNodeName == "CollisionSound" && data.Name == "UUID") ||
                            (parentNodeName == "AssetID" && data.Name == "UUID"))
                        {
                            UUID id = UUID.Parse(data.ReadContentAsString());
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
                            string base64 = data.ReadContentAsString();
                            List<UUID> texlist = new TextureEntry(System.Convert.FromBase64String(base64)).References;
                            foreach(UUID tex in texlist)
                            {
                                if(!reflist.Contains(tex))
                                {
                                    reflist.Add(tex);
                                }
                            }
                        }
#if PARTICLE_DECODER
                        else if(data.Name == "ParticleSystem")
                        {
                            string base64 = data.ReadContentAsString();
                            List<UUID> texlist = new ParticleSystem(System.Convert.FromBase64String(base64), 0).References;
                            foreach(UUID tex in texlist)
                            {
                                if(!reflist.Contains(tex))
                                {
                                    reflist.Add(tex);
                                }
                            }
                        }
#endif
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
