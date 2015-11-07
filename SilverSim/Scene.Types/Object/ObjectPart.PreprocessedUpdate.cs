// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart
    {
        private byte[] m_FullUpdateFixedBlock1 = new byte[(int)FullFixedBlock1Offset.BlockLength];
        private byte[] m_FullUpdateFixedBlock2 = new byte[(int)FullFixedBlock2Offset.BlockLength];
        private byte[] m_PropUpdateFixedBlock = new byte[(int)PropertiesFixedBlockOffset.BlockLength];

        byte[] m_FullUpdateData;
        byte[] m_TerseUpdateData;
        byte[] m_PropUpdateData;
        object m_UpdateDataLock = new object();

        int m_ObjectSerial;

        public int SerialNumber
        {
            get
            {
                return m_ObjectSerial;
            }
        }

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
        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public enum UpdateDataFlags : uint
        {
            Full = 1,
            Terse = 2,
            Properties = 4,
            All = 0xFFFFFFFF
        }

        void PutInt32LEToBytes(byte[] b, int offset, Int32 vs)
        {
            UInt32 v = (UInt32)vs;
            b[offset + 0] = (byte)((v >> 0) & 0xFF);
            b[offset + 1] = (byte)((v >> 8) & 0xFF);
            b[offset + 2] = (byte)((v >> 16) & 0xFF);
            b[offset + 3] = (byte)((v >> 24) & 0xFF);
        }

        public void UpdateData(UpdateDataFlags flags)
        {
            UpdateData(flags, true);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void UpdateData(UpdateDataFlags flags, bool incSerial)
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

                #region ObjectUpdate
                if((flags & UpdateDataFlags.Full) != 0)
                {
                    byte[] textureEntry = TextureEntryBytes;
                    byte[] textureAnimEntry = TextureAnimationBytes;
                    TextParam text = Text;
                    byte[] textBytes = UTF8NoBOM.GetBytes(text.Text);
                    byte[] psBlock = ParticleSystemBytes;
                    byte[] extraParams = ExtraParamsBytes;
                    byte[] mediaUrlBytes = UTF8NoBOM.GetBytes(MediaURL);

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

                    uint primUpdateFlags = 0;

                    string name;

                    if (ObjectGroup != null)
                    {
                        if (IsAllowedDrop)
                        {
                            primUpdateFlags |= (uint)PrimitiveFlags.AllowInventoryDrop;
                        }
                        if (Inventory.Count == 0)
                        {
                            primUpdateFlags |= (uint)PrimitiveFlags.InventoryEmpty;
                        }
                        if (ObjectGroup.IsPhysics)
                        {
                            primUpdateFlags |= (uint)PrimitiveFlags.Physics;
                        }
                        if (Inventory.CountScripts != 0)
                        {
                            primUpdateFlags |= (uint)PrimitiveFlags.Scripted;
                        }
                        if (ObjectGroup.IsGroupOwned)
                        {
                            primUpdateFlags |= (uint)PrimitiveFlags.ObjectGroupOwned;
                        }
                        if (ObjectGroup.IsTemporary)
                        {
                            primUpdateFlags |= (uint)PrimitiveFlags.Temporary;
                        }
                        if (ObjectGroup.IsTempOnRez)
                        {
                            primUpdateFlags |= (uint)PrimitiveFlags.TemporaryOnRez;
                        }

                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.UpdateFlags] = (byte)(primUpdateFlags & 0xFF);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.UpdateFlags + 1] = (byte)((primUpdateFlags >> 8) & 0xFF);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.UpdateFlags + 2] = (byte)((primUpdateFlags >> 16) & 0xFF);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.UpdateFlags + 3] = (byte)((primUpdateFlags >> 24) & 0xFF);

                        if (ObjectGroup.RootPart != this)
                        {
                            uint parentID = ObjectGroup.RootPart.LocalID;
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID] = (byte)(parentID & 0xFF);
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 1] = (byte)((parentID >> 8) & 0xFF);
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 2] = (byte)((parentID >> 16) & 0xFF);
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 3] = (byte)((parentID >> 24) & 0xFF);
                        }
                        else if (ObjectGroup.IsAttached)
                        {
                            uint parentID;

                            try
                            {
                                parentID = ObjectGroup.Scene.Agents[Owner.ID].LocalID;
                            }
                            catch
                            {
                                parentID = 0;
                            }
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID] = (byte)(parentID & 0xFF);
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 1] = (byte)((parentID >> 8) & 0xFF);
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 2] = (byte)((parentID >> 16) & 0xFF);
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 3] = (byte)((parentID >> 24) & 0xFF);
                        }
                        else
                        {
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID] = 0;
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 1] = 0;
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 2] = 0;
                            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ParentID + 3] = 0;
                        }
                    }

                    if(ObjectGroup == null)
                    {
                        name = string.Empty;
                    }
                    else if (ObjectGroup.IsAttached)
                    {
                        name = string.Format("AttachItemID STRING RW SV {0}", ObjectGroup.FromItemID);
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.State]  = (byte)(((byte)ObjectGroup.AttachPoint % 16) * 16 + (((byte)ObjectGroup.AttachPoint / 16)));
                    }
                    else
                    {
                        name = string.Empty;
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.State] = ObjectGroup.RootPart.Shape.State;
                    }

                    byte[] nameBytes = UTF8NoBOM.GetBytes(name);

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

                    byte[] newFullData = new byte[blockSize];
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
                    byte[] textureEntry = m_TextureEntryBytes;

                    byte[] newTerseData = new byte[3 + terseData.Length + textureEntry.Length];

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

                #region ObjectProperties
                if ((flags & UpdateDataFlags.Properties) != 0)
                {
                    byte[] nameBytes = UTF8NoBOM.GetBytes(Name);
                    byte[] descriptionBytes = UTF8NoBOM.GetBytes(Description);
                    byte[] touchNameBytes = UTF8NoBOM.GetBytes(TouchText);
                    byte[] sitNameBytes = UTF8NoBOM.GetBytes(SitText);
                    
                    int propDataLength = m_PropUpdateFixedBlock.Length + 9 + 
                        nameBytes.Length + 
                        descriptionBytes.Length + 
                        touchNameBytes.Length + 
                        sitNameBytes.Length;

                    byte[] newPropData = new byte[propDataLength];

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
                        //AggregatePerms = SalePrice + 4,
                        //AggregatePermTextures = AggregatePerms + 1,
                        //AggregatePermTexturesOwner = AggregatePermTextures + 1,
                        //ItemID = InventorySerial + 2,
                        //FolderID = ItemID + 16,
                        //FromTaskID = FolderID + 16,

                        m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.SaleType] = (byte)ObjectGroup.SaleType;
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
