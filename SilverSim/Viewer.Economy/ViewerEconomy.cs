// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Economy;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.Types.Economy;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Economy
{
    public class ViewerEconomy : IPlugin, IPacketHandlerExtender, ICapabilityExtender, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL ECONOMY");

        [PacketHandler(MessageType.MoneyBalanceRequest)]
        [PacketHandler(MessageType.EconomyDataRequest)]
        readonly BlockingQueue<KeyValuePair<AgentCircuit, Message>> RequestQueue = new BlockingQueue<KeyValuePair<AgentCircuit, Message>>();

        bool m_ShutdownEconomy;

        public ViewerEconomy()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {
            new Thread(HandlerThread).Start();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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

                Message m = req.Value;

                try
                {
                    switch (m.Number)
                    {
                        case MessageType.MoneyBalanceRequest:
                            HandleMoneyBalanceRequest(req.Key, req.Value);
                            break;

                        case MessageType.EconomyDataRequest:
                            HandleEconomyDataRequest(req.Key, req.Value);
                            break;
                    }
                }
                catch(Exception e)
                {
                    m_Log.Debug("Exception encountered " + e.Message, e);
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void HandleEconomyDataRequest(AgentCircuit circuit, Message m)
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
            EconomyInfo ei = scene.EconomyData;
            EconomyData ed = new EconomyData();
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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void HandleMoneyBalanceRequest(AgentCircuit circuit, Message m)
        {
            Messages.Economy.MoneyBalanceRequest mbr = (Messages.Economy.MoneyBalanceRequest)m;
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
