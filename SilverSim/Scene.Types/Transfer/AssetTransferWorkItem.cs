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
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scene.Types.Transfer
{
    public abstract class AssetTransferWorkItem
    {
        private static readonly ILog m_Log = LogManager.GetLogger("ASSET TRANSFER");

        private readonly AssetServiceInterface m_DestinationAssetService;
        private readonly AssetServiceInterface m_SourceAssetService;
        protected UUID AssetID { get; }
        private readonly List<UUID> m_AssetIDList;

        public enum ReferenceSource
        {
            Source,
            Destination
        }

        private readonly ReferenceSource m_RefSource;

        protected AssetTransferWorkItem(AssetServiceInterface dest, AssetServiceInterface source, UUID assetid, ReferenceSource refsource)
        {
            m_DestinationAssetService = dest;
            m_SourceAssetService = source;
            AssetID = assetid;
            m_RefSource = refsource;
            m_AssetIDList = new List<UUID>
            {
                assetid
            };
        }

        protected AssetTransferWorkItem(AssetServiceInterface dest, AssetServiceInterface source, List<UUID> assetids, ReferenceSource refsource)
        {
            m_DestinationAssetService = dest;
            m_SourceAssetService = source;
            AssetID = UUID.Zero;
            m_RefSource = refsource;
            m_AssetIDList = new List<UUID>(assetids);
        }

        public void ProcessAssetTransfer()
        {
            List<UUID> new_assetids = m_AssetIDList;
            List<UUID> current_assetids;
            var processed_assetids = new List<UUID>();

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
                            try
                            {
                                m_DestinationAssetService.Store(m_SourceAssetService[assetid]);
                            }
                            catch(AssetNotFoundException)
                            {
                                /* ignore this one since there is simply too many of incomplete items */
                            }
                        }

                        List<UUID> ref_assetids = m_RefSource == ReferenceSource.Destination ?
                            m_DestinationAssetService.References[assetid] :
                            m_SourceAssetService.References[assetid];

                        foreach (UUID assetid_new in ref_assetids)
                        {
                            if (!new_assetids.Contains(assetid_new) &&
                                !processed_assetids.Contains(assetid_new))
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

        private static void HandleWorkItem(object o)
        {
            var wi = (AssetTransferWorkItem)o;
            try
            {
                wi.ProcessAssetTransfer();
            }
            catch(Exception e)
            {
                m_Log.Error("Unhandled exception at ProcessAssetTransfer", e);
            }
        }

        public void QueueWorkItem()
        {
            ThreadPool.UnsafeQueueUserWorkItem(HandleWorkItem, this);
        }
    }
}
