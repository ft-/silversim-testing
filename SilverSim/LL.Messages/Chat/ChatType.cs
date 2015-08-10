// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.LL.Messages.Chat
{
    public enum ChatType : byte
    {
        Whisper = 0,
        Say = 1,
        Shout = 2,
        // 3 is an obsolete version of Say
        StartTyping = 4,
        StopTyping = 5,
        DebugChannel = 6,
        Region = 7,
        Owner = 8,
        Broadcast = 0xFF
    }
}
