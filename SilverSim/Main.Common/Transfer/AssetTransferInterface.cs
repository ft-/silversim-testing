// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Main.Common.Transfer
{
    public abstract class AssetTransferWorkItem
    {
        readonly AssetServiceInterface m_DestinationAssetService;
        readonly AssetServiceInterface m_SourceAssetService;
        protected UUID m_AssetID { get; private set; }
        readonly List<UUID> m_AssetIDList;

        public enum ReferenceSource
        {
            Source,
            Destination
        }

        readonly ReferenceSource m_RefSource;

        protected AssetTransferWorkItem(AssetServiceInterface dest, AssetServiceInterface source, UUID assetid, ReferenceSource refsource)
        {
            m_DestinationAssetService = dest;
            m_SourceAssetService = source;
            m_AssetID = assetid;
            m_RefSource = refsource;
            m_AssetIDList = new List<UUID>();
            m_AssetIDList.Add(assetid);
        }

        protected AssetTransferWorkItem(AssetServiceInterface dest, AssetServiceInterface source, List<UUID> assetids, ReferenceSource refsource)
        {
            m_DestinationAssetService = dest;
            m_SourceAssetService = source;
            m_AssetID = UUID.Zero;
            m_RefSource = refsource;
            m_AssetIDList = new List<UUID>(assetids);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void ProcessAssetTransfer()
        {
            List<UUID> new_assetids = m_AssetIDList;
            List<UUID> current_assetids;
            List<UUID> processed_assetids = new List<UUID>();

            try
            {
                while (new_assetids.Count != 0)
                {
                    current_assetids = new_assetids;
                    new_assetids = new List<UUID>();

                    Dictionary<UUID, bool> exists_assetids = m_DestinationAssetService.Exists(current_assetids);

                    foreach(UUID assetid_new in current_assetids)
                    {
                        if(processed_assetids.Contains(assetid_new))
                        {
                            current_assetids.Remove(assetid_new);
                        }
                    }

                    foreach (UUID assetid in current_assetids)
                    {
                        if(!exists_assetids[assetid])
                        {
                            m_DestinationAssetService.Store(m_SourceAssetService[assetid]);
                        }

                        List<UUID> ref_assetids = m_RefSource == ReferenceSource.Destination ?
                            m_DestinationAssetService.References[assetid] :
                            m_SourceAssetService.References[assetid];

                        foreach (UUID assetid_new in ref_assetids)
                        {
                            if (!new_assetids.Contains(assetid_new))
                            {
                                new_assetids.Add(assetid_new);
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                AssetTransferFailed(e);
                return;
            }
            AssetTransferComplete();
        }

        public abstract void AssetTransferComplete();
        public abstract void AssetTransferFailed(Exception e);
    }
}
