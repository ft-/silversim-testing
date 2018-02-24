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

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format.Mesh;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Types.StructuredData.Llsd;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SilverSim.Scene.Types.Object.Mesh.Item
{
    public sealed class MeshInventoryItem : InventoryItem
    {
        public readonly List<AssetData> Assets = new List<AssetData>();
        public readonly Dictionary<int, UUID> TextureMap = new Dictionary<int, UUID>();
        public readonly Dictionary<int, UUID> MeshMap = new Dictionary<int, UUID>();
        public readonly Dictionary<int, int> MeshFaces = new Dictionary<int, int>();
        public readonly List<InventoryItem> TextureItems = new List<InventoryItem>();

        public static MeshInventoryItem FromUploadFormat(string name, Stream s, UUI creator, AssetData objectAsset = null)
        {
            var map = (Map)LlsdXml.Deserialize(s);
            var item = new MeshInventoryItem
            {
                Name = name,
                AssetType = AssetType.Object,
                InventoryType = InventoryType.Object,
                Owner = creator,
                LastOwner = creator,
                Creator = creator
            };
            item.Permissions.Base = InventoryPermissionsMask.Every;
            item.Permissions.Current = InventoryPermissionsMask.Every;
            ProcessTextures((AnArray)map["texture_list"], item);
            ProcessMeshes((AnArray)map["mesh_list"], item);
            ProcessInstances((AnArray)map["instance_list"], item, objectAsset);
            return item;
        }

        private static void ProcessTextures(AnArray textureList, MeshInventoryItem item)
        {
            int idx = 0;
            foreach(IValue iv in textureList)
            {
                var newasset = new AssetData
                {
                    ID = UUID.Random,
                    Type = AssetType.Texture,
                    Data = (BinaryData)iv,
                    Name = item.Name + " - Texture " + idx.ToString(),
                };
                item.Assets.Add(newasset);
                item.TextureMap.Add(idx, newasset.ID);

                var textureitem = new InventoryItem
                {
                    AssetID = newasset.ID,
                    AssetType = AssetType.Texture,
                    Creator = item.Creator,
                    InventoryType = InventoryType.Texture,
                    LastOwner = item.Creator,
                    Name = item.Name + " - Texture " + idx.ToString(),
                    Owner = item.Creator,
                    ParentFolderID = UUID.Zero
                };
                item.Permissions.Base = InventoryPermissionsMask.All;
                item.Permissions.Current = InventoryPermissionsMask.Every;
                item.Permissions.EveryOne = InventoryPermissionsMask.None;
                item.Permissions.Group = InventoryPermissionsMask.All;
                item.Permissions.NextOwner = InventoryPermissionsMask.All;
                item.SaleInfo.Price = 10;
                item.SaleInfo.Type = SaleInfoData.SaleType.NoSale;

                item.TextureItems.Add(item);

                ++idx;
            }
        }

        private static void ProcessMeshes(AnArray meshList, MeshInventoryItem item)
        {
            int idx = 0;
            foreach(IValue iv in meshList)
            {
                var newasset = new AssetData
                {
                    ID = UUID.Random,
                    Type = AssetType.Mesh,
                    Name = item.Name + " - Mesh " + (idx + 1).ToString()
                };

                using (var meshstream = new MemoryStream())
                {
                    Map meshData;
                    /* add the version tag */
                    using (var inputstream = new MemoryStream((BinaryData)iv))
                    {
                        meshData = (Map)LlsdBinary.Deserialize(inputstream);
                        meshData["version"] = new Integer(1);
                        LlsdBinary.Serialize(meshData, meshstream);
                        inputstream.CopyTo(meshstream);
                    }
                    newasset.Data = meshstream.ToArray();
                }

                item.Assets.Add(newasset);
                item.MeshMap.Add(idx, newasset.ID);
                var m = new LLMesh(newasset);
                MeshLOD lod = m.GetLOD(LLMesh.LodLevel.LOD3);
                if(lod.NumFaces >= 1 && lod.NumFaces <= 9)
                {
                    item.MeshFaces.Add(idx, lod.NumFaces);
                }
                ++idx;
            }
        }

        private static Vector3 GetVector(IValue iv)
        {
            var a = (AnArray)iv;
            return new Vector3(a[0].AsReal, a[1].AsReal, a[2].AsReal);
        }

        private static Quaternion GetRotation(IValue iv)
        {
            var a = (AnArray)iv;
            return new Quaternion(a[0].AsReal, a[1].AsReal, a[2].AsReal, a[3].AsReal);
        }

        private static ColorAlpha GetColorAlpha(IValue iv)
        {
            var a = (AnArray)iv;
            return new ColorAlpha(a[0].AsReal, a[1].AsReal, a[2].AsReal, a[3].AsReal);
        }

        private static void ProcessInstances(AnArray instanceList, MeshInventoryItem item, AssetData objectAsset = null)
        {
            var grp = new ObjectGroup();
            foreach(Map instanceData in instanceList.OfType<Map>())
            {
                var shape = new ObjectPart.PrimitiveShape
                {
                    SculptType = PrimitiveSculptType.Mesh,
                    ProfileCurve = 1,
                    PathBegin = 0,
                    PathCurve = 16,
                    PathEnd = 0,
                    PathRadiusOffset = 0,
                    PathRevolutions = 0,
                    PathScaleX = 100,
                    PathScaleY = 100,
                    PathShearX = 0,
                    PathShearY = 0,
                    PathSkew = 0,
                    PathTaperX = 0,
                    PathTaperY = 0,
                    PathTwist = 0,
                    PathTwistBegin = 0,
                    PCode = PrimitiveCode.Prim,
                    ProfileBegin = 9375,
                    ProfileEnd = 0,
                    ProfileHollow = 0,
                    State = 0,
                    SculptMap = item.MeshMap[instanceData["mesh"].AsInt]
                };

                int numfaces;
                if(item.MeshFaces.TryGetValue(instanceData["mesh"].AsInt, out numfaces))
                {
                    shape.SetMeshNumFaces(numfaces);
                }

                var part = new ObjectPart
                {
                    Name = item.Name,
                    Shape = shape,
                    IsReturnAtEdge = true,
                    Size = GetVector(instanceData["scale"]),
                    PhysicsShapeType = (PrimitivePhysicsShapeType)instanceData["physics_shape_type"].AsInt,
                    Material = (PrimitiveMaterial)instanceData["material"].AsInt
                };

                grp.AddLink(part);

                /* use our build in transformation */
                grp.GlobalPosition = GetVector(instanceData["position"]);
                grp.GlobalRotation = GetRotation(instanceData["rotation"]);

                var faceList = (AnArray)instanceData["face_list"];
                var te = new TextureEntry();
                uint faceidx = 0;

                foreach(Map face in faceList.OfType<Map>())
                {
                    TextureEntryFace teface = te[faceidx];
                    IValue iv;

                    teface.FullBright = face["fullbright"].AsBoolean;
                    teface.TextureColor = GetColorAlpha(face["diffuse_color"]);

                    if(face.TryGetValue("image", out iv))
                    {
                        teface.TextureID = item.TextureMap[iv.AsInt];
                    }

                    if(face.TryGetValue("scales", out iv))
                    {
                        teface.RepeatU = (float)iv.AsReal;
                    }

                    if(face.TryGetValue("scalet", out iv))
                    {
                        teface.RepeatV = (float)iv.AsReal;
                    }

                    if(face.TryGetValue("offsets", out iv))
                    {
                        teface.OffsetU = (float)iv.AsReal;
                    }

                    if(face.TryGetValue("offsett", out iv))
                    {
                        teface.OffsetV = (float)iv.AsReal;
                    }

                    if(face.TryGetValue("imagerot", out iv))
                    {
                        teface.Rotation = (float)iv.AsReal;
                    }

                    ++faceidx;
                }
                part.TextureEntry = te;
                part.Owner = item.Owner;
                part.Creator = item.Owner;
                part.OwnerMask = InventoryPermissionsMask.Every;
                part.BaseMask = InventoryPermissionsMask.Every;
            }

            grp.Owner = item.Owner;
            grp.LastOwner = item.Owner;

            AssetData objectasset = grp.Asset(XmlSerializationOptions.WriteXml2 | XmlSerializationOptions.WriteOwnerInfo);
            if (objectAsset != null)
            {
                objectAsset.Data = objectasset.Data;
                item.AssetID = objectAsset.ID;
                objectAsset.Type = AssetType.Object;
            }
            else
            {
                item.AssetID = objectasset.ID;
                item.Assets.Add(objectasset);
            }
        }
    }
}
