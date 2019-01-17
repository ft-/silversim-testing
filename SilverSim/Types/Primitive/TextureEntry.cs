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
using System.IO;
using System.Linq;

namespace SilverSim.Types.Primitive
{
    public class TextureEntry : Asset.Format.IReferencesAccessor
    {
        public const int MAX_TEXTURE_FACES = 45;
        private readonly TextureEntryFace[] m_FaceTextures = new TextureEntryFace[MAX_TEXTURE_FACES];
        public TextureEntryFace DefaultTexture;
        private readonly object m_Lock = new object();

        public TextureEntry()
        {
            DefaultTexture = new TextureEntryFace();
        }

        public TextureEntry(byte[] data, int pos, int length)
        {
            FromBytes(data, pos, length);
        }

        public TextureEntry(byte[] data)
        {
            FromBytes(data, 0, data.Length);
        }

        public TextureEntry(TextureEntry src)
        {
            DefaultTexture = new TextureEntryFace(src.DefaultTexture);
            for(int i = 0; i < MAX_TEXTURE_FACES; ++i)
            {
                TextureEntryFace face = src.m_FaceTextures[i];
                if(face != null)
                {
                    m_FaceTextures[i] = new TextureEntryFace(face);
                }
            }
        }

        #region References accessor
        public List<UUID> References
        {
            get
            {
                var reflist = new List<UUID>();
                foreach(UUID id in DefaultTexture.References)
                {
                    if(!reflist.Contains(id))
                    {
                        reflist.Add(id);
                    }
                }

                foreach (var face in m_FaceTextures)
                {
                    if (face != null)
                    {
                        foreach (var id in face.References)
                        {
                            if (!reflist.Contains(id))
                            {
                                reflist.Add(id);
                            }
                        }
                    }
                }

                return reflist;
            }
        }
        #endregion

        public TextureEntryFace this[uint index]
        {
            get
            {
                if (index >= MAX_TEXTURE_FACES)
                {
                    throw new KeyNotFoundException(index.ToString());
                }

                lock (m_Lock)
                {
                    if (m_FaceTextures[index] == null)
                    {
                        m_FaceTextures[index] = new TextureEntryFace(DefaultTexture);
                    }
                }

                return m_FaceTextures[index];
            }
        }

        public bool TryGetValue(uint index, out TextureEntryFace face)
        {
            face = default(TextureEntryFace);
            if (index >= MAX_TEXTURE_FACES)
            {
                return false;
            }

            lock(m_Lock)
            {
                if (m_FaceTextures[index] == null)
                {
                    return false;
                }
            }

            face = m_FaceTextures[index];
            return true;
        }

        private static float BytesToFloat(byte[] bytes, int pos)
        {
            if(!BitConverter.IsLittleEndian)
            {
                var newBytes = new byte[4];
                Buffer.BlockCopy(bytes, pos, newBytes, 0, 4);
                Array.Reverse(newBytes);
                bytes = newBytes;
                pos = 0;
            }
            return BitConverter.ToSingle(bytes, pos);
        }

        private static float TEOffsetFloat(byte[] bytes, int pos)
        {
            if (!BitConverter.IsLittleEndian)
            {
                var newBytes = new byte[2];
                Buffer.BlockCopy(bytes, pos, newBytes, 0, 2);
                Array.Reverse(newBytes);
                bytes = newBytes;
                pos = 0;
            }
            short offset = BitConverter.ToInt16(bytes, pos);
            return offset / 32767.0f;
        }

        private const float TwoPi = (float)Math.PI * 2;
        private static float TERotationFloat(byte[] bytes, int pos) => (bytes[pos] | (bytes[pos + 1] << 8)) / 32768.0f * TwoPi;

        private static float TEGlowFloat(byte[] bytes, int pos) => bytes[pos] / 255.0f;

        private static ColorAlpha ColorFromBytes(byte[] data, int pos) => new ColorAlpha
        {
            R = (255 - data[pos + 0]) / 255f,
            G = (255 - data[pos + 1]) / 255f,
            B = (255 - data[pos + 2]) / 255f,
            A = (255 - data[pos + 3]) / 255f
        };

        private void FromBytes(byte[] data, int pos, int length)
        {
            DefaultTexture = new TextureEntryFace();
            if (length < 16)
            {
                for (int idx = m_FaceTextures.Length; idx-- != 0;)
                {
                    m_FaceTextures[idx] = null;
                }
                return;
            }

            uint bitfieldSize = 0;
            ulong faceBits = 0;
            int i = pos;

            #region Texture
            DefaultTexture.TextureID = new UUID(data, i);
            i += 16;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                var tmpUUID = new UUID(data, i);
                i += 16;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].TextureID = tmpUUID;
                    }
                }
            }
            #endregion Texture

            #region Color
            DefaultTexture.TextureColor = ColorFromBytes(data, i);
            i += 4;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                var tmpColor = ColorFromBytes(data, i);
                i += 4;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].TextureColor = tmpColor;
                    }
                }
            }
            #endregion Color

            #region RepeatU
            DefaultTexture.RepeatU = BytesToFloat(data, i);
            i += 4;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                float tmpFloat = BytesToFloat(data, i);
                i += 4;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].RepeatU = tmpFloat;
                    }
                }
            }
            #endregion RepeatU

            #region RepeatV
            DefaultTexture.RepeatV = BytesToFloat(data, i);
            i += 4;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                float tmpFloat = BytesToFloat(data, i);
                i += 4;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].RepeatV = tmpFloat;
                    }
                }
            }
            #endregion RepeatV

            #region OffsetU
            DefaultTexture.OffsetU = TEOffsetFloat(data, i);
            i += 2;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                float tmpFloat = TEOffsetFloat(data, i);
                i += 2;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].OffsetU = tmpFloat;
                    }
                }
            }
            #endregion OffsetU

            #region OffsetV
            DefaultTexture.OffsetV = TEOffsetFloat(data, i);
            i += 2;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                float tmpFloat = TEOffsetFloat(data, i);
                i += 2;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].OffsetV = tmpFloat;
                    }
                }
            }
            #endregion OffsetV

            #region Rotation
            DefaultTexture.Rotation = TERotationFloat(data, i);
            i += 2;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                float tmpFloat = TERotationFloat(data, i);
                i += 2;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].Rotation = tmpFloat;
                    }
                }
            }
            #endregion Rotation

            #region Material
            DefaultTexture.Material = data[i];
            i++;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                byte tmpByte = data[i];
                i++;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].Material = tmpByte;
                    }
                }
            }
            #endregion Material

            #region Media
            DefaultTexture.Media = data[i];
            i++;

            while (i - pos < length && ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                byte tmpByte = data[i];
                i++;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].Media = tmpByte;
                    }
                }
            }
            #endregion Media

            #region Glow
            DefaultTexture.Glow = TEGlowFloat(data, i);
            i++;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                float tmpFloat = TEGlowFloat(data, i);
                i++;

                for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                {
                    if ((faceBits & bit) != 0)
                    {
                        this[face].Glow = tmpFloat;
                    }
                }
            }
            #endregion Glow

            #region MaterialID
            if (i - pos + 16 <= length)
            {
                DefaultTexture.MaterialID = new UUID(data, i);
                i += 16;

                while (i - pos + 16 <= length && ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
                {
                    var tmpUUID = new UUID(data, i);
                    i += 16;

                    for (uint face = 0, bit = 1; face < bitfieldSize; face++, bit <<= 1)
                    {
                        if ((faceBits & bit) != 0)
                        {
                            this[face].MaterialID = tmpUUID;
                        }
                    }
                }
            }
            #endregion MaterialID
        }

        private static byte TEGlowByte(float glow) => (byte)(glow * 255.0f);

        private static short TEOffsetShort(float offset)
        {
            offset = Math.Min(Math.Max(offset, -1.0f), 1.0f);
            offset *= 32767.0f;
            return (short)Math.Round(offset);
        }

        private static short TERotationShort(float rotation) => (short)Math.Floor(((Math.IEEERemainder(rotation, TwoPi) / TwoPi) * 32768.0f) + 0.5f);

        public static implicit operator byte[] (TextureEntry e) => e.GetBytes();

        private static byte[] ColorToBytes(ColorAlpha color) => new byte[] 
        {
            (byte)(255 - color.R_AsByte),
            (byte)(255 - color.G_AsByte),
            (byte)(255 - color.B_AsByte),
            (byte)(255 - color.A_AsByte)
        };

        private static byte[] FloatToBytes(float f)
        {
            byte[] b = BitConverter.GetBytes(f);
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            return b;
        }

        private const byte FULLBRIGHT_MASK = 0x20;

        public byte[] GetBytes(bool fullbrightdisable = false, float glowintensitylimit = 1.0f)
        {
            if (DefaultTexture == null)
            {
                return new byte[0];
            }

            using (var memStream = new MemoryStream())
            {
                using (var binWriter = new BinaryWriter(memStream))
                {
                    #region Bitfield Setup

                    ulong textures = 0;
                    ulong texturecolors = 0;
                    ulong repeatus = 0;
                    ulong repeatvs = 0;
                    ulong offsetus = 0;
                    ulong offsetvs = 0;
                    ulong rotations = 0;
                    ulong materials = 0;
                    ulong medias = 0;
                    ulong glows = 0;
                    ulong materialIDs = 0;

                    ulong mask;
                    int i;
                    int j;
                    ulong mask2;

                    for (i = MAX_TEXTURE_FACES, mask = (ulong)1 << (MAX_TEXTURE_FACES - 1); i-- != 0; mask >>= 1)
                    {
                        if (m_FaceTextures[i] == null)
                        {
                            continue;
                        }

                        if (m_FaceTextures[i].TextureID != DefaultTexture.TextureID)
                        {
                            textures |= mask;
                        }
                        if (m_FaceTextures[i].TextureColor != DefaultTexture.TextureColor)
                        {
                            texturecolors |= mask;
                        }
                        if (m_FaceTextures[i].RepeatU != DefaultTexture.RepeatU)
                        {
                            repeatus |= mask;
                        }
                        if (m_FaceTextures[i].RepeatV != DefaultTexture.RepeatV)
                        {
                            repeatvs |= mask;
                        }
                        if (TEOffsetShort(m_FaceTextures[i].OffsetU) != TEOffsetShort(DefaultTexture.OffsetU))
                        {
                            offsetus |= mask;
                        }
                        if (TEOffsetShort(m_FaceTextures[i].OffsetV) != TEOffsetShort(DefaultTexture.OffsetV))
                        {
                            offsetvs |= mask;
                        }
                        if (TERotationShort(m_FaceTextures[i].Rotation) != TERotationShort(DefaultTexture.Rotation))
                        {
                            rotations |= mask;
                        }
                        if (m_FaceTextures[i].Material != DefaultTexture.Material)
                        {
                            materials |= mask;
                        }
                        if (m_FaceTextures[i].Media != DefaultTexture.Media)
                        {
                            medias |= mask;
                        }
                        if (TEGlowByte(m_FaceTextures[i].Glow) != TEGlowByte(DefaultTexture.Glow))
                        {
                            glows |= mask;
                        }
                        if (m_FaceTextures[i].MaterialID != DefaultTexture.MaterialID)
                        {
                            materialIDs |= mask;
                        }
                    }

                    #endregion Bitfield Setup

                    #region Texture
                    binWriter.Write(DefaultTexture.TextureID.GetBytes());
                    for (i = 0, mask = 1; textures != 0; i++, mask <<= 1)
                    {
                        if ((textures & mask) == 0)
                        {
                            continue;
                        }

                        ulong finalmask = mask;
                        for(j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= textures; j++, mask2 <<= 1)
                        {
                            if((textures & mask2) == 0)
                            {
                                continue;
                            }
                            if(m_FaceTextures[j].TextureID == m_FaceTextures[i].TextureID)
                            {
                                finalmask |= mask2;
                            }
                        }
                        textures &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(m_FaceTextures[i].TextureID.GetBytes());
                    }
                    binWriter.Write((byte)0);
                    #endregion Texture

                    #region Color
                    // Serialize the color bytes inverted to optimize for zerocoding
                    binWriter.Write(ColorToBytes(DefaultTexture.TextureColor));
                    for (i = 0, mask = 1; texturecolors != 0; i++, mask <<= 1)
                    {
                        if ((texturecolors & mask) == 0)
                        {
                            continue;
                        }

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= texturecolors; j++, mask2 <<= 1)
                        {
                            if ((texturecolors & mask2) == 0)
                            {
                                continue;
                            }
                            if (m_FaceTextures[j].TextureColor == m_FaceTextures[i].TextureColor)
                            {
                                finalmask |= mask2;
                            }
                        }
                        texturecolors &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        // Serialize the color bytes inverted to optimize for zerocoding
                        binWriter.Write(ColorToBytes(m_FaceTextures[i].TextureColor));
                    }
                    binWriter.Write((byte)0);
                    #endregion Color

                    #region RepeatU
                    binWriter.Write(DefaultTexture.RepeatU);
                    for (i = 0, mask = 1; repeatus != 0; i++, mask <<= 1)
                    {
                        if ((repeatus & mask) == 0)
                        {
                            continue;
                        }
                        float repeatu = m_FaceTextures[i].RepeatU;

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= repeatus; j++, mask2 <<= 1)
                        {
                            if ((repeatus & mask2) == 0)
                            {
                                continue;
                            }
                            if (m_FaceTextures[j].RepeatU == repeatu)
                            {
                                finalmask |= mask2;
                            }
                        }
                        repeatus &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(FloatToBytes(repeatu));
                    }
                    binWriter.Write((byte)0);
                    #endregion RepeatU

                    #region RepeatV
                    binWriter.Write(DefaultTexture.RepeatV);
                    for (i = 0, mask = 1; repeatvs != 0; i++, mask <<= 1)
                    {
                        if ((repeatvs & mask) == 0)
                        {
                            continue;
                        }

                        float repeatv = m_FaceTextures[i].RepeatV;

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= repeatvs; j++, mask2 <<= 1)
                        {
                            if ((repeatvs & mask2) == 0)
                            {
                                continue;
                            }
                            if (m_FaceTextures[j].RepeatV == repeatv)
                            {
                                finalmask |= mask2;
                            }
                        }
                        repeatvs &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(FloatToBytes(repeatv));
                    }
                    binWriter.Write((byte)0);
                    #endregion RepeatV

                    #region OffsetU
                    binWriter.Write(TEOffsetShort(DefaultTexture.OffsetU));
                    for (i = 0, mask = 1; offsetus != 0; i++, mask <<= 1)
                    {
                        short offsetudata;
                        if ((offsetus & mask) == 0)
                        {
                            continue;
                        }
                        offsetudata = TEOffsetShort(m_FaceTextures[i].OffsetU);

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= offsetus; j++, mask2 <<= 1)
                        {
                            if ((offsetus & mask2) == 0)
                            {
                                continue;
                            }
                            if (TEOffsetShort(m_FaceTextures[j].OffsetU) == offsetudata)
                            {
                                finalmask |= mask2;
                            }
                        }
                        offsetus &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(offsetudata);
                    }
                    binWriter.Write((byte)0);
                    #endregion OffsetU

                    #region OffsetV
                    binWriter.Write(TEOffsetShort(DefaultTexture.OffsetV));
                    for (i = 0, mask = 1; offsetvs != 0; i++, mask <<= 1)
                    {
                        short offsetvdata;
                        if ((offsetvs & mask) == 0)
                        {
                            continue;
                        }
                        offsetvdata = TEOffsetShort(m_FaceTextures[i].OffsetV);

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= offsetvs; j++, mask2 <<= 1)
                        {
                            if ((offsetvs & mask2) == 0)
                            {
                                continue;
                            }
                            if (TEOffsetShort(m_FaceTextures[j].OffsetV) == offsetvdata)
                            {
                                finalmask |= mask2;
                            }
                        }
                        offsetvs &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(offsetvdata);
                    }
                    binWriter.Write((byte)0);
                    #endregion OffsetV

                    #region Rotation
                    binWriter.Write(TERotationShort(DefaultTexture.Rotation));
                    for (i = 0, mask = 1; rotations != 0; i++, mask <<= 1)
                    {
                        short rotationdata;
                        if ((rotations & mask) == 0)
                        {
                            continue;
                        }

                        rotationdata = TERotationShort(m_FaceTextures[i].Rotation);

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= rotations; j++, mask2 <<= 1)
                        {
                            if ((rotations & mask2) == 0)
                            {
                                continue;
                            }
                            if (TERotationShort(m_FaceTextures[j].Rotation) == rotationdata)
                            {
                                finalmask |= mask2;
                            }
                        }
                        rotations &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(rotationdata);
                    }
                    binWriter.Write((byte)0);
                    #endregion Rotation

                    #region Material
                    byte fbright_mask = (byte)~(fullbrightdisable ? FULLBRIGHT_MASK : 0);
                    binWriter.Write(DefaultTexture.Material & fbright_mask);
                    for (i = 0, mask = 1; materials != 0; i++, mask <<= 1)
                    {
                        if ((materials & mask) == 0)
                        {
                            continue;
                        }

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= materials; j++, mask2 <<= 1)
                        {
                            if ((materials & mask2) == 0)
                            {
                                continue;
                            }
                            if (m_FaceTextures[j].Material == m_FaceTextures[i].Material)
                            {
                                finalmask |= mask2;
                            }
                        }
                        materials &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(m_FaceTextures[i].Material & fbright_mask);
                    }
                    binWriter.Write((byte)0);
                    #endregion Material

                    #region Media
                    binWriter.Write(DefaultTexture.Media);
                    for (i = 0, mask = 1; medias != 0; i++, mask <<= 1)
                    {
                        if ((medias & mask) == 0)
                        {
                            continue;
                        }

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= medias; j++, mask2 <<= 1)
                        {
                            if ((medias & mask2) == 0)
                            {
                                continue;
                            }
                            if (m_FaceTextures[j].Media == m_FaceTextures[i].Media)
                            {
                                finalmask |= mask2;
                            }
                        }
                        medias &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(m_FaceTextures[i].Media);
                    }
                    binWriter.Write((byte)0);
                    #endregion Media

                    #region Glow
                    binWriter.Write(TEGlowByte(Math.Min(glowintensitylimit, DefaultTexture.Glow)));
                    for (i = 0, mask = 1; glows != 0; i++, mask <<= 1)
                    {
                        byte glowbyte;
                        if ((glows & mask) == 0)
                        {
                            continue;
                        }
                        glowbyte = TEGlowByte(Math.Min(glowintensitylimit, m_FaceTextures[i].Glow));

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= glows; j++, mask2 <<= 1)
                        {
                            if ((glows & mask2) == 0)
                            {
                                continue;
                            }
                            if (TEGlowByte(Math.Min(glowintensitylimit, m_FaceTextures[j].Glow)) == glowbyte)
                            {
                                finalmask |= mask2;
                            }
                        }
                        glows &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(glowbyte);
                    }
                    binWriter.Write((byte)0);
                    #endregion Glow

                    #region MaterialID
                    binWriter.Write(DefaultTexture.MaterialID.GetBytes());
                    for (i = 0, mask = 1; 0 != materialIDs; i++, mask <<= 1)
                    {
                        if ((materialIDs & mask) == 0)
                        {
                            continue;
                        }

                        ulong finalmask = mask;
                        for (j = i + 1, mask2 = mask << 1; j < MAX_TEXTURE_FACES && mask2 <= materialIDs; j++, mask2 <<= 1)
                        {
                            if ((materialIDs & mask2) == 0)
                            {
                                continue;
                            }
                            if (m_FaceTextures[j].MaterialID == m_FaceTextures[i].MaterialID)
                            {
                                finalmask |= mask2;
                            }
                        }
                        materialIDs &= ~finalmask;

                        binWriter.Write(GetFaceBitfieldBytes(finalmask));
                        binWriter.Write(m_FaceTextures[i].MaterialID.GetBytes());
                    }
                    binWriter.Write((byte)0);
                    #endregion MaterialID

                    return memStream.ToArray();
                }
            }
        }

        #region Helpers
        private bool ReadFaceBitfield(byte[] data, ref int pos, ref ulong faceBits, ref uint bitfieldSize)
        {
            faceBits = 0;
            bitfieldSize = 0;

            if (pos >= data.Length)
            {
                return false;
            }

            byte b = 0;
            do
            {
                b = data[pos];
                faceBits = (faceBits << 7) | (uint)(b & 0x7F);
                bitfieldSize += 7;
                pos++;
            } while ((b & 0x80) != 0);

            return faceBits != 0;
        }

        private byte[] GetFaceBitfieldBytes(ulong bitfield)
        {
            int byteLength = 0;
            ulong tmpBitfield = bitfield;

            while (tmpBitfield != 0)
            {
                tmpBitfield >>= 7;
                byteLength++;
            }

            if (byteLength == 0)
            {
                return new byte[1] { 0 };
            }

            var bytes = new byte[byteLength];
            for (int i = 0; i < byteLength; i++)
            {
                bytes[i] = (byte)((bitfield >> (7 * (byteLength - i - 1))) & 0x7F);
                if (i < byteLength - 1)
                {
                    bytes[i] |= 0x80;
                }
            }
            return bytes;
        }

        #endregion Helpers

        public void OptimizeDefault(int optimizeForNumfaces)
        {
            if(DefaultTexture == null)
            {
                return;
            }
            var textureCounts = new Dictionary<UUID, int>();
            var repeatUCounts = new Dictionary<float, int>();
            var repeatVCounts = new Dictionary<float, int>();
            var offsetUCounts = new Dictionary<short, KeyValuePair<float, int>>();
            var offsetVCounts = new Dictionary<short, KeyValuePair<float, int>>();
            var rotationCounts = new Dictionary<short, KeyValuePair<float, int>>();
            var glowCounts = new Dictionary<byte, KeyValuePair<float, int>>();
            var materialCounts = new Dictionary<byte, int>();
            var mediaCounts = new Dictionary<byte, int>();
            var materialIDCounts = new Dictionary<UUID, int>();
            for (int i = MAX_TEXTURE_FACES; i-- != 0; )
            {
                int cnt;
                TextureEntryFace face = this[(uint)i];

                float repeatU = face.RepeatU;
                repeatUCounts.TryGetValue(repeatU, out cnt);
                repeatUCounts[repeatU] = cnt + 1;

                float repeatV = face.RepeatV;
                repeatVCounts.TryGetValue(repeatV, out cnt);
                repeatVCounts[repeatV] = cnt + 1;

                KeyValuePair<float, int> fk;
                short offsetu = TEOffsetShort(face.OffsetU);
                offsetUCounts.TryGetValue(offsetu, out fk);
                offsetUCounts[offsetu] = new KeyValuePair<float, int>(fk.Key, cnt + 1);

                short offsetv = TEOffsetShort(face.OffsetV);
                offsetVCounts.TryGetValue(offsetv, out fk);
                offsetVCounts[offsetv] = new KeyValuePair<float, int>(fk.Key, cnt + 1);

                short rotation = TERotationShort(face.Rotation);
                rotationCounts.TryGetValue(rotation, out fk);
                rotationCounts[rotation] = new KeyValuePair<float, int>(fk.Key, cnt + 1);

                byte glow = TEGlowByte(face.Glow);
                glowCounts.TryGetValue(glow, out fk);
                glowCounts[glow] = new KeyValuePair<float, int>(fk.Key, cnt + 1);

                byte material = face.Material;
                materialCounts.TryGetValue(material, out cnt);
                materialCounts[material] = cnt + 1;

                byte media = face.Media;
                mediaCounts.TryGetValue(media, out cnt);
                mediaCounts[media] = cnt + 1;

                UUID materialID = face.MaterialID;
                materialIDCounts.TryGetValue(materialID, out cnt);
                materialIDCounts[materialID] = cnt + 1;
            }

            if (textureCounts.Count > 0)
            {
                DefaultTexture.TextureID = textureCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }

            if(repeatUCounts.Count > 0)
            {
                DefaultTexture.RepeatU = repeatUCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }

            if (repeatVCounts.Count > 0)
            {
                DefaultTexture.RepeatV = repeatVCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }

            if (offsetUCounts.Count > 0)
            {
                DefaultTexture.OffsetU = offsetUCounts.Aggregate((l, r) => l.Value.Value > r.Value.Value ? l : r).Value.Key;
            }

            if (offsetVCounts.Count > 0)
            {
                DefaultTexture.OffsetV = offsetUCounts.Aggregate((l, r) => l.Value.Value > r.Value.Value ? l : r).Value.Key;
            }

            if(rotationCounts.Count > 0)
            {
                DefaultTexture.Rotation = rotationCounts.Aggregate((l, r) => l.Value.Value > r.Value.Value ? l : r).Value.Key;
            }

            if(glowCounts.Count > 0)
            {
                DefaultTexture.Glow = glowCounts.Aggregate((l, r) => l.Value.Value > r.Value.Value ? l : r).Value.Key;
            }

            if(materialCounts.Count > 0)
            {
                DefaultTexture.Material = materialCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }

            if(mediaCounts.Count > 0)
            {
                DefaultTexture.Media = mediaCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }

            if(materialIDCounts.Count > 0)
            {
                DefaultTexture.MaterialID = materialIDCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }

            for (int i = 0; i < MAX_TEXTURE_FACES; ++i)
            {
                TextureEntryFace face = m_FaceTextures[i];
                if(face == null)
                {
                    continue;
                }

                if(face.IsSame(DefaultTexture))
                {
                    m_FaceTextures[i] = null;
                }
            }
        }
    }
}
