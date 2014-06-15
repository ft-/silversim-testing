﻿/*

ArribaSim is distributed under the terms of the
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

namespace ArribaSim.Linden.Messages
{
    public enum MessageType : uint
    {
        High = 0x00,
        Medium = 0xFF00,
        Low = 0xFFFF0000,

        Test = Low | 1,
        PacketAck = Low | 0xFFFB,
        OpenCircuit = Low | 0xFFFC,
        CloseCircuit = Low | 0xFFFD,
        StartPingCheck = High | 1,
        CompletePingCheck = High | 2,
        AddCircuitCode = Low | 2,
        UseCircuitCode = Low | 3,
        NeighborList = High | 3,
        AvatarTextureUpdate = Low | 4,
        SimulatorMapUpdate = Low | 5,
        SimulatorSetMap = Low | 6,
        SubscribeLoad = Low | 7,
        UnsubscribeLoad = Low |  8,
        SimulatorReady = Low | 9,
        TelehubInfo = Low | 10,
        SimulatorPresentAtLocation = Low | 11,
        SimulatorLoad = Low | 12,
        RegionPresenceRequestByRegionID = Low | 14,
        RegionPresenceRequestByHandle = Low | 15,
        RegionPresenceResponse = Low | 16,
        UpdateSimulator = Low | 17,
        LogDwellTime = Low | 18,
        FeatureDisabled = Low | 19,
        LogFailedMoneyTransaction = Low | 20,
        UserReportInternal = Low | 21,
        SetSimStatusInDatabase = Low | 22,
        SetSimPresenceInDatabase = Low | 23,
        EconomyDataRequest = Low | 24,
        EconomyData = Low | 25,
        AvatarPickerRequest = Low | 26,
        AvatarPickerRequestBackend = Low | 27,
        AvatarPickerReply = Low | 28,
        PlacesQuery = Low | 29,
        PlacesReply = Low | 30,
        DirFindQuery = Low | 31,
        DirFindQueryBackend = Low | 32,
        DirPlacesQuery = Low | 33,
        DirPlacesQueryBackend = Low | 34,
        DirPlacesReply = Low | 35,
        DirPeopleReply = Low | 36,
        DirEventsReply = Low | 37,
        DirGroupsReply = Low | 38,
        DirClassifiedQuery = Low | 39,
        DirClassifiedQueryBackend = Low | 40,
        DirClassifiedReply = Low | 41,
        AvatarClassifiedReply = Low | 42,
        ClassifiedInfoRequest = Low | 43,
        ClassifiedInfoReply = Low | 44,
        ClassifiedInfoUpdate = Low | 45,
        ClassifiedDelete = Low | 46,
        ClassifiedGodDelete = Low | 47,
        DirLandQuery = Low | 48,
        DirLandQueryBackend = Low | 49,
        DirLandReply = Low | 50,
        DirPopularQuery = Low | 51,
        DirPopularQueryBackend = Low | 52,
        DirPopularReply = Low | 53,
        ParcelInfoRequest = Low | 54,
        ParcelInfoReply = Low | 55,
        ParcelObjectOwnersRequest = Low | 56,
        ParcelObjectOwnersReply = Low | 57,
        GroupNoticesListRequest = Low | 58,
        GroupNoticesListReply = Low | 59,
        GroupNoticeRequest = Low | 60,
        GroupNoticeAdd = Low | 61,
        TeleportRequest = Low | 62,
        TeleportLocationRequest = Low | 63,
        TeleportLocal = Low | 64,
        TeleportLandmarkRequest = Low | 65,
        TeleportProgress = Low | 66,
        DataHomeLocationRequest = Low | 67,
        DataHomeLocationReply = Low | 68,
        TeleportFinish = Low | 69,
        StartLure = Low | 70,
        TeleportLureRequest = Low | 71,
        TeleportCancel = Low | 72,
        TeleportStart = Low | 73,
        TeleportFailed = Low | 74,
        Undo = Low | 75,
        Redo = Low | 76,
        UndoLand = Low | 77,
        AgentPause = Low | 78,
        AgentResume = Low | 79,
        AgentUpdate = High | 4,
        ChatFromViewer = Low | 80,
        AgentThrottle = Low | 81,
        AgentFOV = Low | 82,
        AgentHeightWidth = Low | 83,
        AgentSetAppearance = Low | 84,
        AgentAnimation = High | 5,
        AgentRequestSit = High | 6,
        AgentSit = High | 7,
        AgentQuitCopy = Low | 85,
        RequestImage = High | 8,
        ImageNotInDatabase = Low | 86,
        RebakeAvatarTextures = Low | 87,
        SetAlwaysRun = Low | 88,
        ObjectAdd = Medium | 1,
        ObjectDelete = Low | 89,
        ObjectDuplicate = Low | 90,
        ObjectDuplicateOnRay = Low | 91,
        MultipleObjectUpdate = Medium | 2,
        RequestMultipleObjects = Medium | 3,
        ObjectRotation = Low | 93,
        ObjectFlagUpdate = Low | 94,
        ObjectClickAction = Low | 95,
        ObjectImage = Low | 96,
        ObjectMaterial = Low | 97,
        ObjectShape = Low | 98,
        ObjectExtraParams = Low | 99,
        ObjectOwner = Low | 100,
        ObjectGroup = Low | 101,
        ObjectBuy = Low | 102,
        BuyObjectInventory = Low | 103,
        DerezContainer = Low | 104,
        ObjectPermissions = Low | 105,
        ObjectSaleInfo = Low | 106,
        ObjectName = Low | 107,
        ObjectDescription = Low | 108,
        ObjectCategory = Low | 109,
        ObjectSelect = Low | 110,
        ObjectDeselet = Low | 111,
        ObjectAttach = Low | 112,
        ObjectDetach = Low | 113,
        ObjectDrop = Low | 114,
        ObjectLink = Low | 115,
        ObjectDelink = Low | 116,
        ObjectGrab = Low | 117,
        ObjectGrabUpdate = Low | 118,
        ObjectDeGrab = Low | 119,
        ObjectSpinStart = Low | 120,
        ObjectSpinUpdate = Low | 121,
        ObjectSpinStop = Low | 122,
        ObjectExportSelected = Low | 123,
        ModifyLand = Low | 124,
        VelocityInterpolateOn = Low | 125,
        VelocityInterpolateOff = Low | 126,
        StateSave = Low | 127,
        ReportAutosaveCrash = Low | 128,
        SimWideDeletes = Low | 129,
        RequestObjectPropertiesFamily = Medium | 5,
        TrackAgent = Low | 130,
        ViewerStats = Low | 131,
        ScriptAnswerYes = Low | 132,
        UserReport = Low | 133,
        AlertMessage = Low | 134,
        AgentAlertMessage = Low | 135,
        MeanCollisionAlert = Low | 136,
        ViewerFrozenMessage = Low | 137,
        HealthMessage = Low | 138,
        ChatFromSimulator = Low | 139,
        SimStats = Low | 140,
        RequestRegionInfo = Low | 141,
        RegionInfo = Low | 142,
        GodUpdateRegionInfo = Low | 143,
        NearestLandingRegionRequest = Low | 144,
        NearestLandingRegionReply = Low | 145,
        NearestLandingPointUpdated = Low | 146,
        TeleportLandingStatusChanged = Low | 147,
        RegionHandshake = Low | 148,
        RegionHandshakeReply = Low | 149,
        CoarseLocationUpdate = Medium | 6,
        ImageData = High | 9,
        ImagePacket = High | 10,
        LayerData = High | 11,
        ObjectUpdate = High | 12,
        ObjectUpdateCompressed = High | 13,
        ObjectUpdateCached = High | 14,
        ImprovedTerseObjectUpdate = High | 15,
        KillObject = High | 16,
        CrossedRegion = Medium | 7,
        SimulatorViewerTimeMessage = Low | 150,
        EnableSimulator = Low | 151,
        DisableSimulator = Low | 152,
        ConfirmEnableSimulator = Medium | 8,
        TransferRequest = Low | 153,
        TransferInfo = Low | 154,
        TransferPacket = High | 17,
        TransferAbort = Low | 155,
        RequestXfer = Low | 156,
        SendXferPacket = High | 18,
        ConfirmXferPacket = High | 19,
        AbortXfer = Low | 157,
        AvatarAnimation = High | 20,
        AvatarAppearance = Low | 158,
        AvatarSitResponse = High | 21,
        SetFollowCamProperties = Low | 159,
        ClearFollowCamProperties = Low | 160,
        CameraConstraint = High | 22,
        ObjectProperties = Medium | 9,
        ObjectPropertiesFamily = Medium | 10,
        RequestPayPrice = Low | 161,
        PayPriceReply = Low | 162,
        KickUser = Low | 163,
        KickUserAck = Low | 164,
        GodKickUser = Low | 165,
        SystemKickUser = Low | 166,
        EjectUser = Low | 167,
        FreezeUser = Low | 168,
        AvatarPropertiesRequest = Low | 169,
        AvatarPropertiesRequestBackend = Low | 170,
        AvatarPropertiesReply = Low | 171,
        AvatarInterestsReply = Low | 172,
        AvatarGroupsReply = Low | 173,
        AvatarPropertiesUpdate = Low | 174,
        AvatarInterestsUpdate = Low | 175,
        AvatarNotesReply = Low | 176,
        AvatarNotesUpdate = Low | 177,
        AvatarPicksReply = Low | 178,
        EventInfoRequest = Low | 179,
        EventInfoReply = Low | 180,
        EventNotificationAddRequest = Low | 181,
        EventNotificationRemoveRequest = Low | 182,
        EventGodDelete = Low | 183,
        PickInfoReply = Low | 184,
        PickInfoUpdate = Low | 185,
        PickDelete = Low | 186,
        PickGodDelete = Low | 187,
        ScriptQuestion = Low | 188,
        ScriptControlChange = Low | 189,
        ScriptDialog = Low | 190,
        ScriptDialogReply = Low | 191,
        ForceScriptControlRelease = Low | 192,
        RevokePermissions = Low | 193,
        LoadURL = Low | 194,
        ScriptTeleportRequest = Low | 195,
        ParcelOverlay = Low | 196,
        ParcelPropertiesRequest = Medium | 11,
        ParcelPropertiesRequestByID = Low | 197,
        ParcelProperties = High | 23,
        ParcelPropertiesUpdate = Low | 198,
        ParcelReturnObjects = Low | 199,
        ParcelSetOtherCleanTime = Low | 200,
        ParcelDisableObjects = Low | 201,
        ParcelSelectObjects = Low | 202,
        EstateCovenantRequest = Low | 203,
        EstateCovenantReply = Low | 204,
        ForceObjectSelect = Low | 205,
        ParcelBuyPass = Low | 206,
        ParcelDeedToGroup = Low | 207,
        ParcelReclaim = Low | 208,
        ParcelClaim = Low | 209,
        ParcelJoin = Low | 210,
        ParcelDivide = Low | 211,
        ParcelRelease = Low | 212,
        ParcelBuy = Low | 213,
        ParcelGodForceOwner = Low | 214,
        ParcelAccessListRequest = Low | 215,
        ParcelAccessListReply = Low | 216,
        ParcelAccessListUpdate = Low | 217,
        ParcelDwellRequest = Low | 218,
        ParcelDwellReply = Low | 219,
        RequestParcelTransfer = Low | 220,
        UpdateParcel = Low | 221,
        RemoveParcel = Low | 222,
        MergeParcel = Low | 223,
        LogParcelChanges = Low | 224,
        CheckParcelSales = Low | 225,
        ParcelSales = Low | 226,
        ParcelGodMarkAsContent = Low | 227,
        ViewerStartAuction = Low | 228,
        StartAuction = Low | 229,
        ConfirmAuctionStart = Low | 230,
        CompleteAuction = Low | 231,
        CancelAuction = Low | 232,
        CheckParcelAuctions = Low | 233,
        ParcelAuctions = Low | 234,
        UUIDNameRequest = Low | 235,
        UUIDNameReply = Low | 236,
        UUIDGroupNameRequest = Low | 237,
        UUIDGroupNameBlock = Low | 238,
        ChatPass = Low | 239,
        EdgeDataPacket = High | 24,
        SimStatus = Medium | 12,
        ChildAgentUpdate = High | 25,
        ChildAgentAlive = High | 26,
        ChildAgentPositionUpdate = High | 27,
        ChildAgentDying = Low | 240,
        ChildAgentUnknown = Low | 241,
        AtomicPassObject = High | 28,
        KillChildAgents = Low | 242,
        GetScriptRunning = Low | 243,
        ScriptRunningReply = Low | 244,
        SetScriptRunning = Low | 245,
        ScriptReset = Low | 246,
        ScriptSensorRepeat = Low | 247,
        ScriptSensorReply = Low | 248,
        CompleteAgentMovement = Low | 249,
        AgentMovementComplete = Low | 250,
        DataserverLogout = Low | 251,
        LogoutRequest = Low | 252,
        LogoutReply = Low | 253,
        ImprovedInstantMessage = Low | 254,
        RetrieveInstantMessages = Low | 255,
        FindAgent = Low | 256,
        RequestGodlikePowers = Low | 257,
        GrantGodlikePowers = Low | 258,
        GodlikeMessage = Low | 259,
        EstateOwnerMessage = Low | 260,
        GenericMessage = Low | 261,
        MuteListRequest = Low | 262,
        UpdateMuteListEntry = Low | 263,
        RemoveMuteListEntry = Low | 264,
        CopyInventoryFromNotecard = Low | 265,
        UpdateInventoryItem = Low | 266,
        UpdateCreateInventoryItem = Low | 267,
        MoveInventoryItem = Low | 268,
        CopyInventoryItem = Low | 269,
        RemoveInventoryItem = Low | 270,
        ChangeInventoryItemFlags = Low | 271,
        SaveAssetIntoInventory = Low | 272,
        CreateInventoryFolder = Low | 273,
        UpdateInventoryFolder = Low | 274,
        MoveInventoryFolder = Low | 275,
        RemoveInventoryFolder = Low | 276,
        FetchInventoryDescendents = Low | 277,
        InventoryDescendents = Low | 278,
        FetchInventory = Low | 279,
        FetchInventoryReply = Low | 280,
        BulkUpdateInventory = Low | 281,
        RequestInventoryAsset = Low | 282,
        InventoryAssetResponse = Low | 283,
        RemoveInventoryObjects = Low | 284,
        PurgeInventoryDescendents = Low | 285,
        UpdateTaskInventory = Low | 286,
        RemoveTaskInventory = Low | 287,
        MoveTaskInventory = Low | 288,
        RequestTaskInventory = Low | 289,
        ReplyTaskInventory = Low | 290,
        DeRezObject = Low | 291,
        DeRezAck = Low | 292,
        RezObject = Low | 293,
        RezObjectFromNotecard = Low | 294,
        TransferInventory = Low | 295,
        TransferInventoryAck = Low | 296,
        AcceptFriendship = Low | 297,
        DeclineFriendship = Low | 298,
        FormFriendship = Low | 299,
        TerminateFriendship = Low | 300,
        OfferCallingCard = Low | 301,
        AcceptCallingCard = Low | 302,
        DeclineCallingCard = Low | 303,
        RezScript = Low | 304,
        CreateInventoryItem = Low | 305,
        CreateLandmarkForEvent = Low | 306,
        EventLocationRequest = Low | 307,
        EventLocationReply = Low | 308,
        RegionHandleRequest = Low | 309,
        RegionIDAndHandleReply = Low | 310,
        MoneyTransferRequest = Low | 311,
        MoneyTransferBackend = Low | 312,
        MoneyBalanceRequest = Low | 313,
        MoneyBalanceReply = Low | 314,
        RoutedMoneyBalanceReply = Low | 315,
        ActivateGestures = Low | 316,
        DeactivateGestures = Low | 317,
        MuteListUpdate = Low | 318,
        UseCachedMuteList = Low | 319,
        GrantUserRights = Low | 320,
        ChangeUserRights = Low | 321,
        OnlineNotification = Low | 322,
        OfflineNotification = Low | 323,
        SetStartLocationRequest = Low | 324,
        SetStartLocation = Low | 325,
        NetTest = Low | 326,
        SetCPURatio = Low | 327,
        SimCrashed = Low | 328,
        NameValuePair = Low | 329,
        RemoveNameValuePair = Low | 330,
        UpdateAttachment = Low | 331,
        RemoveAttachment = Low | 332,
        SoundTrigger = High | 29,
        AttachedSound = Medium | 13,
        AttachedSoundGainChange = Medium | 14,
        PreloadSound = Medium | 15,
        AssetUploadRequest = Low | 333,
        AssetUploadComplete = Low | 334,
        EmailMessageRequest = Low | 335,
        EmailMessageReply = Low | 336,
        InternalScriptMail = Medium | 16,
        ScriptDataRequest = Low | 337,
        ScriptDataReply = Low | 338,
        CreateGroupRequest = Low | 339,
        CreateGroupReply = Low | 340,
        UpdateGroupInfo = Low | 341,
        GroupRoleChanges = Low | 342,
        JoinGroupRequest = Low | 343,
        JoinGroupReply = Low | 344,
        EjectGroupMemberRequest = Low | 345,
        EjectGroupMemberReply = Low | 346,
        LeaveGroupRequest = Low | 347,
        LeaveGroupReply = Low | 348,
        InviteGroupRequest = Low | 349,
        InviteGroupResponse = Low | 350,
        GroupProfileRequest = Low | 351,
        GroupProfileReply = Low | 352,
        GroupAccountSummaryRequest = Low | 353,
        GroupAccountSummaryReply = Low | 354,
        GroupAccountDetailsRequest = Low | 355,
        GroupAccountDetailsReply = Low | 356,
        GroupAccountTransactionsRequest = Low | 357,
        GroupAccountTransactionsReply = Low | 358,
        GroupActiveProposalsRequest = Low | 359,
        GroupActiveProposalItemReply = Low | 360,
        GroupVoteHistoryRequest = Low | 361,
        GroupVoteHistoryItemReply = Low | 362,
        StartGroupProposal = Low | 363,
        GroupProposalBallot = Low | 364,
        TallyVotes = Low | 365,
        GroupMembersRequest = Low | 366,
        GroupMembersReply = Low | 367,
        ActivateGroup = Low | 368,
        SetGroupContribution = Low | 369,
        SetGroupAcceptNotices = Low | 370,
        GroupRoleDataRequest = Low | 371,
        GroupRoleDataReply = Low | 372,
        GroupRoleMembersRequest = Low | 373,
        GroupRoleMembersReply = Low | 374,
        GroupTitlesRequest = Low | 375,
        GroupTitlesReply = Low | 376,
        GroupTitleUpdate = Low | 377,
        GroupRoleUpdate = Low | 378,
        LiveHelpGroupRequest = Low | 379,
        LiveHelpGroupReply = Low | 380,
        AgentWearablesRequest = Low | 381,
        AgentWearablesUpdate = Low | 382,
        AgentIsNowWearing = Low | 383,
        AgentCachedTexture = Low | 384,
        AgentCachedTextureResponse = Low | 385,
        AgentDataUpdateRequest = Low | 386,
        AgentDataUpdate = Low | 387,
        GroupDataUpdate = Low | 388,
        AgentGroupDataUpdate = Low | 389,
        AgentDropGroup = Low | 390,
        LogTextMessage = Low | 391,
        ViewerEffect = Medium | 17,
        CreateTrustedCircuit = Low | 392,
        DenyTrustedCircuit = Low | 393,
        RequestTrustedCircuit = Low | 394,
        RezSingleAttachmentFromInv = Low | 395,
        RezMultipleAttachmentFromInv = Low | 396,
        DetachAttachmentIntoInv = Low | 397,
        CreateNewOutfitAttachments = Low | 398,
        UserInfoRequest = Low | 399,
        UserInfoReply = Low | 400,
        UpdateUserInfo = Low | 401,
        ParcelRename = Low | 402,
        InitiateDownload = Low | 403,
        SystemMessage = Low | 404,
        MapLayerRequest = Low | 405,
        MapLayerReply = Low | 406,
        MapBlockRequest = Low | 407,
        MapNameRequest = Low | 408,
        MapBlockReply = Low | 409,
        MapItemRequest = Low | 410,
        MapItemReply = Low | 411,
        SendPostcard = Low | 412,
        RpcChannelRequest = Low | 413,
        RpcChannelReply = Low | 414,
        RpcScriptRequestInbound = Low | 415,
        RpcScriptRequestInboundForward = Low | 416,
        RpcScriptReplyInbound = Low | 417,
        ScriptMailRegistration = Low | 418,
        ParcelMediaCommandMessage = Low | 419,
        ParcelMediaUpdate = Low | 420,
        LandStatRequest = Low | 421,
        LandStatReply = Low | 422,
        Error = Low | 423,
        ObjectIncludeInSearch = Low | 424,
        RezRestoreToWorld = Low | 425,
        LinkInventoryItem = Low | 426
    }
}
