﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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
        Low = 0x40,
        Medium = 0x80,
        High = 0xc0
    }

    public enum MappingType : byte
    {
        Default = 0,
        Planar = 2,
        Spherical = 4,
        Cylindrical = 6
    }

    [Flags]
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

    public enum PrimitiveParamsType : int
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
        AlphaMode = 38
    }

    public enum PrimitivePhysicsShapeType : int
    {
        Prim = 0,
        None = 1,
        Convex = 2,
    }

    public enum PrimitiveShapeType : int
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
        Sphere = 1,
        Torus = 2,
        Plane = 3,
        Cylinder = 4,
        Mesh = 5,
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
        Straight = 16,
        Curve1 = 32,
        Curve2 = 48,
        Flexible = 128
    }

    public enum PrimitiveHoleShape : byte
    {
        Default = 0x00,
        Circle = 0x10,
        Square = 0x20,
        Triangle = 0x30,
    }

    [Flags]
    public enum PrimitiveMediaPermission : int
    {
        None = 0,
        Owner = 1,
        Group = 2,
        Anyone = 4,
        All = 7
    }

    public enum PrimitiveMediaControls : int
    {
        Standard = 0,
        Mini = 1
    }
}
