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

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Main.Common.Transfer
{
    public abstract class AssetTransferWorkItem
    {
        AssetServiceInterface m_DestinationAssetService;
        AssetServiceInterface m_SourceAssetService;
        UUID m_AssetID;

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

                    Dictionary<UUID, bool> exists_assetids = m_DestinationAssetService.exists(current_assetids);

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
