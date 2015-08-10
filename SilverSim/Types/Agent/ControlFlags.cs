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
}
