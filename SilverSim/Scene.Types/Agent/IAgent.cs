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

using SilverSim.LL.Messages;
using SilverSim.Scene.Types.Object;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.IM;
using SilverSim.Types.Script;
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

        RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<int, int>> TransmittedTerrainSerials { get; }

        int LastMeasuredLatencyTickCount /* info from Circuit ping measurement */
        {
            get;
            set;
        }

        Vector4 CollisionPlane
        {
            get;
            set;
        }

        AgentAttachments Attachments
        {
            get;
        }

        AgentWearables Wearables
        {
            get;
            set; /* must not replace data and not the internal reference */
        }

        AppearanceInfo Appearance
        {
            get;
            set; /* must not replace data and not the internal reference */
        }

        byte[] VisualParams
        {
            get;
            set;
        }

        IObject SittingOnObject
        {
            get;
            set;
        }

        AppearanceInfo.AvatarTextureData Textures
        {
            get;
            set; /* must not replace data and not the internal reference */
        }

        AppearanceInfo.AvatarTextureData TextureHashes
        {
            get;
            set; /* must not replace data and not the internal reference */
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

        EconomyServiceInterface EconomyService
        {
            get;
        }

        void HandleAgentMessage(Message m);

        void SendMessageIfRootAgent(Message m, UUID fromSceneID);
        void SendMessageAlways(Message m, UUID fromSceneID);
        void SendAlertMessage(string msg, UUID fromSceneID);

        UGI Group { get; set; }

        bool IsActiveGod
        {
            get;
        }

        Vector3 LookAt
        {
            get;
            set;
        }

        void ResetAnimationOverride(string anim_state);
        void SetAnimationOverride(string anim_state, UUID anim);
        string GetAnimationOverride(string anim_state);
        void PlayAnimation(UUID anim, UUID objectid);
        void StopAnimation(UUID anim, UUID objectid);

        ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions);
        ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions, UUID experienceID);
        void RevokePermissions(UUID sourceID, UUID itemID, ScriptPermissions permissions);
    }
}
