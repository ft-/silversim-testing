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
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Inventory
{
    public abstract class InventoryItemServiceInterface
    {
        #region Accessors
        /* DO NOT USE this[UUID key] anywhere else than in a Robust Inventory handler 
         * Not all connectors / services support this access.
         */
        public abstract InventoryItem this[UUID key]
        {
            get;
        }

        public abstract InventoryItem this[UUID PrincipalID, UUID key]
        {
            get;
        }

        public virtual List<InventoryItem> this[UUID principalID, List<UUID> itemids]
        {
            get
            {
                List<InventoryItem> items = new List<InventoryItem>();
                foreach(UUID itemid in itemids)
                {
                    try
                    {
                        items.Add(this[principalID, itemid]);
                    }
                    catch
                    {

                    }
                }
                return items;
            }
        }

        #endregion

        public abstract void Add(InventoryItem item);
        public abstract void Update(InventoryItem item);

        public abstract void Delete(UUID PrincipalID, UUID ID);
        public abstract void Move(UUID PrincipalID, UUID ID, UUID newFolder);

        public virtual List<UUID> Delete(UUID PrincipalID, List<UUID> IDs)
        {
            List<UUID> deleted = new List<UUID>();
            foreach(UUID id in IDs)
            {
                try
                {
                    Delete(PrincipalID, id);
                    deleted.Add(id);
                }
                catch
                {

                }
            }
            return deleted;
        }

        #region Constructor
        public InventoryItemServiceInterface()
        {

        }
        #endregion
    }
}
