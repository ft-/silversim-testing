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

namespace SilverSim.Types.IM
{
    public enum GridInstantMessageDialog : sbyte
    {
        MessageFromAgent = 0,
        MessageBox = 1,
        GroupInvitation = 3,
        InventoryOffered = 4,
        InventoryAccepted = 5,
        InventoryDeclined = 6,
        GroupVote = 7,
        TaskInventoryOffered = 9,
        TaskInventoryAccepted = 10,
        TaskInventoryDeclined = 11,
        NewUserDefault = 12,
        SessionAdd = 13,
        SessionOfflineAdd = 14,
        SessionGroupStart = 15,
        SessionCardlessStart = 16,
        SessionSend = 17,
        SessionDrop = 18,
        MessageFromObject = 19,
        BusyAutoResponse = 20,
        ConsoleAndChatHistory = 21,
        RequestTeleport = 22,
        AcceptTeleport = 23,
        DenyTeleport = 24,
        GodLikeRequestTeleport = 25,
        RequestLure = 26,
        GotoUrl = 28,
        Session911Start = 29,
        Lure911 = 30,
        FromTaskAsAlert = 31,
        GroupNotice = 32,
        GroupNoticeInventoryAccepted = 33,
        GroupNoticeInventoryDeclined = 34,
        GroupInvitationAccept = 35,
        GroupInvitationDecline = 36,
        GroupNoticeRequested = 37,
        FriendshipOffered = 38,
        FriendshipAccepted = 39,
        FriendshipDeclined = 40,
        StartTyping = 41,
        StopTyping = 42
    }
}
