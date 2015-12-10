// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.ServiceInterfaces.Grid
{
    public abstract class GridInfoServiceInterface
    {
        public GridInfoServiceInterface()
        {

        }

        public abstract string GridNick { get; }
        public abstract string GridName { get; }
        public abstract string LoginURI { get; }
        public abstract string HomeURI { get; }
        public abstract string this[string key] { get; }
        public abstract bool ContainsKey(string key);
        public abstract bool TryGetValue(string key, out string value);
    }
}
