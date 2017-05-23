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

namespace SilverSim.Types.Agent
{
    [Flags]
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
        HindRightFoot = 55,

        PositionMask = 0x7F,
        AppendFlag = 0x80
    }
}
