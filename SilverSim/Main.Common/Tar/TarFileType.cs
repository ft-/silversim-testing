// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Main.Common.Tar
{
    public enum TarFileType : byte
    {
        File = (byte)'0',
        Directory = (byte)'5',
        LongLink = (byte)'L'
    }
}
