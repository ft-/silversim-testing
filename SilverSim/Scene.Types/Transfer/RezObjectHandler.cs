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

namespace SilverSim.Scene.Types.Transfer
{
    public abstract class RezObjectHandler : AssetTransferWorkItem
    {
        private static readonly ILog m_Log = LogManager.GetLogger("REZOBJECT");
        private readonly SceneInterface m_Scene;
        private readonly Vector3 m_TargetPos;
        private readonly UGUI m_RezzingAgent;
        private readonly SceneInterface.RezObjectParams m_RezParams;
        private readonly List<UUID> m_ItemAssetIDs;

        protected RezObjectHandler(SceneInterface scene, Vector3 targetpos, UUID assetid, AssetServiceInterface source, UGUI rezzingagent, SceneInterface.RezObjectParams rezparams)
            : base(scene.AssetService, source, assetid, ReferenceSource.Destination)
        {
            m_ItemAssetIDs = new List<UUID> { assetid };
            m_Scene = scene;
            m_TargetPos = targetpos;
            m_RezzingAgent = rezzingagent;
            m_RezParams = rezparams;
            m_RezParams.RezzingAgent = m_RezzingAgent;
        }

        protected RezObjectHandler(SceneInterface scene, Vector3 targetpos, List<UUID> assetids, AssetServiceInterface source, UGUI rezzingagent, SceneInterface.RezObjectParams rezparams)
            : base(scene.AssetService, source, assetids, ReferenceSource.Destination)
        {
            m_ItemAssetIDs = assetids;
            m_Scene = scene;
            m_TargetPos = targetpos;
            m_RezzingAgent = rezzingagent;
            m_RezParams = rezparams;
            m_RezParams.RezzingAgent = m_RezzingAgent;
        }

        protected void SendAlertMessage(string msg)
        {
            IAgent agent;
            if (m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
            {
                agent.SendAlertMessage(msg, m_Scene.ID);
            }
        }

        public override void AssetTransferComplete()
        {
            AssetData data;
            var objgroups = new List<ObjectGroup>();

            foreach (UUID assetid in m_ItemAssetIDs)
            {
                try
                {
                    data = m_Scene.AssetService[assetid];
                }
                catch (Exception e)
                {
                    m_Log.Error(string.Format("Failed to rez object from asset {0}", assetid), e);
                    SendAlertMessage("ALERT: CantFindObject");
                    return;
                }

                try
                {
                    objgroups.AddRange(ObjectXML.FromAsset(data, m_RezzingAgent));
                }
                catch (Exception e)
                {
                    m_Log.Error(string.Format("Unable to decode asset {0} to rez", data.ID), e);
                    SendAlertMessage("ALERT: RezAttemptFailed");
                    return;
                }
            }

            try
            {
                m_Scene.RezObjects(objgroups, m_RezParams);
            }
            catch (Exception e)
            {
                m_Log.Error(string.Format("Failed to rez object in scene {0} ({1})", m_Scene.Name, m_Scene.ID), e);
                SendAlertMessage("ALERT: RezAttemptFailed");
                return;
            }
        }

        public override void AssetTransferFailed(Exception e)
        {
            IAgent agent;
            m_Log.Error(string.Format("Failed to rez object from asset {0}", AssetID), e);
            if (m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
            {
                agent.SendAlertMessage("ALERT: RezAttemptFailed", m_Scene.ID);
            }
        }
    }

    public abstract class RezRestoreObjectHandler : AssetTransferWorkItem
    {
        private static readonly ILog m_Log = LogManager.GetLogger("REZRESTOREOBJECT");
        private readonly SceneInterface m_Scene;
        private readonly UGUI m_RezzingAgent;
        private readonly UGI m_RezzingGroup;
        private readonly InventoryItem m_SourceItem;

        protected RezRestoreObjectHandler(SceneInterface scene, UUID assetid, AssetServiceInterface source, UGUI rezzingagent, UGI rezzinggroup, InventoryItem sourceItem)
            : base(scene.AssetService, source, assetid, ReferenceSource.Destination)
        {
            m_Scene = scene;
            m_RezzingAgent = rezzingagent;
            m_RezzingGroup = rezzinggroup;
            m_SourceItem = sourceItem;
        }

        protected void SendAlertMessage(string msg)
        {
            IAgent agent;
            if (m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
            {
                agent.SendAlertMessage(msg, m_Scene.ID);
            }
        }

        public override void AssetTransferComplete()
        {
            AssetData data;
            List<ObjectGroup> objgroups;
            try
            {
                data = m_Scene.AssetService[AssetID];
            }
            catch (Exception e)
            {
                m_Log.Error(string.Format("Failed to rez object from asset {0}", AssetID), e);
                SendAlertMessage("ALERT: CantFindObject");
                return;
            }

            try
            {
                objgroups = ObjectXML.FromAsset(data, m_RezzingAgent);
            }
            catch (Exception e)
            {
                m_Log.Error(string.Format("Unable to decode asset {0} to rez", data.ID), e);
                SendAlertMessage("ALERT: RezAttemptFailed");
                return;
            }

            try
            {
                foreach (ObjectGroup grp in objgroups)
                {
                    if (m_Scene.CanRez(m_RezzingAgent.ID, m_RezzingAgent, grp.GlobalPosition))
                    {
                        if (m_Scene.GroupsService?.Members.ContainsKey(m_RezzingAgent, m_RezzingGroup, m_RezzingAgent) ?? false)
                        {
                            grp.Group = m_RezzingGroup;
                        }
                        m_Scene.RezObject(grp, m_RezzingAgent, m_SourceItem);
                    }
                }
            }
            catch (Exception e)
            {
                m_Log.Error(string.Format("Failed to rez object in scene {0} ({1})", m_Scene.Name, m_Scene.ID), e);
                SendAlertMessage("ALERT: RezAttemptFailed");
                return;
            }
        }

        public override void AssetTransferFailed(Exception e)
        {
            IAgent agent;
            m_Log.Error(string.Format("Failed to rez object from asset {0}", AssetID), e);
            if (m_Scene.Agents.TryGetValue(m_RezzingAgent.ID, out agent))
            {
                agent.SendAlertMessage("ALERT: RezAttemptFailed", m_Scene.ID);
            }
        }
    }
}
