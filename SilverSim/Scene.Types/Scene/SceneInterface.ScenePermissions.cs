using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public bool IsEstateManager(UUI agent)
        {
            if(agent.ID == Owner.ID && agent.HomeURI == Owner.HomeURI)
            {
                return true;
            }

            return false;
        }

        public bool IsPossibleGod(UUI agent)
        {
            if (agent.ID == Owner.ID && agent.HomeURI == Owner.HomeURI)
            {
                return true;
            }

            if (ServerParamService.GetBoolean(ID, "estate_manager_is_god", false) && IsEstateManager(agent))
            {
                return true;
            }

            return false;
        }

        public bool IsSimConsoleAllowed(UUI agent)
        {
            if(IsPossibleGod(agent))
            {
                return true;
            }

            if (ServerParamService.GetBoolean(ID, "estate_manager_is_simconsole_user", false) && IsEstateManager(agent))
            {
                return true;
            }

            return false;
        }
    }
}
