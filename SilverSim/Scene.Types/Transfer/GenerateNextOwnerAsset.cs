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

using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Transfer
{
    public static class GenerateNextOwnerAssetFunctions
    {
        public static UUID GenerateNextOwnerAsset(this AssetServiceInterface assetService, UUID firstLevelAssetID)
        {
            var assetIDs = new List<UUID>();
            var replaceAssets = new Dictionary<UUID, UUID>();
            var objectAssetIDs = new List<UUID>();
            assetIDs.Add(firstLevelAssetID);
            int pos = 0;
            while (pos < assetIDs.Count)
            {
                UUID assetid = assetIDs[pos++];
                AssetMetadata objmeta;
                if (assetService.Metadata.TryGetValue(assetid, out objmeta))
                {
                    if (objmeta.Type == AssetType.Object)
                    {
                        if (!objectAssetIDs.Contains(assetid))
                        {
                            objectAssetIDs.Add(assetid);
                        }
                        assetIDs.AddRange(ObjectReferenceDecoder.GetReferences(assetService[assetid]));
                        if (!replaceAssets.ContainsKey(assetid))
                        {
                            replaceAssets.Add(assetid, UUID.Random);
                        }
                    }
                    else if (objmeta.Type == AssetType.Notecard)
                    {
                        if (!objectAssetIDs.Contains(assetid))
                        {
                            objectAssetIDs.Add(assetid);
                        }
                        var nc = new Notecard(assetService[assetid]);
                        foreach (NotecardInventoryItem item in nc.Inventory.Values)
                        {
                            if ((item.AssetType == AssetType.Object || item.AssetType == AssetType.Notecard) &&
                                !objectAssetIDs.Contains(item.AssetID))
                            {
                                objectAssetIDs.Add(item.AssetID);
                            }
                        }
                        assetIDs.InsertRange(0, nc.References);
                        if (!replaceAssets.ContainsKey(assetid))
                        {
                            replaceAssets.Add(assetid, UUID.Random);
                        }
                    }
                }
            }

            objectAssetIDs.Reverse();
            foreach (UUID objectid in objectAssetIDs)
            {
                AssetData data;
                AssetData newAsset;
                if (assetService.TryGetValue(objectid, out data))
                {
                    switch (data.Type)
                    {
                        case AssetType.Object:
                            List<ObjectGroup> grps = ObjectXML.FromAsset(data, UUI.Unknown);
                            foreach (ObjectGroup grp in grps)
                            {
                                foreach (ObjectPart part in grp.Values)
                                {
                                    foreach (ObjectPartInventoryItem item in part.Inventory.Values)
                                    {
                                        if (item.NextOwnerAssetID == UUID.Zero)
                                        {
                                            UUID replaceAssetID;
                                            part.Inventory.SetNextOwnerAssetID(
                                                item.ID,
                                                replaceAssets.TryGetValue(item.AssetID, out replaceAssetID) ? replaceAssetID : item.AssetID);
                                        }
                                    }
                                }
                            }

                            newAsset = (grps.Count == 1) ?
                                grps[0].Asset(UUI.Unknown, XmlSerializationOptions.AdjustForNextOwner | XmlSerializationOptions.WriteXml2) :
                                grps.Asset(UUI.Unknown, XmlSerializationOptions.AdjustForNextOwner | XmlSerializationOptions.WriteXml2);

                            newAsset.ID = replaceAssets[objectid];
                            newAsset.CreateTime = data.CreateTime;
                            assetService.Store(newAsset);
                            break;

                        case AssetType.Notecard:
                            var nc = new Notecard(data);
                            foreach (NotecardInventoryItem item in nc.Inventory.Values)
                            {
                                UUID replace;
                                if (replaceAssets.TryGetValue(item.AssetID, out replace))
                                {
                                    item.AssetID = replace;
                                }
                                break;
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            UUID finalAssetID;
            return replaceAssets.TryGetValue(firstLevelAssetID, out finalAssetID) ? finalAssetID : firstLevelAssetID;
        }

        public static UUID GenerateNextOwnerAssets(this AssetServiceInterface assetService, ObjectGroup grp)
        {
            foreach(ObjectPart part in grp.Values)
            {
                foreach(ObjectPartInventoryItem item in part.Inventory.Values)
                {
                    if(item.NextOwnerAssetID == UUID.Zero)
                    {
                        part.Inventory.SetNextOwnerAssetID(
                            item.ID,
                            assetService.GenerateNextOwnerAsset(item.AssetID));
                    }
                }
            }

            AssetData data = grp.Asset(UUI.Unknown, XmlSerializationOptions.AdjustForNextOwner | XmlSerializationOptions.WriteXml2);
            data.ID = UUID.Random;
            assetService.Store(data);
            grp.NextOwnerAssetID = data.ID;
            return data.ID;
        }
    }
}
