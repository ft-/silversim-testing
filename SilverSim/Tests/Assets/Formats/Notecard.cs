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
using SilverSim.Main.Common;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilverSim.Tests.Assets.Formats
{
    public class NotecardFormat : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool Run()
        {
            UUI theCreator = new UUI();
            Notecard notecard;
            Notecard ncserialized;
            AssetData asset;
            theCreator.HomeURI = new Uri("http://example.com/");
            theCreator.ID = UUID.Random;
            theCreator.FirstName = "The";
            theCreator.LastName = "Creator";

            m_Log.Info("Testing Notecard without Inventory");
            notecard = new Notecard();
            notecard.Text = "The Notecard";

            asset = notecard.Asset();
            ncserialized = new Notecard(asset);

            if (ncserialized.Text != notecard.Text)
            {
                m_Log.Fatal("Notecard Text is not identical");
                return false;
            }

            if (ncserialized.Inventory!= null)
            {
                m_Log.Fatal("Notecard Inventory Count is not identical");
                return false;
            }


            m_Log.Info("Testing Notecard with Inventory");
            notecard = new Notecard();
            notecard.Text = "The Notecard";
            NotecardInventoryItem ncitem = new NotecardInventoryItem();
            ncitem.AssetID = UUID.Random;
            ncitem.AssetType = AssetType.CallingCard;
            ncitem.Creator = theCreator;
            ncitem.Description = "Item Description";
            ncitem.Flags = 1;
            ncitem.Group = UGI.Unknown;
            ncitem.ID = UUID.Random;
            ncitem.InventoryType = InventoryType.CallingCard;
            ncitem.IsGroupOwned = true;
            ncitem.LastOwner = theCreator;
            ncitem.Name = "Item Name";
            ncitem.Owner = theCreator;
            ncitem.ParentFolderID = UUID.Random;
            ncitem.Permissions.Base = InventoryPermissionsMask.All;
            ncitem.Permissions.Current = InventoryPermissionsMask.All;
            ncitem.Permissions.EveryOne = InventoryPermissionsMask.All;
            ncitem.Permissions.Group = InventoryPermissionsMask.All;
            ncitem.Permissions.NextOwner = InventoryPermissionsMask.All;
            ncitem.SaleInfo.Price = 10;
            ncitem.SaleInfo.Type = InventoryItem.SaleInfoData.SaleType.Copy;
            ncitem.ExtCharIndex = 1;
            notecard.Inventory = new NotecardInventory();
            notecard.Inventory.Add(UUID.Random, ncitem);

            asset = notecard.Asset();
            ncserialized = new Notecard(asset);

            if(ncserialized.Text != notecard.Text)
            {
                m_Log.Fatal("Notecard Text is not identical");
                return false;
            }

            if(ncserialized.Inventory.Count != notecard.Inventory.Count)
            {
                m_Log.Fatal("Notecard Inventory Count is not identical");
                return false;
            }

            NotecardInventoryItem ncserializeditem = ncserialized.Inventory[1];
            if(ncitem.AssetID != ncserializeditem.AssetID)
            {
                m_Log.Fatal("Notecard Inventory Item Asset ID is not identical");
                return false;
            }
            if (ncitem.AssetType != ncserializeditem.AssetType)
            {
                m_Log.Fatal("Notecard Inventory Item Asset ID is not identical");
                return false;
            }
            if (ncitem.Creator.ID != ncserializeditem.Creator.ID)
            {
                m_Log.Fatal("Notecard Inventory Item Creator ID is not identical");
                return false;
            }
            if (ncitem.Description != ncserializeditem.Description)
            {
                m_Log.Fatal("Notecard Inventory Item Description is not identical");
                return false;
            }
            if (ncitem.Flags != ncserializeditem.Flags)
            {
                m_Log.Fatal("Notecard Inventory Item Flags is not identical");
                return false;
            }
            if (ncitem.Group.ID != ncserializeditem.Group.ID)
            {
                m_Log.Fatal("Notecard Inventory Item GroupID is not identical");
                return false;
            }
            if (ncitem.ID != ncserializeditem.ID)
            {
                m_Log.Fatal("Notecard Inventory Item ID is not identical");
                return false;
            }
            if (ncitem.InventoryType != ncserializeditem.InventoryType)
            {
                m_Log.Fatal("Notecard Inventory Item InventoryType is not identical");
                return false;
            }
            if (ncitem.LastOwner.ID != ncserializeditem.LastOwner.ID)
            {
                m_Log.Fatal("Notecard Inventory Item LastOwner is not identical");
                return false;
            }
            if (ncitem.ParentFolderID != ncserializeditem.ParentFolderID)
            {
                m_Log.Fatal("Notecard Inventory Item ParentFolderID is not identical");
                return false;
            }
            if (ncitem.Permissions.Base != ncserializeditem.Permissions.Base)
            {
                m_Log.Fatal("Notecard Inventory Item Permissions.Base is not identical");
                return false;
            }
            if (ncitem.Permissions.Current != ncserializeditem.Permissions.Current)
            {
                m_Log.Fatal("Notecard Inventory Item Permissions.Current is not identical");
                return false;
            }
            if (ncitem.Permissions.EveryOne != ncserializeditem.Permissions.EveryOne)
            {
                m_Log.Fatal("Notecard Inventory Item Permissions.EveryOne is not identical");
                return false;
            }
            if (ncitem.Permissions.Group != ncserializeditem.Permissions.Group)
            {
                m_Log.Fatal("Notecard Inventory Item Permissions.Group is not identical");
                return false;
            }
            if (ncitem.Permissions.NextOwner != ncserializeditem.Permissions.NextOwner)
            {
                m_Log.Fatal("Notecard Inventory Item Permissions.NextOwner is not identical");
                return false;
            }
            if (ncitem.SaleInfo.Price != ncserializeditem.SaleInfo.Price)
            {
                m_Log.Fatal("Notecard Inventory Item SaleInfo.Price is not identical");
                return false;
            }
            if (ncitem.SaleInfo.Type != ncserializeditem.SaleInfo.Type)
            {
                m_Log.Fatal("Notecard Inventory Item SaleInfo.Type is not identical");
                return false;
            }

            m_Log.Info("Testing references");
            List<UUID> refs = ncserialized.References;

            if(refs.Count != 1)
            {
                m_Log.Fatal("Notecard Inventory Item Reference count is wrong");
                return false;
            }

            if(!refs.Contains(ncitem.AssetID))
            {
                m_Log.Fatal("Notecard Inventory Item AssetID is not referenced");
                return false;
            }
            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
