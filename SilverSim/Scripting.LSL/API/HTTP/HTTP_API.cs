// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;

namespace SilverSim.Scripting.LSL.API.HTTP
{
    [ScriptApiName("HTTP")]
    [LSLImplementation]
    public partial class HTTP_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        LSLHTTP m_HTTPHandler;
        LSLHTTPClient_RequestQueue m_LSLHTTPClient;

        public HTTP_API()
        {

        }

        [APILevel(APIFlags.LSL)]
        public const string URL_REQUEST_GRANTED = "URL_REQUEST_GRANTED";

        [APILevel(APIFlags.LSL)]
        public const string URL_REQUEST_DENIED = "URL_REQUEST_DENIED";

        public void Startup(ConfigurationLoader loader)
        {
            m_HTTPHandler = loader.GetPluginService<LSLHTTP>("LSLHTTP");
            m_LSLHTTPClient = loader.GetPluginService<LSLHTTPClient_RequestQueue>("LSLHttpClient");
        }

        [ExecutedOnScriptReset]
        public void RemoveURLs(ScriptInstance Instance)
        {
            foreach(UUID ids in ((Script)Instance).m_RequestedURLs)
            {
                m_HTTPHandler.ReleaseURL((string)ids);
            }
        }
    }
}
