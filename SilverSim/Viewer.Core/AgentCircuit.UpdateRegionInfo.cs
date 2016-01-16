﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Region;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.GodUpdateRegionInfo)]
        public void HandleGodUpdateRegionInfo(Message m)
        {
            GodUpdateRegionInfo req = (GodUpdateRegionInfo)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }
            if(!Agent.IsActiveGod || !Agent.IsInScene(Scene))
            {
                return;
            }

            EstateInfo estate;
            EstateServiceInterface estateService = Scene.EstateService;
            uint estateID;
            if (estateService.RegionMap.TryGetValue(Scene.ID, out estateID) &&
                estateService.TryGetValue(estateID, out estate))
            {
                if (estateID != req.EstateID)
                {
                    if (estateService.TryGetValue(req.EstateID, out estate))
                    {
                        estateService.RegionMap[Scene.ID] = req.EstateID;
                    }
                    else
                    {
                        Agent.SendAlertMessage("Unable to change estate", Scene.ID);
                    }
                }

                estate.BillableFactor = req.BillableFactor;
                estate.PricePerMeter = req.PricePerMeter;
                estateService[estate.ID] = estate;
            }

            uint regionFlags = req.RegionFlags;

            if ((req.RedirectGridX != 0 || req.RedirectGridY != 0) && Scene.IsEstateManager(Agent.Owner))
            {
                /* EO and EM only */
                GridVector newLocation = Scene.GridPosition;
                if (req.RedirectGridX > 0 && req.RedirectGridX <= 65535)
                {
                    newLocation.GridX = (ushort)req.RedirectGridX;
                }

                if (req.RedirectGridY > 0 && req.RedirectGridY <= 65535)
                {
                    newLocation.GridY = (ushort)req.RedirectGridY;
                }
                try
                {
                    Scene.RelocateRegion(newLocation);
                }
                catch
                {
                    Agent.SendAlertMessage(this.GetLanguageString(Agent.CurrentCulture, "YouAreNotPermittedToRelocateRegion", "You are not permitted to relocate region."), Scene.ID);
                }
            }

            if (Scene.Name != req.SimName)
            {
                /* only process reregistration when sim name changes */
                Scene.Name = req.SimName;
                Scene.ReregisterRegion();
            }
            Scene.RegionSettings.IsSunFixed = (regionFlags & (uint)RegionOptionFlags.SunFixed) != 0;
            Scene.RegionSettings.BlockDwell = (regionFlags & (uint)RegionOptionFlags.BlockDwell) != 0;
            Scene.RegionSettings.AllowDamage = (regionFlags & (uint)RegionOptionFlags.AllowDamage) != 0;
            Scene.RegionSettings.BlockTerraform = (regionFlags & (uint)RegionOptionFlags.BlockTerraform) != 0;
            Scene.RegionSettings.ResetHomeOnTeleport = (regionFlags & (uint)RegionOptionFlags.ResetHomeOnTeleport) != 0;
            Scene.RegionSettings.Sandbox = (regionFlags & (uint)RegionOptionFlags.Sandbox) != 0;
            Scene.RegionSettings.DisableScripts = (regionFlags & (uint)RegionOptionFlags.DisableScripts) != 0;
            Scene.RegionSettings.DisableCollisions = (regionFlags & (uint)RegionOptionFlags.DisableAgentCollisions) != 0;
            Scene.RegionSettings.DisablePhysics = (regionFlags & (uint)RegionOptionFlags.DisablePhysics) != 0;
            Scene.RegionSettings.BlockShowInSearch = (regionFlags & (uint)RegionOptionFlags.BlockParcelSearch) != 0;
            Scene.TriggerRegionSettingsChanged();
        }
    }
}
