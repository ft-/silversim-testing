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

using SilverSim.Scene.Types.Agent;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.Types;
using SilverSim.Types.Economy.Transactions;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Parcel;
using SilverSim.Viewer.Messages.User;
using System;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public bool CanEjectFromParcel(UGUI requestingAgent, Vector3 position, out ParcelInfo pInfo)
        {
            if (Parcels.TryGetValue(position, out pInfo))
            {
                if(!pInfo.GroupOwned && pInfo.Owner.EqualsGrid(requestingAgent))
                {
                    return true;
                }

                if(pInfo.GroupOwned && HasGroupPower(requestingAgent, pInfo.Group, SilverSim.Types.Groups.GroupPowers.LandEjectAndFreeze))
                {
                    return true;
                }
            }
            return false;
        }

        [PacketHandler(MessageType.EjectUser)]
        public void HandleEjectUser(Message m)
        {
            var p = (EjectUser)m;
            if (p.CircuitAgentID != p.AgentID ||
                p.CircuitSessionID != p.SessionID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(p.AgentID, out agent))
            {
                return;
            }

            IAgent targetAgent;
            if(!Agents.TryGetValue(p.TargetID, out targetAgent))
            {
                return;
            }
            ParcelInfo pInfo;
            UGUI targetId = targetAgent.Owner;
            if(CanEjectFromParcel(agent.Owner, targetAgent.GlobalPosition, out pInfo))
            {
                EjectFromParcel(targetAgent.ID, pInfo.ID);

                if((p.Flags & EjectUserFlags.AddBan) != 0)
                {
                    Parcels.BlackList.Store(new ParcelAccessEntry { Accessor = targetId, ParcelID = pInfo.ID, RegionID = ID });
                }
            }
        }

        [PacketHandler(MessageType.ParcelBuyPass)]
        public void HandleBuyPass(Message m)
        {
            var p = (ParcelBuyPass)m;
            if (p.CircuitAgentID != p.AgentID ||
                p.CircuitSessionID != p.SessionID)
            {
                return;
            }

            IAgent agent;
            if(!Agents.TryGetValue(p.AgentID, out agent))
            {
                return;
            }

            ParcelInfo parcelInfo;
            if(!Parcels.TryGetValue(p.LocalID, out parcelInfo))
            {
                return;
            }

            double passHours = parcelInfo.PassHours;
            int passPrice = parcelInfo.PassPrice;
            if((parcelInfo.Flags & ParcelFlags.UsePassList) == 0 || passPrice < 0 || passHours <= 0)
            {
                return;
            }

            EconomyServiceInterface economyService = agent.EconomyService;
            if (economyService == null)
            {
                agent.SendAlertMessage(this.GetLanguageString(agent.CurrentCulture, "UnableToBuyNoEconomyConfigured", "Unable to buy. No economy configured."), ID);
                return;
            }

            var extendSeconds = (ulong)TimeSpan.FromHours(passHours).Seconds;
            if (parcelInfo.GroupOwned)
            {
                economyService.TransferMoney(agent.Owner, parcelInfo.Group, new LandpassSaleTransaction(
                    GridPosition,
                    ID,
                    Name)
                {
                    ParcelID = parcelInfo.ID,
                    ParcelName = parcelInfo.Name,
                    PassHours = passHours
                }, passPrice, () => Parcels.LandpassList.ExtendExpiry(ID, parcelInfo.ID, agent.Owner, extendSeconds));
            }
            else
            {
                economyService.TransferMoney(agent.Owner, parcelInfo.Owner, new LandpassSaleTransaction(
                    GridPosition,
                    ID,
                    Name)
                {
                    ParcelID = parcelInfo.ID,
                    ParcelName = parcelInfo.Name,
                    PassHours = passHours
                }, passPrice, () => Parcels.LandpassList.ExtendExpiry(ID, parcelInfo.ID, agent.Owner, extendSeconds));
            }
        }

        public void EjectFromParcel(UUID agentID, UUID parcelID)
        {
            IAgent agent;
            ParcelInfo parcelInfo;
            if(RootAgents.TryGetValue(agentID, out agent) &&
                Parcels.TryGetValue(parcelID, out parcelInfo) &&
                parcelInfo.LandBitmap.ContainsLocation(agent.GlobalPosition))
            {
                ParcelInfo newParcel;
                Vector3 newPosition;
                if(TryGetNearestAccessibleParcel(agent, agent.GlobalPosition, out newParcel, out newPosition))
                {
                    agent.UnSit();
                    agent.GlobalPosition = newPosition;
                }
                else if(!agent.TeleportHome(this))
                {
                    agent.KickUser(typeof(SceneInterface).GetLanguageString(agent.CurrentCulture, "YouHaveBeenKickedFromParcel", "You have been kicked from parcel."));
                }
            }
        }
    }
}
