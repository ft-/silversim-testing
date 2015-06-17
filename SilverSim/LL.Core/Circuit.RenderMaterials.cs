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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Object;
using SilverSim.StructuredData.LLSD;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        void Cap_RenderMaterials(HttpRequest httpreq)
        {
            switch(httpreq.Method)
            {
                case "GET":
                    Cap_RenderMaterials_GET(httpreq);
                    break;

                case "POST":
                case "PUT":
                    Cap_RenderMaterials_POST(httpreq);
                    break;

                default:
                    httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");
                    break;
            }
        }

        void Cap_RenderMaterials_GET(HttpRequest httpreq)
        {
            HttpResponse httpres = httpreq.BeginResponse("text/llsd+xml");
            byte[] matdata = Scene.MaterialsData;
            httpres.GetOutputStream().Write(matdata, 0, matdata.Length);
            httpres.Close();
        }

        void Cap_RenderMaterials_POST(HttpRequest httpreq)
        {
            IValue o;
            try
            {
                o = LLSD_XML.Deserialize(httpreq.Body);
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (!(o is Map))
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }
            Map m = (Map)o;
            List<Material> materials = new List<Material>();
            if(m.ContainsKey("Zipped"))
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream((BinaryData)m["Zipped"]))
                    {
                        using (GZipStream gz = new GZipStream(ms, CompressionMode.Decompress))
                        {
                            o = LLSD_XML.Deserialize(gz);
                        }
                    }
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace.ToString());
                    httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                    return;
                }
                if (!(o is AnArray) && !(o is Map))
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted Zipped LLSD-XML");
                    return;
                }

                if(o is AnArray)
                {
                    foreach(IValue v in (AnArray)o)
                    {
                        try
                        {
                            materials.Add(Scene.GetMaterial(v.AsUUID));
                        }
                        catch
                        {

                        }
                    }
                }
                else if(o is Map)
                {
                    m = (Map)o;
                    if(m.ContainsKey("FullMaterialsPerFace"))
                    {
                        o = m["FullMaterialsPerFace"];
                        if(o is AnArray)
                        {
                            foreach(IValue iv in (AnArray)o)
                            {
                                m = (Map)iv;

                                try
                                {
                                    uint primLocalID = m["ID"].AsUInt;
                                    UUID matID = UUID.Random;
                                    Material mat;
                                    try
                                    {
                                        matID = UUID.Random;
                                        mat = new Material(matID, m["Material"] as Map);
                                    }
                                    catch
                                    {
                                        matID = UUID.Zero;
                                        mat = null;
                                    }
                                    if (mat != null)
                                    {
                                        Scene.StoreMaterial(mat);
                                        continue;
                                    }
                                    ObjectPart p = Scene.Primitives[primLocalID];
                                    if(m.ContainsKey("Face"))
                                    {
                                        int face = m["Face"].AsInt;
                                        TextureEntryFace te = p.TextureEntry.FaceTextures[face];
                                        te.MaterialID = matID;
                                        p.TextureEntry.FaceTextures[face] = te;
                                    }
                                    else
                                    {
                                        TextureEntryFace te = p.TextureEntry.DefaultTexture;
                                        te.MaterialID = matID;
                                        p.TextureEntry.DefaultTexture = te;
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                    }
                }
            }

            byte[] buf;
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress))
                {
                    using (XmlTextWriter writer = new XmlTextWriter(ms, UTF8NoBOM))
                    {
                        writer.WriteStartElement("llsd");
                        writer.WriteStartElement("array");
                        foreach (Material matdata in materials)
                        {
                            writer.WriteStartElement("map");
                            writer.WriteNamedValue("key", "ID");
                            writer.WriteNamedValue("uuid", matdata.MaterialID);
                            writer.WriteNamedValue("key", "Material");
                            matdata.WriteMap(writer);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                }
                buf = ms.GetBuffer();
            }

            HttpResponse httpres = httpreq.BeginResponse("text/llsd+xml");
            using (XmlTextWriter writer = new XmlTextWriter(httpres.GetOutputStream(), UTF8NoBOM))
            {
                writer.WriteStartElement("llsd");
                writer.WriteNamedValue("key", "Zipped");
                writer.WriteNamedValue("binary", buf);
                writer.WriteEndElement();
            }
            httpres.Close();
        }
    }
}
