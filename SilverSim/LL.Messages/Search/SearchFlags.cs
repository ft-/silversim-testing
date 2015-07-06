/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

namespace SilverSim.LL.Messages.Search
{
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
