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

using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.StartLocation;
using System;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.SetStartLocationRequest)]
        public void HandleSetStartLocationRequest(Message m)
        {
            var req = (SetStartLocationRequest)m;
            if(req.CircuitAgentID != req.AgentID || req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            /* bypass viewer limitation */
            if (Math.Abs(req.LocationPos.X - 255.5f) < Double.Epsilon)
            {
                req.LocationPos.X = Agent.GlobalPosition.X;
            }
            if (Math.Abs(req.LocationPos.Y - 255.5f) < Double.Epsilon)
            {
                req.LocationPos.Y = Agent.GlobalPosition.Y;
            }

            var agentOwner = Agent.Owner;
            ParcelInfo pInfo = null;

            var canSetHome = false;
            canSetHome = canSetHome || Scene.IsEstateManager(agentOwner);
            canSetHome = canSetHome || Scene.IsRegionOwner(agentOwner);
            canSetHome = canSetHome || Scene.IsPossibleGod(agentOwner);
            if(Scene.Parcels.TryGetValue(req.LocationPos, out pInfo))
            {
                canSetHome = canSetHome || pInfo.Owner.EqualsGrid(agentOwner);
                canSetHome = canSetHome || Scene.HasGroupPower(agentOwner, pInfo.Group, Types.Groups.GroupPowers.AllowSetHome);
            }

            var telehub = Scene.RegionSettings.TelehubObject;
            ObjectPart part;
            if(pInfo != null && telehub != UUID.Zero && Scene.Primitives.TryGetValue(telehub, out part))
            {
                canSetHome = canSetHome || pInfo.LandBitmap.ContainsLocation(part.GlobalPosition);
            }

            if(canSetHome)
            {
                if(Agent.GridUserService == null)
                {
                    Agent.SendAlertMessage(this.GetLanguageString(Agent.CurrentCulture, "SettingHomeForForeignersNotPossible", "Setting home for foreign visitors is not possible."), Scene.ID);
                    return;
                }
                try
                {
                    Agent.GridUserService.SetHome(agentOwner, Scene.ID, req.LocationPos, req.LocationLookAt);
                }
                catch(GridUserSetHomeNotPossibleForForeignerException)
                {
                    Agent.SendAlertMessage(this.GetLanguageString(Agent.CurrentCulture, "SettingHomeForForeignersNotPossible", "Setting home for foreign visitors is not possible."), Scene.ID);
                    return;
                }
                catch(Exception e)
                {
                    Agent.SendAlertMessage(this.GetLanguageString(Agent.CurrentCulture, "SettingHomeWasNotPossible", "Setting home location was not possible."), Scene.ID);
                    m_Log.WarnFormat("Setting home location for {0} ({1}) was not possible due: {2}: {3}\n{4}", agentOwner.FullName, agentOwner.ID, e.GetType().FullName, e.Message, e.StackTrace);
                    return;
                }

                /* this message must have exact the text "Home position set.". Otherwise, viewer thinks the request did not succeed: */
                Agent.SendAlertMessage(
                    "Home position set.", 
                    "HomePositionSet",
                    new Map(),
                    Scene.ID);
            }
            else
            {
                Agent.SendAlertMessage(this.GetLanguageString(Agent.CurrentCulture, "SettingHomeIsNotAllowedHere", "Setting home location is not allowed here."), Scene.ID);
            }
        }
    }
}
