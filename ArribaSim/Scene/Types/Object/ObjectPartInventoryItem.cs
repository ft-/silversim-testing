/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Scene.Types.Script;
using ArribaSim.Types;
using ArribaSim.Types.Inventory;
using System;

namespace ArribaSim.Scene.Types.Object
{
    public class ObjectPartInventoryItem : InventoryItem, IDisposable
    {
        #region Constructors
        public ObjectPartInventoryItem()
        {

        }

        public ObjectPartInventoryItem(InventoryItem item)
        {
            AssetID = new UUID(item.AssetID);
            AssetType = item.AssetType;
            CreationDate = item.CreationDate;
            Creator = new UUI(item.Creator);
            Description = item.Description;
            Flags = item.Flags;
            GroupID = new UUID(item.GroupID);
            GroupOwned = item.GroupOwned;
            ID = new UUID(item.ID);
            InventoryType = item.InventoryType;
            LastOwner = new UUI(item.LastOwner);
            Name = item.Name;
            Owner = new UUI(item.Owner);
            ParentFolderID = new UUID(item.ParentFolderID);
            Permissions = item.Permissions;
            SaleInfo = item.SaleInfo;
        }
        #endregion

        #region Fields
        private IScriptInstance m_ScriptInstance = null;
        #endregion

        #region Properties
        public IScriptInstance ScriptInstance
        {
            get
            {
                return m_ScriptInstance;
            }
            set
            {
                lock(this)
                {
                    m_ScriptInstance.Dispose();
                    m_ScriptInstance = value;
                    if(m_ScriptInstance != null)
                    {
                        m_ScriptInstance.StartScript();
                    }
                }
            }
        }
        #endregion

        public void Dispose()
        {
            lock(this)
            {
                m_ScriptInstance.Dispose();
            }
        }
    }
}
