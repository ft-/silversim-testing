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
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public sealed class ViewerStats : ICapabilityInterface
    {
        private readonly ViewerAgent m_Agent;
        private readonly SceneInterface m_Scene;
        private readonly string m_RemoteIP;

        public ViewerStats(ViewerAgent agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public string CapabilityName => "ViewerStats";

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != m_RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Map reqmap;
            try
            {
                reqmap = LlsdXml.Deserialize(httpreq.Body) as Map;
            }
            catch
            {
                httpreq.ErrorResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type");
                return;
            }
            if (reqmap == null)
            {
                httpreq.ErrorResponse(HttpStatusCode.BadRequest, "Misformatted LLSD-XML");
                return;
            }

            IValue iv;

            Map agent;
            if(reqmap.TryGetValue("agent", out agent))
            {
                if(agent.TryGetValue("fps", out iv))
                {
                    ViewerFps = iv.AsReal;
                }
                if(agent.TryGetValue("version", out iv))
                {
                    ViewerVersion = iv.ToString();
                }
                if(agent.TryGetValue("agents_in_view", out iv))
                {
                    NumAgentsInView = iv.AsInt;
                }
            }

            Map system;
            if(reqmap.TryGetValue("system", out system))
            {
                if(system.TryGetValue("ram", out iv))
                {
                    ViewerPhysicalMemoryKb = iv.AsInt;
                }
                if(system.TryGetValue("os", out iv))
                {
                    ViewerOs = iv.ToString();
                }
                if(system.TryGetValue("cpu", out iv))
                {
                    ViewerCpu = iv.ToString();
                }
                if(system.TryGetValue("mac_address", out iv))
                {
                    ViewerMacAddress = iv.ToString();
                }
                if(system.TryGetValue("gpu", out iv))
                {
                    ViewerGpu = iv.ToString();
                }
                if(system.TryGetValue("gpu_class", out iv))
                {
                    ViewerGpuClass = iv.ToString();
                }
                if(system.TryGetValue("gpu_vendor", out iv))
                {
                    ViewerGpuVendor = iv.ToString();
                }
                if(system.TryGetValue("gpu_version", out iv))
                {
                    ViewerGpuVersion = iv.ToString();
                }
                if(system.TryGetValue("opengl_verion", out iv))
                {
                    OpenGlVersion = iv.ToString();
                }
                if(system.TryGetValue("shader_level", out iv))
                {
                    ShaderLevel = iv.AsInt;
                }
            }

            Map downloads;
            if(reqmap.TryGetValue("downloads", out downloads))
            {
                if(downloads.TryGetValue("world_kbytes", out iv))
                {
                    DownloadWorldKbytes = iv.AsReal;
                }
                if(downloads.TryGetValue("object_kbytes", out iv))
                {
                    DownloadObjectKbytes = iv.AsReal;
                }
                if(downloads.TryGetValue("texture_kbytes", out iv))
                {
                    DownloadTextureKbytes = iv.AsReal;
                }
                if(downloads.TryGetValue("mesh_kbytes", out iv))
                {
                    DownloadMeshKbytes = iv.AsReal;
                }
            }

            Map stats;
            if(reqmap.TryGetValue("stats", out stats))
            {
                Map map;
                if(stats.TryGetValue("in", out map))
                {
                    if(map.TryGetValue("kbytes", out iv))
                    {
                        TotalKBytesIn = iv.AsReal;
                    }
                    if(map.TryGetValue("packets", out iv))
                    {
                        TotalPacketsIn = iv.AsInt;
                    }
                    if(map.TryGetValue("compressed_packets", out iv))
                    {
                        TotalCompressedPacketsIn = iv.AsInt;
                    }
                    if(map.TryGetValue("savings", out iv))
                    {
                        TotalSavingsKBytesIn = iv.AsReal;
                    }
                }
                if (stats.TryGetValue("out", out map))
                {
                    if (map.TryGetValue("kbytes", out iv))
                    {
                        TotalKBytesOut = iv.AsReal;
                    }
                    if (map.TryGetValue("packets", out iv))
                    {
                        TotalPacketsOut = iv.AsInt;
                    }
                    if (map.TryGetValue("compressed_packets", out iv))
                    {
                        TotalCompressedPacketsOut = iv.AsInt;
                    }
                    if (map.TryGetValue("savings", out iv))
                    {
                        TotalSavingsKBytesOut = iv.AsReal;
                    }
                }
                if(stats.TryGetValue("failures", out map))
                {
                    if(map.TryGetValue("send_packet", out iv))
                    {
                        Fail_SendPacket = iv.AsInt;
                    }
                    if(map.TryGetValue("dropped", out iv))
                    {
                        Fail_Dropped = iv.AsInt;
                    }
                    if(map.TryGetValue("resent", out iv))
                    {
                        Fail_Resent = iv.AsInt;
                    }
                    if(map.TryGetValue("failed_resends", out iv))
                    {
                        Fail_FailedResends = iv.AsInt;
                    }
                    if(map.TryGetValue("off_circuit", out iv))
                    {
                        Fail_OffCircuit = iv.AsInt;
                    }
                    if(map.TryGetValue("invalid", out iv))
                    {
                        Fail_Invalid = iv.AsInt;
                    }
                }
            }

            HaveValidData = true;
            httpreq.EmptyResponse();
        }

        public bool HaveValidData { get; private set; }
        public double ViewerFps { get; private set; }
        public string ViewerVersion { get; private set; }
        public int NumAgentsInView { get; private set; }

        public int ViewerPhysicalMemoryKb { get; private set; }
        public string ViewerOs { get; private set; }
        public string ViewerCpu { get; private set; }
        public string ViewerMacAddress { get; private set; }
        public string ViewerGpu { get; private set; }
        public string ViewerGpuClass { get; private set; }
        public string ViewerGpuVendor { get; private set; }
        public string ViewerGpuVersion { get; private set; }
        public string OpenGlVersion { get; private set; }
        public int ShaderLevel { get; private set; }

        public double DownloadWorldKbytes { get; private set; }
        public double DownloadObjectKbytes { get; private set; }
        public double DownloadTextureKbytes { get; private set; }
        public double DownloadMeshKbytes { get; private set; }

        public double TotalKBytesIn { get; private set; }
        public int TotalPacketsIn { get; private set; }
        public int TotalCompressedPacketsIn { get; private set; }
        public double TotalSavingsKBytesIn { get; private set; }

        public double TotalKBytesOut { get; private set; }
        public int TotalPacketsOut { get; private set; }
        public int TotalCompressedPacketsOut { get; private set; }
        public double TotalSavingsKBytesOut { get; private set; }

        public int Fail_SendPacket { get; private set; }
        public int Fail_Dropped { get; private set; }
        public int Fail_Resent { get; private set; }
        public int Fail_FailedResends { get; private set; }
        public int Fail_OffCircuit { get; private set; }
        public int Fail_Invalid { get; private set; }
    }
}
