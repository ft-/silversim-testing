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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Economy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Viewer.Economy
{
    [Description("Viewer Economy Handler")]
    [PluginName("ViewerEconomy")]
    public class ViewerEconomy : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL ECONOMY");

        [PacketHandler(MessageType.MoneyBalanceRequest)]
        [PacketHandler(MessageType.EconomyDataRequest)]
        private readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        private bool m_ShutdownEconomy;

        public void Startup(ConfigurationLoader loader)
        {
            ThreadManager.CreateThread(HandlerThread).Start();
        }

        public void HandlerThread()
        {
            Thread.CurrentThread.Name = "Economy Handler Thread";

            while (!m_ShutdownEconomy)
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

                var m = req.Value;

                try
                {
                    switch (m.Number)
                    {
                        case MessageType.MoneyBalanceRequest:
                            HandleMoneyBalanceRequest(req.Key, req.Value);
                            break;

                        case MessageType.EconomyDataRequest:
                            HandleEconomyDataRequest(req.Key);
                            break;

                        default:
                            break;
                    }
                }
                catch(Exception e)
                {
                    m_Log.Debug("Exception encountered " + e.Message, e);
                }
            }
        }

        private void HandleEconomyDataRequest(AgentCircuit circuit)
        {
            SceneInterface scene;
            ViewerAgent agent;
            try
            {
                scene = circuit.Scene;
                if (scene == null)
                {
                    return;
                }
                agent = circuit.Agent;
            }
            catch
            {
                return;
            }
            var ei = scene.EconomyData;
            var ed = new EconomyData();
            if (ei != null)
            {
                ed.ObjectCapacity = ei.ObjectCapacity;
                ed.ObjectCount = ei.ObjectCount;
                ed.PriceEnergyUnit = ei.PriceEnergyUnit;
                ed.PriceGroupCreate = ei.PriceGroupCreate;
                ed.PriceObjectClaim = ei.PriceObjectClaim;
                ed.PriceObjectRent = ei.PriceObjectRent;
                ed.PriceObjectScaleFactor = ei.PriceObjectScaleFactor;
                ed.PriceParcelClaim = ei.PriceParcelClaim;
                ed.PriceParcelClaimFactor = ei.PriceParcelClaimFactor;
                ed.PriceParcelRent = ei.PriceParcelRent;
                ed.PricePublicObjectDecay = ei.PricePublicObjectDecay;
                ed.PricePublicObjectDelete = ei.PricePublicObjectDelete;
                ed.PriceRentLight = ei.PriceRentLight;
                ed.PriceUpload = ei.PriceUpload;
                ed.TeleportMinPrice = ei.TeleportMinPrice;
                ed.TeleportPriceExponent = ei.TeleportPriceExponent;
            }
            agent.SendMessageAlways(ed, scene.ID);
        }

        private void HandleMoneyBalanceRequest(AgentCircuit circuit, Message m)
        {
            var mbr = (MoneyBalanceRequest)m;
            if (mbr.AgentID == mbr.CircuitAgentID && mbr.SessionID == mbr.CircuitSessionID)
            {
                SceneInterface scene;
                ViewerAgent agent;
                try
                {
                    scene = circuit.Scene;
                    if(scene == null)
                    {
                        return;
                    }
                    agent = circuit.Agent;
                }
                catch
                {
                    return;
                }
                var mbrep = new MoneyBalanceReply()
                {
                    ForceZeroFlag = true, /* lots of NUL at the end of the message */
                    AgentID = mbr.AgentID,
                    TransactionID = mbr.TransactionID
                };
                try
                {
                    var economyService = circuit.Scene.EconomyService;
                    mbrep.MoneyBalance= (economyService != null) ?
                        economyService.MoneyBalance[agent.Owner] :
                        0;
                    mbrep.TransactionSuccess = true;
                }
                catch
                {
                    mbrep.TransactionSuccess = false;
                }
                circuit.SendMessage(mbrep);
            }
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public void Shutdown()
        {
            m_ShutdownEconomy = true;
        }
    }
}
