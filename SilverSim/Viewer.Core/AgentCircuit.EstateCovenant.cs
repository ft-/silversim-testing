// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Names;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Viewer.Messages.Estate;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types.Estate;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.EstateCovenantRequest)]
        public void HandleEstateCovenantRequest(Message m)
        {
            EstateCovenantRequest req = (EstateCovenantRequest)m;

            if(req.SessionID != req.CircuitSessionID ||
                req.AgentID != req.CircuitAgentID)
            {
                return;
            }

            EstateCovenantReply reply = new EstateCovenantReply();
            EstateInfo estate;
            uint estateID;
            EstateServiceInterface estateService = Scene.EstateService;
            if (estateService.RegionMap.TryGetValue(Scene.ID, out estateID) &&
                estateService.TryGetValue(estateID, out estate))
            {
                reply.CovenantID = estate.CovenantID;
                reply.CovenantTimestamp = (uint)estate.CovenantTimestamp.DateTimeToUnixTime();
                reply.EstateName = estate.Name;
                reply.EstateOwnerID = estate.Owner.ID;

                SendMessage(reply);
            }
        }
    }
}
