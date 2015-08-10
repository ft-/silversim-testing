// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Types.Groups
{
    [Flags] public enum GroupPowers : ulong
    {
        None = 0,

        // Membership
        /// <summary>Can send invitations to groups default role</summary>
        Invite = 1UL << 1,
        /// <summary>Can eject members from group</summary>
        Eject = 1UL << 2,
        /// <summary>Can toggle 'Open Enrollment' and change 'Signup fee'</summary>
        ChangeOptions = 1UL << 3,
        /// <summary>Member is visible in the public member list</summary>
        MemberVisible = 1UL << 47,

        // Roles
        /// <summary>Can create new roles</summary>
        CreateRole = 1UL << 4,
        /// <summary>Can delete existing roles</summary>
        DeleteRole = 1UL << 5,
        /// <summary>Can change Role names, titles and descriptions</summary>
        RoleProperties = 1UL << 6,
        /// <summary>Can assign other members to assigners role</summary>
        AssignMemberLimited = 1UL << 7,
        /// <summary>Can assign other members to any role</summary>
        AssignMember = 1UL << 8,
        /// <summary>Can remove members from roles</summary>
        RemoveMember = 1UL << 9,
        /// <summary>Can assign and remove abilities in roles</summary>
        ChangeActions = 1UL << 10,

        // Identity
        /// <summary>Can change group Charter, Insignia, 'Publish on the web' and which
        /// members are publicly visible in group member listings</summary>
        ChangeIdentity = 1UL << 11,

        // Parcel management
        /// <summary>Can buy land or deed land to group</summary>
        LandDeed = 1UL << 12,
        /// <summary>Can abandon group owned land to Governor Linden on mainland, or Estate owner for
        /// private estates</summary>
        LandRelease = 1UL << 13,
        /// <summary>Can set land for-sale information on group owned parcels</summary>
        LandSetSale = 1UL << 14,
        /// <summary>Can subdivide and join parcels</summary>
        LandDivideJoin = 1UL << 15,


        // Chat
        /// <summary>Can join group chat sessions</summary>
        JoinChat = 1UL << 16,
        /// <summary>Can use voice chat in Group Chat sessions</summary>
        AllowVoiceChat = 1UL << 27,
        /// <summary>Can moderate group chat sessions</summary>
        ModerateChat = 1UL << 37,

        // Parcel identity
        /// <summary>Can toggle "Show in Find Places" and set search category</summary>
        FindPlaces = 1UL << 17,
        /// <summary>Can change parcel name, description, and 'Publish on web' settings</summary>
        LandChangeIdentity = 1UL << 18,
        /// <summary>Can set the landing point and teleport routing on group land</summary>
        SetLandingPoint = 1UL << 19,

        // Parcel settings
        /// <summary>Can change music and media settings</summary>
        ChangeMedia = 1UL << 20,
        /// <summary>Can toggle 'Edit Terrain' option in Land settings</summary>
        LandEdit = 1UL << 21,
        /// <summary>Can toggle various About Land > Options settings</summary>
        LandOptions = 1UL << 22,

        // Parcel powers
        /// <summary>Can always terraform land, even if parcel settings have it turned off</summary>
        AllowEditLand = 1UL << 23,
        /// <summary>Can always fly while over group owned land</summary>
        AllowFly = 1UL << 24,
        /// <summary>Can always rez objects on group owned land</summary>
        AllowRez = 1UL << 25,
        /// <summary>Can always create landmarks for group owned parcels</summary>
        AllowLandmark = 1UL << 26,
        /// <summary>Can set home location on any group owned parcel</summary>
        AllowSetHome = 1UL << 28,


        // Parcel access
        /// <summary>Can modify public access settings for group owned parcels</summary>
        LandManageAllowed = 1UL << 29,
        /// <summary>Can manager parcel ban lists on group owned land</summary>
        LandManageBanned = 1UL << 30,
        /// <summary>Can manage pass list sales information</summary>
        LandManagePasses = 1UL << 31,
        /// <summary>Can eject and freeze other avatars on group owned land</summary>
        LandEjectAndFreeze = 1UL << 32,

        // Parcel content
        /// <summary>Can return objects set to group</summary>
        ReturnGroupSet = 1UL << 33,
        /// <summary>Can return non-group owned/set objects</summary>
        ReturnNonGroup = 1UL << 34,
        /// <summary>Can return group owned objects</summary>
        ReturnGroupOwned = 1UL << 48,

        /// <summary>Can landscape using Linden plants</summary>
        LandGardening = 1UL << 35,

        // Objects
        /// <summary>Can deed objects to group</summary>
        DeedObject = 1UL << 36,
        /// <summary>Can move group owned objects</summary>
        ObjectManipulate = 1UL << 38,
        /// <summary>Can set group owned objects for-sale</summary>
        ObjectSetForSale = 1UL << 39,

        /// <summary>Pay group liabilities and receive group dividends</summary>
        Accountable = 1UL << 40,

        /// <summary>List and Host group events</summary>
        HostEvent = 1UL << 41,

        // Notices and proposals
        /// <summary>Can send group notices</summary>
        SendNotices = 1UL << 42,
        /// <summary>Can receive group notices</summary>
        ReceiveNotices = 1UL << 43,
        /// <summary>Can create group proposals</summary>
        StartProposal = 1UL << 44,
        /// <summary>Can vote on group proposals</summary>
        VoteOnProposal = 1UL << 45,

        /// <summary>Default powers for Everyone</summary>
        DefaultEveryonePowers = AllowSetHome | Accountable | JoinChat | AllowVoiceChat | ReceiveNotices | StartProposal | VoteOnProposal,
        /// <summary>Default powers for Owner</summary>
        OwnerPowers = 
                    Accountable |
                    AllowEditLand |
                    AllowFly |
                    AllowLandmark |
                    AllowRez |
                    AllowSetHome |
                    AllowVoiceChat |
                    AssignMember |
                    AssignMemberLimited |
                    ChangeActions | 
                    ChangeIdentity |
                    ChangeMedia |
                    ChangeOptions |
                    CreateRole |
                    DeedObject |
                    DeleteRole |
                    Eject |
                    FindPlaces |
                    Invite |
                    JoinChat |
                    LandChangeIdentity |
                    LandDeed |
                    LandDivideJoin |
                    LandEdit |
                    LandEjectAndFreeze |
                    LandGardening |
                    LandManageAllowed |
                    LandManageBanned |
                    LandManagePasses |
                    LandOptions |
                    LandRelease |
                    LandSetSale |
                    ModerateChat |
                    ObjectManipulate |
                    ObjectSetForSale |
                    ReceiveNotices |
                    RemoveMember |
                    ReturnGroupOwned |
                    ReturnGroupSet |
                    ReturnNonGroup |
                    RoleProperties |
                    SendNotices |
                    SetLandingPoint |
                    StartProposal |
                    VoteOnProposal
    }
}
