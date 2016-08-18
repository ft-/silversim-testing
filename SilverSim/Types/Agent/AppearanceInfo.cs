// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;

namespace SilverSim.Types.Agent
{
    public class AppearanceInfo
    {
        public class AvatarTextureData
        {
            [SuppressMessage("Gendarme.Rules.Performance", "PreferLiteralOverInitOnlyFieldsRule")]
            public readonly static int TextureCount = 21;
            readonly UUID[] m_AvatarTextures = new UUID[TextureCount];
            readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();

            public AvatarTextureData()
            {

            }

            [SuppressMessage("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
            public UUID[] All
            {
                get
                {
                    m_RwLock.AcquireReaderLock(-1);
                    try
                    {
                        UUID[] textures = new UUID[TextureCount];
                        for (int i = 0; i < TextureCount; ++i)
                        {
                            textures[i] = new UUID(m_AvatarTextures[i]);
                        }
                        return textures;
                    }
                    finally
                    {
                        m_RwLock.ReleaseReaderLock();
                    }
                }
                set
                {
                    if(value.Length != TextureCount)
                    {
                        throw new ArgumentException("Invalid number of elements");
                    }
                    m_RwLock.AcquireWriterLock(-1);
                    try
                    {
                        for (int i = 0; i < TextureCount; ++i)
                        {
                            m_AvatarTextures[i] = new UUID(value[i]);
                        }
                    }
                    finally
                    {
                        m_RwLock.ReleaseWriterLock();
                    }
                }
            }

            public UUID this[int texIndex]
            {
                get
                {
                    if(texIndex < 0 || texIndex >= TextureCount)
                    {
                        throw new KeyNotFoundException();
                    }
                    m_RwLock.AcquireReaderLock(-1);
                    try
                    {
                        return m_AvatarTextures[texIndex];
                    }
                    finally
                    {
                        m_RwLock.ReleaseReaderLock();
                    }
                }

                set
                {
                    if (texIndex < 0 || texIndex >= TextureCount)
                    {
                        throw new KeyNotFoundException();
                    }
                    m_RwLock.AcquireWriterLock(-1);
                    try
                    {
                        m_AvatarTextures[texIndex] = value;
                    }
                    finally
                    {
                        m_RwLock.ReleaseWriterLock();
                    }
                }
            }
        }

        readonly AgentWearables m_Wearables = new AgentWearables();
        public AgentWearables Wearables
        {
            get
            {
                return m_Wearables;
            }
            set
            {
                m_Wearables.All = value;
            }
        }

        public readonly RwLockedDictionaryAutoAdd<AttachmentPoint, RwLockedDictionary<UUID, UUID>> Attachments =
            new RwLockedDictionaryAutoAdd<AttachmentPoint, RwLockedDictionary<UUID, UUID>>(delegate() { return new RwLockedDictionary<UUID, UUID>(); });
        readonly ReaderWriterLock m_VisualParamsLock = new ReaderWriterLock();
        private byte[] m_VisualParams = new byte[] { 33, 61, 85, 23, 58, 127, 63, 85, 63, 42, 0, 85, 63, 36, 85, 95, 153, 63, 34, 0, 63, 109, 88, 132, 63, 136, 81, 85, 103, 136, 127, 0, 150, 150, 150, 127, 0, 0, 0, 0, 0, 127, 0, 0, 255, 127, 114, 127, 99, 63, 127, 140, 127, 127, 0, 0, 0, 191, 0, 104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 145, 216, 133, 0, 127, 0, 127, 170, 0, 0, 127, 127, 109, 85, 127, 127, 63, 85, 42, 150, 150, 150, 150, 150, 150, 150, 25, 150, 150, 150, 0, 127, 0, 0, 144, 85, 127, 132, 127, 85, 0, 127, 127, 127, 127, 127, 127, 59, 127, 85, 127, 127, 106, 47, 79, 127, 127, 204, 2, 141, 66, 0, 0, 127, 127, 0, 0, 0, 0, 127, 0, 159, 0, 0, 178, 127, 36, 85, 131, 127, 127, 127, 153, 95, 0, 140, 75, 27, 127, 127, 0, 150, 150, 198, 0, 0, 63, 30, 127, 165, 209, 198, 127, 127, 153, 204, 51, 51, 255, 255, 255, 204, 0, 255, 150, 150, 150, 150, 150, 150, 150, 150, 150, 150, 0, 150, 150, 150, 150, 150, 0, 127, 127, 150, 150, 150, 150, 150, 150, 150, 150, 0, 0, 150, 51, 132, 150, 150, 150 };
        public double AvatarHeight;
        public UInt32 Serial = 1;

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public byte[] BakeIndices
        {
            get
            {
                return new byte[] { 8, 9, 10, 11, 19, 20 };
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "PreferLiteralOverInitOnlyFieldsRule")]
        public static readonly int MaxVisualParams = 260;

        public readonly AvatarTextureData AvatarTextures = new AvatarTextureData();

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidReturningArraysOnPropertiesRule")]
        public byte[] VisualParams
        {
            get
            {
                m_VisualParamsLock.AcquireReaderLock(-1);
                try
                {
                    byte[] res = new byte[m_VisualParams.Length];
                    Buffer.BlockCopy(m_VisualParams, 0, res, 0, m_VisualParams.Length);
                    return res;
                }
                finally
                {
                    m_VisualParamsLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_VisualParamsLock.AcquireWriterLock(-1);
                try
                {
                    int VisualParamCount = MaxVisualParams < value.Length ? MaxVisualParams : value.Length;
                    m_VisualParams = new byte[VisualParamCount];
                    Buffer.BlockCopy(value, 0, m_VisualParams, 0, VisualParamCount);
                }
                finally
                {
                    m_VisualParamsLock.ReleaseWriterLock();
                }
            }
        }

        public AppearanceInfo()
        {

        }

        public class InvalidAppearanceInfoSerializationException : Exception
        {
            public InvalidAppearanceInfoSerializationException()
            {

            }

            public InvalidAppearanceInfoSerializationException(string msg)
                : base(msg)
            {

            }

            protected InvalidAppearanceInfoSerializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public InvalidAppearanceInfoSerializationException(string msg, Exception innerException)
                : base(msg, innerException)
            {

            }
        }

        public static AppearanceInfo FromNotecard(Notecard nc)
        {
            using (MemoryStream ms = new MemoryStream(nc.Text.ToUTF8Bytes()))
            {
                Map m = LlsdXml.Deserialize(ms) as Map;
                if(m == null)
                {
                    throw new InvalidAppearanceInfoSerializationException();
                }

                AppearanceInfo appearanceInfo = new AppearanceInfo();
                appearanceInfo.AvatarHeight = m["height"].AsReal;
                AnArray wearables = m["wearables"] as AnArray;
                AnArray textures = m["textures"] as AnArray;
                AnArray attachments = m["attachments"] as AnArray;
                BinaryData visualparams = m["visualparams"] as BinaryData;

                appearanceInfo.VisualParams = visualparams;
                foreach(IValue iv in attachments)
                {
                    Map im = iv as Map;
                    if(im == null)
                    {
                        throw new InvalidAppearanceInfoSerializationException();
                    }
                    AttachmentPoint ap = (AttachmentPoint)im["point"].AsInt;

                    appearanceInfo.Attachments[ap][im["item"].AsUUID] = im["asset"].AsUUID;
                }
                
                for(int i = 0; i < wearables.Count; ++i)
                {
                    AnArray wearablesAt = wearables[i] as AnArray;
                    if(null == wearablesAt)
                    {
                        throw new InvalidAppearanceInfoSerializationException();
                    }
                    foreach(IValue ivw in wearablesAt)
                    {
                        Map mw = ivw as Map;
                        if(mw == null)
                        {
                            throw new InvalidAppearanceInfoSerializationException();
                        }
                        AgentWearables.WearableInfo wi = new AgentWearables.WearableInfo();
                        wi.ItemID = mw["item"].AsUUID;
                        wi.AssetID = mw["asset"].AsUUID;
                        appearanceInfo.Wearables[(WearableType)i].Add(wi);
                    }
                }

                for(int i = 0; i < textures.Count; ++i)
                {
                    appearanceInfo.AvatarTextures[i] = textures[i].AsUUID;
                }
                if (m.ContainsKey("serial"))
                {
                    appearanceInfo.Serial = (uint)m["serial"].AsInt;
                }
                return appearanceInfo;
            }
        }

        public static explicit operator Notecard(AppearanceInfo appearanceInfo)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlTextWriter writer = ms.UTF8XmlTextWriter())
                {
                    writer.WriteStartElement("llsd");
                    {
                        writer.WriteStartElement("map");
                        {
                            writer.WriteKeyValuePair("serial", appearanceInfo.Serial);
                            writer.WriteKeyValuePair("height", appearanceInfo.AvatarHeight);
                            writer.WriteNamedValue("key", "wearables");
                            writer.WriteStartElement("array");
                            {
                                for (int i = 0; i < (int)WearableType.NumWearables; ++i)
                                {
                                    writer.WriteStartElement("array");
                                    {
                                        List<AgentWearables.WearableInfo> wearables = appearanceInfo.Wearables[(WearableType)i];
                                        foreach (AgentWearables.WearableInfo wearable in wearables)
                                        {
                                            writer.WriteStartElement("map");
                                            {
                                                writer.WriteKeyValuePair("item", wearable.ItemID);
                                                writer.WriteKeyValuePair("asset", wearable.AssetID);
                                            }
                                            writer.WriteEndElement();
                                        }
                                    }
                                    writer.WriteEndElement();
                                }
                            }
                            writer.WriteEndElement();

                            writer.WriteNamedValue("key", "textures");
                            writer.WriteStartElement("array");
                            {
                                foreach (UUID tex in appearanceInfo.AvatarTextures.All)
                                {
                                    writer.WriteNamedValue("uuid", tex);
                                }
                            }
                            writer.WriteEndElement();

                            writer.WriteNamedValue("key", "visualparams");
                            writer.WriteNamedValue("binary", Convert.ToBase64String(appearanceInfo.VisualParams));

                            writer.WriteNamedValue("key", "attachments");
                            writer.WriteStartElement("array");
                            {
                                foreach (KeyValuePair<AttachmentPoint, RwLockedDictionary<UUID, UUID>> kvpAp in appearanceInfo.Attachments)
                                {
                                    foreach (KeyValuePair<UUID, UUID> attachmentKvp in kvpAp.Value)
                                    {
                                        writer.WriteStartElement("map");
                                        {
                                            writer.WriteKeyValuePair("point", (int)kvpAp.Key);
                                            writer.WriteKeyValuePair("item", attachmentKvp.Key);
                                            writer.WriteKeyValuePair("asset", attachmentKvp.Value);
                                        }
                                        writer.WriteEndElement();
                                    }
                                }
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                
                Notecard nc = new Notecard();
                nc.Text = ms.ToArray().FromUTF8Bytes();
                return nc;
            }
        }
    }
}
