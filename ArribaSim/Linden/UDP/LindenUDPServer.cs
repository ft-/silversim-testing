/*

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

using ArribaSim.Linden.Messages;
using ArribaSim.Scene.ServiceInterfaces.Chat;
using ArribaSim.Scene.Types.Script.Events;
using ArribaSim.ServiceInterfaces.IM;
using ArribaSim.Types.IM;
using log4net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using ThreadedClasses;

namespace ArribaSim.Linden.UDP
{
    #region Rx Buffer
    public class UDPReceivePacket : UDPPacket
    {
        public EndPoint RemoteEndPoint = new IPEndPoint(0, 0);

        public UDPReceivePacket()
            : base()
        {

        }
    }
    #endregion

    #region LLUDP Server
    public class LindenUDPServer
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        IPAddress m_BindAddress;
        int m_BindPort;
        Socket m_UdpSocket;
        NonblockingQueue<UDPReceivePacket> m_InboundBufferQueue = new NonblockingQueue<UDPReceivePacket>();
        RwLockedDoubleDictionary<EndPoint, uint, UDPCircuit> m_Circuits = new RwLockedDoubleDictionary<EndPoint, uint, UDPCircuit>();
        bool m_InboundRunning = false;
        IMServiceInterface m_IMService;
        ChatServiceInterface m_ChatService;

        public LindenUDPServer(IPAddress bindAddress, int port, IMServiceInterface imService, ChatServiceInterface chatService)
        {
            m_IMService = imService;
            m_ChatService = chatService;
            m_BindAddress = bindAddress;
            m_BindPort = port;
            IPEndPoint ep = new IPEndPoint(m_BindAddress, m_BindPort);
            m_UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            for (int i = 0; i < 100; ++i)
            {
                m_InboundBufferQueue.Enqueue(new UDPReceivePacket());
            }

            /* since Win 2000, there is a WSAECONNRESET, we do not want that in our code */
            try
            {
                const int SIO_UDP_CONNRESET = -1744830452;

                m_UdpSocket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null);
            }
            catch (SocketException)
            {
                /* however, mono does not have an idea about this is all about, so we catch that here */
            }

            m_UdpSocket.Bind(ep);
            m_Log.InfoFormat("[LLUDP SERVER]: Initialized UDP Server at {0}:{1}", bindAddress.ToString(), port);
        }

        public void SendMessageToCircuit(UInt32 circuitcode, Message m)
        {
            UDPCircuit circuit;
            if(m_Circuits.TryGetValue(circuitcode, out circuit))
            {
                circuit.SendMessage(m);
            }
        }

        public void Start()
        {
            lock(this)
            {
                if(m_InboundRunning)
                {
                    return;
                }
                try
                {
                    m_InboundRunning = true;
                    BeginUdpReceive();
                    m_Log.InfoFormat("[LLUDP SERVER]: Started at {0}:{1}", m_BindAddress.ToString(), m_BindPort);
                }
                catch(Exception e)
                {
                    m_InboundRunning = false;
                    throw e;
                }
            }
        }

        public void Stop()
        {
            lock(this)
            {
                if(m_InboundRunning)
                {
                    m_Log.InfoFormat("[LLUDP SERVER]: Stopped at {0}:{1}", m_BindAddress.ToString(), m_BindPort);
                }
                m_InboundRunning = false;
            }
        }

        #region UDP Receive Handler
        void BeginUdpReceive()
        {
            UDPReceivePacket pck;

            try
            {
                pck = m_InboundBufferQueue.Dequeue();
            }
            catch
            {
                pck = new UDPReceivePacket();
            }
            
            m_UdpSocket.BeginReceiveFrom(pck.Data, 0, pck.Data.Length, SocketFlags.None, ref pck.RemoteEndPoint,
                UdpReceiveEndHandler, pck);
        }

        void UdpReceiveEndHandler(IAsyncResult ar)
        {
            UDPCircuit circuit;
            UDPReceivePacket pck = (UDPReceivePacket)ar.AsyncState;
            try
            {
                pck.DataLength = m_UdpSocket.EndReceiveFrom(ar, ref pck.RemoteEndPoint);
            }
            catch
            {
                return;
            }
            finally
            {
                if (m_InboundRunning)
                {
                    BeginUdpReceive();
                }
            }

            pck.TransferredAtTime = (uint)Environment.TickCount;
            pck.EnqueuedAtTime = (uint)Environment.TickCount;

            /* we do not want to spend time on decoding packets that are unknown where they belong */
            if(!m_Circuits.TryGetValue(pck.RemoteEndPoint, out circuit))
            {
                /* check whether we got an UseCircuitCode */
                MessageType mType = pck.ReadMessageType();
                if(MessageType.UseCircuitCode == mType)
                {
                    /* it is, so we have actually to look for the circuitcode and set up the remote endpoint here */
                    if(m_Circuits.TryGetValue(pck.ReadUInt32(), out circuit))
                    {
                        /* there it is check for SessionID and AgentID */
                        if(!circuit.SessionID.Equals(pck.ReadUUID()))
                        {
                            /* no match on SessionID */
                        }
                        else if(!circuit.AgentID.Equals(pck.ReadUUID()))
                        {
                            /* no match on AgentID */
                        }
                        else
                        {
                            /* it matches, so we have to change the actual key */
                            IPEndPoint endpoint = new IPEndPoint(0, 0);
                            m_Circuits.Remove(circuit.CircuitCode);
                            m_Circuits.Add(endpoint.Create(pck.RemoteEndPoint.Serialize()), circuit.CircuitCode, circuit);
                            circuit.RemoteEndPoint = endpoint;
                            try
                            {
                                circuit.Start();
                            }
                            catch
                            {
                                circuit.Stop();
                            }
                        }
                    }
                }

                /* back to pool with that packet. Packet holds nothing of interest. */
                m_InboundBufferQueue.Enqueue(pck);
                return;
            }

            /* here we spend time on decoding */
            if(pck.IsUndersized)
            {
                /* packet is undersized so we throw it away as well */
                m_InboundBufferQueue.Enqueue(pck);
                return;
            }

            /* now we know that the packet is at least valid
             * We can pass it to the circuit handler.
             */

            /* we decode the ack numbers here, the code does not need to be implemented in the UDP Circuit Handler */
            List<UInt32> acknumbers = null;

            if (pck.HasAckFlag)
            {
                try
                {
                    acknumbers = pck.Acks;
                }
                catch
                {
                    /* packet is undersized so we throw it away as well */
                    m_InboundBufferQueue.Enqueue(pck);
                    return;
                }
            }

            try
            {
                circuit.PacketReceived(pck.RemoteEndPoint, pck, acknumbers);
            }
            catch(Exception e)
            {
                /* we catch all issues here */
                m_Log.ErrorFormat("[LLUDP SERVER]: Exception {0} => {1} at {2}", e.GetType().Name, e.ToString(), e.StackTrace.ToString());
            }
            /* return the buffer to the pool */
            m_InboundBufferQueue.Enqueue(pck);
        }
        #endregion

        protected internal void SendPacketTo(UDPPacket p, EndPoint ep)
        {
            m_UdpSocket.SendTo(p.Data, 0, p.DataLength, SocketFlags.None, ep);
        }

        public void RouteReceivedMessage(Message m)
        {
            switch(m.Number)
            {
                case MessageType.AvatarPickerRequest:
                case MessageType.PlacesQuery:
                case MessageType.DirFindQuery:
                case MessageType.DirClassifiedQuery:
                case MessageType.DirLandQuery:
                case MessageType.DirPopularQuery:
                    /* Search */
                    break;

                case MessageType.ParcelInfoRequest:
                case MessageType.ParcelObjectOwnersRequest:
                case MessageType.ParcelPropertiesRequest:
                case MessageType.ParcelPropertiesRequestByID:
                case MessageType.ParcelPropertiesUpdate:
                case MessageType.ParcelReturnObjects:
                case MessageType.ParcelSetOtherCleanTime:
                case MessageType.ParcelDisableObjects:
                case MessageType.ParcelSelectObjects:
                case MessageType.ParcelBuyPass:
                case MessageType.ParcelDeedToGroup:
                case MessageType.ParcelReclaim:
                case MessageType.ParcelClaim:
                case MessageType.ParcelJoin:
                case MessageType.ParcelDivide:
                case MessageType.ParcelRelease:
                case MessageType.ParcelBuy:
                case MessageType.ParcelGodForceOwner:
                case MessageType.ParcelAccessListRequest:
                case MessageType.ParcelAccessListUpdate:
                case MessageType.ParcelDwellRequest:
                case MessageType.ParcelGodMarkAsContent:
                    /* Parcel */
                    break;

                case MessageType.ViewerStartAuction:
                    /* Auction */
                    break;

                case MessageType.UUIDNameRequest:
                    break;

                case MessageType.EstateCovenantRequest:
                    /* Estate */
                    break;

                case MessageType.EventInfoRequest:
                case MessageType.EventNotificationAddRequest:
                case MessageType.EventNotificationRemoveRequest:
                case MessageType.EventGodDelete:
                case MessageType.EventLocationRequest:
                    /* Events */
                    break;

                case MessageType.RevokePermissions:
                case MessageType.ForceScriptControlRelease:
                    /* Script Permissions */
                    break;

                case MessageType.ClassifiedInfoRequest:
                case MessageType.ClassifiedInfoUpdate:
                case MessageType.ClassifiedDelete:
                case MessageType.ClassifiedGodDelete:
                case MessageType.AvatarPropertiesRequest:
                case MessageType.AvatarPropertiesUpdate:
                case MessageType.AvatarInterestsUpdate:
                case MessageType.AvatarNotesUpdate:
                case MessageType.PickInfoUpdate:
                case MessageType.PickDelete:
                case MessageType.PickGodDelete:
                case MessageType.UserInfoRequest:
                case MessageType.UpdateUserInfo:
                    /* Profile */
                    break;

                case MessageType.GroupNoticesListRequest:
                case MessageType.GroupNoticeRequest:
                case MessageType.GroupNoticeAdd:
                case MessageType.UUIDGroupNameRequest:
                case MessageType.CreateGroupRequest:
                case MessageType.UpdateGroupInfo:
                case MessageType.GroupRoleChanges:
                case MessageType.JoinGroupRequest:
                case MessageType.EjectGroupMemberRequest:
                case MessageType.LeaveGroupRequest:
                case MessageType.InviteGroupRequest:
                case MessageType.GroupProfileRequest:
                case MessageType.GroupAccountSummaryRequest:
                case MessageType.GroupAccountDetailsRequest:
                case MessageType.GroupAccountTransactionsRequest:
                case MessageType.GroupActiveProposalsRequest:
                case MessageType.GroupVoteHistoryRequest:
                case MessageType.StartGroupProposal:
                case MessageType.ActivateGroup:
                case MessageType.SetGroupContribution:
                case MessageType.SetGroupAcceptNotices:
                case MessageType.GroupRoleDataRequest:
                case MessageType.GroupTitlesRequest:
                case MessageType.GroupTitleUpdate:
                case MessageType.GroupRoleUpdate:
                    /* Groups */
                    break;

                case MessageType.TeleportRequest:
                case MessageType.TeleportLocationRequest:
                case MessageType.TeleportLandmarkRequest:
                case MessageType.StartLure:
                case MessageType.TeleportLureRequest:
                case MessageType.TeleportCancel:
                    /* Teleport */
                    break;

                case MessageType.Undo:
                case MessageType.Redo:
                case MessageType.UndoLand:
                    /* Undo/Redo logic */
                    break;

                case MessageType.AgentPause:
                case MessageType.AgentResume:
                    /* agent state */
                    break;

                case MessageType.AgentUpdate:
                case MessageType.AgentFOV:
                case MessageType.AgentHeightWidth:
                case MessageType.AgentSetAppearance:
                case MessageType.AgentAnimation:
                case MessageType.AgentRequestSit:
                    /* agent update */
                    break;

                case MessageType.RequestImage:
                    /* image access */
                    break;

                case MessageType.SetAlwaysRun:
                    break;

                case MessageType.ObjectAdd:
                case MessageType.ObjectDelete:
                case MessageType.ObjectDuplicate:
                case MessageType.ObjectDuplicateOnRay:
                case MessageType.MultipleObjectUpdate:
                case MessageType.RequestMultipleObjects:
                case MessageType.ObjectRotation:
                case MessageType.ObjectFlagUpdate:
                case MessageType.ObjectClickAction:
                case MessageType.ObjectImage:
                case MessageType.ObjectMaterial:
                case MessageType.ObjectShape:
                case MessageType.ObjectExtraParams:
                case MessageType.ObjectOwner:
                case MessageType.ObjectGroup:
                case MessageType.ObjectBuy:
                case MessageType.BuyObjectInventory:
                case MessageType.ObjectPermissions:
                case MessageType.ObjectSaleInfo:
                case MessageType.ObjectName:
                case MessageType.ObjectDescription:
                case MessageType.ObjectCategory:
                case MessageType.ObjectSelect:
                case MessageType.ObjectDeselet:
                case MessageType.ObjectAttach:
                case MessageType.ObjectDetach:
                case MessageType.ObjectDrop:
                case MessageType.ObjectLink:
                case MessageType.ObjectDelink:
                case MessageType.ObjectGrab:
                case MessageType.ObjectGrabUpdate:
                case MessageType.ObjectDeGrab:
                case MessageType.ObjectExportSelected:
                case MessageType.RequestObjectPropertiesFamily:
                case MessageType.RequestPayPrice:
                case MessageType.ObjectIncludeInSearch:
                    /* objects */
                    break;

                case MessageType.GodKickUser:
                case MessageType.EjectUser:
                case MessageType.FreezeUser:
                    break;

                case MessageType.ModifyLand:
                    break;

                case MessageType.VelocityInterpolateOn:
                case MessageType.VelocityInterpolateOff:
                    break;

                case MessageType.StateSave:
                    break;

                case MessageType.TrackAgent:
                    break;

                case MessageType.UserReport:
                    break;

                case MessageType.RequestRegionInfo:
                case MessageType.GodUpdateRegionInfo:
                    break;

                case MessageType.RegionHandshakeReply:
                    break;

                case MessageType.GetScriptRunning:
                case MessageType.SetScriptRunning:
                case MessageType.ScriptReset:
                    /* Scripts */
                    break;

                case MessageType.CompleteAgentMovement:
                    /* agent */
                    break;

                case MessageType.LogoutRequest:
                    break;

                case MessageType.RetrieveInstantMessages:
                    break;

                case MessageType.RequestGodlikePowers:
                case MessageType.GodlikeMessage:
                    /* God rights */
                    break;

                case MessageType.EstateOwnerMessage:
                    break;

                case MessageType.GenericMessage:
                    break;

                case MessageType.MuteListRequest:
                case MessageType.UpdateMuteListEntry:
                case MessageType.RemoveMuteListEntry:
                    /* Mute list */
                    break;

                case MessageType.CopyInventoryFromNotecard:
                case MessageType.UpdateInventoryItem:
                case MessageType.MoveInventoryItem:
                case MessageType.RemoveInventoryItem:
                case MessageType.ChangeInventoryItemFlags:
                case MessageType.CreateInventoryFolder:
                case MessageType.UpdateInventoryFolder:
                case MessageType.MoveInventoryFolder:
                case MessageType.RemoveInventoryFolder:
                case MessageType.FetchInventoryDescendents:
                case MessageType.FetchInventory:
                case MessageType.RemoveInventoryObjects:
                case MessageType.PurgeInventoryDescendents:
                case MessageType.CreateInventoryItem:
                case MessageType.CreateLandmarkForEvent:
                case MessageType.LinkInventoryItem:
                    /* Inventory */
                    break;

                case MessageType.UpdateTaskInventory:
                case MessageType.RemoveTaskInventory:
                case MessageType.RequestTaskInventory:
                    /* Object Inventory */
                    break;

                case MessageType.DeRezObject:
                case MessageType.RezObject:
                case MessageType.RezObjectFromNotecard:
                case MessageType.RezScript:
                case MessageType.RezRestoreToWorld:
                    /* Rez and Derez */
                    break;

                case MessageType.TerminateFriendship:
                case MessageType.GrantUserRights:
                case MessageType.ChangeUserRights:
                    /* friends */
                    break;

                case MessageType.OfferCallingCard:
                case MessageType.AcceptCallingCard:
                case MessageType.DeclineCallingCard:
                    break;

                case MessageType.RegionHandleRequest:
                    /* Regions */
                    break;

                case MessageType.MoneyTransferRequest:
                case MessageType.MoneyBalanceRequest:
                    /* Money */
                    break;

                case MessageType.ActivateGestures:
                case MessageType.DeactivateGestures:
                    /* Gestures */
                    break;

                case MessageType.SetStartLocationRequest:
                    /* Home location */
                    break;

                case MessageType.AssetUploadRequest:
                    /* Asset */
                    break;

                case MessageType.AgentWearablesRequest:
                case MessageType.AgentIsNowWearing:
                case MessageType.AgentCachedTexture:
                case MessageType.ViewerEffect:
                case MessageType.RezSingleAttachmentFromInv:
                case MessageType.RezMultipleAttachmentFromInv:
                case MessageType.DetachAttachmentIntoInv:
                case MessageType.CreateNewOutfitAttachments:
                    /* Appearance */
                    break;

                case MessageType.MapLayerRequest:
                case MessageType.MapBlockRequest:
                case MessageType.MapNameRequest:
                case MessageType.MapItemRequest:
                    /* Map */
                    break;

                case MessageType.SendPostcard:
                    break;

                case MessageType.ParcelMediaCommandMessage:
                case MessageType.ParcelMediaUpdate:
                    break;

                case MessageType.LandStatRequest:
                    break;
            }
        }

        public void RouteIM(GridInstantMessage im)
        {
            if (null != m_IMService)
            {
                m_IMService.Send(im);
            }
        }

        public void RouteChat(ListenEvent ev)
        {
            if (null != m_ChatService)
            {
                m_ChatService.Send(ev);
            }
        }
    }
    #endregion
}
