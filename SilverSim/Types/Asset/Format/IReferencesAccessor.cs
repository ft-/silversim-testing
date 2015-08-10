// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Types.Asset.Format
{
    public interface IReferencesAccessor
    {
        List<UUID> References
        {
            get;
        }
    }
}
