// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types;
using ThreadedClasses;
using SilverSim.StructuredData.LLSD;
using System.IO;
using SilverSim.Types.Primitive;
using System.Xml;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class NewFileAgentInventoryVariablePrice : UploadAssetAbstractCapability
    {
        private InventoryServiceInterface m_InventoryService;
        private AssetServiceInterface m_AssetService;

        private readonly RwLockedDictionary<UUID, InventoryItem> m_Transactions = new RwLockedDictionary<UUID, InventoryItem>();

        public override string CapabilityName
        {
            get
            {
                return "NewFileAgentInventoryVariablePrice";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public NewFileAgentInventoryVariablePrice(UUI creator, InventoryServiceInterface inventoryService, AssetServiceInterface assetService, string serverURI)
            : base(creator, serverURI)
        {
            m_InventoryService = inventoryService;
            m_AssetService = assetService;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            UUID transaction = UUID.Random;
            InventoryItem item = new InventoryItem();
            item.ID = UUID.Random;
            item.Description = reqmap["description"].ToString();
            item.Name = reqmap["name"].ToString();
            item.ParentFolderID = reqmap["folder_id"].AsUUID;
            item.AssetTypeName = reqmap["asset_type"].ToString();
            item.InventoryTypeName = reqmap["inventory_type"].ToString();
            item.LastOwner = m_Creator;
            item.Owner = m_Creator;
            item.Creator = m_Creator;
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
            if (m_Transactions.RemoveIf(transactionID, delegate(InventoryItem v) { return true; }, out kvp))
            {
                Map m = new Map();
                m.Add("new_inventory_item", kvp.Value.ID.ToString());
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
                    throw new UploadErrorException("Could not store asset");
                }

                try
                {
                    m_InventoryService.Item.Add(kvp.Value);
                }
                catch
                {
                    throw new UploadErrorException("Could not store new inventory item");
                }
                return m;
            }
            else
            {
                throw new UrlNotFoundException();
            }
        }

        UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);

        void UploadObject(UUID transactionID, AssetData data)
        {
            Map m = (Map)LLSD_XML.Deserialize(data.InputStream);
            AnArray instance_list = (AnArray)m["instance_list"];
            AnArray mesh_list = (AnArray)m["mesh_list"];
            AnArray texture_list = (AnArray)m["texture_list"];

            List<UUID> textureids = new List<UUID>();
            Quaternion[] primrots = new Quaternion[instance_list.Count];
            Vector3[] primpositions = new Vector3[instance_list.Count];
            Vector3[] primscales = new Vector3[instance_list.Count];

            using (MemoryStream objectms = new MemoryStream())
            {
                using(XmlTextWriter writer = new XmlTextWriter(objectms, UTF8NoBOM))
                {
                    writer.WriteStartElement("SceneObjectGroup");
                    /* we serialize the assets straight away, no allocating SOG in between here. Allocating a SOG would just take senselessly a lot of time and memory. */
                    if (texture_list.Count > 0)
                    {
                        UUID textureFolder = m_InventoryService.Folder[m_Creator.ID, AssetType.Texture].ID;
                        InventoryFolder folder = new InventoryFolder();
                        folder.Name = data.Name + " - Textures";
                        folder.Owner = m_Creator;
                        folder.InventoryType = InventoryType.Unknown;
                        folder.ParentFolderID = textureFolder;
                        folder.Version = 1;
                        m_InventoryService.Folder.Add(folder);

                        int idx = 0;
                        foreach(IValue iv in texture_list)
                        {
                            ++idx;

                            AssetData newasset = new AssetData();
                            newasset.ID = UUID.Random;
                            newasset.Type = AssetType.Texture;
                            newasset.Creator = m_Creator;
                            newasset.Data = (BinaryData)iv;
                            newasset.Name = data.Name + " - Texture " + idx.ToString();
                            textureids.Add(newasset.ID);
                            m_AssetService.Store(newasset);

                            InventoryItem item = new InventoryItem();
                            item.AssetID = newasset.ID;
                            item.AssetType = AssetType.Texture;
                            item.Creator = m_Creator;
                            item.InventoryType = InventoryType.Texture;
                            item.LastOwner = m_Creator;
                            item.Name = data.Name + " - Texture " + idx.ToString();
                            item.Owner = m_Creator;
                            item.ParentFolderID = folder.ID;
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
                            Map inner_instance = (Map)instance_list[idx];
                            primpositions[idx] = inner_instance["position"].AsVector3;
                            primscales[idx] = inner_instance["scale"].AsVector3;
                            primrots[idx] = inner_instance["rotation"].AsQuaternion;
                        }

                        Quaternion rootRotConjugated = Quaternion.Inverse(primrots[0]);
                        for(idx = 1; idx < primscales.Length; ++idx)
                        {
                            primpositions[idx] = primpositions[idx] - primpositions[0];
                            primrots[idx] = (rootRotConjugated * primrots[idx]) * rootRotConjugated;
                        }
                    }

                    if (mesh_list.Count > 0)
                    {
                        int idx = 0;
                        bool wroteOtherParts = false;
                        for (idx = 0; idx < mesh_list.Count; ++idx)
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
                                using (MemoryStream meshstream = new MemoryStream())
                                {
                                    if (mesh_list[idx] is BinaryData)
                                    {
                                        BinaryData bin = (BinaryData)mesh_list[idx];
                                        meshstream.Write(bin, 0, bin.Length);
                                    }
                                    else
                                    {
                                        LLSD_Binary.Serialize(mesh_list[idx], meshstream);
                                    }
                                    meshstream.Flush();
                                    AssetData newasset = new AssetData();
                                    newasset.ID = UUID.Random;
                                    meshassetid = newasset.ID;
                                    newasset.Type = AssetType.Mesh;
                                    newasset.Creator = m_Creator;
                                    newasset.Data = meshstream.GetBuffer();
                                    newasset.Name = data.Name + " - Mesh " + (idx + 1).ToString();
                                    m_AssetService.Store(newasset);
                                }

                                TextureEntry texentry = new TextureEntry();
                                texentry.DefaultTexture.TextureID = TextureEntry.WHITE_TEXTURE;
                                Map inner_instance = (Map)instance_list[idx];
                                AnArray face_list = (AnArray)(inner_instance["face_list"]);
                                for (int faceidx = 0; faceidx < face_list.Count; ++faceidx)
                                {
                                    Map faceMap = (Map)(face_list[faceidx]);
                                    TextureEntryFace face = texentry.FaceTextures[faceidx];
                                    if (faceMap.ContainsKey("fullbright"))
                                    {
                                        face.FullBright = faceMap["fullbright"].AsBoolean;
                                    }
                                    if (faceMap.ContainsKey("diffuse_color"))
                                    {
                                        AnArray color4 = (AnArray)faceMap["diffuse_color"];
                                        if (color4.Count == 4)
                                        {
                                            face.TextureColor.R = color4[0].AsReal;
                                            face.TextureColor.G = color4[1].AsReal;
                                            face.TextureColor.B = color4[2].AsReal;
                                            face.TextureColor.A = color4[3].AsReal;
                                        }
                                    }
                                    int textureNum = faceMap["image"].AsInt;
                                    float imagerot = (float)faceMap["imagerot"].AsReal;
                                    float offsets = (float)faceMap["offsets"].AsReal;
                                    float offsett = (float)faceMap["offsett"].AsReal;
                                    float scales = (float)faceMap["scales"].AsReal;
                                    float scalet = (float)faceMap["scalet"].AsReal;

                                    face.Rotation = imagerot;
                                    face.OffsetU = offsets;
                                    face.OffsetV = offsett;
                                    face.RepeatU = scales;
                                    face.RepeatV = scalet;

                                    if (textureids.Count > textureNum)
                                    {
                                        face.TextureID = textureids[textureNum];
                                    }
                                    else
                                    {
                                        face.TextureID = TextureEntry.WHITE_TEXTURE;
                                    }
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
                    data.Data = objectms.GetBuffer();
                    data.Type = AssetType.Object;
                }
            }
        }

        void WritePart(
            XmlTextWriter writer, 
            string assetName, 
            Vector3 position, Vector3 scale, Quaternion rotation, 
            UUID meshID, TextureEntry te, int linknumber,
            Date creationDate)
        {
            writer.WriteStartElement("SceneObjectPart");
            {
                writer.WriteNamedValue("AllowedDrop", "false");
                writer.WriteUUID("CreatorID", m_Creator.ID);
                writer.WriteNamedValue("CreatorData", m_Creator.CreatorData);
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
                    byte[] extraParams = new byte[1 + 4 + 2 + 17];
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
                writer.WriteUUID("OwnerID", m_Creator.ID);
                writer.WriteUUID("LastOwnerID", m_Creator.ID);
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

        protected override UUID NewAssetID
        {
            get
            {
                return UUID.Random;
            }
        }

        protected override bool AssetIsLocal
        {
            get
            {
                return false;
            }
        }

        protected override bool AssetIsTemporary
        {
            get
            {
                return false;
            }
        }

        protected override AssetType NewAssetType
        {
            get
            {
                return AssetType.Unknown;
            }
        }
    }
}
