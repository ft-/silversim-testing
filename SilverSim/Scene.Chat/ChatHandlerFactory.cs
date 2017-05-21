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
        private readonly double m_WhisperDistance;
        private readonly double m_SayDistance;
        private readonly double m_ShoutDistance;

        public ChatHandlerFactory(double whisperDistance, double sayDistance, double shoutDistance)
        {
            m_WhisperDistance = whisperDistance;
            m_SayDistance = sayDistance;
            m_ShoutDistance = shoutDistance;
        }

        public override ChatServiceInterface Instantiate() =>
            new ChatHandler(m_WhisperDistance, m_SayDistance, m_ShoutDistance);

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
