﻿// SilverSim is distributed under the terms of the
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
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages.Script;
using System;

namespace SilverSim.Scene.Npc
{
    public partial class NpcAgent
    {
        public override void ScheduleUpdate(AgentUpdateInfo info, UUID fromSceneID)
        {
            /* ignored */
        }

        public override void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            /* ignored */
        }

        public override void SendAlertMessage(string msg, UUID fromSceneID)
        {
            /* ignored */
        }

        public override void SendAlertMessage(string msg, string notification, IValue llsd, UUID fromSceneID)
        {
            /* ignored */
        }


        public override void SendEstateUpdateInfo(UUID invoice, UUID transactionID, EstateInfo estate, UUID fromSceneID, bool sendToAgentOnly = true)
        {
            /* ignored */
        }

        public override void SendMessageAlways(SilverSim.Viewer.Messages.Message m, UUID fromSceneID)
        {
            Type t = m.GetType();
            if(t == typeof(ScriptDialog))
            {
                HandleScriptDialog((ScriptDialog)m, fromSceneID);
            }
        }

        public override void SendMessageIfRootAgent(SilverSim.Viewer.Messages.Message m, UUID fromSceneID)
        {
            Type t = m.GetType();
            if (t == typeof(ScriptDialog))
            {
                HandleScriptDialog((ScriptDialog)m, fromSceneID);
            }
        }

        public override void SendRegionNotice(UGUI fromAvatar, string message, UUID fromSceneID)
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
