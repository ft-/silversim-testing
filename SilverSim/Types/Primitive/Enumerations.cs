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
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Primitive
{
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
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

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Design", "UseFlagsAttributeRule")]
    public enum Shininess : byte
    {
        None = 0,
        Low = 0x40,
        Medium = 0x80,
        High = 0xc0
    }

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Design", "UseFlagsAttributeRule")]
    public enum MappingType : byte
    {
        Default = 0,
        Planar = 2,
        Spherical = 4,
        Cylindrical = 6
    }

    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum TextureAttributes : uint
    {
        None = 0,
        TextureID = 1 << 0,
        RGBA = 1 << 1,
        RepeatU = 1 << 2,
        RepeatV = 1 << 3,
        OffsetU = 1 << 4,
        OffsetV = 1 << 5,
        Rotation = 1 << 6,
        Material = 1 << 7,
        Media = 1 << 8,
        Glow = 1 << 9,
        MaterialID = 1 << 10,
        All = 0xFFFFFFFF
    }

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
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

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
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

        Alpha = 11001,

        Projector = 11110,
        ProjectorEnabled = 11101,
        ProjectorTexture = 11102,
        ProjectorFov = 11103,
        ProjectorFocus = 11104,
        ProjectorAmbience = 11105
    }

    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum PrimitivePhysicsShapeType
    {
        Prim = 0,
        None = 1,
        Convex = 2,
    }

    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
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
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public enum PrimitiveSculptType : byte
    {
        Sphere = 1,
        Torus = 2,
        Plane = 3,
        Cylinder = 4,
        Mesh = 5,

        TypeMask = 0x3F,

        Invert = 64,
        Mirror = 128
    }

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
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

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Design", "UseFlagsAttributeRule")]
    public enum PrimitiveProfileHollowShape : byte
    {
        Same = 0,
        Circle = 16,
        Square = 32,
        Triangle = 48,

        Mask = 0xF0
    }

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Design", "UseFlagsAttributeRule")]
    public enum PrimitiveExtrusion : byte
    {
        Straight = 16,
        Curve1 = 32,
        Curve2 = 48,
        Flexible = 128
    }

    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    [SuppressMessage("Gendarme.Rules.Design", "UseFlagsAttributeRule")]
    public enum PrimitiveHoleShape : byte
    {
        Default = 0x00,
        Circle = 0x10,
        Square = 0x20,
        Triangle = 0x30,
    }

    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "FlagsShouldNotDefineAZeroValueRule")]
    [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
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
