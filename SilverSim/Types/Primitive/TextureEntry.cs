// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilverSim.Types.Primitive
{
    public class TextureEntry : Asset.Format.IReferencesAccessor
    {
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const int MAX_TEXTURE_FACES = 32;
        readonly TextureEntryFace[] m_FaceTextures = new TextureEntryFace[MAX_TEXTURE_FACES];
        public TextureEntryFace DefaultTexture;
        public static readonly UUID WHITE_TEXTURE = "5748decc-f629-461c-9a36-a35a221fe21f";
        readonly object m_Lock = new object();

        public TextureEntry()
        {

        }
        
        public TextureEntry(byte[] data, int pos, int length)
        {
            FromBytes(data, pos, length);
        }

        public TextureEntry(byte[] data)
        {
            FromBytes(data, 0, data.Length);
        }

        #region References accessor
        public List<UUID> References
        {
            get
            {
                List<UUID> reflist = new List<UUID>();
                foreach(UUID id in DefaultTexture.References)
                {
                    if(!reflist.Contains(id))
                    {
                        reflist.Add(id);
                    }
                }

                foreach (TextureEntryFace face in m_FaceTextures)
                {
                    if (face != null)
                    {
                        foreach (UUID id in face.References)
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
                byte[] newBytes = new byte[4];
                Buffer.BlockCopy(bytes, pos, newBytes, 0, 4);
                Array.Reverse(newBytes);
                bytes = newBytes;
                pos = 0;
            }
            return BitConverter.ToSingle(bytes, 0);
        }

        private static float TEOffsetFloat(byte[] bytes, int pos)
        {
            if (!BitConverter.IsLittleEndian)
            {
                byte[] newBytes = new byte[2];
                Buffer.BlockCopy(bytes, pos, newBytes, 0, 2);
                Array.Reverse(newBytes);
                bytes = newBytes;
                pos = 0;
            }
            float offset = (float)BitConverter.ToInt16(bytes, pos);
            return offset / 32767.0f;
        }

        private static float TERotationFloat(byte[] bytes, int pos)
        {
            const float TWO_PI = 6.283185307179586476925286766559f;
            return ((float)(bytes[pos] | (bytes[pos + 1] << 8)) / 32768.0f) * TWO_PI;
        }

        private static float TEGlowFloat(byte[] bytes, int pos)
        {
            return (float)bytes[pos] / 255.0f;
        }

        private void FromBytes(byte[] data, int pos, int length)
        {
            if(length < 16)
            {
                DefaultTexture = new TextureEntryFace(null);
                return;
            }
            else
            {
                DefaultTexture = new TextureEntryFace(null);
            }

            uint bitfieldSize = 0;
            uint faceBits = 0;
            int i = pos;

            #region Texture
            DefaultTexture.TextureID = new UUID(data, i);
            i += 16;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                UUID tmpUUID = new UUID(data, i);
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
            DefaultTexture.TextureColor = new ColorAlpha(data, i);
            i += 4;

            while (ReadFaceBitfield(data, ref i, ref faceBits, ref bitfieldSize))
            {
                ColorAlpha tmpColor = new ColorAlpha(data, i);
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
                    UUID tmpUUID = new UUID(data, i);
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


        private static byte TEGlowByte(float glow)
        {
            return (byte)(glow * 255.0f);
        }

        private static short TEOffsetShort(float offset)
        {
            offset = Math.Min(Math.Max(offset, -1.0f), 1.0f);
            offset *= 32767.0f;
            return (short)Math.Round(offset);
        }

        private static short TERotationShort(float rotation)
        {
            const float TWO_PI = 6.283185307179586476925286766559f;
            return (short)Math.Round(((Math.IEEERemainder(rotation, TWO_PI) / TWO_PI) * 32768.0f) + 0.5f);
        }

        public static implicit operator byte[](TextureEntry e)
        {
            return e.GetBytes();
        }

        [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule")]
        public byte[] GetBytes()
        {
            if (DefaultTexture == null)
                return new byte[0];

            using (MemoryStream memStream = new MemoryStream())
            {
                using (BinaryWriter binWriter = new BinaryWriter(memStream))
                {
                    #region Bitfield Setup

                    uint[] textures = new uint[m_FaceTextures.Length];
                    InitializeArray(ref textures);
                    uint[] texturecolors = new uint[m_FaceTextures.Length];
                    InitializeArray(ref texturecolors);
                    uint[] repeatus = new uint[m_FaceTextures.Length];
                    InitializeArray(ref repeatus);
                    uint[] repeatvs = new uint[m_FaceTextures.Length];
                    InitializeArray(ref repeatvs);
                    uint[] offsetus = new uint[m_FaceTextures.Length];
                    InitializeArray(ref offsetus);
                    uint[] offsetvs = new uint[m_FaceTextures.Length];
                    InitializeArray(ref offsetvs);
                    uint[] rotations = new uint[m_FaceTextures.Length];
                    InitializeArray(ref rotations);
                    uint[] materials = new uint[m_FaceTextures.Length];
                    InitializeArray(ref materials);
                    uint[] medias = new uint[m_FaceTextures.Length];
                    InitializeArray(ref medias);
                    uint[] glows = new uint[m_FaceTextures.Length];
                    InitializeArray(ref glows);
                    uint[] materialIDs = new uint[m_FaceTextures.Length];
                    InitializeArray(ref materialIDs);

                    for (int i = 0; i < m_FaceTextures.Length; i++)
                    {
                        if (m_FaceTextures[i] == null) continue;

                        if (m_FaceTextures[i].TextureID != DefaultTexture.TextureID)
                        {
                            if (textures[i] == UInt32.MaxValue)
                            {
                                textures[i] = 0;
                            }
                            textures[i] |= (uint)(1 << i);
                        }
                        if (m_FaceTextures[i].TextureColor != DefaultTexture.TextureColor)
                        {
                            if (texturecolors[i] == UInt32.MaxValue)
                            {
                                texturecolors[i] = 0;
                            }
                            texturecolors[i] |= (uint)(1 << i);
                        }
                        if (m_FaceTextures[i].RepeatU != DefaultTexture.RepeatU)
                        {
                            if (repeatus[i] == UInt32.MaxValue)
                            {
                                repeatus[i] = 0;
                            }
                            repeatus[i] |= (uint)(1 << i);
                        }
                        if (m_FaceTextures[i].RepeatV != DefaultTexture.RepeatV)
                        {
                            if (repeatvs[i] == UInt32.MaxValue)
                            {
                                repeatvs[i] = 0;
                            }
                            repeatvs[i] |= (uint)(1 << i);
                        }
                        if (TEOffsetShort(m_FaceTextures[i].OffsetU) != TEOffsetShort(DefaultTexture.OffsetU))
                        {
                            if (offsetus[i] == UInt32.MaxValue)
                            {
                                offsetus[i] = 0;
                            }
                            offsetus[i] |= (uint)(1 << i);
                        }
                        if (TEOffsetShort(m_FaceTextures[i].OffsetV) != TEOffsetShort(DefaultTexture.OffsetV))
                        {
                            if (offsetvs[i] == UInt32.MaxValue)
                            {
                                offsetvs[i] = 0;
                            }
                            offsetvs[i] |= (uint)(1 << i);
                        }
                        if (TERotationShort(m_FaceTextures[i].Rotation) != TERotationShort(DefaultTexture.Rotation))
                        {
                            if (rotations[i] == UInt32.MaxValue)
                            {
                                rotations[i] = 0;
                            }
                            rotations[i] |= (uint)(1 << i);
                        }
                        if (m_FaceTextures[i].Material != DefaultTexture.Material)
                        {
                            if (materials[i] == UInt32.MaxValue)
                            {
                                materials[i] = 0;
                            }
                            materials[i] |= (uint)(1 << i);
                        }
                        if (m_FaceTextures[i].Media != DefaultTexture.Media)
                        {
                            if (medias[i] == UInt32.MaxValue)
                            {
                                medias[i] = 0;
                            }
                            medias[i] |= (uint)(1 << i);
                        }
                        if (TEGlowByte(m_FaceTextures[i].Glow) != TEGlowByte(DefaultTexture.Glow))
                        {
                            if (glows[i] == UInt32.MaxValue)
                            {
                                glows[i] = 0;
                            }
                            glows[i] |= (uint)(1 << i);
                        }
                        if (m_FaceTextures[i].MaterialID != DefaultTexture.MaterialID)
                        {
                            if (materialIDs[i] == UInt32.MaxValue) materialIDs[i] = 0;
                            materialIDs[i] |= (uint)(1 << i);
                        }
                    }

                    #endregion Bitfield Setup

                    #region Texture
                    binWriter.Write(DefaultTexture.TextureID.GetBytes());
                    for (int i = 0; i < textures.Length; i++)
                    {
                        if (textures[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(textures[i]));
                            binWriter.Write(m_FaceTextures[i].TextureID.GetBytes());
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion Texture

                    #region Color
                    // Serialize the color bytes inverted to optimize for zerocoding
                    binWriter.Write(DefaultTexture.TextureColor.AsByte);
                    for (int i = 0; i < texturecolors.Length; i++)
                    {
                        if (texturecolors[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(texturecolors[i]));
                            // Serialize the color bytes inverted to optimize for zerocoding
                            binWriter.Write(m_FaceTextures[i].TextureColor.AsByte);
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion Color

                    #region RepeatU
                    binWriter.Write(DefaultTexture.RepeatU);
                    for (int i = 0; i < repeatus.Length; i++)
                    {
                        if (repeatus[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(repeatus[i]));
                            binWriter.Write(m_FaceTextures[i].RepeatU);
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion RepeatU

                    #region RepeatV
                    binWriter.Write(DefaultTexture.RepeatV);
                    for (int i = 0; i < repeatvs.Length; i++)
                    {
                        if (repeatvs[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(repeatvs[i]));
                            binWriter.Write(m_FaceTextures[i].RepeatV);
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion RepeatV

                    #region OffsetU
                    binWriter.Write(TEOffsetShort(DefaultTexture.OffsetU));
                    for (int i = 0; i < offsetus.Length; i++)
                    {
                        if (offsetus[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(offsetus[i]));
                            binWriter.Write(TEOffsetShort(m_FaceTextures[i].OffsetU));
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion OffsetU

                    #region OffsetV
                    binWriter.Write(TEOffsetShort(DefaultTexture.OffsetV));
                    for (int i = 0; i < offsetvs.Length; i++)
                    {
                        if (offsetvs[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(offsetvs[i]));
                            binWriter.Write(TEOffsetShort(m_FaceTextures[i].OffsetV));
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion OffsetV

                    #region Rotation
                    binWriter.Write(TERotationShort(DefaultTexture.Rotation));
                    for (int i = 0; i < rotations.Length; i++)
                    {
                        if (rotations[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(rotations[i]));
                            binWriter.Write(TERotationShort(m_FaceTextures[i].Rotation));
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion Rotation

                    #region Material
                    binWriter.Write(DefaultTexture.Material);
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(materials[i]));
                            binWriter.Write(m_FaceTextures[i].Material);
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion Material

                    #region Media
                    binWriter.Write(DefaultTexture.Media);
                    for (int i = 0; i < medias.Length; i++)
                    {
                        if (medias[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(medias[i]));
                            binWriter.Write(m_FaceTextures[i].Media);
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion Media

                    #region Glow
                    binWriter.Write(TEGlowByte(DefaultTexture.Glow));
                    for (int i = 0; i < glows.Length; i++)
                    {
                        if (glows[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(glows[i]));
                            binWriter.Write(TEGlowByte(m_FaceTextures[i].Glow));
                        }
                    }
                    binWriter.Write((byte)0);
                    #endregion Glow

                    #region MaterialID
                    binWriter.Write(DefaultTexture.MaterialID.GetBytes());
                    for (int i = 0; i < materialIDs.Length; i++)
                    {
                        if (materialIDs[i] != UInt32.MaxValue)
                        {
                            binWriter.Write(GetFaceBitfieldBytes(materialIDs[i]));
                            binWriter.Write(m_FaceTextures[i].MaterialID.GetBytes());
                        }
                    }
                    #endregion MaterialID

                    return memStream.ToArray();
                }
            }
        }

        #region Helpers

        private void InitializeArray(ref uint[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = UInt32.MaxValue;
            }
        }

        private bool ReadFaceBitfield(byte[] data, ref int pos, ref uint faceBits, ref uint bitfieldSize)
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

            return (faceBits != 0);
        }

        private byte[] GetFaceBitfieldBytes(uint bitfield)
        {
            int byteLength = 0;
            uint tmpBitfield = bitfield;

            while (tmpBitfield != 0)
            {
                tmpBitfield >>= 7;
                byteLength++;
            }

            if (byteLength == 0)
            {
                return new byte[1] { 0 };
            }

            byte[] bytes = new byte[byteLength];
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
    }
}
