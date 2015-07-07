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
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Economy;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Economy
{
    public class ViewerEconomy : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL ECONOMY");

        [PacketHandler(MessageType.MoneyBalanceRequest)]
        BlockingQueue<KeyValuePair<Circuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<Circuit, Message>>();

        bool m_ShutdownEconomy = false;

        public ViewerEconomy()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Economy Handler Thread";

            while (!m_ShutdownEconomy)
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

                try
                {
                    switch (m.Number)
                    {
                        case MessageType.MoneyBalanceRequest:
                            HandleMoneyBalanceRequest(req.Key, req.Value);
                            break;
                    }
                }
                catch(Exception e)
                {
                    m_Log.Debug("Exception encountered " + e.Message, e);
                }
            }
        }

        void HandleMoneyBalanceRequest(Circuit circuit, Message m)
        {
            Messages.Economy.MoneyBalanceRequest mbr = (Messages.Economy.MoneyBalanceRequest)m;
            if (mbr.AgentID == mbr.CircuitAgentID && mbr.SessionID == mbr.CircuitSessionID)
            {
                SceneInterface scene;
                LLAgent agent;
                try
                {
                    scene = SceneManager.Scenes[mbr.CircuitSceneID];
                    agent = circuit.Agent;
                }
                catch
                {
                    return;
                }
                Messages.Economy.MoneyBalanceReply mbrep = new Messages.Economy.MoneyBalanceReply();
                mbrep.ForceZeroFlag = true; /* lots of NUL at the end of the message */
                mbrep.AgentID = mbr.AgentID;
                mbrep.TransactionID = mbr.TransactionID;
                try
                {
                    EconomyServiceInterface economyService = circuit.Scene.EconomyService;
                    if (economyService != null)
                    {
                        mbrep.MoneyBalance = economyService.MoneyBalance[agent.Owner];
                    }
                    else
                    {
                        mbrep.MoneyBalance = 0;
                    }
                    mbrep.TransactionSuccess = true;
                }
                catch
                {
                    mbrep.TransactionSuccess = false;
                }
                circuit.SendMessage(mbrep);
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
            m_ShutdownEconomy = true;
        }
    }

    [PluginName("ViewerEconomy")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerEconomy();
        }
    }
}
