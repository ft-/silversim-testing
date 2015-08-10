// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types
{
    public sealed class ResourceUUI
    {
        public UUID ID = UUID.Zero;
        public Uri LocationURI = null;

        public ResourceUUI()
        {

        }

        public static ResourceUUI Unknown
        {
            get
            {
                return new ResourceUUI();
            }
        }
    }
}
