/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;

namespace SilverSim.Main.Common.Transfer
{
    public abstract class RezObjectHandler : AssetTransferWorkItem
    {
        SceneInterface m_Scene;
        Vector3 m_TargetPos;
        UUID m_RezzingAgentID;

        public abstract void PostProcessObjectGroups(List<ObjectGroup> grp);

        public RezObjectHandler(SceneInterface scene, Vector3 targetpos, UUID assetid, AssetServiceInterface source, UUID rezzingagent)
            : base(scene.AssetService, source, assetid, ReferenceSource.Destination)
        {
            m_Scene = scene;
            m_TargetPos = targetpos;
            m_RezzingAgentID = rezzingagent;
        }

        protected void SendAlertMessage(string msg)
        {
            try
            {
                IAgent agent = m_Scene.Agents[m_RezzingAgentID];
                agent.SendAlertMessage(msg, m_Scene.ID);
            }
            catch
            {

            }

        }

        public override void AssetTransferComplete()
        {
            AssetData data;
            List<ObjectGroup> objgroups;
            try
            {
                data = m_Scene.AssetService[m_AssetID];
            }
            catch
            {
                SendAlertMessage("ALERT: CantFindObject");
                return;
            }

            try
            {
                objgroups = ObjectXML.fromAsset(data);
            }
            catch
            {
                SendAlertMessage("ALERT: RezAttemptFailed");
                return;
            }

            try
            {
                PostProcessObjectGroups(objgroups);
            }
            catch
            {
                SendAlertMessage("ALERT: RezAttemptFailed");
                return;
            }

            foreach(ObjectGroup group in objgroups)
            {
                group.GlobalPosition += m_TargetPos;
                foreach(ObjectPart part in group.Values)
                {
                    part.ID = UUID.Random;
                }
                m_Scene.Add(group);
            }
        }

        public override void AssetTransferFailed(Exception e)
        {
            try
            {
                IAgent agent = m_Scene.Agents[m_RezzingAgentID];
                agent.SendAlertMessage("ALERT: CantFindObject", m_Scene.ID);
            }
            catch
            {

            }
        }
    }
}
