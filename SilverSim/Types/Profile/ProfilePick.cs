﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Profile
{
    public struct ProfilePick
    {
        public UUID PickID;
        public UUI Creator;
        public bool TopPick;
        public string Name;
        public string OriginalName;
        public string Description;
        public UUID ParcelID;
        public UUID SnapshotID;
        public string ParcelName;
        public string SimName;
        public Vector3 GlobalPosition;
        public int SortOrder;
        public bool Enabled;
        public string GatekeeperURI;
    }
}
