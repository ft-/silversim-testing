// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using SilverSim.Types.IM;
using SilverSim.Types.Script;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Agent
{
    public interface IAgent : IObject, ISceneListener
    {
        string DisplayName { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        UUID SceneID { get; set; }

        ClientInfo Client { get; }
        SessionInfo Session { get; }

        List<GridType> SupportedGridTypes { get; }

        bool IMSend(GridInstantMessage im);

        RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials { get; }

        IAgentTeleportServiceInterface ActiveTeleportService
        {
            get;
            set;
        }

        int NextParcelSequenceId
        {
            get;
        }

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

        OfflineIMServiceInterface OfflineIMService
        {
            get;
        }

        void SendMessageIfRootAgent(Message m, UUID fromSceneID);
        void SendMessageAlways(Message m, UUID fromSceneID);
        void SendAlertMessage(string msg, UUID fromSceneID);
        void SendRegionNotice(UUI fromAvatar, string message, UUID fromSceneID);
        void HandleMessage(ChildAgentUpdate m);
        void HandleMessage(ChildAgentPositionUpdate m);

        RwLockedList<UUID> SelectedObjects(UUID scene);

        ulong AddNewFile(string filename, byte[] data);

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

        Quaternion BodyRotation { get; set; }

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
