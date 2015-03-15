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

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public abstract class InventoryFolderServiceInterface
    {
        #region Accessors
        public abstract InventoryFolder this[UUID PrincipalID, UUID key]
        {
            get;
        }

        public abstract InventoryFolder this[UUID PrincipalID, InventoryType type]
        {
            get;
        }

        public abstract List<InventoryFolder> getFolders(UUID PrincipalID, UUID key);
        public abstract List<InventoryItem> getItems(UUID PrincipalID, UUID key);

        public virtual List<InventoryFolder> getInventorySkeleton(UUID PrincipalID)
        {
            throw new NotSupportedException(GetType().FullName + ": getInventorySkeleton not supported");
        }
        #endregion

        #region Methods
        public abstract void Add(InventoryFolder folder);
        public abstract void Update(InventoryFolder folder);
        public abstract void Move(UUID PrincipalID, UUID folderID, UUID toFolderID);
        public abstract void Delete(UUID PrincipalID, UUID folderID);
        public abstract void Purge(UUID PrincipalID, UUID folderID);
        public abstract void IncrementVersion(UUID PrincipalID, UUID folderID);
        public virtual List<UUID> Delete(UUID PrincipalID, List<UUID> folderIDs)
        {
            List<UUID> deleted = new List<UUID>();
            foreach(UUID folderID in folderIDs)
            {
                try
                {
                    Delete(PrincipalID, folderID);
                    deleted.Add(folderID);
                }
                catch
                {

                }
            }
            return deleted;
        }
        #endregion

        #region Constructor
        public InventoryFolderServiceInterface()
        {
        }
        #endregion
    }
}
