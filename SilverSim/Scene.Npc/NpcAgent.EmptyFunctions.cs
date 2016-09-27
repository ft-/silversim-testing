// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Parcel;
using System;

namespace SilverSim.Scene.Npc
{
    public partial class NpcAgent
    {
        public override void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            /* ignored */
        }

        public override void SendAlertMessage(string msg, UUID fromSceneID)
        {
            /* ignored */
        }

        public override void SendEstateUpdateInfo(UUID invoice, UUID transactionID, EstateInfo estate, UUID fromSceneID, bool sendToAgentOnly = true)
        {
            /* ignored */
        }

        public override void SendMessageAlways(SilverSim.Viewer.Messages.Message m, UUID fromSceneID)
        {
            /* ignored */
        }

        public override void SendMessageIfRootAgent(SilverSim.Viewer.Messages.Message m, UUID fromSceneID)
        {
            /* ignored */
        }

        public override void SendRegionNotice(UUI fromAvatar, string message, UUID fromSceneID)
        {
            /* ignored */
        }

        public override void SendUpdatedParcelInfo(ParcelInfo pinfo, UUID fromSceneID)
        {
            /* ignored */
        }

        public override void KickUser(string msg)
        {
            /* ignored */
        }

        public override void KickUser(string msg, Action<bool> callbackDelegate)
        {
            /* ignored */
        }

    }
}
