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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Viewer.Messages.Appearance;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scene.Agent
{
    public partial class Agent
    {
        private readonly AgentWearables m_Wearables = new AgentWearables();

        private Vector3 m_AvatarSize = new Vector3(0.45, 0.6, 1.9);
        public Vector3 Size
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_AvatarSize;
                }
            }
            set
            {
                bool isUpdated;
                Vector3 v = value;
                v.X = v.X.Clamp(0.1, 32);
                v.Y = v.Y.Clamp(0.1, 32);
                v.Z = v.Z.Clamp(0.1, 32);
                lock(m_DataLock)
                {
                    isUpdated = !m_AvatarSize.ApproxEquals(v, double.Epsilon);
                    m_AvatarSize = v;
                }
                if(isUpdated)
                {
                    InvokeOnAppearanceUpdate();
                }
            }
        }

        public AgentAttachments Attachments { get; }

        public AgentWearables Wearables
        {
            get { return m_Wearables; }

            set { m_Wearables.All = value; }
        }

        private readonly ReaderWriterLock m_VisualParamsLock = new ReaderWriterLock();
        private byte[] m_VisualParams = new byte[] { 33, 61, 85, 23, 58, 127, 63, 85, 63, 42, 0, 85, 63, 36, 85, 95, 153, 63, 34, 0, 63, 109, 88, 132, 63, 136, 81, 85, 103, 136, 127, 0, 150, 150, 150, 127, 0, 0, 0, 0, 0, 127, 0, 0, 255, 127, 114, 127, 99, 63, 127, 140, 127, 127, 0, 0, 0, 191, 0, 104, 0, 0, 0, 0, 0, 0, 0, 0, 0, 145, 216, 133, 0, 127, 0, 127, 170, 0, 0, 127, 127, 109, 85, 127, 127, 63, 85, 42, 150, 150, 150, 150, 150, 150, 150, 25, 150, 150, 150, 0, 127, 0, 0, 144, 85, 127, 132, 127, 85, 0, 127, 127, 127, 127, 127, 127, 59, 127, 85, 127, 127, 106, 47, 79, 127, 127, 204, 2, 141, 66, 0, 0, 127, 127, 0, 0, 0, 0, 127, 0, 159, 0, 0, 178, 127, 36, 85, 131, 127, 127, 127, 153, 95, 0, 140, 75, 27, 127, 127, 0, 150, 150, 198, 0, 0, 63, 30, 127, 165, 209, 198, 127, 127, 153, 204, 51, 51, 255, 255, 255, 204, 0, 255, 150, 150, 150, 150, 150, 150, 150, 150, 150, 150, 0, 150, 150, 150, 150, 150, 0, 127, 127, 150, 150, 150, 150, 150, 150, 150, 150, 0, 0, 150, 51, 132, 150, 150, 150 };
        private readonly AppearanceInfo.AvatarTextureData m_TextureHashes = new AppearanceInfo.AvatarTextureData();
        private readonly AppearanceInfo.AvatarTextureData m_Textures = new AppearanceInfo.AvatarTextureData();
        public int Serial = 1;
        public const int MaxVisualParams = 260;
        protected const int NUM_AVATAR_TEXTURES = 21;

        public Action<IAgent> OnAppearanceUpdate;

        protected void InvokeOnAppearanceUpdate()
        {
            foreach(Action<IAgent> del in OnAppearanceUpdate?.GetInvocationList() ?? new Delegate[0])
            {
                try
                {
                    del(this);
                }
                catch(Exception e)
                {
                    m_Log.Debug("Exception on OnAppearanceUpdate", e);
                }
            }
        }

        public AvatarAppearance GetAvatarAppearanceMsg()
        {
            var appearance = new AvatarAppearance()
            {
                Sender = ID,
                TextureEntry = TextureEntry,
                VisualParams = VisualParams
            };
            var e = new AvatarAppearance.AppearanceDataEntry()
            {
                AppearanceVersion = 1,
                CofVersion = Appearance.Serial
            };
            appearance.AppearanceData.Add(e);
            return appearance;
        }

        public TextureEntry TextureEntry
        {
            get
            {
                var te = new TextureEntry();
                UUID[] textures = Textures.All;
                te.DefaultTexture.TextureID = textures[0];
                for (int i = 0; i < AppearanceInfo.AvatarTextureData.TextureCount; ++i)
                {
                    if (te.DefaultTexture.TextureID != textures[i])
                    {
                        te[(uint)i].TextureID = textures[i];
                    }
                }
                return te;
            }
        }

        protected void SetTextureEntryBytes(byte[] data)
        {
#warning implement if supporting fallback to non-SSB
        }

        public AppearanceInfo.AvatarTextureData Textures
        {
            get { return m_Textures; }

            set { m_Textures.All = value.All; }
        }

        public AppearanceInfo.AvatarTextureData TextureHashes
        {
            get { return m_TextureHashes; }
            set { m_TextureHashes.All = value.All; }
        }

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
                bool updated = false;
                m_VisualParamsLock.AcquireWriterLock(() =>
                {
                    int VisualParamCount = MaxVisualParams < value.Length ? MaxVisualParams : value.Length;
                    if (VisualParamCount == m_VisualParams.Length)
                    {
                        int i;
                        for (i = 0; i < m_VisualParams.Length; ++i)
                        {
                            if (m_VisualParams[i] != value[i])
                            {
                                updated = true;
                            }
                        }
                    }
                    else
                    {
                        updated = true;
                    }
                    m_VisualParams = new byte[VisualParamCount];
                    Buffer.BlockCopy(value, 0, m_VisualParams, 0, VisualParamCount);
                });

                if (updated)
                {
                    InvokeOnAppearanceUpdate();
                }
            }
        }

        private readonly object m_AppearanceUpdateLock = new object();
        public AppearanceInfo Appearance
        {
            get
            {
                var ai = new AppearanceInfo()
                {
                    Wearables = Wearables,
                    VisualParams = VisualParams,
                    AvatarHeight = Size.Z
                };
                ai.Attachments.Clear();
                foreach (ObjectGroup grp in Attachments.All)
                {
                    ai.Attachments[grp.AttachPoint][grp.FromItemID] = grp.OriginalAssetID;
                }
                ai.Serial = Serial;
                ai.AvatarTextures.All = Textures.All;
                return ai;
            }

            set
            {
                /* check for assets being valid */
                Dictionary<WearableType, List<AgentWearables.WearableInfo>> aw = value.Wearables;
                foreach (KeyValuePair<WearableType, List<AgentWearables.WearableInfo>> kvp in aw)
                {
                    List<AgentWearables.WearableInfo> lwi = kvp.Value;
                    int c = 0;
                    while (c < kvp.Value.Count)
                    {
                        if (lwi[c].AssetID.Equals(UUID.Zero))
                        {
                            try
                            {
                                InventoryItem item = InventoryService.Item[ID, lwi[c].ItemID];
                                AgentWearables.WearableInfo wi = lwi[c];
                                wi.AssetID = item.AssetID;
                                lwi[c++] = wi;
                            }
                            catch
                            {
                                lwi.RemoveAt(c);
                            }
                        }
                        else
                        {
                            ++c;
                        }
                    }
                }
                lock (m_AppearanceUpdateLock)
                {
                    Wearables.All = aw;
                    VisualParams = value.VisualParams;
                    Serial = value.Serial;
                    m_AvatarSize = new Vector3(0.45, 0.6, value.AvatarHeight);
                    Textures.All = value.AvatarTextures.All;
                    //value.Attachments;
                }
#if DEBUG
                m_Log.DebugFormat("Appearance property setter called for {0}", Owner.FullName);
#endif
                InvokeOnAppearanceUpdate();
            }
        }

        protected virtual AssetServiceInterface SceneAssetService => AssetService;

        public void RebakeAppearance(Action<string> logOutput = null)
        {
            AgentBakeAppearance.LoadAppearanceFromCurrentOutfit(this, SceneAssetService, true, logOutput);
            InvokeOnAppearanceUpdate();
        }

        private void ToUInt16Bytes(double value, double min, double max, byte[] buf, int pos)
        {
            double v = value;
            if (v < min)
            {
                v = min;
            }
            else if (v > max)
            {
                v = max;
            }
            v -= min;
            v = v * 65535 / (max - min);
            byte[] b = BitConverter.GetBytes((UInt16)v);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            Buffer.BlockCopy(b, 0, buf, pos, 2);
        }

        public byte[] TerseData
        {
            get
            {
                Quaternion rotation = Rotation;
                if (SittingOnObject == null)
                {
                    rotation.X = 0;
                    rotation.Y = 0;
                }
                Vector3 angvel = AngularVelocity;
                Vector3 vel = Velocity;
                Vector3 accel = Acceleration;

                var data = new byte[60];
                int pos = 0;
                {
                    byte[] b = BitConverter.GetBytes(LocalID);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    Buffer.BlockCopy(b, 0, data, pos, 4);
                    pos += 4;
                }
                data[pos++] = 0; //State
                data[pos++] = 1;

                /* Collision Plane */
                Vector4 collPlane = CollisionPlane;
                if (collPlane == Vector4.Zero)
                {
                    collPlane = Vector4.UnitW;
                }
                collPlane.ToBytes(data, pos);
                pos += 16;

                Position.ToBytes(data, pos);
                pos += 12;

                ToUInt16Bytes(vel.X, -128f, 128f, data, pos);
                pos += 2;
                ToUInt16Bytes(vel.Y, -128f, 128f, data, pos);
                pos += 2;
                ToUInt16Bytes(vel.Z, -128f, 128f, data, pos);
                pos += 2;

                ToUInt16Bytes(accel.X, -64, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(accel.Y, -64, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(accel.Z, -64, 64f, data, pos);
                pos += 2;

                ToUInt16Bytes(rotation.X, -1f, 1f, data, pos);
                pos += 2;
                ToUInt16Bytes(rotation.Y, -1f, 1f, data, pos);
                pos += 2;
                ToUInt16Bytes(rotation.Z, -1f, 1f, data, pos);
                pos += 2;
                ToUInt16Bytes(rotation.W, -1f, 1f, data, pos);
                pos += 2;

                ToUInt16Bytes(angvel.X, -64f, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(angvel.Y, -64f, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(angvel.Z, -64f, 64f, data, pos);

                return data;
            }
        }
    }
}
