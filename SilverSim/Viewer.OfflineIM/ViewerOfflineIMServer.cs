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
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Threading;
using SilverSim.Types.IM;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.IM;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Viewer.OfflineIM
{
    [Description("Viewer Offline IM Handler")]
    [PluginName("ViewerOfflineIMServer")]
    public class ViewerOfflineIMServer : IPlugin, IPacketHandlerExtender, IPluginShutdown
    {
        [PacketHandler(MessageType.RetrieveInstantMessages)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        private bool m_ShutdownOfflineIM;

        public void Startup(ConfigurationLoader loader)
        {
            ThreadManager.CreateThread(HandlerThread).Start();
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

                if(req.Key == null)
                {
                    continue;
                }

                var imreq = (RetrieveInstantMessages)m;
                if(imreq.SessionID != imreq.CircuitSessionID ||
                    imreq.AgentID != imreq.CircuitAgentID)
                {
                    continue;
                }

                ViewerAgent agent = req.Key.Agent;

                OfflineIMServiceInterface offlineIMService = agent.OfflineIMService;
                if(offlineIMService != null)
                {
                    try
                    {
                        foreach (GridInstantMessage gim in offlineIMService.GetOfflineIMs(agent.Owner.ID))
                        {
                            try
                            {
                                agent.IMSend(gim);
                            }
                            catch
                            {
                                /* do not pass exceptions to caller */
                            }
                        }
                    }
                    catch
                    {
                        /* do not pass exceptions to caller */
                    }
                }
            }
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_ShutdownOfflineIM = true;
        }
    }
}
