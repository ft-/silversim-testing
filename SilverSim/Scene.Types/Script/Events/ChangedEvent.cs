// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Script.Events
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct ChangedEvent : IScriptEvent
    {
        [Flags]
        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public enum ChangedFlags
        {
            Inventory = 0x001,
            Color = 0x002,
            Shape = 0x004,
            Scale = 0x008,
            Texture = 0x010,
            Link = 0x020,
            AllowedDrop = 0x040,
            Owner = 0x080,
            Region = 0x100,
            Teleport = 0x200,
            RegionStart = 0x400,
            Media = 0x800
        }

        public ChangedFlags Flags;
    }
}
