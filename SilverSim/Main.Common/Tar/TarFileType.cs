// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Main.Common.Tar
{
    [SuppressMessage("Gendarme.Rules.Design", "EnumsShouldUseInt32Rule")]
    public enum TarFileType : byte
    {
        File = (byte)'0',
        Directory = (byte)'5',
        LongLink = (byte)'L'
    }
}
