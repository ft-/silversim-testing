// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types.Grid;

namespace SilverSim.ServiceInterfaces.Grid
{
    public abstract class ForeignGridConnector
    {
        public ForeignGridConnector()
        {

        }

        public abstract RegionInfo this[string name] /* specifying empty string results in DefaultRegion lookup */
        {
            get;
        }
    }
}
