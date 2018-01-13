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

#pragma warning disable IDE0018

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class NewFileAgentInventoryVariablePrice : UploadAssetAbstractCapability
    {
        private readonly InventoryServiceInterface m_InventoryService;
        private readonly AssetServiceInterface m_AssetService;
        private readonly ViewerAgent m_Agent;

        private readonly RwLockedDictionary<UUID, InventoryItem> m_Transactions = new RwLockedDictionary<UUID, InventoryItem>();

        public override string CapabilityName => "NewFileAgentInventoryVariablePrice";

        public override int ActiveUploads => m_Transactions.Count;

        public NewFileAgentInventoryVariablePrice(ViewerAgent agent, string serverURI, string remoteip)
            : base(agent.Owner, serverURI, remoteip)
        {
            m_Agent = agent;
            m_InventoryService = agent.InventoryService;
            m_AssetService = agent.AssetService;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            var transaction = UUID.Random;
            var item = new InventoryItem
            {
                Description = reqmap["description"].ToString(),
                Name = reqmap["name"].ToString(),
                ParentFolderID = reqmap["folder_id"].AsUUID,
                AssetTypeName = reqmap["asset_type"].ToString(),
                InventoryTypeName = reqmap["inventory_type"].ToString(),
                LastOwner = Creator,
                Owner = Creator,
                Creator = Creator
            };
            item.Permissions.Base = InventoryPermissionsMask.All;
            item.Permissions.Current = InventoryPermissionsMask.Every;
            item.Permissions.EveryOne = (InventoryPermissionsMask)reqmap["everyone_mask"].AsUInt;
            item.Permissions.Group = (InventoryPermissionsMask)reqmap["group_mask"].AsUInt;
            item.Permissions.NextOwner = (InventoryPermissionsMask)reqmap["next_owner_mask"].AsUInt;
            m_Transactions.Add(transaction, item);
            return transaction;
        }

        public override Map UploadedData(UUID transactionID, AssetData data)
        {
            KeyValuePair<UUID, InventoryItem> kvp;
            if (m_Transactions.RemoveIf(transactionID, (InventoryItem v) => true, out kvp))
            {
                var m = new Map
                {
                    { "new_inventory_item", kvp.Value.ID.ToString() }
                };
                kvp.Value.AssetID = data.ID;
                data.Type = kvp.Value.AssetType;
                data.Name = kvp.Value.Name;

                if (kvp.Value.AssetType == AssetType.Object)
                {
                    /* special upload format for objects */
                    UploadObject(transactionID, data);
                }

                m.Add("new_asset", data.ID);
                try
                {
                    m_AssetService.Store(data);
                }
                catch
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreAsset", "Failed to store asset"));
                }

                try
                {
                    m_InventoryService.Item.Add(kvp.Value);
                }
                catch
#if DEBUG
                (Exception e)
#endif
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreNewInventoryItem", "Failed to store new inventory item"));
                }
                return m;
            }
            else
            {
                throw new UrlNotFoundException();
            }
        }

        private void UploadObject(UUID transactionID, AssetData data)
        {
            var m = (Map)LlsdXml.Deserialize(data.InputStream);
            var instance_list = (AnArray)m["instance_list"];
            var mesh_list = (AnArray)m["mesh_list"];
            var texture_list = (AnArray)m["texture_list"];

            var textureids = new List<UUID>();
            var primrots = new Quaternion[instance_list.Count];
            var primpositions = new Vector3[instance_list.Count];
            var primscales = new Vector3[instance_list.Count];

            using (var objectms = new MemoryStream())
            {
                using(var writer = objectms.UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("SceneObjectGroup");
                    /* we serialize the assets straight away, no allocating SOG in between here. Allocating a SOG would just take senselessly a lot of time and memory. */
                    if (texture_list.Count > 0)
                    {
                        var textureFolder = m_InventoryService.Folder[Creator.ID, AssetType.Texture].ID;
                        var folder = new InventoryFolder
                        {
                            Name = data.Name + " - Textures",
                            Owner = Creator,
                            DefaultType = AssetType.Unknown,
                            ParentFolderID = textureFolder,
                            Version = 1
                        };
                        m_InventoryService.Folder.Add(folder);

                        int idx = 0;
                        foreach(var iv in texture_list)
                        {
                            ++idx;

                            var newasset = new AssetData
                            {
                                ID = UUID.Random,
                                Type = AssetType.Texture,
                                Data = (BinaryData)iv,
                                Name = data.Name + " - Texture " + idx.ToString()
                            };
                            textureids.Add(newasset.ID);
                            m_AssetService.Store(newasset);

                            var item = new InventoryItem
                            {
                                AssetID = newasset.ID,
                                AssetType = AssetType.Texture,
                                Creator = Creator,
                                InventoryType = InventoryType.Texture,
                                LastOwner = Creator,
                                Name = data.Name + " - Texture " + idx.ToString(),
                                Owner = Creator,
                                ParentFolderID = folder.ID
                            };
                            item.Permissions.Base = InventoryPermissionsMask.All;
                            item.Permissions.Current = InventoryPermissionsMask.Every;
                            item.Permissions.EveryOne = InventoryPermissionsMask.None;
                            item.Permissions.Group = InventoryPermissionsMask.All;
                            item.Permissions.NextOwner = InventoryPermissionsMask.All;
                            item.SaleInfo.Price = 10;
                            item.SaleInfo.Type = InventoryItem.SaleInfoData.SaleType.NoSale;
                            m_InventoryService.Item.Add(item);
                        }
                    }

                    if (instance_list.Count > 0)
                    {
                        int idx;
                        for (idx = 0; idx < instance_list.Count; ++idx)
                        {
                            var inner_instance = (Map)instance_list[idx];
                            primpositions[idx] = inner_instance["position"].AsVector3;
                            primscales[idx] = inner_instance["scale"].AsVector3;
                            primrots[idx] = inner_instance["rotation"].AsQuaternion;
                        }

                        var rootRotConjugated = Quaternion.Inverse(primrots[0]);
                        for(idx = 1; idx < primscales.Length; ++idx)
                        {
                            primpositions[idx] -= primpositions[0];
                            primrots[idx] = (rootRotConjugated * primrots[idx]) * rootRotConjugated;
                        }
                    }

                    if (mesh_list.Count > 0)
                    {
                        var wroteOtherParts = false;
                        for (int idx = 0; idx < mesh_list.Count; ++idx)
                        {
                            UUID meshassetid;
                            if (0 == idx)
                            {
                                writer.WriteStartElement("RootPart");
                            }
                            else
                            {
                                if (!wroteOtherParts)
                                {
                                    writer.WriteStartElement("OtherParts");
                                    wroteOtherParts = true;
                                }
                                writer.WriteStartElement("Part");
                            }

                            {
                                using (var meshstream = new MemoryStream())
                                {
                                    Map meshData;
                                    /* add the version tag */
                                    using (var inputstream = new MemoryStream((BinaryData)mesh_list[idx]))
                                    {
                                        meshData = (Map)LlsdBinary.Deserialize(inputstream);
                                        meshData["version"] = new Integer(1);
                                        LlsdBinary.Serialize(meshData, meshstream);
                                        inputstream.CopyTo(meshstream);
                                    }
                                    meshstream.Flush();
                                    var newasset = new AssetData
                                    {
                                        ID = UUID.Random,
                                        Type = AssetType.Mesh,
                                        Data = meshstream.ToArray(),
                                        Name = data.Name + " - Mesh " + (idx + 1).ToString()
                                    };
                                    meshassetid = newasset.ID;
                                    m_AssetService.Store(newasset);
                                }

                                var texentry = new TextureEntry();
                                texentry.DefaultTexture.TextureID = TextureEntry.WHITE_TEXTURE;
                                var inner_instance = (Map)instance_list[idx];
                                var face_list = (AnArray)(inner_instance["face_list"]);
                                for (uint faceidx = 0; faceidx < face_list.Count; ++faceidx)
                                {
                                    var faceMap = (Map)(face_list[(int)faceidx]);
                                    var face = texentry[faceidx];
                                    if (faceMap.ContainsKey("fullbright"))
                                    {
                                        face.FullBright = faceMap["fullbright"].AsBoolean;
                                    }
                                    if (faceMap.ContainsKey("diffuse_color"))
                                    {
                                        var color4 = (AnArray)faceMap["diffuse_color"];
                                        if (color4.Count == 4)
                                        {
                                            face.TextureColor = new ColorAlpha
                                            {
                                                R = color4[0].AsReal,
                                                G = color4[1].AsReal,
                                                B = color4[2].AsReal,
                                                A = color4[3].AsReal
                                            };
                                        }
                                    }
                                    int textureNum = faceMap["image"].AsInt;
                                    var imagerot = (float)faceMap["imagerot"].AsReal;
                                    var offsets = (float)faceMap["offsets"].AsReal;
                                    var offsett = (float)faceMap["offsett"].AsReal;
                                    var scales = (float)faceMap["scales"].AsReal;
                                    var scalet = (float)faceMap["scalet"].AsReal;

                                    face.Rotation = imagerot;
                                    face.OffsetU = offsets;
                                    face.OffsetV = offsett;
                                    face.RepeatU = scales;
                                    face.RepeatV = scalet;

                                    face.TextureID = (textureids.Count > textureNum) ?
                                        textureids[textureNum] :
                                        TextureEntry.WHITE_TEXTURE;
                                }
                                WritePart(writer, data.Name, primpositions[idx], primscales[idx], primrots[idx], meshassetid, texentry, idx + 1, data.CreateTime);
                            }
                            writer.WriteEndElement(); /* RootPart/Part */
                        }
                        if(!wroteOtherParts)
                        {
                            writer.WriteStartElement("OtherParts");
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.Flush();
                    objectms.Flush();
                    data.Data = objectms.ToArray();
                    data.Type = AssetType.Object;
                }
            }
        }

        private void WritePart(
            XmlTextWriter writer,
            string assetName,
            Vector3 position, Vector3 scale, Quaternion rotation,
            UUID meshID, TextureEntry te, int linknumber,
            Date creationDate)
        {
            writer.WriteStartElement("SceneObjectPart");
            {
                writer.WriteNamedValue("AllowedDrop", "false");
                writer.WriteUUID("CreatorID", Creator.ID);
                writer.WriteNamedValue("CreatorData", Creator.CreatorData);
                writer.WriteNamedValue("InventorySerial", 1);
                writer.WriteUUID("UUID", UUID.Random);
                writer.WriteNamedValue("LocalId", linknumber);
                writer.WriteNamedValue("Name", assetName);
                writer.WriteNamedValue("Material", 3);
                writer.WriteNamedValue("PassTouch", 0);
                writer.WriteNamedValue("PassCollisions", 0);
                writer.WriteNamedValue("ScriptAccessPin", 0);
                writer.WriteNamedValue("GroupPosition", position);
                writer.WriteNamedValue("OffsetPosition", position);
                writer.WriteNamedValue("RotationOffset", rotation);
                writer.WriteNamedValue("Velocity", Vector3.Zero);
                writer.WriteNamedValue("AngularVelocity", Vector3.Zero);
                writer.WriteNamedValue("Acceleration", Vector3.Zero);
                writer.WriteNamedValue("Description", string.Empty);
                writer.WriteNamedValue("Color", new ColorAlpha(1, 1, 1, 1));
                writer.WriteNamedValue("Text", string.Empty);
                writer.WriteNamedValue("SitName", string.Empty);
                writer.WriteNamedValue("TouchName", string.Empty);
                writer.WriteNamedValue("LinkNum", linknumber);
                writer.WriteNamedValue("ClickAction", 0);
                writer.WriteStartElement("Shape");
                {
                    writer.WriteNamedValue("ProfileCurve", 1);
                    writer.WriteNamedValue("TextureEntry", Convert.ToBase64String(te.GetBytes()));
                    var extraParams = new byte[1 + 4 + 2 + 17];
                    extraParams[0] = 1;
                    extraParams[1] = 0x30;
                    extraParams[2] = 0;
                    extraParams[3] = 17;
                    extraParams[4] = 0;
                    extraParams[5] = 0;
                    extraParams[6] = 0;
                    meshID.ToBytes(extraParams, 7);
                    extraParams[7 + 16] = (int)PrimitiveSculptType.Mesh;
                    writer.WriteNamedValue("ExtraParams", Convert.ToBase64String(extraParams));
                    writer.WriteNamedValue("PathBegin", 0);
                    writer.WriteNamedValue("PathCurve", 16);
                    writer.WriteNamedValue("PathEnd", 0);
                    writer.WriteNamedValue("PathRadiusOffset", 0);
                    writer.WriteNamedValue("PathRevolutions", 0);
                    writer.WriteNamedValue("PathScaleX", 100);
                    writer.WriteNamedValue("PathScaleY", 100);
                    writer.WriteNamedValue("PathShearX", 0);
                    writer.WriteNamedValue("PathShearY", 0);
                    writer.WriteNamedValue("PathSkew", 0);
                    writer.WriteNamedValue("PathTaperX", 0);
                    writer.WriteNamedValue("PathTaperY", 0);
                    writer.WriteNamedValue("PathTwist", 0);
                    writer.WriteNamedValue("PathTwistBegin", 0);
                    writer.WriteNamedValue("PCode", 9);
                    writer.WriteNamedValue("ProfileBegin", 0);
                    writer.WriteNamedValue("ProfileEnd", 0);
                    writer.WriteNamedValue("ProfileHollow", 0);
                    writer.WriteNamedValue("Scale", scale);
                    writer.WriteNamedValue("State", 0);
                    writer.WriteNamedValue("ProfileShape", "Square");
                    writer.WriteNamedValue("HollowShape", "Same");
                    writer.WriteUUID("SculptTexture", meshID);
                    writer.WriteNamedValue("SculptType", (int)PrimitiveSculptType.Mesh);
                    writer.WriteNamedValue("FlexiSoftness", 0);
                    writer.WriteNamedValue("FlexiTension", 0);
                    writer.WriteNamedValue("FlexiDrag", 0);
                    writer.WriteNamedValue("FlexiGravity", 0);
                    writer.WriteNamedValue("FlexiForce", Vector3.Zero, true);
                    writer.WriteNamedValue("LightColor", ColorAlpha.Black, true);
                    writer.WriteNamedValue("LightRadius", 0);
                    writer.WriteNamedValue("LightCutoff", 0);
                    writer.WriteNamedValue("LightFalloff", 0);
                    writer.WriteNamedValue("LightIntensity", 1);
                    writer.WriteNamedValue("FlexiEntry", false);
                    writer.WriteNamedValue("LightEntry", false);
                    writer.WriteNamedValue("SculptEntry", true);
                }
                writer.WriteEndElement();
                writer.WriteNamedValue("Scale", scale);
                writer.WriteNamedValue("UpdateFlag", 0);
                writer.WriteNamedValue("SitTargetOrientation", Quaternion.Identity);
                writer.WriteNamedValue("SitTargetPosition", Vector3.Zero);
                writer.WriteNamedValue("SitTargetPositionLL", Vector3.Zero);
                writer.WriteNamedValue("SitTargetOrientationLL", Quaternion.Identity);
                writer.WriteNamedValue("ParentID", 1);
                writer.WriteNamedValue("CreationDate", creationDate.DateTimeToUnixTime());
                writer.WriteNamedValue("Category", 0);
                writer.WriteNamedValue("SalePrice", 10);
                writer.WriteNamedValue("ObjectSaleType", 0);
                writer.WriteNamedValue("OwnershipCost", 0);
                writer.WriteUUID("GroupID", UUID.Zero);
                writer.WriteUUID("OwnerID", Creator.ID);
                writer.WriteUUID("LastOwnerID", Creator.ID);
                writer.WriteNamedValue("BaseMask", (uint)InventoryPermissionsMask.All);
                writer.WriteNamedValue("OwnerMask", (uint)InventoryPermissionsMask.Every);
                writer.WriteNamedValue("EveryoneMask", 0);
                writer.WriteNamedValue("NextOwnerMask", (uint)InventoryPermissionsMask.None);
                writer.WriteNamedValue("Flags", "None");
                writer.WriteUUID("CollisionSound", UUID.Zero);
                writer.WriteNamedValue("CollisionSoundVolume", 0);
                writer.WriteStartElement("TextureAnimation");
                writer.WriteEndElement();
                writer.WriteStartElement("ParticleSystem");
                writer.WriteEndElement();
                writer.WriteNamedValue("PayPrice0", -2);
                writer.WriteNamedValue("PayPrice1", -2);
                writer.WriteNamedValue("PayPrice2", -2);
                writer.WriteNamedValue("PayPrice3", -2);
                writer.WriteNamedValue("PayPrice4", -2);
            }
            writer.WriteEndElement(); /* SceneObjectPart */
        }

        protected override UUID NewAssetID => UUID.Random;

        protected override bool AssetIsLocal => false;

        protected override bool AssetIsTemporary => false;

        protected override AssetType NewAssetType => AssetType.Unknown;
    }
}
