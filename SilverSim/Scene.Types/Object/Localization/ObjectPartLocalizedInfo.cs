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

using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Scene.Types.Physics;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Viewer.Messages.Object;
using System;

namespace SilverSim.Scene.Types.Object.Localization
{
    public sealed partial class ObjectPartLocalizedInfo
    {
        private readonly ObjectPart m_Part;
        private readonly ObjectPartLocalizedInfo m_ParentInfo;
        private readonly object m_DataLock = new object();
        private string m_Name;
        private string m_Description;
        private string m_TouchText;
        private string m_SitText;

        private void InitRootInfo()
        {
            m_Projection = new ProjectionParam();
            m_Sound = new SoundParam();
            m_Name = string.Empty;
            m_Description = string.Empty;
            m_ParticleSystem = new byte[0];
            m_TextureAnimationBytes = new byte[0];
            m_TextureEntry = new TextureEntry();
            m_TextureEntryBytes = new byte[0];
            m_CollisionSound = new CollisionSoundParam();
            m_TouchText = string.Empty;
            m_SitText = string.Empty;
            m_MediaURL = string.Empty;
            m_TextureAnimationBytes = new byte[0];
            m_Text = new TextParam();
        }

        public ObjectPartLocalizedInfo(ObjectPart part, ObjectPartLocalizedInfo src, ObjectPartLocalizedInfo parentInfo)
        {
            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ObjectDataLength] = (byte)60;

            if (parentInfo == null)
            {
                InitRootInfo();
            }

            m_Part = part;
            m_ParentInfo = parentInfo;
            m_TextureEntry = src.TextureEntry;
            m_TextureEntryBytes = m_TextureEntry?.GetBytes();
            m_CollisionSound = src.CollisionSound;
            m_Name = src.Name;
            m_Description = src.Description;
            UpdateMedia(src.Media, UUID.Zero);
            m_MediaURL = src.MediaURL;
            m_ParticleSystem = src.ParticleSystemBytes;
            m_SitText = src.SitText;
            m_Sound = src.Sound;
            m_TextureAnimationBytes = src.TextureAnimationBytes;
            m_TouchText = src.TouchText;
        }

        public ObjectPartLocalizedInfo(ObjectPart part, ObjectPartLocalizedInfo parentInfo = null)
        {
            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ObjectDataLength] = (byte)60;

            if (parentInfo == null)
            {
                InitRootInfo();
            }

            m_Part = part;
            m_ParentInfo = parentInfo;
        }

        internal void SetBaseMask(InventoryPermissionsMask perms)
        {
            byte[] b = BitConverter.GetBytes((uint)perms);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            lock (m_UpdateDataLock)
            {
                Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.BaseMask, b.Length);
            }
        }

        internal void SetOwnerMask(InventoryPermissionsMask perms)
        {
            byte[] b = BitConverter.GetBytes((uint)perms);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            lock (m_UpdateDataLock)
            {
                Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.OwnerMask, b.Length);
            }
        }

        internal void SetGroupMask(InventoryPermissionsMask perms)
        {
            byte[] b = BitConverter.GetBytes((uint)perms);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            lock (m_UpdateDataLock)
            {
                Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.GroupMask, b.Length);
            }
        }

        internal void SetEveryoneMask(InventoryPermissionsMask perms)
        {
            byte[] b = BitConverter.GetBytes((uint)perms);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            lock (m_UpdateDataLock)
            {
                Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.EveryoneMask, b.Length);
            }
        }

        internal void SetNextOwnerMask(InventoryPermissionsMask perms)
        {
            byte[] b = BitConverter.GetBytes((uint)perms);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            lock (m_UpdateDataLock)
            {
                Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.NextOwnerMask, b.Length);
            }
        }

        internal void SetCreationDate(Date date)
        {
            byte[] b = BitConverter.GetBytes(date.DateTimeToUnixTime());
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            lock (m_UpdateDataLock)
            {
                Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.CreationDate, 8);
            }
        }

        internal void SetCreator(UUI id)
        {
            lock (m_DataLock)
            {
                id.ID.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.CreatorID);
            }
        }

        internal void SetClickAction(ClickActionType clickaction)
        {
            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ClickAction] = (byte)clickaction;
        }

        internal void SetLocalID(uint localid)
        {
            lock (m_DataLock)
            {
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID] = (byte)(localid & 0xFF);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID + 1] = (byte)((localid >> 8) & 0xFF);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID + 2] = (byte)((localid >> 16) & 0xFF);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID + 3] = (byte)((localid >> 24) & 0xFF);
            }
        }

        internal void SetVelocity(Vector3 vel)
        {
            lock (m_UpdateDataLock)
            {
                vel.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Velocity);
            }
        }

        internal void SetAngularVelocity(Vector3 vel)
        {
            lock (m_UpdateDataLock)
            {
                vel.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_AngularVelocity);
            }
        }

        internal void SetMaterial(PrimitiveMaterial material)
        {
            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.Material] = (byte)material;
        }

        internal void SetScale(Vector3 scale)
        {
            lock (m_UpdateDataLock)
            {
                scale.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.Scale);
            }
        }

        internal void SetID(UUID id)
        {
            lock (m_UpdateDataLock)
            {
                id.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.FullID);
                id.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.ObjectID);
            }
        }

        internal void SetPosition(Vector3 pos)
        {
            lock (m_UpdateDataLock)
            {
                pos.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Position);
            }
        }

        internal void SetRotation(Quaternion rot)
        {
            lock (m_UpdateDataLock)
            {
                rot.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Rotation);
            }
        }

        internal void PhysicsUpdate(PhysicsStateData data)
        {
            lock (m_UpdateDataLock)
            {
                data.Position.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Position);
                data.Rotation.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Rotation);
                data.Velocity.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Velocity);
                data.AngularVelocity.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_AngularVelocity);
            }
        }

        internal void SetPrimitiveShape(ObjectPart.PrimitiveShape shape)
        {
            lock (m_UpdateDataLock)
            {
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathBegin] = (byte)(shape.PathBegin % 256);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathBegin + 1] = (byte)(shape.PathBegin / 256);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathEnd] = (byte)(shape.PathEnd % 256);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathEnd + 1] = (byte)(shape.PathEnd / 256);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathCurve] = shape.PathCurve;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathRadiusOffset] = (byte)shape.PathRadiusOffset;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathRevolutions] = shape.PathRevolutions;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathScaleX] = shape.PathScaleX;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathScaleY] = shape.PathScaleY;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathShearX] = shape.PathShearX;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathShearY] = shape.PathShearY;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathSkew] = (byte)shape.PathSkew;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathTaperX] = (byte)shape.PathTaperX;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathTaperY] = (byte)shape.PathTaperY;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathTwist] = (byte)shape.PathTwist;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PathTwistBegin] = (byte)shape.PathTwistBegin;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.PCode] = (byte)shape.PCode;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin] = (byte)(shape.ProfileBegin % 256);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin + 1] = (byte)(shape.ProfileBegin / 256);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileCurve] = shape.ProfileCurve;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd] = (byte)(shape.ProfileEnd % 256);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd + 1] = (byte)(shape.ProfileEnd / 256);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow] = (byte)(shape.ProfileHollow % 256);
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow + 1] = (byte)(shape.ProfileHollow / 256);

                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathBegin] = (byte)(shape.PathBegin % 256);
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathBegin + 1] = (byte)(shape.PathBegin / 256);
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathEnd] = (byte)(shape.PathEnd % 256);
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathEnd + 1] = (byte)(shape.PathEnd / 256);
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathCurve] = shape.PathCurve;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathRadiusOffset] = (byte)shape.PathRadiusOffset;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathRevolutions] = shape.PathRevolutions;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathScaleX] = shape.PathScaleX;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathScaleY] = shape.PathScaleY;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathShearX] = shape.PathShearX;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathShearY] = shape.PathShearY;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathSkew] = (byte)shape.PathSkew;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathTaperX] = (byte)shape.PathTaperX;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathTaperY] = (byte)shape.PathTaperY;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathTwist] = (byte)shape.PathTwist;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.PathTwistBegin] = (byte)shape.PathTwistBegin;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileBegin] = (byte)(shape.ProfileBegin % 256);
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileBegin + 1] = (byte)(shape.ProfileBegin / 256);
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileCurve] = shape.ProfileCurve;
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileEnd] = (byte)(shape.ProfileEnd % 256);
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileEnd + 1] = (byte)(shape.ProfileEnd / 256);
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileHollow] = (byte)(shape.ProfileHollow % 256);
                m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileHollow + 1] = (byte)(shape.ProfileHollow / 256);

                if (shape.SculptType == PrimitiveSculptType.Mesh)
                {
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin] = 12500 % 256;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileBegin + 1] = 12500 / 256;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd] = 0;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileEnd + 1] = 0;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow] = 27500 % 256;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ProfileHollow + 1] = 27500 / 256;

                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileBegin] = 12500 % 256;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileBegin + 1] = 12500 / 256;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileEnd] = 0;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileEnd + 1] = 0;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileHollow] = 27500 % 256;
                    m_CompressedUpdateFixedBlock[(int)CompressedUpdateOffset.ProfileHollow + 1] = 27500 / 256;
                }
            }
        }

        internal void SetInventorySerial(int invSerial)
        {
            lock (m_DataLock)
            {
                m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.InventorySerial] = (byte)(invSerial % 256);
                m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.InventorySerial + 1] = (byte)(invSerial / 256);
            }
        }

        public string Name
        {
            get
            {
                return m_Name ?? m_ParentInfo.Name;
            }

            set
            {
                if (value == null)
                {
                    if(m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_Name = null;
                }
                else
                {
                    m_Name = value.FilterToAscii7Printable().TrimToMaxLength(63);
                }
                m_Part.TriggerOnUpdate(0);
            }
        }

        public string Description
        {
            get
            {
                return m_Description ?? m_ParentInfo.Description;
            }

            set
            {
                if (value == null)
                {
                    if (m_ParentInfo == null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_Description = null;
                }
                else
                {
                    m_Description = value.FilterToNonControlChars().TrimToMaxLength(127);
                }
                m_Part.TriggerOnUpdate(0);
            }
        }

        public string SitText
        {
            get
            {
                return m_SitText ?? m_ParentInfo.SitText;
            }
            set
            {
                if (value == null && m_ParentInfo == null)
                {
                    throw new InvalidOperationException();
                }
                m_SitText = value;
                m_Part.TriggerOnUpdate(0);
            }
        }

        public string TouchText
        {
            get
            {
                return m_TouchText ?? m_ParentInfo.TouchText;
            }
            set
            {
                if (value == null && m_ParentInfo == null)
                {
                    throw new InvalidOperationException();
                }
                m_TouchText = value;
                m_Part.TriggerOnUpdate(0);
            }
        }

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
                if (m_FullUpdateData == null)
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
                if (m_TerseUpdateData == null)
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
                if (m_CompressedUpdateData == null)
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
                if (m_PropUpdateData == null)
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

        internal void UpdateData(UpdateDataFlags flags, bool incSerial)
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

                if (m_FullUpdateData == null)
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
                if (m_CompressedUpdateData == null)
                {
                    flags |= UpdateDataFlags.Compressed;
                }

                var primUpdateFlags = (uint)m_Part.Flags;
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

                var objectGroup = m_Part.ObjectGroup;
                if (objectGroup != null)
                {
                    if (m_Part.IsAllowedDrop)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.AllowInventoryDrop;
                    }
                    if (m_Part.Inventory.Count == 0)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.InventoryEmpty;
                    }
                    if (objectGroup.IsPhysics)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.Physics;
                    }
                    if (objectGroup.IsPhantom)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.Phantom;
                    }
                    if (m_Part.IsVolumeDetect)
                    {
                        primUpdateFlags |= (uint)PrimitiveFlags.VolumeDetect;
                    }
                    if (m_Part.IsScripted)
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

                    if (objectGroup == null)
                    {
                        name = string.Empty;
                    }
                    else if (objectGroup.IsAttached)
                    {
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.State] = (byte)(((byte)objectGroup.AttachPoint % 16) * 16 + (((byte)objectGroup.AttachPoint / 16)));
                    }
                    else
                    {
                        m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.State] = objectGroup.RootPart.Shape.State;
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
                    byte[] terseData = m_Part.TerseData;
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
                if ((flags & UpdateDataFlags.Compressed) != 0)
                {
                    var partsystem = ParticleSystemBytes;
                    if ((partsystem.Length != 0 && partsystem.Length != 86) || m_Part.Velocity.Length > double.Epsilon)
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

                        if (extrabytes == null || extrabytes.Length == 0)
                        {
                            extrabytes = new byte[1] { 0 };
                        }

                        compressedSize += (4 + textureentry.Length) + extrabytes.Length + m_CompressedUpdateFixedBlock.Length;

                        if (m_Part.AngularVelocity.Length > double.Epsilon)
                        {
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasAngularVelocity;
                            compressedSize += 12;
                        }

                        if (textureanimbytes != null && textureanimbytes.Length != 0)
                        {
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.TextureAnimation;
                            compressedSize += textureanimbytes.Length + 4;
                        }

                        compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasParent;
                        compressedSize += 4;

                        if (!string.IsNullOrEmpty(name))
                        {
                            namebytes = (name + "\0").ToUTF8Bytes();
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasNameValues;
                            compressedSize += namebytes.Length;
                        }

                        if (textparam.Text.Length != 0)
                        {
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasText;
                            textbytes = (textparam.Text + "\0").ToUTF8Bytes();
                            compressedSize += (textbytes.Length + 4);
                        }

                        string mediaUrl = MediaURL;
                        if (!string.IsNullOrEmpty(mediaUrl))
                        {
                            mediaurlbytes = (MediaURL + "\0").ToUTF8Bytes();
                            compressedSize += mediaurlbytes.Length;
                        }

                        if (partsystem.Length != 0)
                        {
                            compressedflags |= ObjectUpdateCompressed.CompressedFlags.HasParticles;
                            compressedSize += partsystem.Length;
                        }

                        var compressedData = new byte[compressedSize];
                        m_Part.ID.ToBytes(compressedData, 0);
                        //LocalID integrated later in sender
                        compressedData[16] = 0;//(byte)(LocalID & 0xFF);
                        compressedData[17] = 0;//(byte)((LocalID >> 8) & 0xFF);
                        compressedData[18] = 0;//(byte)((LocalID >> 16) & 0xFF);
                        compressedData[19] = 0;// (byte)((LocalID >> 24) & 0xFF);
                        var shape = m_Part.Shape;
                        compressedData[20] = (byte)shape.PCode;
                        compressedData[21] = shape.State;
                        //CRC
                        compressedData[22] = 0;
                        compressedData[23] = 0;
                        compressedData[24] = 0;
                        compressedData[25] = 0;
                        compressedData[26] = (byte)m_Part.Material;
                        compressedData[27] = (byte)m_Part.ClickAction;
                        m_Part.Size.ToBytes(compressedData, 28);
                        m_Part.Position.ToBytes(compressedData, 40);
                        m_Part.Rotation.ToBytes(compressedData, 52);
                        compressedData[64] = (byte)((uint)compressedflags & 0xFF);
                        compressedData[65] = (byte)(((uint)compressedflags >> 8) & 0xFF);
                        compressedData[66] = (byte)(((uint)compressedflags >> 16) & 0xFF);
                        compressedData[67] = (byte)(((uint)compressedflags >> 24) & 0xFF);
                        m_Part.Owner.ID.ToBytes(compressedData, 68);

                        int offset = 80;

                        //Angular velocity
                        if ((compressedflags & ObjectUpdateCompressed.CompressedFlags.HasAngularVelocity) != 0)
                        {
                            m_Part.AngularVelocity.ToBytes(compressedData, offset);
                            offset += 12;
                        }

                        //ParentID integrated later in sender
                        compressedData[offset] = 0;
                        compressedData[offset + 1] = 0;
                        compressedData[offset + 2] = 0;
                        compressedData[offset + 3] = 0;
                        offset += 4;

                        //Hover text
                        if (textbytes != null)
                        {
                            Buffer.BlockCopy(textbytes, 0, compressedData, offset, textbytes.Length);
                            offset += textbytes.Length;
                            compressedData[offset++] = textparam.TextColor.R_AsByte;
                            compressedData[offset++] = textparam.TextColor.G_AsByte;
                            compressedData[offset++] = textparam.TextColor.B_AsByte;
                            compressedData[offset++] = (byte)(255 - textparam.TextColor.A_AsByte);
                        }

                        //Media url
                        if (mediaurlbytes != null)
                        {
                            Buffer.BlockCopy(mediaurlbytes, 0, compressedData, offset, mediaurlbytes.Length);
                            offset += mediaurlbytes.Length;
                        }

                        //Particle system
                        if (partsystem.Length != 0)
                        {
                            Buffer.BlockCopy(partsystem, 0, compressedData, offset, partsystem.Length);
                            offset += partsystem.Length;
                        }

                        //ExtraParams
                        Buffer.BlockCopy(extrabytes, 0, compressedData, offset, extrabytes.Length);
                        offset += extrabytes.Length;

                        if ((compressedflags & ObjectUpdateCompressed.CompressedFlags.HasNameValues) != 0)
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
                        if ((compressedflags & ObjectUpdateCompressed.CompressedFlags.TextureAnimation) != 0)
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

                    if (objectGroup != null)
                    {
                        objectGroup.Owner.ID.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.OwnerID);
                        objectGroup.Group.ID.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.GroupID);
                        objectGroup.LastOwner.ID.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.LastOwnerID);
                        m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.SaleType] = (byte)objectGroup.SaleType;
                        PutInt32LEToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.SalePrice, objectGroup.SalePrice);
                        PutInt32LEToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.OwnershipCost, objectGroup.OwnershipCost);
                        PutInt32LEToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.Category, (int)objectGroup.Category);
                        m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.AggregatePerms] = (byte)m_Part.BaseMask.GetAggregatePermissions();
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
