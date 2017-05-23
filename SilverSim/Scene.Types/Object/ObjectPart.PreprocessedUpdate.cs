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
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using SilverSim.Viewer.Messages.Object;
using System;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        private readonly byte[] m_FullUpdateFixedBlock1 = new byte[(int)FullFixedBlock1Offset.BlockLength];
        private readonly byte[] m_FullUpdateFixedBlock2 = new byte[(int)FullFixedBlock2Offset.BlockLength];
        private readonly byte[] m_PropUpdateFixedBlock = new byte[(int)PropertiesFixedBlockOffset.BlockLength];
        private readonly byte[] m_CompressedUpdateFixedBlock = new byte[(int)CompressedUpdateOffset.BlockLength];

        private byte[] m_FullUpdateData;
        private byte[] m_TerseUpdateData;
        private byte[] m_PropUpdateData;
        private byte[] m_CompressedUpdateData;
        private readonly object m_UpdateDataLock = new object();

        private int m_ObjectSerial;

        public int SerialNumber => m_ObjectSerial;

        public byte[] FullUpdateData
        {
            get
            {
                if(m_FullUpdateData == null)
                {
                    UpdateData(UpdateDataFlags.Full);
                }
                return m_FullUpdateData;
            }
        }

        public byte[] TerseUpdateData
        {
            get
            {
                if(m_TerseUpdateData == null)
                {
                    UpdateData(UpdateDataFlags.Terse);
                }
                return m_TerseUpdateData;
            }
        }

        public byte[] CompressedUpdateData
        {
            get
            {
                if(m_CompressedUpdateData == null)
                {
                    UpdateData(UpdateDataFlags.Compressed);
                }
                return m_CompressedUpdateData;
            }
        }

        public byte[] PropertiesUpdateData
        {
            get
            {
                if(m_PropUpdateData == null)
                {
                    UpdateData(UpdateDataFlags.Properties);
                }
                return m_PropUpdateData;
            }
        }

        public enum CompressedUpdateOffset
        {
            PathCurve = 0,
            PathBegin = PathCurve + 1,
            PathEnd = PathBegin + 2,
            PathScaleX = PathEnd + 2,
            PathScaleY = PathScaleX + 1,
            PathShearX = PathScaleY + 1,
            PathShearY = PathShearX + 1,
            PathTwist = PathShearY + 1,
            PathTwistBegin = PathTwist + 1,
            PathRadiusOffset = PathTwistBegin + 1,
            PathTaperX = PathRadiusOffset + 1,
            PathTaperY = PathTaperX + 1,
            PathRevolutions = PathTaperY + 1,
            PathSkew = PathRevolutions + 1,
            ProfileCurve = PathSkew + 1,
            ProfileBegin = ProfileCurve + 1,
            ProfileEnd = ProfileBegin + 2,
            ProfileHollow = ProfileEnd + 2,
            
            BlockLength = ProfileHollow + 2
        }

        public enum FullFixedBlock1Offset
        {
            LocalID = 0,
            State = LocalID + 4,
            FullID = State + 1,
            CRC = FullID + 16,
            PCode = CRC + 4,
            Material = PCode + 1,
            ClickAction = Material + 1,
            Scale = ClickAction + 1,
            ObjectDataLength = Scale + 12,
            ObjectData = ObjectDataLength + 1,
                ObjectData_Position = ObjectData + 0,
                ObjectData_Velocity = ObjectData + 12,
                ObjectData_Acceleration = ObjectData + 24,
                ObjectData_Rotation = ObjectData + 36,
                ObjectData_AngularVelocity = ObjectData + 48,
            ParentID = ObjectData + 60,
            UpdateFlags = ParentID + 4,
            PathCurve = UpdateFlags + 4,
            ProfileCurve = PathCurve + 1,
            PathBegin = ProfileCurve + 1,
            PathEnd = PathBegin + 2,
            PathScaleX = PathEnd + 2,
            PathScaleY = PathScaleX + 1,
            PathShearX = PathScaleY + 1,
            PathShearY = PathShearX + 1,
            PathTwist = PathShearY + 1,
            PathTwistBegin = PathTwist + 1,
            PathRadiusOffset = PathTwistBegin + 1,
            PathTaperX = PathRadiusOffset + 1,
            PathTaperY = PathTaperX + 1,
            PathRevolutions = PathTaperY + 1,
            PathSkew = PathRevolutions + 1,
            ProfileBegin = PathSkew + 1,
            ProfileEnd = ProfileBegin + 2,
            ProfileHollow = ProfileEnd + 2,

            BlockLength = ProfileHollow + 2
        }

        public enum FullFixedBlock2Offset
        {
            LoopedSound = 0,
            SoundOwner = LoopedSound + 16,
            SoundGain = SoundOwner + 16,
            SoundFlags = SoundGain + 4,
            SoundRadius = SoundFlags + 1,
            JointType = SoundRadius + 4,
            JointPivot = JointType + 1,
            JointAxisOrAnchor = JointPivot + 12,

            BlockLength = JointAxisOrAnchor + 12
        }

        public enum PropertiesFixedBlockOffset
        {
            ObjectID = 0,
            CreatorID = ObjectID + 16,
            OwnerID = CreatorID + 16,
            GroupID = OwnerID + 16,
            CreationDate = GroupID + 16,
            BaseMask = CreationDate + 8,
            OwnerMask = BaseMask + 4,
            GroupMask = OwnerMask + 4,
            EveryoneMask = GroupMask + 4,
            NextOwnerMask = EveryoneMask + 4,
            OwnershipCost = NextOwnerMask + 4,
            SaleType = OwnershipCost + 4,
            SalePrice = SaleType + 1,
            AggregatePerms = SalePrice + 4,
            AggregatePermTextures = AggregatePerms + 1,
            AggregatePermTexturesOwner = AggregatePermTextures + 1,
            Category = AggregatePermTexturesOwner + 1,
            InventorySerial = Category + 4,
            ItemID = InventorySerial + 2,
            FolderID = ItemID + 16,
            FromTaskID = FolderID + 16,
            LastOwnerID = FromTaskID + 16,
            BlockLength = LastOwnerID + 16
        }

        [Flags]
        public enum UpdateDataFlags : uint
        {
            None = 0,
            Full = 1,
            Terse = 2,
            Properties = 4,
            Compressed = 8,
            All = 0xFFFFFFFF
        }

        void PutInt32LEToBytes(byte[] b, int offset, Int32 vs)
        {
            var v = (UInt32)vs;
            b[offset + 0] = (byte)((v >> 0) & 0xFF);
            b[offset + 1] = (byte)((v >> 8) & 0xFF);
            b[offset + 2] = (byte)((v >> 16) & 0xFF);
            b[offset + 3] = (byte)((v >> 24) & 0xFF);
        }

        public void UpdateData(UpdateDataFlags flags)
        {
            UpdateData(flags, true);
        }

        private void UpdateData(UpdateDataFlags flags, bool incSerial)
        {
            lock (m_UpdateDataLock)
            {
                if (incSerial || 0 == m_ObjectSerial)
                {
                    int objectSerial = m_ObjectSerial + 1;
                    if (objectSerial == 0)
                    {
                        objectSerial = 1;
                    }
                    m_ObjectSerial = objectSerial;
                }

                if(m_FullUpdateData == null)
                {
                    flags |= UpdateDataFlags.Full;
                }
                if (m_TerseUpdateData == null)
                {
                    flags |= UpdateDataFlags.Terse;
                }
                if (m_PropUpdateData == null)
                {
                    flags |= UpdateDataFlags.Properties;
                }
                if(m_CompressedUpdateData == null)
                {
                    flags |= UpdateDataFlags.Compressed;
                }

                var primUpdateFlags = (uint)m_PrimitiveFlags;
                uint parentID = 0;
                string name = string.Empty;

                primUpdateFlags &= (uint)(~(
                    PrimitiveFlags.AllowInventoryDrop |
                    PrimitiveFlags.InventoryEmpty |
                    PrimitiveFlags.Physics |
                    PrimitiveFlags.Scripted |
                    PrimitiveFlags.VolumeDetect |
                    PrimitiveFlags.ObjectGroupOwned |
                    PrimitiveFlags.Temporary |
                    PrimitiveFlags.TemporaryOnRez));

                var objectGroup = ObjectGroup;
                if (objectGroup != null)
                {
                    if (IsAllowedDrop)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.AllowInventoryDrop;
                    }
                    if (Inventory.Count == 0)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.InventoryEmpty;
                    }
                    if (objectGroup.IsPhysics)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.Physics;
                    }
                    if (IsScripted)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.Scripted;
                    }
                    if (objectGroup.IsVolumeDetect)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.VolumeDetect;
                    }
                    if (objectGroup.IsGroupOwned)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.ObjectGroupOwned;
                    }
                    if (objectGroup.IsTemporary)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.Temporary;
                    }
                    if (objectGroup.IsTempOnRez)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.TemporaryOnRez;
                    }

                    if (objectGroup.RootPart != this)
                    {
                        parentID = ObjectGroup.RootPart.LocalID;
                    }
                    else if (objectGroup.IsAttached)
                    {
                        IAgent agent;
                        SceneInterface scene = objectGroup.Scene;
                        if(scene != null && scene.Agents.TryGetValue(Owner.ID, out agent))
                        {
                            parentID = agent.LocalID;
                        }
                        else
                        {
#if DEBUG
                            m_Log.DebugFormat("Failed to find agent for attachment");
#endif
                        }
                    }
                    if (objectGroup.IsAttached)
                    {
                        name = string.Format("AttachItemID STRING RW SV {0}", objectGroup.FromItemID);
                    }
                    else
                    {
                        name = string.Empty;
                    }
                }

                #region ObjectUpdate
                if ((flags & UpdateDataFlags.Full) != 0)
                {
                    var textureEntry = TextureEntryBytes;
                    var textureAnimEntry = TextureAnimationBytes;
                    var text = Text;
                    var textBytes = text.Text.ToUTF8Bytes();
                    var psBlock = ParticleSystemBytes;
                    var extraParams = ExtraParamsBytes;
                    var mediaUrlBytes = MediaURL.ToUTF8Bytes();

                    byte[] data;
                    switch (m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PCode])
                    {
                        case (byte)PrimitiveCode.Grass:
                        case (byte)PrimitiveCode.Tree:
                        case (byte)PrimitiveCode.NewTree:
                            data = new byte[] { m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.State] };
                            break;
                        default:
                            data = new byte[0];
                            break;
                    }

                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.UpdateFlags] = (byte)(primUpdateFlags & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.UpdateFlags + 1] = (byte)((primUpdateFlags >> 8) & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.UpdateFlags + 2] = (byte)((primUpdateFlags >> 16) & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.UpdateFlags + 3] = (byte)((primUpdateFlags >> 24) & 0xFF);

                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID] = (byte)(parentID & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 1] = (byte)((parentID >> 8) & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 2] = (byte)((parentID >> 16) & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 3] = (byte)((parentID >> 24) & 0xFF);

                    if(objectGroup == null)
                    {
                        name = string.Empty;
                    }
                    else if (objectGroup.IsAttached)
                    {
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.State]  = (byte)(((byte)ObjectGroup.AttachPoint % 16) * 16 + (((byte)ObjectGroup.AttachPoint / 16)));
                    }
                    else
                    {
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.State] = ObjectGroup.RootPart.Shape.State;
                    }

                    var nameBytes = name.ToUTF8Bytes();

                    int blockSize = m_FullUpdateFixedBlock1.Length + m_FullUpdateFixedBlock2.Length;
                    blockSize += textureEntry.Length + 2;
                    blockSize += textureAnimEntry.Length + 1;
                    blockSize += textBytes.Length + 2;
                    blockSize += nameBytes.Length + 3;
                    blockSize += psBlock.Length + 1;
                    blockSize += extraParams.Length + 1;
                    blockSize += data.Length + 2;
                    blockSize += mediaUrlBytes.Length + 2;
                    blockSize += 4;

                    var newFullData = new byte[blockSize];
                    int offset = 0;

                    Buffer.BlockCopy(m_FullUpdateFixedBlock1, 0, newFullData, 0, m_FullUpdateFixedBlock1.Length);
                    offset += m_FullUpdateFixedBlock1.Length;

                    newFullData[offset++] = (byte)(textureEntry.Length % 256);
                    newFullData[offset++] = (byte)(textureEntry.Length / 256);
                    Buffer.BlockCopy(textureEntry, 0, newFullData, offset, textureEntry.Length);
                    offset += textureEntry.Length;

                    newFullData[offset++] = (byte)textureAnimEntry.Length;
                    Buffer.BlockCopy(textureAnimEntry, 0, newFullData, offset, textureAnimEntry.Length);
                    offset += textureAnimEntry.Length;

                    newFullData[offset++] = (byte)((nameBytes.Length + 1) % 256);
                    newFullData[offset++] = (byte)((nameBytes.Length + 1) / 256);
                    Buffer.BlockCopy(nameBytes, 0, newFullData, offset, nameBytes.Length);
                    offset += nameBytes.Length;
                    newFullData[offset++] = 0;

                    newFullData[offset++] = (byte)(data.Length % 256);
                    newFullData[offset++] = (byte)(data.Length / 256);
                    Buffer.BlockCopy(data, 0, newFullData, offset, data.Length);
                    offset += data.Length;

                    newFullData[offset++] = (byte)(textBytes.Length + 1);
                    Buffer.BlockCopy(textBytes, 0, newFullData, offset, textBytes.Length);
                    offset += textBytes.Length;
                    newFullData[offset++] = 0;

                    newFullData[offset++] = text.TextColor.R_AsByte;
                    newFullData[offset++] = text.TextColor.G_AsByte;
                    newFullData[offset++] = text.TextColor.B_AsByte;
                    newFullData[offset++] = (byte)(255 - text.TextColor.A_AsByte);

                    newFullData[offset++] = (byte)(mediaUrlBytes.Length + 1);
                    Buffer.BlockCopy(mediaUrlBytes, 0, newFullData, offset, mediaUrlBytes.Length);
                    offset += mediaUrlBytes.Length;
                    newFullData[offset++] = 0;

                    newFullData[offset++] = (byte)psBlock.Length;
                    Buffer.BlockCopy(psBlock, 0, newFullData, offset, psBlock.Length);
                    offset += psBlock.Length;

                    newFullData[offset++] = (byte)extraParams.Length;
                    Buffer.BlockCopy(extraParams, 0, newFullData, offset, extraParams.Length);
                    offset += extraParams.Length;

                    Buffer.BlockCopy(m_FullUpdateFixedBlock2, 0, newFullData, offset, m_FullUpdateFixedBlock2.Length);

                    m_FullUpdateData = newFullData;
                }
                #endregion

                #region Terse Update
                if ((flags & UpdateDataFlags.Terse) != 0)
                {
                    byte[] terseData = TerseData;
                    byte[] textureEntry = TextureEntryBytes;

                    var newTerseData = new byte[3 + terseData.Length + textureEntry.Length];

                    int offset = 0;
                    newTerseData[offset++] = (byte)terseData.Length;
                    Buffer.BlockCopy(terseData, 0, newTerseData, offset, terseData.Length);
                    offset += terseData.Length;

                    newTerseData[offset++] = (byte)(textureEntry.Length % 256);
                    newTerseData[offset++] = (byte)(textureEntry.Length / 256);
                    Buffer.BlockCopy(textureEntry, 0, newTerseData, offset, textureEntry.Length);
                    m_TerseUpdateData = newTerseData;
                }
                #endregion

                #region CompressedUpdate
                if((flags & UpdateDataFlags.Compressed) != 0)
                {
                    var partsystem = ParticleSystemBytes;
                    if ((partsystem.Length != 0 && partsystem.Length != 86) || Velocity.Length > double.Epsilon)
                    {
                        m_CompressedUpdateData = null;
                    }
                    else
                    {
                        var compressedflags = ObjectUpdateCompressed.CompressedFlags.None;
                        TextParam textparam = Text;
                        int compressedSize = 80;
                        byte[] textbytes = null;
                        byte[] mediaurlbytes = null;
                        byte[] namebytes = null;
                        var textureanimbytes = TextureAnimationBytes;
                        var textureentry = TextureEntryBytes;
                        var extrabytes = ExtraParamsBytes;

                        if(extrabytes == null || extrabytes.Length == 0)
                        {
                            extrabytes = new byte[1] { 0 };
                        }

                        compressedSize += (4 + textureentry.Length) + extrabytes.Length + m_CompressedUpdateFixedBlock.Length;

                        if (AngularVelocity.Length > double.Epsilon)
                        {
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasAngularVelocity;
                            compressedSize += 12;
                        }

                        if(textureanimbytes != null && textureanimbytes.Length != 0)
                        {
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.TextureAnimation;
                            compressedSize += textureanimbytes.Length + 4;
                        }

                        if(parentID != 0)
                        {
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasParent;
                            compressedSize += 4;
                        }

                        if(!string.IsNullOrEmpty(name))
                        {
                            namebytes = (name + "\0").ToUTF8Bytes();
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasNameValues;
                            compressedSize += namebytes.Length;
                        }

                        if(textparam.Text.Length != 0)
                        {
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasText;
                            textbytes = (textparam.Text + "\0").ToUTF8Bytes();
                            compressedSize += (textbytes.Length + 4);
                        }

                        string mediaUrl = MediaURL;
                        if(!string.IsNullOrEmpty(mediaUrl))
                        {
                            mediaurlbytes = (MediaURL + "\0").ToUTF8Bytes();
                            compressedSize += mediaurlbytes.Length;
                        }

                        if(partsystem.Length != 0)
                        {
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasParticles;
                            compressedSize += partsystem.Length;
                        }

                        var compressedData = new byte[compressedSize];
                        ID.ToBytes(compressedData, 0);
                        compressedData[16] = (byte)(LocalID & 0xFF);
                        compressedData[17] = (byte)((LocalID >> 8) & 0xFF);
                        compressedData[18] = (byte)((LocalID >> 16) & 0xFF);
                        compressedData[19] = (byte)((LocalID >> 24) & 0xFF);
                        var shape = Shape;
                        compressedData[20] = (byte)shape.PCode;
                        compressedData[21] = shape.State;
                        //CRC
                        compressedData[22] = 0;
                        compressedData[23] = 0;
                        compressedData[24] = 0;
                        compressedData[25] = 0;
                        compressedData[26] = (byte)Material;
                        compressedData[27] = (byte)ClickAction;
                        Size.ToBytes(compressedData, 28);
                        Position.ToBytes(compressedData, 40);
                        // 12 byte rotation at 52
                        compressedData[64] = (byte)((uint)compressedflags & 0xFF);
                        compressedData[65] = (byte)(((uint)compressedflags >> 8) & 0xFF);
                        compressedData[66] = (byte)(((uint)compressedflags >> 16) & 0xFF);
                        compressedData[67] = (byte)(((uint)compressedflags >> 24) & 0xFF);
                        Owner.ID.ToBytes(compressedData, 68);

                        int offset = 80;

                        //Angular velocity
                        if((compressedflags & ObjectUpdateCompressed.CompressedFlags.HasAngularVelocity) != 0)
                        {
                            AngularVelocity.ToBytes(compressedData, offset);
                            offset += 12;
                        }

                        //Parent
                        if(parentID != 0)
                        {
                            compressedData[offset] = (byte)(parentID & 0xFF);
                            compressedData[offset + 1] = (byte)((parentID >> 8) & 0xFF);
                            compressedData[offset + 2] = (byte)((parentID >> 16) & 0xFF);
                            compressedData[offset + 3] = (byte)((parentID >> 24) & 0xFF);
                            offset += 4;
                        }
                        
                        //Hover text
                        if(textbytes != null)
                        {
                            Buffer.BlockCopy(textbytes, 0, compressedData, offset, textbytes.Length);
                            offset += textbytes.Length;
                            compressedData[offset++] = textparam.TextColor.R_AsByte;
                            compressedData[offset++] = textparam.TextColor.G_AsByte;
                            compressedData[offset++] = textparam.TextColor.B_AsByte;
                            compressedData[offset++] = (byte)(255 - textparam.TextColor.A_AsByte);
                        }

                        //Media url
                        if(mediaurlbytes != null)
                        {
                            Buffer.BlockCopy(mediaurlbytes, 0, compressedData, offset, mediaurlbytes.Length);
                            offset += mediaurlbytes.Length;
                        }

                        //Particle system
                        if(partsystem.Length != 0)
                        {
                            Buffer.BlockCopy(partsystem, 0, compressedData, offset, partsystem.Length);
                            offset += partsystem.Length;
                        }

                        //ExtraParams
                        Buffer.BlockCopy(extrabytes, 0, compressedData, offset, extrabytes.Length);
                        offset += extrabytes.Length;

                        if((compressedflags & ObjectUpdateCompressed.CompressedFlags.HasNameValues) != 0)
                        {
                            Buffer.BlockCopy(namebytes, 0, compressedData, offset, namebytes.Length);
                            offset += namebytes.Length;
                        }

                        //PShape
                        Buffer.BlockCopy(m_CompressedUpdateFixedBlock, 0, compressedData, offset, m_CompressedUpdateFixedBlock.Length);
                        offset += m_CompressedUpdateFixedBlock.Length;

                        //TextureEntry
                        compressedData[offset++] = (byte)(textureentry.Length & 0xFF);
                        compressedData[offset++] = (byte)((textureentry.Length >> 8) & 0xFF);
                        compressedData[offset++] = (byte)((textureentry.Length >> 16) & 0xFF);
                        compressedData[offset++] = (byte)((textureentry.Length >> 24) & 0xFF);

                        //TextureEntry
                        Buffer.BlockCopy(textureentry, 0, compressedData, offset, textureentry.Length);
                        offset += textureentry.Length;

                        //TextureAnim
                        if((compressedflags & ObjectUpdateCompressed.CompressedFlags.TextureAnimation) != 0)
                        {
                            compressedData[offset++] = (byte)(textureanimbytes.Length & 0xFF);
                            compressedData[offset++] = (byte)((textureanimbytes.Length >> 8) & 0xFF);
                            compressedData[offset++] = (byte)((textureanimbytes.Length >> 16) & 0xFF);
                            compressedData[offset++] = (byte)((textureanimbytes.Length >> 24) & 0xFF);

                            Buffer.BlockCopy(textureanimbytes, 0, compressedData, offset, textureanimbytes.Length);
                        }
                        m_CompressedUpdateData = compressedData;
                    }
                }
                #endregion

                #region ObjectProperties
                if ((flags & UpdateDataFlags.Properties) != 0)
                {
                    var nameBytes = Name.ToUTF8Bytes();
                    var descriptionBytes = Description.ToUTF8Bytes();
                    var touchNameBytes = TouchText.ToUTF8Bytes();
                    var sitNameBytes = SitText.ToUTF8Bytes();
                    
                    int propDataLength = m_PropUpdateFixedBlock.Length + 9 + 
                        nameBytes.Length + 
                        descriptionBytes.Length + 
                        touchNameBytes.Length + 
                        sitNameBytes.Length;

                    var newPropData = new byte[propDataLength];

                    int offset = 0;

                    if (ObjectGroup != null)
                    {
                        ObjectGroup.Owner.ID.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.OwnerID);
                        ObjectGroup.Group.ID.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.GroupID);
                        ObjectGroup.LastOwner.ID.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.LastOwnerID);
                        m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.SaleType] = (byte)ObjectGroup.SaleType;
                        PutInt32LEToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.SalePrice, ObjectGroup.SalePrice);
                        PutInt32LEToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.OwnershipCost, ObjectGroup.OwnershipCost);
                        PutInt32LEToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.Category, (int)ObjectGroup.Category);
                        m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.AggregatePerms] = (byte)BaseMask.GetAggregatePermissions();
                        //AggregatePermTextures = AggregatePerms + 1,
                        //AggregatePermTexturesOwner = AggregatePermTextures + 1,
                        //ItemID = InventorySerial + 2,
                        //FolderID = ItemID + 16,
                        //FromTaskID = FolderID + 16,
                    }

                    Buffer.BlockCopy(m_PropUpdateFixedBlock, 0, newPropData, offset, m_PropUpdateFixedBlock.Length);
                    offset += m_PropUpdateFixedBlock.Length;

                    newPropData[offset++] = (byte)(nameBytes.Length + 1);
                    Buffer.BlockCopy(nameBytes, 0, newPropData, offset, nameBytes.Length);
                    offset += nameBytes.Length;
                    newPropData[offset++] = 0;

                    newPropData[offset++] = (byte)(descriptionBytes.Length + 1);
                    Buffer.BlockCopy(descriptionBytes, 0, newPropData, offset, descriptionBytes.Length);
                    offset += descriptionBytes.Length;
                    newPropData[offset++] = 0;

                    newPropData[offset++] = (byte)(touchNameBytes.Length + 1);
                    Buffer.BlockCopy(touchNameBytes, 0, newPropData, offset, touchNameBytes.Length);
                    offset += touchNameBytes.Length;
                    newPropData[offset++] = 0;

                    newPropData[offset++] = (byte)(sitNameBytes.Length + 1);
                    Buffer.BlockCopy(sitNameBytes, 0, newPropData, offset, sitNameBytes.Length);
                    offset += sitNameBytes.Length;
                    newPropData[offset++] = 0;

                    newPropData[offset++] = 0;

                    m_PropUpdateData = newPropData;
                }
                #endregion
            }
        }
    }
}
