// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SilverSim.Database.Memory.Inventory
{
    #region Service Implementation
    [Description("Memory Inventory Backend")]
    public sealed partial class MemoryInventoryService : InventoryServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly DefaultInventoryFolderContentService m_ContentService;

        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, InventoryFolder>> m_Folders;
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, InventoryItem>> m_Items;

        public MemoryInventoryService()
        {
            m_ContentService = new DefaultInventoryFolderContentService(this);
            m_Folders = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, InventoryFolder>>(delegate () { return new RwLockedDictionary<UUID, InventoryFolder>(); });
            m_Items = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, InventoryItem>>(delegate () { return new RwLockedDictionary<UUID, InventoryItem>(); });
        }

        IInventoryFolderContentServiceInterface IInventoryFolderServiceInterface.Content
        {
            get
            {
                return m_ContentService;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override IInventoryFolderServiceInterface Folder
        {
            get
            {
                return this;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override IInventoryItemServiceInterface Item
        {
            get 
            {
                return this;
            }
        }

        public override List<InventoryItem> GetActiveGestures(UUID principalID)
        {
            RwLockedDictionary<UUID, InventoryItem> agentitems;
            return (m_Items.TryGetValue(principalID, out agentitems)) ?
                new List<InventoryItem>(from item in agentitems.Values where item.AssetType == AssetType.Gesture && (item.Flags & InventoryFlags.GestureActive) != 0 select new InventoryItem(item)) :
                new List<InventoryItem>();
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }

        public override void Remove(UUID scopeID, UUID userAccount)
        {
            m_Items.Remove(userAccount);
            m_Folders.Remove(userAccount);
        }
    }
    #endregion

    #region Factory
    [PluginName("Inventory")]
    public class MemoryInventoryServiceFactory : IPluginFactory
    {
        public MemoryInventoryServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryInventoryService();
        }
    }
    #endregion
}
