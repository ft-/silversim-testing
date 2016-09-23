// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Agent
{
    [Flags]
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum AttachmentPoint : byte
    {
        NotAttached = 0,
        Default = 0,

        Chest = 1,
        Head = 2,
        LeftShoulder = 3,
        RightShoulder = 4,
        LeftHand = 5,
        RightHand = 6,
        LeftFoot = 7,
        RightFoot = 8,
        Back = 9,
        Pelvis = 10,
        Mouth = 11,
        Chin = 12,
        LeftEar = 13,
        RightEar = 14,
        LeftEye = 15,
        RightEye = 16,
        Nose = 17,
        RightUpperArm = 18,
        RightLowerArm = 19,
        LeftUpperArm = 20,
        LeftLowerArm = 21,
        RightHip = 22,
        RightUpperLeg = 23,
        RightLowerLeg = 24,
        LeftHip = 25,
        LeftUpperLeg = 26,
        LeftLowerLeg = 27,
        Belly = 28,
        RightPec = 29,
        LeftPec = 30,
        HudCenter2 = 31,
        HudTopRight = 32,
        HudTopCenter = 33,
        HudTopLeft = 34,
        HudCenter1 = 35,
        HudBottomLeft = 36,
        HudBottom = 37,
        HudBottomRight = 38,
        Neck = 39,
        AvatarCenter = 40,
        LeftHandRing1 = 41,
        RightHandRing1 = 42,
        TailBase = 43,
        TailTip = 44,
        LeftWing = 45,
        RightWing = 46,
        FaceJaw = 47,
        FaceLeftEar = 48,
        FaceRightEar = 49,
        FaceLeftEye = 50,
        FaceRightEye = 51,
        FaceTongue = 52,
        Groin = 53,
        HindLeftFoot = 54,
        HindRightFot = 55,

        PositionMask = 0x7F,
        AppendFlag = 0x80
    }
}
