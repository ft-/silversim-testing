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

using Nini.Config;
using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.IM;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types.IM;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.OfflineIM
{
    public class ViewerOfflineIMServer : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        [PacketHandler(MessageType.RetrieveInstantMessages)]
        BlockingQueue<KeyValuePair<Circuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<Circuit, Message>>();

        bool m_ShutdownOfflineIM = false;

        public ViewerOfflineIMServer()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "OfflineIM Retrieve Handler Thread";

            while (!m_ShutdownOfflineIM)
            {
                KeyValuePair<Circuit, Message> req;
                try
                {
                    req = RequestQueue.Dequeue(1000);
                }
                catch
                {
                    continue;
                }

                Message m = req.Value;

                RetrieveInstantMessages imreq = (RetrieveInstantMessages)m;
                if(imreq.SessionID != imreq.CircuitSessionID ||
                    imreq.AgentID != imreq.CircuitAgentID)
                {
                    continue;
                }

                LLAgent agent = req.Key.Agent;

                OfflineIMServiceInterface offlineIMService = agent.OfflineIMService;
                if(null != offlineIMService)
                {
                    try
                    {
                        foreach (GridInstantMessage gim in offlineIMService.getOfflineIMs(agent.Owner.ID))
                        {
                            try
                            {
                                agent.IMSend(gim);
                            }
                            catch
                            {

                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }

        public ShutdownOrder ShutdownOrder
        {
            get 
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            m_ShutdownOfflineIM = true;
        }
    }

    [PluginName("ViewerOfflineIMServer")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerOfflineIMServer();
        }
    }
}
