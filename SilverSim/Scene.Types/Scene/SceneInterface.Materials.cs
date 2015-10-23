// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Xml;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        byte[] m_MaterialsData;
        readonly Dictionary<UUID, Material> m_Materials = new Dictionary<UUID, Material>();
        ReaderWriterLock m_MaterialsRwLock = new ReaderWriterLock();

        public byte[] MaterialsData
        {
            get
            {
                m_MaterialsRwLock.AcquireReaderLock(-1);
                try
                {
                    byte[] outb = new byte[m_MaterialsData.Length];
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
        protected void AddLegacyMaterials(ObjectGroup sog)
        {
            List<Material> mats = new List<Material>();
            foreach(ObjectPart part in sog.Values)
            {
                Map m;
                if(!part.DynAttrs.ContainsKey("OpenSim"))
                {
                    continue;
                }
                m = (Map)part.DynAttrs["OpenSim"];

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
                    Map mmap = iv as Map;
                    if(null == mmap)
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

            }
        }

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
        void UpdateMaterials()
        {
            byte[] buf;
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress))
                {
                    using (XmlTextWriter writer = new XmlTextWriter(ms, UTF8NoBOM))
                    {
                        writer.WriteStartElement("llsd");
                        writer.WriteStartElement("array");
                        foreach (KeyValuePair<UUID, Material> kvp in m_Materials)
                        {
                            writer.WriteStartElement("map");
                            writer.WriteNamedValue("key", "ID");
                            writer.WriteNamedValue("uuid", kvp.Key);
                            writer.WriteNamedValue("key", "Material");
                            kvp.Value.WriteMap(writer);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                }
                buf = ms.GetBuffer();
            }
            using(MemoryStream ms = new MemoryStream())
            {
                using(XmlTextWriter writer = new XmlTextWriter(ms, UTF8NoBOM))
                {
                    writer.WriteStartElement("llsd");
                    writer.WriteNamedValue("key", "Zipped");
                    writer.WriteNamedValue("binary", buf);
                    writer.WriteEndElement();
                }
                m_MaterialsData = ms.GetBuffer();
            }
        }
    }
}
