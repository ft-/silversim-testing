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

using log4net;
using SilverSim.LL.Messages;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.LL.Core
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
    public class LLUDPServer : IDisposable, ILLUDPServer
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LLUDP SERVER");
        IPAddress m_BindAddress;
        int m_BindPort;
        Socket m_UdpSocket;
        NonblockingQueue<UDPReceivePacket> m_InboundBufferQueue = new NonblockingQueue<UDPReceivePacket>();
        RwLockedDoubleDictionary<EndPoint, uint, Circuit> m_Circuits = new RwLockedDoubleDictionary<EndPoint, uint, Circuit>();
        bool m_InboundRunning = false;
        IMServiceInterface m_IMService;
        ChatServiceInterface m_ChatService;
        BlockingQueue<IScriptEvent> m_ChatQueue = new BlockingQueue<IScriptEvent>();
        RwLockedDictionary<UUID, LLAgent> m_Agents = new RwLockedDictionary<UUID, LLAgent>();
        Thread m_ChatThread;
        Dictionary<MessageType, Action<Message>> m_Routing = new Dictionary<MessageType, Action<Message>>();
        
        public SceneInterface Scene { get; private set; }

        public LLUDPServer(IPAddress bindAddress, int port, IMServiceInterface imService, ChatServiceInterface chatService, SceneInterface scene)
        {
            Scene = scene;
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

            try
            {
                if (m_UdpSocket.Ttl < 128)
                {
                    m_UdpSocket.Ttl = 128;
                }
            }
            catch (SocketException)
            {
                m_Log.Debug("Failed to increase default TTL");
            }

            /* since Win 2000, there is a WSAECONNRESET, we do not want that in our code */
            try
            {
                const int SIO_UDP_CONNRESET = -1744830452;

                m_UdpSocket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null);
            }
            catch (SocketException)
            {
                /* however, mono does not have an idea about what this is all about, so we catch that here */
            }

            m_ChatThread = new Thread(ChatSendHandler);
            m_ChatThread.Start();
            InitRouting();
            m_UdpSocket.Bind(ep);
            m_Log.InfoFormat("Initialized UDP Server at {0}:{1}", bindAddress.ToString(), port);
        }

        public void SendMessageToCircuit(UInt32 circuitcode, Message m)
        {
            Circuit circuit;
            if(m_Circuits.TryGetValue(circuitcode, out circuit))
            {
                circuit.SendMessage(m);
            }
        }

        public void Dispose()
        {
            Stop();
            m_ChatQueue.Enqueue(new ShutdownEvent());
            Scene = null;
        }

        public void Shutdown()
        {
            Stop();
            m_ChatQueue.Enqueue(new ShutdownEvent());
            Scene = null;
        }

        #region Chat Thread
        public void ChatSendHandler()
        {
            Thread.CurrentThread.Name = "Chat:Routing Thread";
            while(true)
            {
                IScriptEvent ev = m_ChatQueue.Dequeue();
                if(ev is ShutdownEvent)
                {
                    break;
                }
                else if(ev is ListenEvent)
                {
                    m_ChatService.Send((ListenEvent)ev);
                }
            }
        }
        #endregion

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
                    m_Log.InfoFormat("Started at {0}:{1}", m_BindAddress.ToString(), m_BindPort);
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
                    m_Log.InfoFormat("Stopped at {0}:{1}", m_BindAddress.ToString(), m_BindPort);
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
            Circuit circuit;
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
                try
                {
                    /* check whether we got an UseCircuitCode */
                    MessageType mType = pck.ReadMessageType();
                    if (MessageType.UseCircuitCode == mType)
                    {
                        UInt32 circuitcode = pck.ReadUInt32();
                        /* it is, so we have actually to look for the circuitcode and set up the remote endpoint here */
                        if (m_Circuits.TryGetValue(circuitcode, out circuit))
                        {
                            UUID sessionID = pck.ReadUUID();
                            UUID agentID = pck.ReadUUID();
                            /* there it is check for SessionID and AgentID */
                            if (!circuit.SessionID.Equals(sessionID))
                            {
                                /* no match on SessionID */
                            }
                            else if (!circuit.AgentID.Equals(agentID))
                            {
                                /* no match on AgentID */
                            }
                            else
                            {
                                /* it matches, so we have to change the actual key */
                                IPEndPoint endpoint = new IPEndPoint(0, 0);
                                EndPoint ep = endpoint.Create(pck.RemoteEndPoint.Serialize());
                                m_Circuits.Remove(circuit.CircuitCode);
                                m_Circuits.Add(ep, circuit.CircuitCode, circuit);
                                circuit.RemoteEndPoint = ep;
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
                }
                catch
                {

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
                m_Log.ErrorFormat("Exception {0} => {1} at {2}", e.GetType().Name, e.ToString(), e.StackTrace.ToString());
            }
            /* return the buffer to the pool */
            m_InboundBufferQueue.Enqueue(pck);
        }
        #endregion

        protected internal void SendPacketTo(UDPPacket p, EndPoint ep)
        {
            m_UdpSocket.SendTo(p.Data, 0, p.DataLength, SocketFlags.None, ep);
        }

        void HandleAgentUpdateMessage(Message m)
        {
            LLAgent agent;
            if (m_Agents.TryGetValue(m.CircuitAgentID, out agent))
            {
                agent.HandleAgentUpdateMessage(m);
            }
        }

        void HandleAgentMessage(Message m)
        {
            LLAgent agent;
            if (m_Agents.TryGetValue(m.CircuitAgentID, out agent))
            {
                agent.HandleAgentMessage(m);
            }
        }

        void HandleAgentInventoryMessage(Message m)
        {
            LLAgent agent;
            if (m_Agents.TryGetValue(m.CircuitAgentID, out agent))
            {
                agent.HandleInventoryMessage(m);
            }
        }

        void InitRouting()
        {
            /* Objects */
            m_Routing[MessageType.ObjectGrab] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectGrabUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDeGrab] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectAdd] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDelete] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDuplicate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDuplicateOnRay] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.MultipleObjectUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectRotation] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectClickAction] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RequestMultipleObjects] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectFlagUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectImage] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectMaterial] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectShape] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectExtraParams] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectOwner] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectGroup] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectBuy] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.BuyObjectInventory] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectPermissions] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectSaleInfo] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectName] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDescription] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectCategory] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectSelect] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDeselect] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectAttach] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDetach] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDrop] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectLink] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectDelink] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectExportSelected] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RequestObjectPropertiesFamily] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RequestPayPrice] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ObjectIncludeInSearch] = Scene.HandleSimulatorMessage;

            /* Scripts */
            m_Routing[MessageType.GetScriptRunning] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.SetScriptRunning] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ScriptReset] = Scene.HandleSimulatorMessage;

            /* Agent Inventory */
            m_Routing[MessageType.CopyInventoryFromNotecard] = HandleAgentInventoryMessage;
            m_Routing[MessageType.UpdateInventoryItem] = HandleAgentInventoryMessage;
            m_Routing[MessageType.MoveInventoryItem] = HandleAgentInventoryMessage;
            m_Routing[MessageType.RemoveInventoryItem] = HandleAgentInventoryMessage;
            m_Routing[MessageType.ChangeInventoryItemFlags] = HandleAgentInventoryMessage;
            m_Routing[MessageType.CreateInventoryFolder] = HandleAgentInventoryMessage;
            m_Routing[MessageType.UpdateInventoryFolder] = HandleAgentInventoryMessage;
            m_Routing[MessageType.MoveInventoryFolder] = HandleAgentInventoryMessage;
            m_Routing[MessageType.RemoveInventoryFolder] = HandleAgentInventoryMessage;
            m_Routing[MessageType.FetchInventoryDescendents] = HandleAgentInventoryMessage;
            m_Routing[MessageType.FetchInventory] = HandleAgentInventoryMessage;
            m_Routing[MessageType.RemoveInventoryObjects] = HandleAgentInventoryMessage;
            m_Routing[MessageType.PurgeInventoryDescendents] = HandleAgentInventoryMessage;
            m_Routing[MessageType.CreateInventoryItem] = HandleAgentInventoryMessage;
            m_Routing[MessageType.CreateLandmarkForEvent] = HandleAgentInventoryMessage;
            m_Routing[MessageType.LinkInventoryItem] = HandleAgentInventoryMessage;

            /* Agent Update */
            m_Routing[MessageType.AgentUpdate] = HandleAgentUpdateMessage;
            m_Routing[MessageType.AgentFOV] = HandleAgentMessage;
            m_Routing[MessageType.AgentHeightWidth] = HandleAgentMessage;
            m_Routing[MessageType.AgentSetAppearance] = HandleAgentMessage;
            m_Routing[MessageType.AgentAnimation] = HandleAgentMessage;
            m_Routing[MessageType.AgentRequestSit] = HandleAgentMessage;

            /* Appearance */
            m_Routing[MessageType.AgentWearablesRequest] = HandleAgentMessage;
            m_Routing[MessageType.AgentIsNowWearing] = HandleAgentMessage;
            m_Routing[MessageType.AgentCachedTexture] = HandleAgentMessage;
            m_Routing[MessageType.ViewerEffect] = HandleAgentMessage;
            m_Routing[MessageType.RezSingleAttachmentFromInv] = HandleAgentMessage;
            m_Routing[MessageType.RezMultipleAttachmentFromInv] = HandleAgentMessage;
            m_Routing[MessageType.DetachAttachmentIntoInv] = HandleAgentMessage;
            m_Routing[MessageType.CreateNewOutfitAttachments] = HandleAgentMessage;

            /* Agent State */
            m_Routing[MessageType.AgentPause] = HandleAgentMessage;
            m_Routing[MessageType.AgentResume] = HandleAgentMessage;
            m_Routing[MessageType.SetAlwaysRun] = HandleAgentMessage;

            /* Region Handshake */
            m_Routing[MessageType.RegionHandshakeReply] = HandleAgentMessage;
            m_Routing[MessageType.CompleteAgentMovement] = HandleAgentMessage;

            /* Logout Request */
            m_Routing[MessageType.LogoutRequest] = HandleAgentMessage;

            /* Parcel */
            m_Routing[MessageType.ParcelInfoRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelObjectOwnersRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelPropertiesRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelPropertiesRequestByID] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelPropertiesUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelReturnObjects] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelSetOtherCleanTime] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelDisableObjects] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelSelectObjects] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelBuyPass] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelDeedToGroup] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelReclaim] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelClaim] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelJoin] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelDivide] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelRelease] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelBuy] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelGodForceOwner] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelAccessListRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelAccessListUpdate] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelDwellRequest] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelGodMarkAsContent] = Scene.HandleSimulatorMessage;

            m_Routing[MessageType.RequestRegionInfo] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.GodUpdateRegionInfo] = Scene.HandleSimulatorMessage;

            /* Undo/Redo logic */
            m_Routing[MessageType.Undo] = HandleAgentMessage;
            m_Routing[MessageType.Redo] = HandleAgentMessage;

            /* Land */
            m_Routing[MessageType.ModifyLand] = HandleAgentMessage;
            m_Routing[MessageType.UndoLand] = HandleAgentMessage;

            /* Object Inventory */
            m_Routing[MessageType.UpdateTaskInventory] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RemoveTaskInventory] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.RequestTaskInventory] = Scene.HandleSimulatorMessage;

            /* Rez and Derez */
            m_Routing[MessageType.DeRezObject] = HandleAgentMessage;
            m_Routing[MessageType.RezObject] = HandleAgentMessage;
            m_Routing[MessageType.RezObjectFromNotecard] = HandleAgentMessage;
            m_Routing[MessageType.RezScript] = HandleAgentMessage;
            m_Routing[MessageType.RezRestoreToWorld] = HandleAgentMessage;

            m_Routing[MessageType.ParcelMediaCommandMessage] = Scene.HandleSimulatorMessage;
            m_Routing[MessageType.ParcelMediaUpdate] = Scene.HandleSimulatorMessage;

#if OLD
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


                case MessageType.RequestImage:
                    /* image access */
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

                case MessageType.MapLayerRequest:
                case MessageType.MapBlockRequest:
                case MessageType.MapNameRequest:
                case MessageType.MapItemRequest:
                    /* Map */
                    break;

                case MessageType.SendPostcard:
                    break;

                case MessageType.LandStatRequest:
                    break;
            }
#endif
        }

        public void RouteReceivedMessage(Message m)
        {
            Action<Message> action;
            if(m_Routing.TryGetValue(m.Number, out action))
            {
                action(m);
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

        public void AddCircuit(Circuit c)
        {
            m_Circuits.Add(c.RemoteEndPoint, c.CircuitCode, c);
        }
    }
    #endregion
}
