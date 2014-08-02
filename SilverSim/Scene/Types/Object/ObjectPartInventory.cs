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
using ThreadedClasses;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectPartInventory : RwLockedSortedDoubleDictionary<UUID, string, ObjectPartInventoryItem>
    {
        public delegate void OnChangeDelegate();
        public event OnChangeDelegate OnChange;

        public ObjectPartInventory()
        {
        }

        #region LSL style accessors
        public ObjectPartInventoryItem this[InventoryType type, uint index]
        {
            get
            {
                foreach (ObjectPartInventoryItem item in ValuesByKey2)
                {
                    if(type == item.InventoryType)
                    {
                        if(index-- == 0)
                        {
                            return item;
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        public ObjectPartInventoryItem this[uint index]
        {
            get
            {
                foreach (ObjectPartInventoryItem item in ValuesByKey2)
                {
                    if (index-- == 0)
                    {
                        return item;
                    }
                }
                throw new KeyNotFoundException();
            }
        }
        #endregion

        #region Count specific types
        public int CountType(InventoryType type)
        {
            int n = 0;
            foreach(ObjectPartInventoryItem item in this.Values)
            {
                if(item.InventoryType == type)
                {
                    ++n;
                }
            }

            return n;
        }

        public int CountScripts()
        {
            int n = 0;
            foreach (ObjectPartInventoryItem item in this.Values)
            {
                if (item.InventoryType == InventoryType.LSLText || item.InventoryType == InventoryType.LSLBytecode)
                {
                    ++n;
                }
            }

            return n;
        }

        #endregion

        #region Overrides
        public new void Add(UUID key1, string key2, ObjectPartInventoryItem item)
        {
            base.Add(key1, key2, item);
            
            var addDelegate = OnChange;
            if(addDelegate != null)
            {
                foreach (OnChangeDelegate d in addDelegate.GetInvocationList())
                {
                    d();
                }
            }
        }

        public new void ChangeKey(string newKey, string oldKey)
        {
            base.ChangeKey(newKey, oldKey);

            var updateDelegate = OnChange;
            if(updateDelegate != null)
            {
                foreach (OnChangeDelegate d in updateDelegate.GetInvocationList())
                {
                    d();
                }
            }
        }

        public new bool Remove(UUID key1)
        {
            if (base.Remove(key1))
            {
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (OnChangeDelegate d in updateDelegate.GetInvocationList())
                    {
                        d();
                    }
                }
                return true;
            }
            return false;
        }

        public new bool Remove(string key2)
        {
            if (base.Remove(key2))
            {
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (OnChangeDelegate d in updateDelegate.GetInvocationList())
                    {
                        d();
                    }
                }
                return true;
            }
            return false;
        }

        public new bool Remove(UUID key1, string key2)
        {
            if (base.Remove(key1, key2))
            {
                var updateDelegate = OnChange;
                if (updateDelegate != null)
                {
                    foreach (OnChangeDelegate d in updateDelegate.GetInvocationList())
                    {
                        d();
                    }
                }
                return true;
            }
            return false;
        }
        #endregion
    }
}
