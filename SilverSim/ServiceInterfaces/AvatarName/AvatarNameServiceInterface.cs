// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.ServiceInterfaces.AvatarName
{
    public abstract class AvatarNameServiceInterface
    {
        public AvatarNameServiceInterface()
        {

        }

        public abstract UUI this[UUID key] { get; set; } /* setting to null clears an entry if supported */
        /* if setting is not supported, the set access is ignored */
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract UUI this[string firstName, string lastName] { get; }
        public abstract List<UUI> Search(string[] names); /* returns empty list when not supported */

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public UUI this[UUI input]
        {
            get
            {
                try
                {
                    if (!input.IsAuthoritative)
                    {
                        return this[input.ID];
                    }
                }
                catch
                {
                }
                return input;
            }
        }
    }
}
