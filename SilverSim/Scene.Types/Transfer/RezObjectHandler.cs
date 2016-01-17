// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Transfer
{
    public abstract class RezObjectHandler : AssetTransferWorkItem
    {
        readonly SceneInterface m_Scene;
        readonly Vector3 m_TargetPos;
        readonly UUI m_RezzingAgent;
        readonly InventoryPermissionsMask m_ItemOwnerPermissions;
        readonly SceneInterface.RezObjectParams m_RezParams;

        public abstract void PostProcessObjectGroups(List<ObjectGroup> grp);

        protected RezObjectHandler(SceneInterface scene, Vector3 targetpos, UUID assetid, AssetServiceInterface source, UUI rezzingagent, SceneInterface.RezObjectParams rezparams, InventoryPermissionsMask itemOwnerPermissions = InventoryPermissionsMask.Every)
            : base(scene.AssetService, source, assetid, ReferenceSource.Destination)
        {
            m_Scene = scene;
            m_TargetPos = targetpos;
            m_RezzingAgent = rezzingagent;
            m_ItemOwnerPermissions = itemOwnerPermissions;
            m_RezParams = rezparams;
        }

        protected void SendAlertMessage(string msg)
        {
            IAgent agent;
            if(m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
            { 
                agent.SendAlertMessage(msg, m_Scene.ID);
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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
                objgroups = ObjectXML.FromAsset(data, m_RezzingAgent);
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

            try
            {
                m_Scene.RezObjects(objgroups, m_RezParams);
            }
            catch
            {
                SendAlertMessage("ALERT: RezAttemptFailed");
                return;
            }
        }

        public override void AssetTransferFailed(Exception e)
        {
            IAgent agent;
            if(m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
            {
                agent.SendAlertMessage("ALERT: CantFindObject", m_Scene.ID);
            }
        }
    }
}
