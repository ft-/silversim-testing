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

using System;

namespace SilverSim.Types.Groups
{
    [Flags] public enum GroupPowers : ulong
    {
        None = 0,
        Invite = 2,
        Eject = 4,
        ChangeOptions = 8,
        CreateRole = 16,
        DeleteRole = 32,
        RoleProperties = 64,
        AssignMemberLimited = 128,
        AssignMember = 256,
        RemoveMember = 512,
        ChangeActions = 1024,
        ChangeIdentity = 2048,
        LandDeed = 4096,
        LandRelease = 8192,
        LandSetSale = 16384,
        LandDivideJoin = 32768,
        JoinChat = 65536,
        FindPlaces = 131072,
        LandChangeIdentity = 262144,
        SetLandingPoint = 524288,
        ChangeMedia = 1048576,
        LandEdit = 2097152,
        LandOptions = 4194304,
        AllowEditLand = 8388608,
        AllowFly = 16777216,
        AllowRez = 33554432,
        AllowLandmark = 67108864,
        AllowVoiceChat = 134217728,
        AllowSetHome = 268435456,
        LandManageAllowed = 536870912,
        LandManageBanned = 1073741824,
        LandManagePasses = 2147483648,
        LandEjectAndFreeze = 4294967296,
        ReturnGroupSet = 8589934592,
        ReturnNonGroup = 17179869184,
        LandGardening = 34359738368,
        DeedObject = 68719476736,
        ModerateChat = 137438953472,
        ObjectManipulate = 274877906944,
        ObjectSetForSale = 549755813888,
        Accountable = 1099511627776,
        HostEvent = 2199023255552,
        SendNotices = 4398046511104,
        ReceiveNotices = 8796093022208,
        StartProposal = 17592186044416,
        VoteOnProposal = 35184372088832,
        MemberVisible = 140737488355328,
        ReturnGroupOwned = 281474976710656,

        DefaultEveryonePowers = AllowSetHome | Accountable | JoinChat | AllowVoiceChat | ReceiveNotices | StartProposal | VoteOnProposal,
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
