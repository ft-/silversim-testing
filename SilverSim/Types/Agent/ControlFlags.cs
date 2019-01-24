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
    public enum ControlFlags : uint
    {
        None = 0,

        AtPos = 1 << 0,
        AtNeg = 1 << 1,
        LeftPos = 1 << 2,
        LeftNeg = 1 << 3,
        UpPos = 1 << 4,
        UpNeg = 1 << 5,
        PitchPos = 1 << 6, /* joystick input */
        PitchNeg = 1 << 7, /* joystick input */
        YawPos = 1 << 8, /* joystick input */
        YawNeg = 1 << 9, /* joystick input */
        FastAt = 1 << 10, /* fast movement flag for AtPos/AtNeg */
        FastLeft = 1 << 11, /* fast movement flag for LeftPos/LeftNeg */
        FastUp = 1 << 12, /* fast movement flag for UpPos/UpNeg */
        Fly = 1 << 13, /* viewer is in fly when set */
        Stop = 1 << 14,
        FinishAnim = 1 << 15,
        StandUp = 1 << 16,
        SitOnGround = 1 << 17,
        MouseLook = 1 << 18, /* viewer is in mouselook when set */
        NudgeAtPos = 1 << 19,
        NudgeAtNeg = 1 << 20,
        NudgeLeftPos = 1 << 21,
        NudgeLeftNeg = 1 << 22,
        NudgeUpPos = 1 << 23,
        NudgeUpNeg = 1 << 24,
        TurnLeft = 1 << 25,
        TurnRight = 1 << 26,
        Away = 1 << 27, /* viewer is in away mode when set */
        LButtonDown = 1 << 28,
        LButtonUp = 1 << 29,
        MouseLookLButtonDown = 1 << 30,
        MouseLookLButtonUp = 1u << 31
    }

    public static class ControlFlagsExtension
    {
        public static bool HasMouselook(this ControlFlags flags) => (flags & ControlFlags.MouseLook) != ControlFlags.None;

        public static bool HasLeftButtonDown(this ControlFlags flags) => (flags & ControlFlags.MouseLookLButtonDown) != ControlFlags.None;

        public static bool HasStandUp(this ControlFlags flags) => (flags & ControlFlags.StandUp) != ControlFlags.None;

        public static bool HasSitOnGround(this ControlFlags flags) => (flags & ControlFlags.SitOnGround) != ControlFlags.None;

        public static bool HasFly(this ControlFlags flags) => (flags & ControlFlags.Fly) != ControlFlags.None;

        public static bool HasAway(this ControlFlags flags) => (flags & ControlFlags.Away) != ControlFlags.None;

        public static bool HasStop(this ControlFlags flags) => (flags & ControlFlags.Stop) != ControlFlags.None;

        #region Controls
        public static bool HasForward(this ControlFlags flags) => (flags & ControlFlags.AtPos) != ControlFlags.None;

        public static bool HasBack(this ControlFlags flags) => (flags & ControlFlags.AtNeg) != ControlFlags.None;

        public static bool HasLeft(this ControlFlags flags) => (flags & ControlFlags.LeftPos) != ControlFlags.None;

        public static bool HasRight(this ControlFlags flags) => (flags & ControlFlags.LeftNeg) != ControlFlags.None;

        public static bool HasUp(this ControlFlags flags) => (flags & ControlFlags.UpPos) != ControlFlags.None;

        public static bool HasDown(this ControlFlags flags) => (flags & ControlFlags.UpNeg) != ControlFlags.None;

        public static bool HasForwardNudge(this ControlFlags flags) => (flags & ControlFlags.NudgeAtPos) != ControlFlags.None;

        public static bool HasBackwardNudge(this ControlFlags flags) => (flags & ControlFlags.NudgeAtNeg) != ControlFlags.None;

        public static bool HasLeftNudge(this ControlFlags flags) => (flags & ControlFlags.NudgeLeftPos) != ControlFlags.None;

        public static bool HasRightNudge(this ControlFlags flags) => (flags & ControlFlags.NudgeLeftNeg) != ControlFlags.None;

        public static bool HasUpNudge(this ControlFlags flags) => (flags & ControlFlags.NudgeUpPos) != ControlFlags.None;

        public static bool HasDownNudge(this ControlFlags flags) => (flags & ControlFlags.NudgeUpNeg) != ControlFlags.None;

        public static bool HasTurnLeft(this ControlFlags flags) => (flags & ControlFlags.TurnLeft) != ControlFlags.None;

        public static bool HasTurnRight(this ControlFlags flags) => (flags & ControlFlags.TurnRight) != ControlFlags.None;
        #endregion
    }
}
