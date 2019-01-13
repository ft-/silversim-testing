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

using SilverSim.Threading;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
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
            public readonly static int TextureCount = (int)AvatarTextureIndex.NumTextures;
            public static readonly UUID DefaultAvatarTextureID = new UUID("c228d1cf-4b5d-4ba8-84f4-899a0796aa97");

            private readonly UUID[] m_AvatarTextures = new UUID[TextureCount];
            private readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();

            public UUID[] All
            {
                get
                {
                    return m_RwLock.AcquireReaderLock(() =>
                    {
                        var textures = new UUID[TextureCount];
                        for (int i = 0; i < TextureCount; ++i)
                        {
                            textures[i] = m_AvatarTextures[i];
                        }
                        return textures;
                    });
                }
                set
                {
                    if(value.Length != TextureCount)
                    {
                        throw new ArgumentException("Invalid number of elements");
                    }
                    m_RwLock.AcquireWriterLock(() =>
                    {
                        for (int i = 0; i < TextureCount; ++i)
                        {
                            m_AvatarTextures[i] = value[i];
                        }
                    });
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
                    return m_RwLock.AcquireReaderLock(() => m_AvatarTextures[texIndex]);
                }

                set
                {
                    if (texIndex < 0 || texIndex >= TextureCount)
                    {
                        return;
                    }
                    m_RwLock.AcquireWriterLock(() =>
                    {
                        m_AvatarTextures[texIndex] = value;
                    });
                }
            }
        }

        private readonly AgentWearables m_Wearables = new AgentWearables();
        public AgentWearables Wearables
        {
            get { return m_Wearables; }

            set { m_Wearables.All = value; }
        }

        public readonly RwLockedDictionaryAutoAdd<AttachmentPoint, RwLockedDictionary<UUID, UUID>> Attachments =
            new RwLockedDictionaryAutoAdd<AttachmentPoint, RwLockedDictionary<UUID, UUID>>(() => new RwLockedDictionary<UUID, UUID>());
        private readonly ReaderWriterLock m_VisualParamsLock = new ReaderWriterLock();
        private byte[] m_VisualParams = new byte[] { 33, 61, 85, 23, 58, 127, 63, 85, 63, 42, 0, 85, 63, 36, 85, 95, 153, 63, 34, 0, 63, 109, 88, 132, 63, 136, 81, 85, 103, 136, 127, 0, 150, 150, 150, 127, 0, 0, 0, 0, 0, 127, 0, 0, 255, 127, 114, 127, 99, 63, 127, 140, 127, 127, 0, 0, 0, 191, 0, 104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 145, 216, 133, 0, 127, 0, 127, 170, 0, 0, 127, 127, 109, 85, 127, 127, 63, 85, 42, 150, 150, 150, 150, 150, 150, 150, 25, 150, 150, 150, 0, 127, 0, 0, 144, 85, 127, 132, 127, 85, 0, 127, 127, 127, 127, 127, 127, 59, 127, 85, 127, 127, 106, 47, 79, 127, 127, 204, 2, 141, 66, 0, 0, 127, 127, 0, 0, 0, 0, 127, 0, 159, 0, 0, 178, 127, 36, 85, 131, 127, 127, 127, 153, 95, 0, 140, 75, 27, 127, 127, 0, 150, 150, 198, 0, 0, 63, 30, 127, 165, 209, 198, 127, 127, 153, 204, 51, 51, 255, 255, 255, 204, 0, 255, 150, 150, 150, 150, 150, 150, 150, 150, 150, 150, 0, 150, 150, 150, 150, 150, 0, 127, 127, 150, 150, 150, 150, 150, 150, 150, 150, 0, 0, 150, 51, 132, 150, 150, 150 };
        public double AvatarHeight;
        public int Serial = 1;

        public static byte[] BakeIndices => new byte[] { 8, 9, 10, 11, 19, 20, 40, 41, 42, 43, 44 };

        public static readonly int MaxVisualParams = 255;

        public readonly AvatarTextureData AvatarTextures = new AvatarTextureData();

        public byte[] VisualParams
        {
            get
            {
                return m_VisualParamsLock.AcquireReaderLock(() =>
                {
                    var res = new byte[m_VisualParams.Length];
                    Buffer.BlockCopy(m_VisualParams, 0, res, 0, m_VisualParams.Length);
                    return res;
                });
            }
            set
            {
                m_VisualParamsLock.AcquireWriterLock(() =>
                {
                    int VisualParamCount = MaxVisualParams < value.Length ? MaxVisualParams : value.Length;
                    m_VisualParams = new byte[VisualParamCount];
                    Buffer.BlockCopy(value, 0, m_VisualParams, 0, VisualParamCount);
                });
            }
        }

        [Serializable]
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
            using (var ms = new MemoryStream(nc.Text.ToUTF8Bytes()))
            {
                var m = LlsdXml.Deserialize(ms) as Map;
                if(m == null)
                {
                    throw new InvalidAppearanceInfoSerializationException();
                }

                var appearanceInfo = new AppearanceInfo
                {
                    AvatarHeight = m["height"].AsReal,
                    VisualParams = m["visualparams"] as BinaryData
                };
                var wearables = m["wearables"] as AnArray;
                var textures = m["textures"] as AnArray;
                foreach(var iv in m["attachments"] as AnArray)
                {
                    var im = iv as Map;
                    if(im == null)
                    {
                        throw new InvalidAppearanceInfoSerializationException();
                    }
                    var ap = (AttachmentPoint)im["point"].AsInt;

                    appearanceInfo.Attachments[ap][im["item"].AsUUID] = im["asset"].AsUUID;
                }

                AgentWearables wearabledata = new AgentWearables();
                for(int i = 0; i < wearables.Count; ++i)
                {
                    var wearablesAt = wearables[i] as AnArray;
                    if(wearablesAt == null)
                    {
                        throw new InvalidAppearanceInfoSerializationException();
                    }
                    foreach(var ivw in wearablesAt)
                    {
                        var mw = ivw as Map;
                        if(mw == null)
                        {
                            throw new InvalidAppearanceInfoSerializationException();
                        }
                        var wi = new AgentWearables.WearableInfo
                        {
                            ItemID = mw["item"].AsUUID,
                            AssetID = mw["asset"].AsUUID
                        };
                        wearabledata.Add((WearableType)i, wi);
                    }
                }
                appearanceInfo.Wearables = wearabledata;

                for (int i = 0; i < textures.Count; ++i)
                {
                    appearanceInfo.AvatarTextures[i] = textures[i].AsUUID;
                }
                if (m.ContainsKey("serial"))
                {
                    appearanceInfo.Serial = m["serial"].AsInt;
                }
                return appearanceInfo;
            }
        }

        public static explicit operator Notecard(AppearanceInfo appearanceInfo)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = ms.UTF8XmlTextWriter())
                {
                    writer.Formatting = Formatting.Indented;
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
                                        foreach (var wearable in appearanceInfo.Wearables[(WearableType)i])
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
                                foreach (var tex in appearanceInfo.AvatarTextures.All)
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
                                foreach (var kvpAp in appearanceInfo.Attachments)
                                {
                                    foreach (var attachmentKvp in kvpAp.Value)
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

                return new Notecard
                {
                    Text = ms.ToArray().FromUTF8Bytes()
                };
            }
        }
    }
}
