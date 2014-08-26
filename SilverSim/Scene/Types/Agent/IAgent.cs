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

using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.IM;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Agent
{
    public interface IAgent : IObject
    {
        string DisplayName { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        UUID SceneID { get; set; }

        bool IMSend(GridInstantMessage im);

        int LastMeasuredLatencyTickCount /* info from Circuit ping measurement */
        {
            get;
            set;
        }

        AgentAttachments Attachments
        {
            get;
        }

        AssetServiceInterface AssetService
        {
            get;
        }

        InventoryServiceInterface InventoryService
        {
            get;
        }

        GroupsServiceInterface GroupsService
        {
            get;
        }

        ProfileServiceInterface ProfileService
        {
            get;
        }

        FriendsServiceInterface FriendsService
        {
            get;
        }

        UserAgentServiceInterface UserAgentService
        {
            get;
        }

        PresenceServiceInterface PresenceService
        {
            get;
        }

        GridUserServiceInterface GridUserService
        {
            get;
        }

        GridServiceInterface GridService
        {
            get;
        }
    }
}
