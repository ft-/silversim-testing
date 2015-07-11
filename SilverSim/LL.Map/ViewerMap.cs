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

using log4net;
using Nini.Config;
using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Map;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Scene;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Map
{
    public class ViewerMap : IPlugin, IPluginShutdown, IPacketHandlerExtender
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL MAP");

        [PacketHandler(MessageType.MapBlockRequest)]
        [PacketHandler(MessageType.MapLayerRequest)]
        [PacketHandler(MessageType.MapNameRequest)]
        BlockingQueue<KeyValuePair<Circuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<Circuit, Message>>();
        bool m_ShutdownMap = false;

        public ViewerMap()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Map Handler Thread";

            while (!m_ShutdownMap)
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
                SceneInterface scene = req.Key.Scene;
                if (scene == null)
                {
                    continue;
                }
                try
                {
                    switch (m.Number)
                    {
                        case MessageType.MapNameRequest:
                            HandleMapNameRequest(req.Key.Agent, scene, m);
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    m_Log.Debug("Unexpected exception " + e.Message, e);
                }
            }
        }

        void HandleMapNameRequest(LLAgent agent, SceneInterface scene, Message m)
        {
            MapNameRequest req = (MapNameRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
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
            m_ShutdownMap = true;
        }
    }

    [PluginName("ViewerMap")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerMap();
        }
    }
}
