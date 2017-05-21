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

using log4net;
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
        private static readonly ILog m_Log = LogManager.GetLogger("REZOBJECT");
        private readonly SceneInterface m_Scene;
        private readonly Vector3 m_TargetPos;
        private readonly UUI m_RezzingAgent;
        private readonly InventoryPermissionsMask m_ItemOwnerPermissions;
        private readonly SceneInterface.RezObjectParams m_RezParams;

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
                data = m_Scene.AssetService[AssetID];
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
            catch(Exception e)
            {
                m_Log.Error(string.Format("Unable to decode asset {0} to rez", data.ID), e);
                SendAlertMessage("ALERT: RezAttemptFailed");
                return;
            }

            try
            {
                PostProcessObjectGroups(objgroups);
            }
            catch(Exception e)
            {
                m_Log.Error(string.Format("Unable to post process objects {0} ({1})", m_Scene.Name, m_Scene.ID), e);
                SendAlertMessage("ALERT: RezAttemptFailed");
                return;
            }

            try
            {
                m_Scene.RezObjects(objgroups, m_RezParams);
            }
            catch(Exception e)
            {
                m_Log.Error(string.Format("Failed to rez object in scene {0} ({1})", m_Scene.Name, m_Scene.ID), e);
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
