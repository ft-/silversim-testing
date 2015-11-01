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
        AssetServiceInterface m_DestinationAssetService;
        AssetServiceInterface m_SourceAssetService;
        protected UUID m_AssetID { get; private set; }

        public enum ReferenceSource
        {
            Source,
            Destination
        }

        ReferenceSource m_RefSource;

        public AssetTransferWorkItem(AssetServiceInterface dest, AssetServiceInterface source, UUID assetid, ReferenceSource refsource)
        {
            m_DestinationAssetService = dest;
            m_SourceAssetService = source;
            m_AssetID = assetid;
            m_RefSource = refsource;
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void ProcessAssetTransfer()
        {
            List<UUID> new_assetids = new List<UUID>();;
            List<UUID> current_assetids;
            List<UUID> processed_assetids = new List<UUID>();

            new_assetids.Add(m_AssetID);

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
                        List<UUID> ref_assetids;
                        if (m_RefSource == ReferenceSource.Destination)
                        {
                            ref_assetids = m_DestinationAssetService.References[assetid];
                        }
                        else
                        {
                            ref_assetids = m_SourceAssetService.References[assetid];
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
