// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

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
                KeyValuePair<AgentCircuit, Message> req;
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
