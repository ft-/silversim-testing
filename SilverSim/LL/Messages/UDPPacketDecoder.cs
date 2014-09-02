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

using System.Collections.Generic;

namespace SilverSim.LL.Messages
{
    public class UDPPacketDecoder
    {
        public delegate Message PacketDecoderDelegate(UDPPacket p);
        public readonly Dictionary<MessageType, PacketDecoderDelegate> PacketTypes = new Dictionary<MessageType,PacketDecoderDelegate>();

        public UDPPacketDecoder()
        {
            /* Agent */
            PacketTypes.Add(MessageType.TrackAgent, Agent.TrackAgent.Decode);
            PacketTypes.Add(MessageType.AgentUpdate, Agent.AgentUpdate.Decode);
            PacketTypes.Add(MessageType.AgentDataUpdateRequest, Agent.AgentDataUpdateRequest.Decode);

            /* Appearance */
            PacketTypes.Add(MessageType.AgentWearablesRequest, Appearance.AgentWearablesRequest.Decode);
            PacketTypes.Add(MessageType.AgentIsNowWearing, Appearance.AgentIsNowWearing.Decode);
            PacketTypes.Add(MessageType.AgentCachedTexture, Appearance.AgentCachedTexture.Decode);
            PacketTypes.Add(MessageType.ViewerEffect, Appearance.ViewerEffect.Decode);

            /* God */
            PacketTypes.Add(MessageType.RequestGodlikePowers, God.RequestGodlikePowers.Decode);

            /* Circuit */
            PacketTypes.Add(MessageType.CompleteAgentMovement, Circuit.CompleteAgentMovement.Decode);
            PacketTypes.Add(MessageType.LogoutRequest, Circuit.LogoutRequest.Decode);
            PacketTypes.Add(MessageType.ConfirmEnableSimulator, Circuit.ConfirmEnableSimulator.Decode);

            /* Script */
            PacketTypes.Add(MessageType.ScriptDialogReply, Script.ScriptDialogReply.Decode);
            PacketTypes.Add(MessageType.ForceScriptControlRelease, Script.ForceScriptControlRelease.Decode);
            PacketTypes.Add(MessageType.RevokePermissions, Script.RevokePermissions.Decode);
            PacketTypes.Add(MessageType.GetScriptRunning, Script.GetScriptRunning.Decode);
            PacketTypes.Add(MessageType.SetScriptRunning, Script.SetScriptRunning.Decode);
            PacketTypes.Add(MessageType.ScriptReset, Script.ScriptReset.Decode);
            PacketTypes.Add(MessageType.RezScript, Script.RezScript.Decode);
            
            /* Parcel */
            PacketTypes.Add(MessageType.ParcelPropertiesRequest, Parcel.ParcelPropertiesRequest.Decode);
            PacketTypes.Add(MessageType.ParcelPropertiesRequestByID, Parcel.ParcelPropertiesRequestByID.Decode);
            PacketTypes.Add(MessageType.ParcelPropertiesUpdate, Parcel.ParcelPropertiesUpdate.Decode);
            PacketTypes.Add(MessageType.ParcelReturnObjects, Parcel.ParcelReturnObjects.Decode);
            PacketTypes.Add(MessageType.ParcelSetOtherCleanTime, Parcel.ParcelSetOtherCleanTime.Decode);
            PacketTypes.Add(MessageType.ParcelSelectObjects, Parcel.ParcelSelectObjects.Decode);
            PacketTypes.Add(MessageType.ParcelBuyPass, Parcel.ParcelBuyPass.Decode);
            PacketTypes.Add(MessageType.ParcelDeedToGroup, Parcel.ParcelDeedToGroup.Decode);
            PacketTypes.Add(MessageType.ParcelReclaim, Parcel.ParcelReclaim.Decode);
            PacketTypes.Add(MessageType.ParcelClaim, Parcel.ParcelClaim.Decode);
            PacketTypes.Add(MessageType.ParcelJoin, Parcel.ParcelJoin.Decode);
            PacketTypes.Add(MessageType.ParcelDivide, Parcel.ParcelDivide.Decode);
            PacketTypes.Add(MessageType.ParcelRelease, Parcel.ParcelRelease.Decode);
            PacketTypes.Add(MessageType.ParcelBuy, Parcel.ParcelBuy.Decode);
            PacketTypes.Add(MessageType.ParcelGodForceOwner, Parcel.ParcelGodForceOwner.Decode);
            PacketTypes.Add(MessageType.ParcelAccessListRequest, Parcel.ParcelAccessListRequest.Decode);
            PacketTypes.Add(MessageType.ParcelAccessListUpdate, Parcel.ParcelAccessListUpdate.Decode);
            PacketTypes.Add(MessageType.ParcelDwellRequest, Parcel.ParcelDwellRequest.Decode);
            PacketTypes.Add(MessageType.ViewerStartAuction, Parcel.ViewerStartAuction.Decode);
            PacketTypes.Add(MessageType.CancelAuction, Parcel.CancelAuction.Decode);

            /* Land */
            PacketTypes.Add(MessageType.ModifyLand, Land.ModifyLand.Decode);
            PacketTypes.Add(MessageType.LandStatRequest, Land.LandStatRequest.Decode);

            /* Inventory */
            PacketTypes.Add(MessageType.CopyInventoryFromNotecard, Inventory.CopyInventoryFromNotecard.Decode);
            PacketTypes.Add(MessageType.UpdateInventoryItem, Inventory.UpdateInventoryItem.Decode);
            PacketTypes.Add(MessageType.MoveInventoryItem, Inventory.MoveInventoryItem.Decode);
            PacketTypes.Add(MessageType.CopyInventoryItem, Inventory.CopyInventoryItem.Decode);
            PacketTypes.Add(MessageType.RemoveInventoryItem, Inventory.RemoveInventoryItem.Decode);
            PacketTypes.Add(MessageType.ChangeInventoryItemFlags, Inventory.ChangeInventoryItemFlags.Decode);
            PacketTypes.Add(MessageType.CreateInventoryFolder, Inventory.CreateInventoryFolder.Decode);
            PacketTypes.Add(MessageType.UpdateInventoryFolder, Inventory.UpdateInventoryFolder.Decode);
            PacketTypes.Add(MessageType.MoveInventoryFolder, Inventory.MoveInventoryFolder.Decode);
            PacketTypes.Add(MessageType.RemoveInventoryFolder, Inventory.RemoveInventoryFolder.Decode);
            PacketTypes.Add(MessageType.FetchInventoryDescendents, Inventory.FetchInventoryDescendents.Decode);
            PacketTypes.Add(MessageType.FetchInventory, Inventory.FetchInventory.Decode);
            PacketTypes.Add(MessageType.RemoveInventoryObjects, Inventory.RemoveInventoryObjects.Decode);
            PacketTypes.Add(MessageType.LinkInventoryItem, Inventory.LinkInventoryItem.Decode);

            /* Objects */
            PacketTypes.Add(MessageType.ObjectRotation, Object.ObjectRotation.Decode);
            PacketTypes.Add(MessageType.ObjectFlagUpdate, Object.ObjectFlagUpdate.Decode);
            PacketTypes.Add(MessageType.ObjectClickAction, Object.ObjectClickAction.Decode);
            PacketTypes.Add(MessageType.ObjectMaterial, Object.ObjectMaterial.Decode);
            PacketTypes.Add(MessageType.ObjectShape, Object.ObjectShape.Decode);
            PacketTypes.Add(MessageType.ObjectExtraParams, Object.ObjectExtraParams.Decode);
            PacketTypes.Add(MessageType.ObjectOwner, Object.ObjectOwner.Decode);
            PacketTypes.Add(MessageType.ObjectGroup, Object.ObjectGroup.Decode);
            PacketTypes.Add(MessageType.ObjectBuy, Object.ObjectBuy.Decode);
            PacketTypes.Add(MessageType.BuyObjectInventory, Object.BuyObjectInventory.Decode);
            PacketTypes.Add(MessageType.ObjectPermissions, Object.ObjectPermissions.Decode);
            PacketTypes.Add(MessageType.ObjectSaleInfo, Object.ObjectSaleInfo.Decode);
            PacketTypes.Add(MessageType.ObjectName, Object.ObjectName.Decode);
            PacketTypes.Add(MessageType.ObjectCategory, Object.ObjectCategory.Decode);
            PacketTypes.Add(MessageType.ObjectSelect, Object.ObjectSelect.Decode);
            PacketTypes.Add(MessageType.ObjectDeselect, Object.ObjectDeselect.Decode);
            PacketTypes.Add(MessageType.ObjectAttach, Object.ObjectAttach.Decode);
            PacketTypes.Add(MessageType.ObjectDetach, Object.ObjectDetach.Decode);
            PacketTypes.Add(MessageType.ObjectDrop, Object.ObjectDrop.Decode);
            PacketTypes.Add(MessageType.ObjectLink, Object.ObjectLink.Decode);
            PacketTypes.Add(MessageType.ObjectDelink, Object.ObjectDelink.Decode);
            PacketTypes.Add(MessageType.ObjectGrab, Object.ObjectGrab.Decode);
            PacketTypes.Add(MessageType.ObjectGrabUpdate, Object.ObjectGroup.Decode);
            PacketTypes.Add(MessageType.ObjectSpinStart, Object.ObjectSpinStart.Decode);
            PacketTypes.Add(MessageType.ObjectSpinUpdate, Object.ObjectSpinUpdate.Decode);
            PacketTypes.Add(MessageType.ObjectSpinStop, Object.ObjectSpinStop.Decode);
            PacketTypes.Add(MessageType.ObjectExportSelected, Object.ObjectExportSelected.Decode);
            PacketTypes.Add(MessageType.RequestObjectPropertiesFamily, Object.RequestObjectPropertiesFamily.Decode);
            PacketTypes.Add(MessageType.RequestPayPrice, Object.RequestPayPrice.Decode);
            PacketTypes.Add(MessageType.DeRezObject, Object.DeRezObject.Decode);
            PacketTypes.Add(MessageType.RezObject, Object.RezObject.Decode);
            PacketTypes.Add(MessageType.RezObjectFromNotecard, Object.RezObjectFromNotecard.Decode);
            PacketTypes.Add(MessageType.ObjectIncludeInSearch, Object.ObjectIncludeInSearch.Decode);
            PacketTypes.Add(MessageType.RezRestoreToWorld, Object.RezRestoreToWorld.Decode);

            /* Task Inventory */
            PacketTypes.Add(MessageType.UpdateTaskInventory, TaskInventory.UpdateTaskInventory.Decode);
            PacketTypes.Add(MessageType.RemoveTaskInventory, TaskInventory.RemoveTaskInventory.Decode);
            PacketTypes.Add(MessageType.MoveTaskInventory, TaskInventory.MoveTaskInventory.Decode);
            PacketTypes.Add(MessageType.RequestTaskInventory, TaskInventory.RequestTaskInventory.Decode);

            /* Region */
            PacketTypes.Add(MessageType.RequestRegionInfo, Region.RequestRegionInfo.Decode);
            PacketTypes.Add(MessageType.GodUpdateRegionInfo, Region.GodUpdateRegionInfo.Decode);
            PacketTypes.Add(MessageType.RegionHandshakeReply, Region.RegionHandshakeReply.Decode);

            /* Generic */
            PacketTypes.Add(MessageType.GodlikeMessage, Generic.GodlikeMessage.Decode);
            PacketTypes.Add(MessageType.EstateOwnerMessage, Generic.EstateOwnerMessage.Decode);
            PacketTypes.Add(MessageType.GenericMessage, Generic.GenericMessage.Decode);

            /* Estate */
            PacketTypes.Add(MessageType.EstateCovenantRequest, Estate.EstateCovenantRequest.Decode);

            /* Names */
            PacketTypes.Add(MessageType.UUIDNameRequest, Names.UUIDNameRequest.Decode);
            PacketTypes.Add(MessageType.UUIDGroupNameRequest, Names.UUIDGroupNameRequest.Decode);

            /* User */
            PacketTypes.Add(MessageType.GodKickUser, User.GodKickUser.Decode);
            PacketTypes.Add(MessageType.EjectUser, User.EjectUser.Decode);

            /* Profile */
            PacketTypes.Add(MessageType.AvatarPropertiesRequest, Profile.AvatarPropertiesRequest.Decode);
            PacketTypes.Add(MessageType.AvatarPropertiesUpdate, Profile.AvatarPropertiesUpdate.Decode);
            PacketTypes.Add(MessageType.AvatarInterestsUpdate, Profile.AvatarInterestsUpdate.Decode);
            PacketTypes.Add(MessageType.AvatarNotesUpdate, Profile.AvatarNotesUpdate.Decode);
            PacketTypes.Add(MessageType.PickInfoUpdate, Profile.PickInfoUpdate.Decode);
            PacketTypes.Add(MessageType.PickDelete, Profile.PickDelete.Decode);
            PacketTypes.Add(MessageType.PickGodDelete, Profile.PickGodDelete.Decode);

            /* Map */
            PacketTypes.Add(MessageType.MapLayerRequest, Map.MapLayerRequest.Decode);
            PacketTypes.Add(MessageType.MapBlockRequest, Map.MapBlockRequest.Decode);
            PacketTypes.Add(MessageType.MapNameRequest, Map.MapNameRequest.Decode);
            PacketTypes.Add(MessageType.MapItemRequest, Map.MapItemRequest.Decode);

            /* Event */
            PacketTypes.Add(MessageType.EventInfoRequest, Event.EventInfoRequest.Decode);
            PacketTypes.Add(MessageType.EventNotificationAddRequest, Event.EventNotificationAddRequest.Decode);
            PacketTypes.Add(MessageType.EventNotificationRemoveRequest, Event.EventNotificationRemoveRequest.Decode);
            PacketTypes.Add(MessageType.EventGodDelete, Event.EventGodDelete.Decode);
            PacketTypes.Add(MessageType.EventLocationRequest, Event.EventLocationRequest.Decode);

            /* Friend */
            PacketTypes.Add(MessageType.AcceptFriendship, Friend.AcceptFriendship.Decode);
            PacketTypes.Add(MessageType.DeclineFriendship, Friend.DeclineFriendship.Decode);
            PacketTypes.Add(MessageType.TerminateFriendship, Friend.TerminateFriendship.Decode);

            /* Calling Card */
            PacketTypes.Add(MessageType.OfferCallingCard, CallingCard.OfferCallingCard.Decode);
            PacketTypes.Add(MessageType.AcceptCallingCard, CallingCard.AcceptCallingCard.Decode);
            PacketTypes.Add(MessageType.DeclineCallingCard, CallingCard.DeclineCallingCard.Decode);
            
            /* Economy */
            PacketTypes.Add(MessageType.EconomyDataRequest, Economy.EconomyDataRequest.Decode);

            /* Search */
            PacketTypes.Add(MessageType.AvatarPickerRequest, Search.AvatarPickerRequest.Decode);
            PacketTypes.Add(MessageType.PlacesQuery, Search.PlacesQuery.Decode);
        }
    }
}
