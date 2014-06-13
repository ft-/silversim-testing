/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Main.Common;
using ArribaSim.Scene.ServiceInterfaces.Chat;
using Nini.Config;

namespace ArribaSim.Scene.Chat
{
    #region Chat Factory Service
    class ChatHandlerFactory : ChatServiceFactoryInterface, IPlugin
    {
        private double m_WhisperDistance;
        private double m_SayDistance;
        private double m_ShoutDistance;

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

        }
    }
    #endregion

    #region Factory Implementation
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
