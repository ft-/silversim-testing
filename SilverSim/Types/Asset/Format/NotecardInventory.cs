// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.Types.Asset.Format
{
    public class NotecardInventory : RwLockedDictionary<UUID, NotecardInventoryItem>
    {
        #region Constructor
        public NotecardInventory()
        {

        }
        #endregion

        #region ExtCharIndex Access
        public NotecardInventoryItem this[uint ExtCharIndex]
        {
            get
            {
                try
                {
                    ForEach(delegate(NotecardInventoryItem item)
                    {
                        if(item.ExtCharIndex == ExtCharIndex)
                        {
                            throw new ReturnValueException<NotecardInventoryItem>(item);
                        }
                    });
                }
                catch(ReturnValueException<NotecardInventoryItem> e)
                {
                    return e.Value;
                }
                throw new KeyNotFoundException("ExtCharIndex " + ExtCharIndex.ToString() + " not found");
            }
        }
        #endregion
    }
}
