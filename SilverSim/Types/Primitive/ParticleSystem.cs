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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Primitive
{
    public class ParticleSystem : Asset.Format.IReferencesAccessor
    {
        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
        [Flags]
        public enum SourcePattern : byte
        {
            /// <summary>None</summary>
            None = 0,
            /// <summary>Drop particles from source position with no force</summary>
            Drop = 0x01,
            /// <summary>"Explode" particles in all directions</summary>
            Explode = 0x02,
            /// <summary>Particles shoot across a 2D area</summary>
            Angle = 0x04,
            /// <summary>Particles shoot across a 3D Cone</summary>
            AngleCone = 0x08,
            /// <summary>Inverse of AngleCone (shoot particles everywhere except the 3D cone defined</summary>
            AngleConeEmpty = 0x10
        }

        /// <summary>
        /// Particle Data Flags
        /// </summary>
        [Flags]
        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public enum ParticleDataFlags : uint
        {
            /// <summary>None</summary>
            None = 0,
            /// <summary>Interpolate color and alpha from start to end</summary>
            InterpColor = 0x001,
            /// <summary>Interpolate scale from start to end</summary>
            InterpScale = 0x002,
            /// <summary>Bounce particles off particle sources Z height</summary>
            Bounce = 0x004,
            /// <summary>velocity of particles is dampened toward the simulators wind</summary>
            Wind = 0x008,
            /// <summary>Particles follow the source</summary>
            FollowSrc = 0x010,
            /// <summary>Particles point towards the direction of source's velocity</summary>
            FollowVelocity = 0x020,
            /// <summary>Target of the particles</summary>
            TargetPos = 0x040,
            /// <summary>Particles are sent in a straight line</summary>
            TargetLinear = 0x080,
            /// <summary>Particles emit a glow</summary>
            Emissive = 0x100,
            /// <summary>used for point/grab/touch</summary>
            Beam = 0x200,
            /// <summary>continuous ribbon particle</summary>
            Ribbon = 0x400,
            /// <summary>particle data contains glow</summary>
            DataGlow = 0x10000,
            /// <summary>particle data contains blend functions</summary>
            DataBlend = 0x20000,
        }

        /// <summary>
        /// Particle Flags Enum
        /// </summary>
        [Flags]
        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public enum ParticleFlags : uint
        {
            /// <summary>None</summary>
            None = 0,
            /// <summary>Acceleration and velocity for particles are
            /// relative to the object rotation</summary>
            ObjectRelative = 0x01,
            /// <summary>Particles use new 'correct' angle parameters</summary>
            UseNewAngle = 0x02
        }

        [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
        public enum BlendFunc : byte
        {
            One = 0,
            Zero = 1,
            DestColor = 2,
            SourceColor = 3,
            OneMinusDestColor = 4,
            OneMinusSourceColor = 5,
            DestAlpha = 6,
            SourceAlpha = 7,
            OneMinusDestAlpha = 8,
            OneMinusSourceAlpha = 9,
        }

        public uint CRC;
        /// <summary>Particle Flags</summary>
        /// <remarks>There appears to be more data packed in to this area
        /// for many particle systems. It doesn't appear to be flag values
        /// and serialization breaks unless there is a flag for every
        /// possible bit so it is left as an unsigned integer</remarks>
        public uint PartFlags;
        /// <summary><seealso cref="T:SourcePattern"/> pattern of particles</summary>
        public SourcePattern Pattern;
        /// <summary>A <see langword="float"/> representing the maximimum age (in seconds) particle will be displayed</summary>
        /// <remarks>Maximum value is 30 seconds</remarks>
        public float MaxAge = 10;
        /// <summary>A <see langword="float"/> representing the number of seconds, 
        /// from when the particle source comes into view, 
        /// or the particle system's creation, that the object will emits particles; 
        /// after this time period no more particles are emitted</summary>
        public float StartAge;
        /// <summary>A <see langword="float"/> in radians that specifies where particles will not be created</summary>
        public float InnerAngle;
        /// <summary>A <see langword="float"/> in radians that specifies where particles will be created</summary>
        public float OuterAngle;
        /// <summary>A <see langword="float"/> representing the number of seconds between burts.</summary>
        public float BurstRate = 0.1f;
        /// <summary>A <see langword="float"/> representing the number of meters
        /// around the center of the source where particles will be created.</summary>
        public float BurstRadius;
        /// <summary>A <see langword="float"/> representing in seconds, the minimum speed between bursts of new particles 
        /// being emitted</summary>
        public float BurstSpeedMin = 1;
        /// <summary>A <see langword="float"/> representing in seconds the maximum speed of new particles being emitted.</summary>
        public float BurstSpeedMax = 1;
        /// <summary>A <see langword="byte"/> representing the maximum number of particles emitted per burst</summary>
        public byte BurstPartCount = 1;
        /// <summary>A <see cref="T:Vector3"/> which represents the velocity (speed) from the source which particles are emitted</summary>
        public Vector3 AngularVelocity = Vector3.Zero;
        /// <summary>A <see cref="T:Vector3"/> which represents the Acceleration from the source which particles are emitted</summary>
        public Vector3 PartAcceleration = Vector3.Zero;
        /// <summary>The <see cref="T:UUID"/> Key of the texture displayed on the particle</summary>
        public UUID Texture = UUID.Zero;
        /// <summary>The <see cref="T:UUID"/> Key of the specified target object or avatar particles will follow</summary>
        public UUID Target = UUID.Zero;
        /// <summary>Flags of particle from <seealso cref="T:ParticleDataFlags"/></summary>
        public ParticleDataFlags PartDataFlags;
        /// <summary>Max Age particle system will emit particles for</summary>
        public float PartMaxAge;
        /// <summary>The <see cref="T:Color4"/> the particle has at the beginning of its lifecycle</summary>
        public ColorAlpha PartStartColor = ColorAlpha.White;
        /// <summary>The <see cref="T:Color4"/> the particle has at the ending of its lifecycle</summary>
        public ColorAlpha PartEndColor = ColorAlpha.White;
        /// <summary>A <see langword="float"/> that represents the starting X size of the particle</summary>
        /// <remarks>Minimum value is 0, maximum value is 4</remarks>
        public float PartStartScaleX = 1;
        /// <summary>A <see langword="float"/> that represents the starting Y size of the particle</summary>
        /// <remarks>Minimum value is 0, maximum value is 4</remarks>
        public float PartStartScaleY = 1;
        /// <summary>A <see langword="float"/> that represents the ending X size of the particle</summary>
        /// <remarks>Minimum value is 0, maximum value is 4</remarks>
        public float PartEndScaleX = 1;
        /// <summary>A <see langword="float"/> that represents the ending Y size of the particle</summary>
        /// <remarks>Minimum value is 0, maximum value is 4</remarks>
        public float PartEndScaleY = 1;

        /// <summary>A <see langword="float"/> that represents the start glow value</summary>
        /// <remarks>Minimum value is 0, maximum value is 1</remarks>
        public float PartStartGlow;
        /// <summary>A <see langword="float"/> that represents the end glow value</summary>
        /// <remarks>Minimum value is 0, maximum value is 1</remarks>
        public float PartEndGlow;

        /// <summary>OpenGL blend function to use at particle source</summary>
        public BlendFunc BlendFuncSource = BlendFunc.SourceAlpha;
        /// <summary>OpenGL blend function to use at particle destination</summary>
        public BlendFunc BlendFuncDest = BlendFunc.OneMinusSourceAlpha;

        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const byte MaxDataBlockSize = 98;
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const byte LegacyDataBlockSize = 86;
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const byte SysDataSize = 68;
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const byte PartDataSize = 18;

        #region References accessor
        public List<UUID> References
        {
            get
            {
                List<UUID> reflist = new List<UUID>();
                if(Texture != UUID.Zero)
                {
                    reflist.Add(Texture);
                }
                return reflist;
            }
        }
        #endregion

        /// <summary>
        /// Can this particle system be packed in a legacy compatible way
        /// </summary>
        /// <returns>True if the particle system doesn't use new particle system features</returns>
        public bool IsLegacyCompatible()
        {
            return !HasGlow() && !HasBlendFunc();
        }

        public bool HasGlow()
        {
            return PartStartGlow > 0f || PartEndGlow > 0f;
        }

        public bool HasBlendFunc()
        {
            return BlendFuncSource != BlendFunc.SourceAlpha || BlendFuncDest != BlendFunc.OneMinusSourceAlpha;
        }

        public ParticleSystem()
        {

        }

        /// <summary>
        /// Decodes a byte[] array into a ParticleSystem Object
        /// </summary>
        /// <param name="data">ParticleSystem object</param>
        /// <param name="pos">Start position for BitPacker</param>
        public ParticleSystem(byte[] data, int pos)
        {
            BlendFuncSource = BlendFunc.SourceAlpha;
            BlendFuncDest = BlendFunc.OneMinusSourceAlpha;

            MaxAge = 0.0f;
            StartAge = 0.0f;
            InnerAngle = 0.0f;
            OuterAngle = 0.0f;
            BurstRate = 0.0f;
            BurstRadius = 0.0f;
            BurstSpeedMin = 0.0f;
            BurstSpeedMax = 0.0f;
            AngularVelocity = Vector3.Zero;
            PartAcceleration = Vector3.Zero;
            Texture = UUID.Zero;
            Target = UUID.Zero;
            PartStartColor = ColorAlpha.Black;
            PartEndColor = ColorAlpha.Black;
            PartStartScaleX = 0.0f;
            PartStartScaleY = 0.0f;
            PartEndScaleX = 0.0f;
            PartEndScaleY = 0.0f;

            int size = data.Length - pos;
            BitPacker pack = new BitPacker(data, pos);

            if (size == LegacyDataBlockSize)
            {
                UnpackSystem(pack);
                UnpackLegacyData(pack);
            }
            else if (size > LegacyDataBlockSize && size <= MaxDataBlockSize)
            {
                int sysSize = pack.UnpackSignedBits(32);
                if (sysSize != SysDataSize)
                {
                    return; // unkown particle system data size
                }
                UnpackSystem(pack);
                /*int dataSize = */pack.UnpackSignedBits(32);
                UnpackLegacyData(pack);


                if ((PartDataFlags & ParticleDataFlags.DataGlow) == ParticleDataFlags.DataGlow)
                {
                    if (pack.Data.Length - pack.BytePos < 2)
                    {
                        return;
                    }
                    uint glow = pack.UnpackUnsignedBits(8);
                    PartStartGlow = glow / 255f;
                    glow = pack.UnpackUnsignedBits(8);
                    PartEndGlow = glow / 255f;
                }

                if ((PartDataFlags & ParticleDataFlags.DataBlend) == ParticleDataFlags.DataBlend)
                {
                    if (pack.Data.Length - pack.BytePos < 2)
                    {
                        return;
                    }
                    BlendFuncSource = (BlendFunc)pack.UnpackUnsignedBits(8);
                    BlendFuncDest = (BlendFunc)pack.UnpackUnsignedBits(8);
                }

            }
        }

        void UnpackSystem(BitPacker pack)
        {
            CRC = pack.UnpackUnsignedBits(32);
            PartFlags = pack.UnpackUnsignedBits(32);
            Pattern = (SourcePattern)pack.ByteValue;
            MaxAge = pack.UnpackFixed(false, 8, 8);
            StartAge = pack.UnpackFixed(false, 8, 8);
            InnerAngle = pack.UnpackFixed(false, 3, 5);
            OuterAngle = pack.UnpackFixed(false, 3, 5);
            BurstRate = pack.UnpackFixed(false, 8, 8);
            BurstRadius = pack.UnpackFixed(false, 8, 8);
            BurstSpeedMin = pack.UnpackFixed(false, 8, 8);
            BurstSpeedMax = pack.UnpackFixed(false, 8, 8);
            BurstPartCount = pack.ByteValue;
            float x = pack.UnpackFixed(true, 8, 7);
            float y = pack.UnpackFixed(true, 8, 7);
            float z = pack.UnpackFixed(true, 8, 7);
            AngularVelocity = new Vector3(x, y, z);
            x = pack.UnpackFixed(true, 8, 7);
            y = pack.UnpackFixed(true, 8, 7);
            z = pack.UnpackFixed(true, 8, 7);
            PartAcceleration = new Vector3(x, y, z);
            Texture = pack.UuidValue;
            Target = pack.UuidValue;
        }

        void UnpackLegacyData(BitPacker pack)
        {
            PartDataFlags = (ParticleDataFlags)pack.UnpackUnsignedBits(32);
            PartMaxAge = pack.UnpackFixed(false, 8, 8);
            PartStartColor = pack.ColorValue;
            PartEndColor = pack.ColorValue;
            PartStartScaleX = pack.UnpackFixed(false, 3, 5);
            PartStartScaleY = pack.UnpackFixed(false, 3, 5);
            PartEndScaleX = pack.UnpackFixed(false, 3, 5);
            PartEndScaleY = pack.UnpackFixed(false, 3, 5);
        }

        /// <summary>
        /// Generate byte[] array from particle data
        /// </summary>
        /// <returns>Byte array</returns>
        public byte[] GetBytes()
        {
            int size = LegacyDataBlockSize;
            if (!IsLegacyCompatible())
            {
                size += 8; // two new ints for size
            }
            if (HasGlow())
            {
                size += 2; // two bytes for start and end glow
            }
            if (HasBlendFunc())
            {
                size += 2; // two bytes for start end end blend function
            }

            byte[] bytes = new byte[size];
            BitPacker pack = new BitPacker(bytes, 0);

            if (IsLegacyCompatible())
            {
                PackSystemBytes(pack);
                PackLegacyData(pack);
            }
            else
            {
                if (HasGlow())
                {
                    PartDataFlags |= ParticleDataFlags.DataGlow;
                }
                if (HasBlendFunc())
                {
                    PartDataFlags |= ParticleDataFlags.DataBlend;
                }

                pack.PackBits(SysDataSize, 32);
                PackSystemBytes(pack);
                int partSize = PartDataSize;
                if (HasGlow())
                {
                    partSize += 2; // two bytes for start and end glow
                }
                if (HasBlendFunc())
                {
                    partSize += 2; // two bytes for start end end blend function
                }
                pack.PackBits(partSize, 32);
                PackLegacyData(pack);

                if (HasGlow())
                {
                    pack.PackBits((byte)(PartStartGlow * 255f), 8);
                    pack.PackBits((byte)(PartEndGlow * 255f), 8);
                }

                if (HasBlendFunc())
                {
                    pack.PackBits((byte)BlendFuncSource, 8);
                    pack.PackBits((byte)BlendFuncDest, 8);
                }
            }

            return bytes;
        }

        void PackSystemBytes(BitPacker pack)
        {
            pack.PackBits(CRC, 32);
            pack.PackBits(PartFlags, 32);
            pack.PackBits((uint)Pattern, 8);
            pack.PackFixed(MaxAge, false, 8, 8);
            pack.PackFixed(StartAge, false, 8, 8);
            pack.PackFixed(InnerAngle, false, 3, 5);
            pack.PackFixed(OuterAngle, false, 3, 5);
            pack.PackFixed(BurstRate, false, 8, 8);
            pack.PackFixed(BurstRadius, false, 8, 8);
            pack.PackFixed(BurstSpeedMin, false, 8, 8);
            pack.PackFixed(BurstSpeedMax, false, 8, 8);
            pack.PackBits(BurstPartCount, 8);
            pack.PackFixed((float)AngularVelocity.X, true, 8, 7);
            pack.PackFixed((float)AngularVelocity.Y, true, 8, 7);
            pack.PackFixed((float)AngularVelocity.Z, true, 8, 7);
            pack.PackFixed((float)PartAcceleration.X, true, 8, 7);
            pack.PackFixed((float)PartAcceleration.Y, true, 8, 7);
            pack.PackFixed((float)PartAcceleration.Z, true, 8, 7);
            pack.UuidValue = Texture;
            pack.UuidValue = Target;
        }

        void PackLegacyData(BitPacker pack)
        {
            pack.PackBits((uint)PartDataFlags, 32);
            pack.PackFixed(PartMaxAge, false, 8, 8);
            pack.ColorValue = PartStartColor;
            pack.ColorValue = PartEndColor;
            pack.PackFixed(PartStartScaleX, false, 3, 5);
            pack.PackFixed(PartStartScaleY, false, 3, 5);
            pack.PackFixed(PartEndScaleX, false, 3, 5);
            pack.PackFixed(PartEndScaleY, false, 3, 5);
        }
    }
}
