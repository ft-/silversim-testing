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
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilverSim.Database.Memory.Asset
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [Description("Memory Asset Backend")]
    public class MemoryAssetService : AssetServiceInterface, IPlugin, IAssetMetadataServiceInterface, IAssetDataServiceInterface
    {
        readonly DefaultAssetReferencesService m_ReferencesService;
        readonly RwLockedDictionary<UUID, AssetData> m_Assets = new RwLockedDictionary<UUID, AssetData>();

        #region Constructor
        public MemoryAssetService()
        {
            m_ReferencesService = new DefaultAssetReferencesService(this);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
        #endregion

        #region Exists methods
        public override bool Exists(UUID key)
        {
            return m_Assets.ContainsKey(key);
        }

        public override Dictionary<UUID, bool> Exists(List<UUID> assets)
        {
            Dictionary<UUID,bool> res = new Dictionary<UUID,bool>();
            foreach(UUID id in assets)
            {
                res[id] = m_Assets.ContainsKey(id);
            }

            return res;
        }

        #endregion

        public override bool IsSameServer(AssetServiceInterface other)
        {
            return other.GetType() == typeof(MemoryAssetService) && other == this;
        }

        #region Accessors
        public override bool TryGetValue(UUID key, out AssetData asset)
        {
            AssetData internalAsset;
            if(m_Assets.TryGetValue(key, out internalAsset))
            {
                internalAsset.CreateTime = Date.Now;
                asset = new AssetData();
                asset.ID = internalAsset.ID;
                asset.Data = new byte[internalAsset.Data.Length];
                Buffer.BlockCopy(internalAsset.Data, 0, asset.Data, 0, internalAsset.Data.Length);
                asset.Type = internalAsset.Type;
                asset.Name = internalAsset.Name;
                asset.CreateTime = internalAsset.CreateTime;
                asset.AccessTime = internalAsset.AccessTime;
                asset.Creator = internalAsset.Creator;
                asset.Flags = internalAsset.Flags;
                asset.Temporary = internalAsset.Temporary;
                return true;
            }
            asset = null;
            return false;
        }

        public override AssetData this[UUID key]
        {
            get
            {
                AssetData asset;
                if(!TryGetValue(key, out asset))
                {
                    throw new AssetNotFoundException(key);
                }
                return asset;
            }
        }

        #endregion

        #region Metadata interface
        public override IAssetMetadataServiceInterface Metadata
        {
            get
            {
                return this;
            }
        }

        AssetMetadata IAssetMetadataServiceInterface.this[UUID key]
        {
            get
            {
                AssetMetadata metadata;
                if (!Metadata.TryGetValue(key, out metadata))
                {
                    throw new AssetNotFoundException(key);
                }
                return metadata;
            }
        }

        bool IAssetMetadataServiceInterface.TryGetValue(UUID key, out AssetMetadata metadata)
        {
            AssetData data;
            if (m_Assets.TryGetValue(key, out data))
            {
                metadata = new AssetMetadata();
                metadata = new AssetData();
                metadata.ID = data.ID;
                metadata.Type = data.Type;
                metadata.Name = data.Name;
                metadata.CreateTime = data.CreateTime;
                metadata.AccessTime = data.AccessTime;
                metadata.Creator = data.Creator;
                metadata.Flags = data.Flags;
                metadata.Temporary = data.Temporary;
                return true;
            }
            else
            {
                metadata = null;
                return false;
            }
        }

        #endregion

        #region References interface
        public override AssetReferencesServiceInterface References
        {
            get
            {
                return m_ReferencesService;
            }
        }
        #endregion

        #region Data interface
        public override IAssetDataServiceInterface Data
        {
            get
            {
                return this;
            }
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        Stream IAssetDataServiceInterface.this[UUID key]
        {
            get
            {
                Stream s;
                if (!Data.TryGetValue(key, out s))
                {
                    throw new AssetNotFoundException(key);
                }
                return s;
            }
        }

        bool IAssetDataServiceInterface.TryGetValue(UUID key, out Stream s)
        {
            AssetData data;
            if (m_Assets.TryGetValue(key, out data))
            {
                s = data.InputStream;
                return true;
            }
            else
            {
                s = null;
                return false;
            }
        }
        #endregion

        #region Store asset method
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override void Store(AssetData asset)
        {
            AssetData internalAsset;
            if(m_Assets.TryGetValue(asset.ID, out internalAsset))
            {
                if(internalAsset.Flags != AssetFlags.Normal)
                {
                    internalAsset = new AssetData();
                    internalAsset.ID = asset.ID;
                    internalAsset.Data = new byte[asset.Data.Length];
                    Buffer.BlockCopy(asset.Data, 0, internalAsset.Data, 0, asset.Data.Length);
                    internalAsset.Type = asset.Type;
                    internalAsset.Name = asset.Name;
                    internalAsset.CreateTime = asset.CreateTime;
                    internalAsset.AccessTime = asset.AccessTime;
                    internalAsset.Creator = asset.Creator;
                    internalAsset.Flags = asset.Flags;
                    internalAsset.Temporary = asset.Temporary;

                    m_Assets[internalAsset.ID] = internalAsset;
                }
            }
            else
            {
                internalAsset = new AssetData();
                internalAsset.ID = asset.ID;
                internalAsset.Data = new byte[asset.Data.Length];
                Buffer.BlockCopy(asset.Data, 0, internalAsset.Data, 0, asset.Data.Length);
                internalAsset.Type = asset.Type;
                internalAsset.Name = asset.Name;
                internalAsset.CreateTime = asset.CreateTime;
                internalAsset.AccessTime = asset.AccessTime;
                internalAsset.Creator = asset.Creator;
                internalAsset.Flags = asset.Flags;
                internalAsset.Temporary = asset.Temporary;

                m_Assets.Add(internalAsset.ID, internalAsset);
            }
        }
        #endregion

        #region Delete asset method
        public override void Delete(UUID id)
        {
            m_Assets.RemoveIf(id, delegate (AssetData d) { return d.Flags != AssetFlags.Normal; });
        }
        #endregion

        private const int MAX_ASSET_NAME = 64;
    }
    #endregion

    #region Factory
    [PluginName("Assets")]
    public class MemoryAssetServiceFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryAssetService();
        }
    }
    #endregion
}
