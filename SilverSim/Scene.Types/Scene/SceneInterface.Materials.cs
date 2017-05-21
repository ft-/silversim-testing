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
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Xml;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        private byte[] m_MaterialsData;
        private readonly Dictionary<UUID, Material> m_Materials = new Dictionary<UUID, Material>();
        private readonly ReaderWriterLock m_MaterialsRwLock = new ReaderWriterLock();

        public byte[] MaterialsData
        {
            get
            {
                m_MaterialsRwLock.AcquireReaderLock(-1);
                try
                {
                    var outb = new byte[m_MaterialsData.Length];
                    Buffer.BlockCopy(m_MaterialsData, 0, outb, 0, m_MaterialsData.Length);
                    return outb;
                }
                finally
                {
                    m_MaterialsRwLock.ReleaseReaderLock();
                }
            }
        }

        public void AddMaterial(Material mat)
        {
            m_MaterialsRwLock.AcquireWriterLock(-1);
            try
            {
                m_Materials.Add(mat.MaterialID, mat);
                UpdateMaterials();
            }
            finally
            {
                m_MaterialsRwLock.ReleaseWriterLock();
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public Material GetMaterial(UUID matid)
        {
            Material mat;
            m_MaterialsRwLock.AcquireReaderLock(-1);
            try
            {
                if (m_Materials.TryGetValue(matid, out mat))
                {
                    return mat;
                }
            }
            finally
            {
                m_MaterialsRwLock.ReleaseReaderLock();
            }

            /* fetch from asset */
            mat = new Material(AssetService[matid]);
            m_MaterialsRwLock.AcquireWriterLock(-1);
            try
            {
                m_Materials.Add(mat.MaterialID, new Material(mat));
                UpdateMaterials();
            }
            catch
            {
                /* ignore this */
            }
            finally
            {
                m_MaterialsRwLock.ReleaseWriterLock();
            }
            return mat;
        }

        public void StoreMaterial(Material mat)
        {
            AssetData ad = mat;
            ad.Name = "Material";
            AssetService.Store(ad);
            AddMaterial(mat);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void AddMaterials(List<Material> mats)
        {
            m_MaterialsRwLock.AcquireWriterLock(-1);
            try
            {
                foreach (Material mat in mats)
                {
                    try
                    {
                        m_Materials.Add(mat.MaterialID, mat);
                    }
                    catch
                    {
                        /* ignore this */
                    }
                }
                UpdateMaterials();
            }
            finally
            {
                m_MaterialsRwLock.ReleaseWriterLock();
            }
        }

        public void RemoveMaterial(Material mat)
        {
            m_MaterialsRwLock.AcquireWriterLock(-1);
            try
            {
                if (m_Materials.Remove(mat.MaterialID))
                {
                    UpdateMaterials();
                }
            }
            finally
            {
                m_MaterialsRwLock.ReleaseWriterLock();
            }
        }

        /* Collect legacy materials and push them to materials */
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        protected void AddLegacyMaterials(ObjectGroup sog)
        {
            var mats = new List<Material>();
            foreach(ObjectPart part in sog.Values)
            {
                if(!part.DynAttrs.ContainsKey("OpenSim"))
                {
                    continue;
                }
                var m = (Map)part.DynAttrs["OpenSim"];

                if(!m.ContainsKey("Materials"))
                {
                    continue;
                }
                if(!(m["Materials"] is AnArray))
                {
                    continue;
                }

                foreach(IValue iv in (AnArray)m["Materials"])
                {
                    var mmap = iv as Map;
                    if(mmap == null)
                    {
                        continue;
                    }
                    if(!mmap.ContainsKey("ID") || !mmap.ContainsKey("Material"))
                    {
                        continue;
                    }

                    if(!(mmap["Material"] is Map))
                    {
                        continue;
                    }

                    try
                    {
                        Material mat = new Material(mmap["ID"].AsUUID, (Map)mmap["Material"]);
                        mats.Add(mat);
                        StoreMaterial(mat);
                    }
                    catch
                    {
                        /* no action required */
                    }
                }

                /* get rid of legacy advanced materials */
                m.Remove("Materials");
            }

            try
            {
                AddMaterials(mats);
            }
            catch
            {
                /* no action required */
            }
        }

        private void UpdateMaterials()
        {
            byte[] buf;
            using (var ms = new MemoryStream())
            {
                var zlibheader = new byte[2] { 0x78, 0xDA };
                ms.Write(zlibheader, 0, 2);

                var matArray = new AnArray();
                foreach(var kvp in m_Materials)
                {
                    matArray.Add(new Map
                    {
                        ["ID"] = kvp.Key,
                        ["Material"] = kvp.Value.WriteMap()
                    });
                }

                using (var gz = new DeflateStream(ms, CompressionMode.Compress))
                {
                    LlsdBinary.Serialize(matArray, gz);
                }
                buf = ms.ToArray();
            }
            using(var ms = new MemoryStream())
            {
                using(var writer = ms.UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("llsd");
                    writer.WriteNamedValue("key", "Zipped");
                    writer.WriteNamedValue("binary", buf);
                    writer.WriteEndElement();
                }
                m_MaterialsData = ms.ToArray();
            }
        }
    }
}
