// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Types.ServerURIs
{
    public class ServerURIs : Dictionary<string, string>
    {
        public ServerURIs()
        {

        }

        public ServerURIs(int capacity)
        {

        }

        public ServerURIs(IDictionary<string, string> dictionary)
        {

        }

        public string IMServerURI
        {
            get
            {
                return this["IMServerURI"];
            }
        }

        public string AssetServerURI
        {
            get
            {
                return this["AssetServerURI"];
            }
        }

        public string InventoryServerURI
        {
            get
            {
                return this["InventoryServerURI"];
            }
        }

        public string ProfileServerURI
        {
            get
            {
                return this["ProfileServerURI"];
            }
        }

        public string FriendsServerURI
        {
            get
            {
                return this["FriendsServerURI"];
            }
        }

        public string GroupsServerURI
        {
            get
            {
                return this["GroupsServerURI"];
            }
        }

        public string HomeURI
        {
            get
            {
                return this["HomeURI"];
            }
        }
    }
}
