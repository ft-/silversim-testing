// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using Nini.Config;
using System.ComponentModel;

namespace SilverSim.Scene.Chat
{
    #region Chat Factory Service
    [Description("Region Chat Handler")]
    public sealed class ChatHandlerFactory : ChatServiceFactoryInterface, IPlugin
    {
        readonly double m_WhisperDistance;
        readonly double m_SayDistance;
        readonly double m_ShoutDistance;

        public ChatHandlerFactory(double whisperDistance, double sayDistance, double shoutDistance)
        {
            m_WhisperDistance = whisperDistance;
            m_SayDistance = sayDistance;
            m_ShoutDistance = shoutDistance;
        }

        public override ChatServiceInterface Instantiate()
        {
            return new ChatHandler(m_WhisperDistance, m_SayDistance, m_ShoutDistance);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
    #endregion

    #region Factory Implementation
    [PluginName("Chat")]
    public class HandlerFactory : IPluginFactory
    {
        public HandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownConfig)
        {
            double whisperDistance = ownConfig.GetDouble("WhisperDistance", 10f);
            double sayDistance = ownConfig.GetDouble("SayDistance", 20f);
            double shoutDistance = ownConfig.GetDouble("ShoutDistance", 20f);

            return new ChatHandlerFactory(whisperDistance, sayDistance, shoutDistance);
        }
    }
    #endregion
}
