// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.Types.Script.Events
{
    public struct ControlEvent : IScriptEvent
    {
        public enum ControlFlags : int
        {
            Forward = 0x00000001,
            Back = 0x00000002,
            Left = 0x00000004,
            Right = 0x00000008,
            RotateLeft = 0x00000100,
            RotateRight = 0x00000200,
            Up = 0x00000010,
            Down = 0x00000020,
            LButton = 0x10000000,
            MouseLook_LButton = 0x40000000
        }

        public UUID AgentID;
        public int Level;
        public int Flags;
    }
}
