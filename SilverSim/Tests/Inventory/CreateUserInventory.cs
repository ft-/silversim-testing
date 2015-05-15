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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Reflection;

namespace SilverSim.Tests.Inventory
{
    public class CreateUserInventory : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        InventoryServiceInterface m_InventoryService;
        UUID m_UserID;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            m_InventoryService = loader.GetService<InventoryServiceInterface>(config.GetString("InventoryService"));
            m_UserID = config.GetString("UserID");
        }

        public bool Run()
        {
            AssetType[] foldersToTest = new AssetType[] 
            {
                AssetType.RootFolder,
                AssetType.Animation,
                AssetType.Bodypart,
                AssetType.CallingCard,
                AssetType.Clothing,
                AssetType.Gesture,
                AssetType.Landmark,
                AssetType.LostAndFoundFolder,
                AssetType.Notecard,
                AssetType.Object,
                AssetType.SnapshotFolder,
                AssetType.LSLText,
                AssetType.Sound,
                AssetType.Texture,
                AssetType.TrashFolder,
                AssetType.CurrentOutfitFolder,
                AssetType.MyOutfitsFolder,
                AssetType.FavoriteFolder
            };
            m_Log.Info("Testing non-existence of folders");
            InventoryFolder folder;
            foreach (AssetType type in foldersToTest)
            {
                try
                {
                    m_Log.InfoFormat("{0}...", type.ToString());
                    folder = m_InventoryService.Folder[m_UserID, type];
                    return false;
                }
                catch
                {

                }
            }

            m_Log.Info("Create User Inventory");
            try
            {
                m_InventoryService.checkInventory(m_UserID);
            }
            catch
            {
                return false;
            }

            m_Log.Info("Testing existence of created folders");
            foreach (AssetType type in foldersToTest)
            {
                try
                {
                    m_Log.InfoFormat("{0}...", type.ToString());
                    folder = m_InventoryService.Folder[m_UserID, type];
                }
                catch
                {
                    return false;
                }
            }

            m_Log.Info("Testing IncrementVersion of RootFolder");
            folder = m_InventoryService.Folder[m_UserID, AssetType.RootFolder];
            int oldVersion = folder.Version;
            if(oldVersion == 1)
            {
                m_Log.Info("Expected a non-zero version due to checkInventory");
                return false;
            }
            m_InventoryService.Folder.IncrementVersion(m_UserID, folder.ID);

            m_Log.Info("Testing IncrementVersion result of RootFolder");
            folder = m_InventoryService.Folder[m_UserID, AssetType.RootFolder];
            if (oldVersion + 1 != folder.Version)
            {
                m_Log.InfoFormat("Expected an incremented version due to checkInventory (old {0} new {1})", oldVersion, folder.Version);
                return false;
            }

            UUID[] folders = new UUID[] 
            {
                UUID.Random,
                UUID.Random,
                UUID.Random,
                UUID.Random,
            };

            UUID rootFolderID = folder.ID;

            m_Log.Info("Creating 4 folders");
            foreach(UUID folderid in folders)
            {
                folder = new InventoryFolder();
                folder.ID = folderid;
                folder.Version = 16;
                folder.InventoryType = InventoryType.Unknown;
                folder.Owner.ID = m_UserID;
                folder.Name = "A " + folderid.ToString();
                folder.ParentFolderID = rootFolderID;
                try
                {
                    m_InventoryService.Folder.Add(folder);
                }
                catch(Exception e)
                {
                    m_Log.Warn("Failed to create folder", e);
                    return false;
                }
            }

            m_Log.Info("Testing existence of new folders");
            foreach (UUID folderid in folders)
            {
                try
                {
                    folder = m_InventoryService.Folder[m_UserID, folderid];
                }
                catch (Exception e)
                {
                    m_Log.Warn("Failed to find folder", e);
                    return false;
                }
                if (folder.ParentFolderID != rootFolderID)
                {
                    m_Log.WarnFormat("Parent folder does not match of folder {0}", folderid);
                    return false;
                }
                if (folder.Name != "A " + folderid.ToString())
                {
                    m_Log.WarnFormat("Name does not match of folder {0}", folderid);
                    return false;
                }
                if (folder.Owner.ID != m_UserID)
                {
                    m_Log.WarnFormat("OwnerID does not match of folder {0}", folderid);
                    return false;
                }
                if (folder.InventoryType != InventoryType.Unknown)
                {
                    m_Log.WarnFormat("InventoryType does not match of folder {0}", folderid);
                    return false;
                }
                if (folder.Version != 16)
                {
                    m_Log.WarnFormat("Version does not match of folder {0}", folderid);
                    return false;
                }
            }

            return true;
        }
    }
}
