// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.Main.Common.HttpServer
{
    public interface IHttpAclListAccess
    {
        HttpAclHandler[] HttpAclLists { get; }
    }
}
