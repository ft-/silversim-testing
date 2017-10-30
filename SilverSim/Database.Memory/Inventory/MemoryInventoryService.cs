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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SilverSim.Database.Memory.Inventory
{
    [Description("Memory Inventory Backend")]
    [PluginName("Inventory")]
    public sealed partial class MemoryInventoryService : InventoryServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly DefaultInventoryFolderContentService m_ContentService;

        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, InventoryFolder>> m_Folders = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, InventoryFolder>>(() => new RwLockedDictionary<UUID, InventoryFolder>());
        internal readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, InventoryItem>> m_Items = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, InventoryItem>>(() => new RwLockedDictionary<UUID, InventoryItem>());

        public MemoryInventoryService()
        {
            m_ContentService = new DefaultInventoryFolderContentService(this);
        }

        IInventoryFolderContentServiceInterface IInventoryFolderServiceInterface.Content => m_ContentService;

        public override IInventoryFolderServiceInterface Folder => this;

        public override IInventoryItemServiceInterface Item => this;

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
}
