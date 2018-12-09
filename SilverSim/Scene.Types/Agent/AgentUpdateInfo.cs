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
using SilverSim.Types.Primitive;
using System;
using System.Globalization;
using System.Threading;

namespace SilverSim.Scene.Types.Agent
{
    public sealed class AgentUpdateInfo : IObjUpdateInfo
    {
        private bool m_Killed;
        public uint LocalID { get; set; }
        public IAgent Agent { get; }
        public UUID ID { get; internal set; }
        public UUID SceneID { get; set; }

        public bool IsAlwaysFull => true;

        public UGUI Owner => Agent.Owner;

        private readonly byte[] m_UpdateDataBlock;

        public bool IsAttached => false;

        public bool IsTemporary => true;

        public bool IsMoving => false;

        public bool IsAttachedToPrivate => false;

        private int m_UpdateSerialNumber = 1;

        public void IncSerialNumber()
        {
            if(0 == Interlocked.Increment(ref m_UpdateSerialNumber))
            {
                Interlocked.CompareExchange(ref m_UpdateSerialNumber, 1, 0);
            }
        }

        public int SerialNumber
        {
            get
            {
                int sno = Interlocked.CompareExchange(ref m_UpdateSerialNumber, 1, 0);
                if(sno == 0)
                {
                    sno = 1;
                }
                return sno;
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
                ObjectData_CollisionPlane = ObjectData + 0,
                ObjectData_Position = ObjectData + 16,
                ObjectData_Velocity = ObjectData + 28, 
                ObjectData_Acceleration = ObjectData + 40,
                ObjectData_Rotation = ObjectData + 52, 
                ObjectData_AngularVelocity = ObjectData + 64, 
            ParentID = ObjectData + 76,
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
            TextureEntry = ProfileHollow + 2,
            TextureAnim = TextureEntry + 2,

            BlockLength = TextureAnim + 1
        }

        public enum FullFixedBlock2Offset
        {
            Data = 0,
            Text = Data + 2,
            TextColor = Text + 2,
            MediaURL = TextColor + 4,
            PSBlock = MediaURL + 1,
            ExtraParams = PSBlock + 1,
            LoopedSound = ExtraParams + 2,
            SoundOwner = LoopedSound + 16,
            SoundGain = SoundOwner + 16,
            SoundFlags = SoundGain + 4,
            SoundRadius = SoundFlags + 1,
            JointType = SoundRadius + 4,
            JointPivot = JointType + 1,
            JointAxisOrAnchor = JointPivot + 12,

            BlockLength = JointAxisOrAnchor + 12
        }

        public AgentUpdateInfo(IAgent agent, UUID sceneID)
        {
            Agent = agent;
            SceneID = sceneID;
            ID = agent.ID;

            m_UpdateDataBlock = GeneratePreprocessedUpdateBlock(agent);
        }

        public uint ParentID => Agent.SittingOnObject?.LocalID[SceneID] ?? 0;

        private static byte[] GeneratePreprocessedUpdateBlock(IAgent agent)
        {
            byte[] newupdateblock;
            /* last element is group title */
            byte[] nameblock = string.Format("FirstName STRING RW SV {0}\nLastName STRING RW SV {1}\nTitle STRING RW SV {2}\0", agent.FirstName, agent.LastName, string.Empty).ToUTF8Bytes();
            newupdateblock = new byte[nameblock.Length + 2 + (int)FullFixedBlock1Offset.BlockLength + (int)FullFixedBlock2Offset.BlockLength];
            Array.Clear(newupdateblock, 0, newupdateblock.Length);
            agent.ID.ToBytes(newupdateblock, (int)FullFixedBlock1Offset.FullID);
            newupdateblock[(int)FullFixedBlock1Offset.ObjectDataLength] = 76;
            newupdateblock[(int)FullFixedBlock1Offset.Material] = (byte)PrimitiveMaterial.Flesh;
            newupdateblock[(int)FullFixedBlock1Offset.PathCurve] = 16;
            newupdateblock[(int)FullFixedBlock1Offset.PathScaleX] = 100;
            newupdateblock[(int)FullFixedBlock1Offset.PathScaleY] = 100;
            newupdateblock[(int)FullFixedBlock1Offset.ProfileCurve] = 1;
            newupdateblock[(int)FullFixedBlock1Offset.PCode] = (byte)PrimitiveCode.Avatar;

            PrimitiveFlags primUpdateFlags = PrimitiveFlags.Physics | PrimitiveFlags.ObjectModify | PrimitiveFlags.ObjectCopy | PrimitiveFlags.ObjectAnyOwner |
                            PrimitiveFlags.ObjectYouOwner | PrimitiveFlags.ObjectMove | PrimitiveFlags.InventoryEmpty | PrimitiveFlags.ObjectTransfer |
                            PrimitiveFlags.ObjectOwnerModify;
            newupdateblock[(int)FullFixedBlock1Offset.UpdateFlags] = (byte)((uint)primUpdateFlags & 0xFF);
            newupdateblock[(int)FullFixedBlock1Offset.UpdateFlags + 1] = (byte)(((uint)primUpdateFlags >> 8) & 0xFF);
            newupdateblock[(int)FullFixedBlock1Offset.UpdateFlags + 2] = (byte)(((uint)primUpdateFlags >> 16) & 0xFF);
            newupdateblock[(int)FullFixedBlock1Offset.UpdateFlags + 3] = (byte)(((uint)primUpdateFlags >> 24) & 0xFF);

            var offset = (int)FullFixedBlock1Offset.BlockLength;

            newupdateblock[offset] = (byte)(nameblock.Length & 0xFF);
            ++offset;
            newupdateblock[offset] = (byte)((nameblock.Length >> 8) & 0xFF);
            ++offset;

            Buffer.BlockCopy(nameblock, 0, newupdateblock, offset, nameblock.Length);
            offset += nameblock.Length;

            newupdateblock[offset + (int)FullFixedBlock2Offset.ExtraParams] = 1;
            newupdateblock[offset + (int)FullFixedBlock2Offset.Text] = 1;
            byte[] floatbytes = BitConverter.GetBytes((float)0);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(floatbytes);
            }
            Buffer.BlockCopy(floatbytes, 0, newupdateblock, offset + (int)FullFixedBlock2Offset.SoundGain, 4);
            Buffer.BlockCopy(floatbytes, 0, newupdateblock, offset + (int)FullFixedBlock2Offset.SoundRadius, 4);

            return newupdateblock;
        }

        public void KillObject()
        {
            m_Killed = true;
        }

        public bool IsKilled => m_Killed;

        public bool IsPhysics
        {
            get
            {
                if (Agent != null && !m_Killed)
                {
                    return Agent.SittingOnObject == null;
                }
                return false;
            }
        }

        public byte[] GetCompressedUpdate(CultureInfo cultureInfo) => null;

        public byte[] GetCompressedUpdateLimited(CultureInfo cultureInfo) => null;

        public byte[] GetTerseUpdate(CultureInfo cultureInfo) => null;

        public byte[] GetTerseUpdateLimited(CultureInfo cultureInfo) => null;

        public byte[] GetPropertiesUpdate(CultureInfo cultureInfo) => null;

        public byte[] GetFullUpdateLimited(CultureInfo cultureInfo) => GetFullUpdate(cultureInfo);

        public byte[] GetFullUpdate(CultureInfo cultureInfo)
        {
            if (Agent != null && !m_Killed)
            {
                byte[] updateDataBlock = m_UpdateDataBlock; /* we use the GC nature here */
                var newUpdateDataBlock = new byte[updateDataBlock.Length];
                Buffer.BlockCopy(updateDataBlock, 0, newUpdateDataBlock, 0, updateDataBlock.Length);
                Agent.Size.ToBytes(newUpdateDataBlock, (int)FullFixedBlock1Offset.Scale);
                Agent.CollisionPlane.ToBytes(newUpdateDataBlock, (int)FullFixedBlock1Offset.ObjectData_CollisionPlane);
                Agent.Position.ToBytes(newUpdateDataBlock, (int)FullFixedBlock1Offset.ObjectData_Position);
                Agent.Velocity.ToBytes(newUpdateDataBlock, (int)FullFixedBlock1Offset.ObjectData_Velocity);
                Agent.Acceleration.ToBytes(newUpdateDataBlock, (int)FullFixedBlock1Offset.ObjectData_Acceleration);
                Vector3.Zero.ToBytes(newUpdateDataBlock, (int)FullFixedBlock1Offset.ObjectData_AngularVelocity); /* set to zero as per SL ObjectUpdate definition for the 76 byte format */
                Quaternion rot = Agent.Rotation;
                IObject sittingobj = Agent.SittingOnObject;
                uint parentID;
                if (sittingobj == null)
                {
                    rot.X = 0;
                    rot.Y = 0;
                    rot.NormalizeSelf();
                    parentID = 0;
                }
                else
                {
                    parentID = sittingobj.LocalID[SceneID];
                }
                newUpdateDataBlock[(int)FullFixedBlock1Offset.ParentID] = (byte)(parentID & 0xFF);
                newUpdateDataBlock[(int)FullFixedBlock1Offset.ParentID + 1] = (byte)((parentID >> 8) & 0xFF);
                newUpdateDataBlock[(int)FullFixedBlock1Offset.ParentID + 2] = (byte)((parentID >> 16) & 0xFF);
                newUpdateDataBlock[(int)FullFixedBlock1Offset.ParentID + 3] = (byte)((parentID >> 24) & 0xFF);

                rot.ToBytes(newUpdateDataBlock, (int)FullFixedBlock1Offset.ObjectData_Rotation);

                return newUpdateDataBlock;
            }
            return null;
        }
    }
}
