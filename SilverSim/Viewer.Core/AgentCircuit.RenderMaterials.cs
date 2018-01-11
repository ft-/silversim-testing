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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Primitive;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Xml;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        private void Cap_RenderMaterials(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            switch (httpreq.Method)
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

        private void Cap_RenderMaterials_GET(HttpRequest httpreq)
        {
            using (var httpres = httpreq.BeginResponse("application/llsd+xml"))
            {
#if DEBUG
                m_Log.DebugFormat("Retrieving RenderMaterials block scene={0}", Scene.ID);
#endif
                var matdata = Scene.MaterialsData;
                using (Stream s = httpres.GetOutputStream())
                {
#if DEBUG
                    m_Log.DebugFormat("Sending out RenderMaterials block scene={0} length={1}", Scene.ID, matdata.Length);
#endif
                    s.Write(matdata, 0, matdata.Length);
                }
            }
        }

        private void Cap_RenderMaterials_POST(HttpRequest httpreq)
        {
            Map reqmap;
            try
            {
                reqmap = LlsdXml.Deserialize(httpreq.Body) as Map;
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            var materials = new List<Material>();
            if(reqmap.ContainsKey("Zipped"))
            {
                AnArray zippedDataArray;
                Map zippedDataMap;
                try
                {
                    using (var ms = new MemoryStream((BinaryData)reqmap["Zipped"]))
                    {
                        var skipheader = new byte[2];
                        if(ms.Read(skipheader, 0, 2) != 2)
                        {
                            throw new InvalidDataException("Missing header in materials data");
                        }
                        using (var gz = new DeflateStream(ms, CompressionMode.Decompress))
                        {
                            var inp = LlsdBinary.Deserialize(gz);
                            zippedDataArray = inp as AnArray;
                            zippedDataMap = inp as Map;
                        }
                    }
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("Invalid LLSD_XML: {0} {1}", e.Message, e.StackTrace);
                    httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                    return;
                }
                if (zippedDataArray == null && zippedDataMap == null)
                {
                    httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted Zipped LLSD-XML");
                    return;
                }

                if(zippedDataArray != null)
                {
                    foreach (var v in zippedDataArray)
                    {
                        try
                        {
                            materials.Add(Scene.GetMaterial(v.AsUUID));
                        }
                        catch
                        {
                            /* adding faled due to duplicate */
                        }
                    }
                }
                else if(zippedDataMap?.ContainsKey("FullMaterialsPerFace") ?? false)
                {
                    var faceData = zippedDataMap["FullMaterialsPerFace"] as AnArray;
                    if (faceData != null)
                    {
                        foreach (var face_iv in faceData)
                        {
                            var faceDataMap = face_iv as Map;
                            if(faceDataMap == null)
                            {
                                continue;
                            }

                            try
                            {
                                uint primLocalID = faceDataMap["ID"].AsUInt;
                                var matID = UUID.Random;
                                Material mat;
                                try
                                {
                                    matID = UUID.Random;
                                    mat = new Material(matID, faceDataMap["Material"] as Map);
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
                                var p = Scene.Primitives[primLocalID];
                                if (faceDataMap.ContainsKey("Face"))
                                {
                                    var face = faceDataMap["Face"].AsUInt;
                                    var te = p.TextureEntry[face];
                                    te.MaterialID = matID;
                                }
                                else
                                {
                                    var te = p.TextureEntry.DefaultTexture;
                                    te.MaterialID = matID;
                                    p.TextureEntry.DefaultTexture = te;
                                }
                            }
                            catch
                            {
                                /* no action possible */
                            }
                        }
                    }
                }
            }

            byte[] buf;
            using (var ms = new MemoryStream())
            {
                var zlibheader = new byte[] { 0x78, 0xDA };
                ms.Write(zlibheader, 0, 2);
                using (var gz = new DeflateStream(ms, CompressionMode.Compress))
                {
                    var matdata = Scene.MaterialsData;
                    gz.Write(matdata, 0, matdata.Length);
                }
                buf = ms.ToArray();
            }

            using (var httpres = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (var writer = httpres.GetOutputStream().UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("llsd");
                    writer.WriteNamedValue("key", "Zipped");
                    writer.WriteNamedValue("binary", buf);
                    writer.WriteEndElement();
                }
            }
        }
    }
}
