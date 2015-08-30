// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Types.Agent
{
    [Flags] public enum ControlFlags : uint
    {
        None = 0,

        AtPos = 1 << 0,
        AtNeg = 1 << 1,
        LeftPos = 1 << 2,
        LeftNeg = 1 << 3,
        UpPos = 1 << 4,
        UpNeg = 1 << 5,
        PitchPos = 1 << 6,
        PitchNeg = 1 << 7,
        YawPos = 1 << 8,
        YawNeg = 1 << 9,
        FastAt = 1 << 10,
        FastLeft = 1 << 11,
        FastUp = 1 << 12,
        Fly = 1 << 13,
        Stop = 1 << 14,
        FinishAnim = 1 << 15,
        StandUp = 1 << 16,
        SitOnGround = 1 << 17,
        MouseLook = 1 << 18,
        NudgeAtPos = 1 << 19,
        NudgeAtNeg = 1 << 20,
        NudgeLeftPos = 1 << 21,
        NudgeLeftNeg = 1 << 22,
        NudgeUpPos = 1 << 23,
        NudgeUpNeg = 1 << 24,
        TurnLeft = 1 << 25,
        TurnRight = 1 << 26,
        Away = 1 << 27,
        LButtonDown = 1 << 28,
        LButtonUp = 1 << 29,
        MouseLookLButtonDown = 1 << 30,
        MouseLookLButtonUp = 1u << 31
    }

    public static class ControlFlagsExtension
    {
        public static bool HasMouselook(this ControlFlags flags)
        {
            return (flags & ControlFlags.MouseLook) != ControlFlags.None;
        }

        public static bool HasLeftButtonDown(this ControlFlags flags)
        {
            return (flags & ControlFlags.MouseLookLButtonDown) != ControlFlags.None;
        }

        public static bool HasStandUp(this ControlFlags flags)
        {
            return (flags & ControlFlags.StandUp) != ControlFlags.None;
        }

        public static bool HasSitOnGround(this ControlFlags flags)
        {
            return (flags & ControlFlags.SitOnGround) != ControlFlags.None;
        }

        public static bool HasFly(this ControlFlags flags)
        {
            return (flags & ControlFlags.Fly) != ControlFlags.None;
        }

        public static bool HasStop(this ControlFlags flags)
        {
            return (flags & ControlFlags.Stop) != ControlFlags.None;
        }

        #region Controls
        public static bool HasForward(this ControlFlags flags)
        {
            return (flags & ControlFlags.AtPos) != ControlFlags.None;
        }

        public static bool HasBack(this ControlFlags flags)
        {
            return (flags & ControlFlags.AtNeg) != ControlFlags.None;
        }

        public static bool HasLeft(this ControlFlags flags)
        {
            return (flags & ControlFlags.LeftPos) != ControlFlags.None;
        }

        public static bool HasRight(this ControlFlags flags)
        {
            return (flags & ControlFlags.LeftNeg) != ControlFlags.None;
        }

        public static bool HasUp(this ControlFlags flags)
        {
            return (flags & ControlFlags.UpPos) != ControlFlags.None;
        }

        public static bool HasDown(this ControlFlags flags)
        {
            return (flags & ControlFlags.UpNeg) != ControlFlags.None;
        }

        public static bool HasForwardNudge(this ControlFlags flags)
        {
            return (flags & ControlFlags.NudgeAtPos) != ControlFlags.None;
        }

        public static bool HasBackwardNudge(this ControlFlags flags)
        {
            return (flags & ControlFlags.NudgeAtNeg) != ControlFlags.None;
        }

        public static bool HasLeftNudge(this ControlFlags flags)
        {
            return (flags & ControlFlags.NudgeLeftPos) != ControlFlags.None;
        }

        public static bool HasRightNudge(this ControlFlags flags)
        {
            return (flags & ControlFlags.NudgeLeftNeg) != ControlFlags.None;
        }

        public static bool HasUpNudge(this ControlFlags flags)
        {
            return (flags & ControlFlags.NudgeUpPos) != ControlFlags.None;
        }

        public static bool HasDownNudge(this ControlFlags flags)
        {
            return (flags & ControlFlags.NudgeUpNeg) != ControlFlags.None;
        }

        public static bool HasTurnLeft(this ControlFlags flags)
        {
            return (flags & ControlFlags.TurnLeft) != ControlFlags.None;
        }

        public static bool HasTurnRight(this ControlFlags flags)
        {
            return (flags & ControlFlags.TurnRight) != ControlFlags.None;
        }
        #endregion
    }
}
