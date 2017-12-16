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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.ServiceInterfaces.Voice;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Groups;
using SilverSim.Types.Parcel;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Core;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Voice
{
    [Description("Viewer Voice Handler")]
    [PluginName("ViewerVoice")]
    public sealed class ViewerVoice : IPlugin, ICapabilityExtender
    {
        private readonly string m_VoiceServiceName;
        private VoiceServiceInterface m_VoiceService;

        public ViewerVoice(IConfig ownSection)
        {
            m_VoiceServiceName = ownSection.GetString("VoiceService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_VoiceService = loader.GetService<VoiceServiceInterface>(m_VoiceServiceName);
        }

        [CapabilityHandler("ProvisionVoiceAccountRequest")]
        public void HandleProvisionVoiceAccountRequest(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            if (req.CallerIP != circuit.RemoteIP)
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (req.Method != "POST")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            VoiceAccountInfo info;

            try
            {
                info = m_VoiceService.ProvisionAccount(agent.Owner);
            }
            catch
            {
                req.ErrorResponse(HttpStatusCode.Unauthorized);
                return;
            }

            var resdata = new Map
            {
                { "username", info.AgentName },
                { "password", info.Password },
                { "voice_sip_uri_hostname", info.SipUri },
                { "voice_account_server_name", info.VoiceAccountApi }
            };

            using (HttpResponse res = req.BeginResponse("application/llsd+xml"))
            using (Stream s = res.GetOutputStream())
            {
                LlsdXml.Serialize(resdata, s);
            }
        }

        [CapabilityHandler("ParcelVoiceInfoRequest")]
        public void HandleParcelVoiceInfoRequest(ViewerAgent agent, AgentCircuit circuit, HttpRequest req)
        {
            if (req.CallerIP != circuit.RemoteIP)
            {
                req.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            if (req.Method != "POST")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            SceneInterface scene = circuit.Scene;
            ParcelInfo pInfo;
            IValue res;

            if(!scene.Parcels.TryGetValue(agent.GlobalPosition, out pInfo))
            {
                res = new Undef();
            }
            else if(scene.EstateAllowsVoice &&
                ((pInfo.Flags & ParcelFlags.AllowVoiceChat) != 0 || scene.HasGroupPower(agent.Owner, pInfo.Group, GroupPowers.AllowVoiceChat)))
            {
                VoiceChannelInfo channelInfo;

                try
                {
                    channelInfo = m_VoiceService.GetParcelChannel(scene, pInfo);
                    res = new Map { { "channel_uri", channelInfo.ChannelSipUri } };
                }
                catch
                {
                    res = new Map { { "channel_uri", string.Empty } };
                }
            }
            else
            {
                res = new Map { { "channel_uri", string.Empty } };
            }

            using (HttpResponse resp = req.BeginResponse("application/llsd+xml"))
            using (Stream s = resp.GetOutputStream())
            {
                LlsdXml.Serialize(res, s);
            }
        }
    }
}
