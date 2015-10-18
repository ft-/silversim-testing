// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Viewer.Messages.Search
{
    [Flags]
    public enum SearchFlags : uint
    {
        People = 1,
        Online = 2,
        Events = 8,
        Groups = 16,
        DateEvents = 32,
        AgentOwned = 64,
        ForSale = 128,
        GroupOwned = 256,
        DwellSort = 1024,
        PgSimsOnly = 2048,
        PicturesOnly = 4096,
        PgEventsOnly = 8192,
        MatureSimsOnly = 16384,
        SortAsc = 32768,
        PricesSort = 65536,
        PerMeterSort = 131072,
        AreaSort = 262144,
        NameSort = 524288,
        LimitByPrice = 1048576,
        LimitByArea = 2097152,
        FilterMature = 4194304,
        PGOnly = 8388608,
        IncludePG = 16777216,
        IncludeMature = 33554432,
        IncludeAdult = 67108864,
        AdultOnly = 134217728,
    }
}
