// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Types.Profile
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct ProfileClassified
    {
        public UUID ClassifiedID;
        public UUI Creator;
        public Date CreationDate;
        public Date ExpirationDate;
        public int Category;
        public string Name;
        public string Description;
        public UUID ParcelID;
        public string ParcelName;
        public int ParentEstate;
        public UUID SnapshotID;
        public string SimName;
        public Vector3 GlobalPos;
        public byte Flags;
        public int Price;
    }
}
