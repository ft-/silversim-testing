// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.ServiceInterfaces.Grid
{
    public struct GridType : IEquatable<GridType>
    {
        public string Name;

        public GridType(string name)
        {
            Name = name;
        }

        public bool Equals(GridType other)
        {
            return Name == other.Name;
        }
    }
}
