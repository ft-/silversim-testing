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
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Object.Localization
{
    public partial class ObjectPartLocalizedInfo
    {
        #region Primitive Methods
        private const int PRIM_ALPHA_MODE_BLEND = 1;

        public const int ALL_SIDES = -1;

        public ICollection<TextureEntryFace> GetFaces(int face)
        {
            if (face == ALL_SIDES)
            {
                var list = new List<TextureEntryFace>();
                for (uint i = 0; i < m_Part.NumberOfSides; ++i)
                {
                    list.Add(m_TextureEntry[i]);
                }
                return list;
            }
            else
            {
                return new List<TextureEntryFace>
                {
                    m_TextureEntry[(uint)face]
                };
            }
        }

        internal void GetTexPrimitiveParams(IEnumerator<IValue> enumerator, PrimitiveParamsType type, AnArray paramList, string paramtypename)
        {
            m_TextureEntryLock.AcquireReaderLock(() =>
            {
                foreach (TextureEntryFace face in GetFaces(ParamsHelper.GetInteger(enumerator, paramtypename)))
                {
                    GetTexPrimitiveParams(face, type, paramList);
                }
            });
        }

        private void GetTexPrimitiveParams(TextureEntryFace face, PrimitiveParamsType type, AnArray paramList)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    paramList.Add(m_Part.GetTextureInventoryItem(face.TextureID));
                    paramList.Add(new Vector3(face.RepeatU, face.RepeatV, 0));
                    paramList.Add(new Vector3(face.OffsetU, face.OffsetV, 0));
                    paramList.Add(face.Rotation);
                    break;

                case PrimitiveParamsType.Color:
                    paramList.Add(face.TextureColor.AsVector3);
                    paramList.Add(face.TextureColor.A);
                    break;

                case PrimitiveParamsType.Alpha:
                    paramList.Add(face.TextureColor.A);
                    break;

                case PrimitiveParamsType.BumpShiny:
                    paramList.Add((int)face.Shiny);
                    paramList.Add((int)face.Bump);
                    break;

                case PrimitiveParamsType.FullBright:
                    paramList.Add(face.FullBright);
                    break;

                case PrimitiveParamsType.TexGen:
                    paramList.Add((int)face.TexMapType);
                    break;

                case PrimitiveParamsType.Glow:
                    paramList.Add(face.Glow);
                    break;

                case PrimitiveParamsType.AlphaMode:
                    /* [ PRIM_ALPHA_MODE, integer face, integer alpha_mode, integer mask_cutoff ] */
                    if (face.MaterialID == UUID.Zero)
                    {
                        paramList.Add(PRIM_ALPHA_MODE_BLEND);
                        paramList.Add(0);
                    }
                    else
                    {
                        try
                        {
                            var mat = m_Part.ObjectGroup.Scene.GetMaterial(face.MaterialID);
                            paramList.Add(mat.DiffuseAlphaMode);
                            paramList.Add(mat.AlphaMaskCutoff);
                        }
                        catch
                        {
                            paramList.Add(PRIM_ALPHA_MODE_BLEND);
                            paramList.Add(0);
                        }
                    }
                    break;

                case PrimitiveParamsType.Normal:
                    /* [ PRIM_NORMAL, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians ] */
                    if (face.MaterialID == UUID.Zero)
                    {
                        paramList.Add(UUID.Zero);
                        paramList.Add(new Vector3(1, 1, 0));
                        paramList.Add(Vector3.Zero);
                        paramList.Add(0f);
                    }
                    else
                    {
                        try
                        {
                            var mat = m_Part.ObjectGroup.Scene.GetMaterial(face.MaterialID);
                            paramList.Add(m_Part.GetTextureInventoryItem(mat.NormMap));
                            paramList.Add(new Vector3(mat.NormRepeatX, mat.NormRepeatY, 0) / Material.MATERIALS_MULTIPLIER);
                            paramList.Add(new Vector3(mat.NormOffsetX, mat.NormOffsetY, 0) / Material.MATERIALS_MULTIPLIER);
                            paramList.Add(mat.NormRotation);
                        }
                        catch
                        {
                            paramList.Add(UUID.Zero);
                            paramList.Add(new Vector3(1, 1, 0));
                            paramList.Add(Vector3.Zero);
                            paramList.Add(0f);
                        }
                    }
                    break;

                case PrimitiveParamsType.Specular:
                    /* [ PRIM_SPECULAR, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians, vector color, integer glossiness, integer environment ] */
                    if (face.MaterialID == UUID.Zero)
                    {
                        paramList.Add(UUID.Zero);
                        paramList.Add(new Vector3(1, 1, 0));
                        paramList.Add(Vector3.Zero);
                        paramList.Add(0f);
                        paramList.Add(Vector3.One);
                        paramList.Add(0);
                        paramList.Add(0);
                    }
                    else
                    {
                        try
                        {
                            var mat = m_Part.ObjectGroup.Scene.GetMaterial(face.MaterialID);
                            paramList.Add(m_Part.GetTextureInventoryItem(mat.SpecMap));
                            paramList.Add(new Vector3(mat.SpecRepeatX, mat.SpecRepeatY, 0) / Material.MATERIALS_MULTIPLIER);
                            paramList.Add(new Vector3(mat.SpecOffsetX, mat.SpecOffsetY, 0) / Material.MATERIALS_MULTIPLIER);
                            paramList.Add(mat.SpecRotation / Material.MATERIALS_MULTIPLIER);
                            paramList.Add(mat.SpecColor.AsVector3);
                            paramList.Add(mat.SpecExp);
                            paramList.Add(mat.EnvIntensity);
                        }
                        catch
                        {
                            paramList.Add(UUID.Zero);
                            paramList.Add(new Vector3(1, 1, 0));
                            paramList.Add(Vector3.Zero);
                            paramList.Add(0f);
                            paramList.Add(Vector3.One);
                            paramList.Add(0);
                            paramList.Add(0);
                        }
                    }
                    break;

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }

        internal void SetTexPrimitiveParams(PrimitiveParamsType paramtype, AnArray.MarkEnumerator enumerator, ref UpdateChangedFlags flags, ref bool isUpdated, string paramtypename)
        {
            m_TextureEntryLock.AcquireWriterLock(-1);
            try
            {
                var faces = GetFaces(ParamsHelper.GetInteger(enumerator, paramtypename));
                enumerator.MarkPosition();
                foreach (var face in faces)
                {
                    enumerator.GoToMarkPosition();
                    SetTexPrimitiveParams(face, paramtype, enumerator, ref flags, ref isUpdated);
                }
                m_TextureEntryBytes = m_TextureEntry.GetBytes();
            }
            finally
            {
                m_TextureEntryLock.ReleaseWriterLock();
            }
        }

        public void SetTexPrimitiveParams(TextureEntryFace face, PrimitiveParamsType type, AnArray.MarkEnumerator enumerator, ref UpdateChangedFlags flags, ref bool isUpdated)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    {
                        UUID textureID = m_Part.GetTextureParam(enumerator, "PRIM_TEXTURE");
                        if(m_Part.TryFetchTexture(textureID))
                        {
                            face.TextureID = textureID;
                        }
                        Vector3 v = ParamsHelper.GetVector(enumerator, "PRIM_TEXTURE");
                        face.RepeatU = (float)v.X;
                        face.RepeatV = (float)v.Y;
                        v = ParamsHelper.GetVector(enumerator, "PRIM_TEXTURE");
                        face.OffsetU = (float)v.X;
                        face.OffsetV = (float)v.Y;
                        face.Rotation = (float)ParamsHelper.GetDouble(enumerator, "PRIM_TEXTURE");
                    }
                    flags |= UpdateChangedFlags.Texture;
                    isUpdated = true;
                    break;

                case PrimitiveParamsType.Color:
                    {
                        Vector3 color = ParamsHelper.GetVector(enumerator, "PRIM_COLOR");
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_COLOR").Clamp(0, 1);
                        face.TextureColor = new ColorAlpha(color, alpha);
                    }
                    flags |= UpdateChangedFlags.Color;
                    isUpdated = true;
                    break;

                case PrimitiveParamsType.Alpha:
                    {
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_ALPHA").Clamp(0, 1);
                        ColorAlpha color = face.TextureColor;
                        color.A = alpha;
                        face.TextureColor = color;
                    }
                    flags |= UpdateChangedFlags.Color;
                    isUpdated = true;
                    break;

                case PrimitiveParamsType.BumpShiny:
                    face.Shiny = (Shininess)ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY");
                    face.Bump = (Bumpiness)ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY");
                    flags |= UpdateChangedFlags.Texture;
                    isUpdated = true;
                    break;

                case PrimitiveParamsType.FullBright:
                    face.FullBright = ParamsHelper.GetBoolean(enumerator, "PRIM_FULLBRIGHT");
                    flags |= UpdateChangedFlags.Color;
                    isUpdated = true;
                    break;

                case PrimitiveParamsType.TexGen:
                    face.TexMapType = (MappingType)ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN");
                    flags |= UpdateChangedFlags.Texture;
                    isUpdated = true;
                    break;

                case PrimitiveParamsType.Glow:
                    face.Glow = (float)ParamsHelper.GetDouble(enumerator, "PRIM_GLOW").Clamp(0, 1);
                    flags |= UpdateChangedFlags.Color;
                    isUpdated = true;
                    break;

                case PrimitiveParamsType.AlphaMode:
                    /* [ PRIM_ALPHA_MODE, integer face, integer alpha_mode, integer mask_cutoff ] */
                    {
                        Material mat;
                        try
                        {
                            mat = m_Part.ObjectGroup.Scene.GetMaterial(face.MaterialID);
                        }
                        catch
                        {
                            mat = new Material();
                        }
                        mat.DiffuseAlphaMode = ParamsHelper.GetInteger(enumerator, "PRIM_ALPHA_MODE");
                        mat.AlphaMaskCutoff = ParamsHelper.GetInteger(enumerator, "PRIM_ALPHA_MODE");
                        mat.DiffuseAlphaMode = mat.DiffuseAlphaMode.Clamp(0, 3);
                        mat.AlphaMaskCutoff = mat.AlphaMaskCutoff.Clamp(0, 3);
                        mat.MaterialID = UUID.Random;
                        m_Part.ObjectGroup.Scene.StoreMaterial(mat);
                        face.MaterialID = mat.MaterialID;
                    }
                    flags |= UpdateChangedFlags.Texture;
                    isUpdated = true;
                    break;

                case PrimitiveParamsType.Normal:
                    /* [ PRIM_NORMAL, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians ] */
                    {
                        UUID texture = m_Part.GetTextureParam(enumerator, "PRIM_NORMAL");
                        Vector3 repeats = ParamsHelper.GetVector(enumerator, "PRIM_NORMAL");
                        Vector3 offsets = ParamsHelper.GetVector(enumerator, "PRIM_NORMAL");
                        double rotation = ParamsHelper.GetDouble(enumerator, "PRIM_NORMAL");

                        repeats.X *= Material.MATERIALS_MULTIPLIER;
                        repeats.Y *= Material.MATERIALS_MULTIPLIER;
                        offsets.X *= Material.MATERIALS_MULTIPLIER;
                        offsets.Y *= Material.MATERIALS_MULTIPLIER;
                        rotation %= Math.PI * 2;
                        rotation *= Material.MATERIALS_MULTIPLIER;

                        Material mat;
                        try
                        {
                            mat = m_Part.ObjectGroup.Scene.GetMaterial(face.MaterialID);
                        }
                        catch
                        {
                            mat = new Material();
                        }
                        mat.NormMap = texture;
                        mat.NormOffsetX = (int)Math.Round(offsets.X);
                        mat.NormOffsetY = (int)Math.Round(offsets.Y);
                        mat.NormRepeatX = (int)Math.Round(repeats.X);
                        mat.NormRepeatY = (int)Math.Round(repeats.Y);
                        mat.NormRotation = (int)Math.Round(rotation);
                        mat.MaterialID = UUID.Random;
                        if (m_Part.TryFetchTexture(texture))
                        {
                            m_Part.ObjectGroup.Scene.StoreMaterial(mat);
                            face.MaterialID = mat.MaterialID;
                        }
                    }
                    flags |= UpdateChangedFlags.Texture;
                    isUpdated = true;
                    break;

                case PrimitiveParamsType.Specular:
                    /* [ PRIM_SPECULAR, integer face, string texture, vector repeats, vector offsets, float rotation_in_radians, vector color, integer glossiness, integer environment ] */
                    {
                        UUID texture = m_Part.GetTextureParam(enumerator, "PRIM_NORMAL");
                        Vector3 repeats = ParamsHelper.GetVector(enumerator, "PRIM_SPECULAR");
                        Vector3 offsets = ParamsHelper.GetVector(enumerator, "PRIM_SPECULAR");
                        repeats *= Material.MATERIALS_MULTIPLIER;
                        offsets *= Material.MATERIALS_MULTIPLIER;
                        double rotation = ParamsHelper.GetDouble(enumerator, "PRIM_SPECULAR");
                        rotation %= Math.PI * 2;
                        rotation *= Material.MATERIALS_MULTIPLIER;
                        var color = new ColorAlpha(ParamsHelper.GetVector(enumerator, "PRIM_SPECULAR"), 1);
                        int glossiness = ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR");
                        int environment = ParamsHelper.GetInteger(enumerator, "PRIM_SPECULAR");
                        environment = environment.Clamp(0, 255);
                        glossiness = glossiness.Clamp(0, 255);
                        Material mat;
                        try
                        {
                            mat = m_Part.ObjectGroup.Scene.GetMaterial(face.MaterialID);
                        }
                        catch
                        {
                            mat = new Material();
                        }
                        mat.SpecColor = color;
                        mat.SpecMap = texture;
                        mat.SpecOffsetX = (int)Math.Round(offsets.X);
                        mat.SpecOffsetY = (int)Math.Round(offsets.Y);
                        mat.SpecRepeatX = (int)Math.Round(repeats.X);
                        mat.SpecRepeatY = (int)Math.Round(repeats.Y);
                        mat.SpecRotation = (int)Math.Round(rotation);
                        mat.EnvIntensity = environment;
                        mat.SpecExp = glossiness;
                        mat.MaterialID = UUID.Random;
                        if (m_Part.TryFetchTexture(texture))
                        {
                            m_Part.ObjectGroup.Scene.StoreMaterial(mat);
                            face.MaterialID = mat.MaterialID;
                        }
                    }
                    flags |= UpdateChangedFlags.Texture;
                    isUpdated = true;
                    break;

                default:
                    throw new ArgumentException($"Internal error! Primitive parameter type {type} should not be passed to PrimitiveFace");
            }
        }
        #endregion
    }
}
