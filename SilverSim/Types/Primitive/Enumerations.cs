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

#pragma warning disable RCS1154

using System;

namespace SilverSim.Types.Primitive
{
    public enum Bumpiness : byte
    {
        None = 0,
        Brightness = 1,
        Darkness = 2,
        Woodgrain = 3,
        Bark = 4,
        Bricks = 5,
        Checker = 6,
        Concrete = 7,
        Crustytile = 8,
        Cutstone = 9,
        Discs = 10,
        Gravel = 11,
        Petridish = 12,
        Siding = 13,
        Stonetile = 14,
        Stucco = 15,
        Suction = 16,
        Weave = 17
    }

    public enum Shininess : byte
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum MappingType : byte
    {
        Default = 0,
        Planar = 1,
        Spherical = 2,
        Cylindrical = 3
    }

    public enum ClickActionType : byte
    {
        None = 0,
        Touch = 0,
        Sit = 1,
        Buy = 2,
        Pay = 3,
        Open = 4,
        Play = 5,
        OpenMedia = 6,
        Zoom = 7
    }

    public enum PrimitiveParamsType
    {
        Name = 27,
        Desc = 28,
        Type = 9,
        Slice = 35,
        PhysicsShapeType = 30,
        Material = 2,
        Physics = 3,
        TempOnRez = 4,
        Phantom = 5,
        Position = 6,
        PosLocal = 33,
        Rotation = 8,
        RotLocal = 29,
        Size = 7,
        Texture = 17,
        Text = 26,
        Color = 18,
        BumpShiny = 19,
        PointLight = 23,
        FullBright = 20,
        Flexible = 21,
        TexGen = 22,
        Glow = 25,
        Omega = 32,
        LinkTarget = 34,
        Specular = 36,
        Normal = 37,
        AlphaMode = 38,
        AllowUnsit = 39,
        ScriptedSitOnly = 40,
        SitTarget = 41,
        CastShadows = 24,
        PhysicsMaterial = 31,

        Alpha = 11001,

        Projector = 11100,
        ProjectorEnabled = 11101,
        ProjectorTexture = 11102,
        ProjectorFov = 11103,
        ProjectorFocus = 11104,
        ProjectorAmbience = 11105,

        UnSitTarget = 12000,
        SitAnimation = 12001,
        Language = 12002,
        RemoveLanguage = 12003,
        RemoveAllLanguages = 12004,
        LoopSound = 12005,
        SoundRadius = 12006,
        SoundVolume = 12007,
        SoundQueueing = 12008,
        AllLanguages = 12009,
        ResetParamToDefaultLang = 12010,
        SitText = 12011,
        TouchText = 12012,
        TextureAnim = 12013,
        CollisionSound = 12014,
        Velocity = 12015,
        Acceleration = 12016
    }

    public static class PrimitiveParamsToLslMethodsExtension
    {
        public static string GetLslName(this PrimitiveParamsType paramtype)
        {
            switch(paramtype)
            {
                case PrimitiveParamsType.AllowUnsit: return "PRIM_ALLOW_UNSIT";
                case PrimitiveParamsType.Alpha: return "PRIM_ALPHA";
                case PrimitiveParamsType.AlphaMode: return "PRIM_ALPHA_MODE";
                case PrimitiveParamsType.BumpShiny: return "PRIM_BUMP_SHINY";
                case PrimitiveParamsType.CastShadows: return "PRIM_CAST_SHADOWS";
                case PrimitiveParamsType.Color: return "PRIM_COLOR";
                case PrimitiveParamsType.Desc: return "PRIM_DESC";
                case PrimitiveParamsType.Flexible: return "PRIM_FLEXIBLE";
                case PrimitiveParamsType.FullBright: return "PRIM_FULLBRIGHT";
                case PrimitiveParamsType.Glow: return "PRIM_GLOW";
                case PrimitiveParamsType.Language: return "PRIM_LANGUAGE";
                case PrimitiveParamsType.LinkTarget: return "PRIM_LINK_TARGET";
                case PrimitiveParamsType.LoopSound: return "PRIM_LOOP_SOUND";
                case PrimitiveParamsType.Material: return "PRIM_MATERIAL";
                case PrimitiveParamsType.Name: return "PRIM_NAME";
                case PrimitiveParamsType.Normal: return "PRIM_NORMAL";
                case PrimitiveParamsType.Omega: return "PRIM_OMEGA";
                case PrimitiveParamsType.Phantom: return "PRIM_PHANTOM";
                case PrimitiveParamsType.Physics: return "PRIM_PHYSICS";
                case PrimitiveParamsType.PhysicsShapeType: return "PRIM_PHYSICS_SHAPE_TYPE";
                case PrimitiveParamsType.PointLight: return "PRIM_POINT_LIGHT";
                case PrimitiveParamsType.Position: return "PRIM_POSITION";
                case PrimitiveParamsType.PosLocal: return "PRIM_POS_LOCAL";
                case PrimitiveParamsType.Projector: return "PRIM_PROJECTOR";
                case PrimitiveParamsType.ProjectorAmbience: return "PRIM_PROJECTOR_AMBIENCE";
                case PrimitiveParamsType.ProjectorEnabled: return "PRIM_PROJECTOR_ENABLED";
                case PrimitiveParamsType.ProjectorFocus: return "PRIM_PROJECTOR_FOCUS";
                case PrimitiveParamsType.ProjectorFov: return "PRIM_PROJECTOR_FOV";
                case PrimitiveParamsType.ProjectorTexture: return "PRIM_PROJECTOR_TEXTURE";
                case PrimitiveParamsType.RemoveAllLanguages: return "PRIM_REMOVE_ALL_LANGUAGES";
                case PrimitiveParamsType.RemoveLanguage: return "PRIM_REMOVE_LANGUAGE";
                case PrimitiveParamsType.AllLanguages: return "PRIM_ALL_LANGUAGES";
                case PrimitiveParamsType.ResetParamToDefaultLang: return "PRIM_RESET_PARAM_TO_DEFAULT_LANGUAGE";
                case PrimitiveParamsType.Rotation: return "PRIM_ROTATION";
                case PrimitiveParamsType.RotLocal: return "PRIM_ROT_LOCAL";
                case PrimitiveParamsType.ScriptedSitOnly: return "PRIM_SCRIPTED_SIT_ONLY";
                case PrimitiveParamsType.SitAnimation: return "PRIM_SIT_ANIMATION";
                case PrimitiveParamsType.SitTarget: return "PRIM_SIT_TARGET";
                case PrimitiveParamsType.Size: return "PRIM_SIZE";
                case PrimitiveParamsType.Slice: return "PRIM_SLICE";
                case PrimitiveParamsType.SoundQueueing: return "PRIM_SOUND_QUEUEING";
                case PrimitiveParamsType.SoundRadius: return "PRIM_SOUND_RADIUS";
                case PrimitiveParamsType.SoundVolume: return "PRIM_SOUND_VOLUME";
                case PrimitiveParamsType.Specular: return "PRIM_SPECULAR";
                case PrimitiveParamsType.TempOnRez: return "PRIM_TEMP_ON_REZ";
                case PrimitiveParamsType.TexGen: return "PRIM_TEXGEN";
                case PrimitiveParamsType.Text: return "PRIM_TEXT";
                case PrimitiveParamsType.Texture: return "PRIM_TEXTURE";
                case PrimitiveParamsType.Type: return "PRIM_TYPE";
                case PrimitiveParamsType.UnSitTarget: return "PRIM_UNSIT_TARGET";
                case PrimitiveParamsType.SitText: return "PRIM_SIT_TEXT";
                case PrimitiveParamsType.TouchText: return "PRIM_TOUCH_TEXT";
                case PrimitiveParamsType.TextureAnim: return "PRIM_TEXTURE_ANIM";
                case PrimitiveParamsType.CollisionSound: return "PRIM_COLLISION_SOUND";
                case PrimitiveParamsType.Velocity: return "PRIM_VELOCITY";
                case PrimitiveParamsType.Acceleration: return "PRIM_ACCELERATION";
            }
            return string.Format("PRIM_{0}", (int)paramtype);
        }
    }

    public enum PrimitivePhysicsShapeType
    {
        Prim = 0,
        None = 1,
        Convex = 2,
    }

    public enum PrimitiveShapeType
    {
        Box = 0,
        Cylinder = 1,
        Prism = 2,
        Sphere = 3,
        Torus = 4,
        Tube = 5,
        Ring = 6,
        Sculpt = 7
    }

    [Flags]
    public enum PrimitiveSculptType : byte
    {
        None = 0,
        Sphere = 1,
        Torus = 2,
        Plane = 3,
        Cylinder = 4,
        Mesh = 5,

        TypeMask = 0x3F,

        Invert = 64,
        Mirror = 128
    }

    public enum PrimitiveProfileShape : byte
    {
        Circle = 0,
        Square = 1,
        IsometricTriangle = 2,
        EquilateralTriangle = 3,
        RightTriangle = 4,
        HalfCircle = 5,

        Mask = 0x0F
    }

    public enum PrimitiveProfileHollowShape : byte
    {
        Same = 0,
        Circle = 16,
        Square = 32,
        Triangle = 48,

        Mask = 0xF0
    }

    public enum PrimitiveExtrusion : byte
    {
        Default = 0,
        Straight = 16,
        Curve1 = 32,
        Curve2 = 48,
        Flexible = 128,
    }

    public enum PrimitiveHoleShape : byte
    {
        Default = 0x00,
        Circle = 0x10,
        Square = 0x20,
        Triangle = 0x30,
    }

    [Flags]
    public enum PrimitiveMediaPermission
    {
        None = 0,
        Owner = 1,
        Group = 2,
        Anyone = 4,
        All = 7
    }

    public enum PrimitiveMediaControls
    {
        Standard = 0,
        Mini = 1
    }
}
