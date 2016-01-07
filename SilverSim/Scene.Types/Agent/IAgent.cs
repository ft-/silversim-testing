// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
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
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Agent;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

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
        UserAccount UntrustedAccountInfo { get; }
        CultureInfo CurrentCulture { get; }

        List<GridType> SupportedGridTypes { get; }

        Dictionary<UUID, AgentChildInfo> ActiveChilds { get; }

        bool IMSend(GridInstantMessage im);

        RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials { get; }

        void EnableSimulator(UUID originSceneID, uint circuitCode, string capsURI, DestinationInfo destinationInfo);
        void SendUpdatedParcelInfo(ParcelInfo pinfo, UUID fromSceneID);

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

        Vector3 CameraPosition
        {
            get;
            set;
        }

        Quaternion CameraRotation
        {
            get;
            set;
        }

        Vector3 CameraAtAxis
        {
            get;
            set;
        }

        Vector3 CameraLeftAxis
        {
            get;
            set;
        }

        Vector3 CameraUpAxis
        {
            get;
            set;
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

        [Description("Health in %")]
        double Health
        {
            get;
            set;
        }

        void IncreaseHealth(double v);
        void DecreaseHealth(double v);

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
        bool UnSit();

        RwLockedList<UUID> SelectedObjects(UUID scene);

        ulong AddNewFile(string filename, byte[] data);

        bool IsActiveGod
        {
            get;
        }

        bool IsNpc
        {
            get;
        }

        bool IsInMouselook
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
        string GetDefaultAnimation(); /* locomotion */
        List<UUID> GetPlayingAnimations();

        ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions);
        ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions, UUID experienceID);
        void RevokePermissions(UUID sourceID, UUID itemID, ScriptPermissions permissions);

        void TakeControls(ScriptInstance instance, int controls, int accept, int pass_on);
        void ReleaseControls(ScriptInstance instance);

        string AgentLanguage { get; }

        /* following five functions return true if they accept a teleport request or if they want to distribute more specific error messages except region not found */
        bool TeleportTo(SceneInterface sceneInterface, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        bool TeleportTo(SceneInterface sceneInterface, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        bool TeleportTo(SceneInterface sceneInterface, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags);
        bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags);

        /* following function returns true if it accepts a teleport request or if it wants to distribute more specific error message except home location not available */
        bool TeleportHome(SceneInterface sceneInterface);

        void KickUser(string msg);
        void KickUser(string msg, Action<bool> callbackDelegate);
    }
}
