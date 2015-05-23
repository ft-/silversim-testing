using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.ServiceInterfaces.Account
{
    /* this is a multi-service interface, that has to be implemented by all relevant classes */
    public interface IUserAccountDeleteServiceInterface
    {
        void Remove(UUID scopeID, UUID userAccount);
    }
}
