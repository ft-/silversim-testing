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
            var req = (GodUpdateRegionInfo)m;
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
            var estateService = Scene.EstateService;
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

            var regionFlags = req.RegionFlags;

            if (req.RedirectGridX != 0 || req.RedirectGridY != 0)
            {
                if (Scene.IsEstateManager(Agent.Owner))
                {
                    /* EO and EM only */
                    var newLocation = Scene.GridPosition;
                    var oldLocation = newLocation;
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
                        m_Log.InfoFormat("Changed location of {0} ({1}) from {2} to {3}", Scene.Name, Scene.ID, oldLocation.GridLocation, newLocation.GridLocation);
                    }
                    catch
                    {
                        Agent.SendAlertMessage(this.GetLanguageString(Agent.CurrentCulture, "ItHasNotBeenPossibleToRelocateRegion", "It has not been possible to relocate region."), Scene.ID);
                    }
                }
                else
                {
                    Agent.SendAlertMessage(this.GetLanguageString(Agent.CurrentCulture, "YouAreNotPermittedToRelocateRegion", "You are not permitted to relocate region."), Scene.ID);
                }
            }

            if (Scene.Name != req.SimName)
            {
                /* only process reregistration when sim name changes */
                m_Log.InfoFormat("Changing name of {0} ({1}) to {2}", Scene.Name, Scene.ID, req.SimName);
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
