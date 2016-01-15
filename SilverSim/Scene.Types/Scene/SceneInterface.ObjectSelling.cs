// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {

        [PacketHandler(MessageType.ObjectSaleInfo)]
        public void HandleObjectSaleInfo(Message m)
        {
            ObjectSaleInfo req = (ObjectSaleInfo)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            using (ObjectPropertiesSendHandler propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
                foreach (ObjectSaleInfo.Data d in req.ObjectData)
                {
#if DEBUG
                    m_Log.DebugFormat("ObjectSaleInfo localid={0}", d.ObjectLocalID);
#endif

                    ObjectPart prim;
                    if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                    {
                        continue;
                    }

                    if (!CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                    {
                        continue;
                    }

                    prim.ObjectGroup.SalePrice = d.SalePrice;
                    prim.ObjectGroup.SaleType = d.SaleType;
                    propHandler.Send(prim);
                }
            }
        }

        [PacketHandler(MessageType.ObjectBuy)]
        public void HandleObjectBuy(Message m)
        {
            ObjectBuy req = (ObjectBuy)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectBuy.Data data in req.ObjectData)
            {
                ObjectPart part;
                if(!Primitives.TryGetValue(data.ObjectLocalID, out part))
                {
                    agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToBuyDueNotFoundObject", "Unable to buy. The object was not found."), ID);
                }
                else
                {
                    Object.ObjectGroup grp = part.ObjectGroup;
                    if(grp.SalePrice != data.SalePrice || grp.SaleType != data.SaleType)
                    {
                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "BuyingCurrentlyNotPossibleDueInvalidRequest", "Buying currently not possible since the viewer request is invalid. You might have to relog."), ID);
                    }
                    else if(grp.SalePrice != 0 && EconomyService == null)
                    {
                        agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "BuyingForAnythingOtherPriceThanZeroIsNotPossible", "Buying for anything other price than zero is not possible without economy system."), ID);
                    }
                }
            }
        }

        [PacketHandler(MessageType.BuyObjectInventory)]
        public void HandleBuyObjectInventory(Message m)
        {
            BuyObjectInventory req = (BuyObjectInventory)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }
    }
}
