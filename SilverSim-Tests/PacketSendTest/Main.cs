using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Msg = SilverSim.LL.Messages;
using SilverSim.Types;
using System.IO;

namespace Tests.PacketSendTest
{
    public static class PacketSendTest
    {
        private static Socket socket;
        private static void sendMessage(Msg.Message m)
        {
            Msg.UDPPacket p = new Msg.UDPPacket();
            m.Serialize(p);
            socket.SendTo(p.Data, p.DataLength, SocketFlags.None, DestinationEndPoint);
        }
        private static readonly UUID TestUUID = new UUID("00112233-1122-1122-1122-001122334455");
        private static IPEndPoint DestinationEndPoint = new IPEndPoint(new IPAddress(new byte[] {192,168,99,4}), 9300);
        public static void Main(string[] args)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(new IPAddress(0), 9300));

            {
                Msg.Agent.AgentDataUpdate m = new Msg.Agent.AgentDataUpdate();
                m.GroupPowers = 10;
                m.FirstName = "FirstName";
                m.LastName = "LastName";
                m.AgentID = TestUUID;
                m.ActiveGroupID = TestUUID;
                m.GroupTitle = "GroupTitle";
                m.GroupName = "Test";

                sendMessage(m);
            }

            {
                Msg.Agent.AgentDropGroup m = new Msg.Agent.AgentDropGroup();
                m.AgentID = TestUUID;
                m.GroupID = TestUUID;

                sendMessage(m);
            }

            {
                Msg.Agent.AgentGroupDataUpdate m = new Msg.Agent.AgentGroupDataUpdate();
                m.AgentID = TestUUID;
                Msg.Agent.AgentGroupDataUpdate.GroupDataEntry e = new Msg.Agent.AgentGroupDataUpdate.GroupDataEntry();
                e.AcceptNotices = true;
                e.Contribution = 10;
                e.GroupID = TestUUID;
                e.GroupInsigniaID = TestUUID;
                e.GroupName = "GroupName";
                e.GroupPowers = 10;
                m.GroupData.Add(e);

                sendMessage(m);
            }

            {
                Msg.Agent.CoarseLocationUpdate m = new Msg.Agent.CoarseLocationUpdate();
                m.You = 0;
                m.Prey = 1;
                Msg.Agent.CoarseLocationUpdate.AgentDataEntry e;
                e = new Msg.Agent.CoarseLocationUpdate.AgentDataEntry();
                e.X = 1;
                e.Y = 2;
                e.Z = 3;
                e.AgentID = TestUUID;
                m.AgentData.Add(e);
                e = new Msg.Agent.CoarseLocationUpdate.AgentDataEntry();
                e.X = 1;
                e.Y = 2;
                e.Z = 3;
                e.AgentID = TestUUID;
                m.AgentData.Add(e);

                sendMessage(m);
            }

            {
                Msg.Agent.HealthMessage m = new Msg.Agent.HealthMessage();
                m.Health = 10;

                sendMessage(m);
            }

            {
                Msg.Alert.AlertMessage m = new Msg.Alert.AlertMessage();
                m.Message = "Message";
                Msg.Alert.AlertMessage.Data e = new Msg.Alert.AlertMessage.Data();
                e.ExtraParams = new byte[] {1, 2, 3, 4};
                e.Message = "Message";
                m.AlertInfo.Add(e);
                e = new Msg.Alert.AlertMessage.Data();
                e.ExtraParams = new byte[] { 1, 2, 3, 4 };
                e.Message = "Message";
                m.AlertInfo.Add(e);

                sendMessage(m);
            }

            {
                Msg.Appearance.AgentCachedTextureResponse m = new Msg.Appearance.AgentCachedTextureResponse();
                m.AgentID = TestUUID;
                m.SessionID = TestUUID;
                m.SerialNum = 55;
                Msg.Appearance.AgentCachedTextureResponse.WearableDataEntry e;

                e = new Msg.Appearance.AgentCachedTextureResponse.WearableDataEntry();
                e.HostName = "HostName";
                e.TextureID = TestUUID;
                e.TextureIndex = 1;
                m.WearableData.Add(e);

                e = new Msg.Appearance.AgentCachedTextureResponse.WearableDataEntry();
                e.HostName = "HostName";
                e.TextureID = TestUUID;
                e.TextureIndex = 2;
                m.WearableData.Add(e);

                sendMessage(m);
            }

            {
                Msg.Appearance.AgentWearablesUpdate m = new Msg.Appearance.AgentWearablesUpdate();
                m.AgentID = TestUUID;
                m.SessionID = TestUUID;
                m.SerialNum = 55;
                Msg.Appearance.AgentWearablesUpdate.WearableDataEntry e;

                e = new Msg.Appearance.AgentWearablesUpdate.WearableDataEntry();
                e.AssetID = TestUUID;
                e.ItemID = TestUUID;
                e.WearableType = SilverSim.Types.Asset.Format.WearableType.Pants;
                m.WearableData.Add(e);

                e = new Msg.Appearance.AgentWearablesUpdate.WearableDataEntry();
                e.AssetID = TestUUID;
                e.ItemID = TestUUID;
                e.WearableType = SilverSim.Types.Asset.Format.WearableType.Shirt;
                m.WearableData.Add(e);

                sendMessage(m);
            }

            {
                Msg.Appearance.ViewerEffect m = new Msg.Appearance.ViewerEffect();
                m.AgentID = TestUUID;
                m.SessionID = TestUUID;
                Msg.Appearance.ViewerEffect.EffectData d;

                d = new Msg.Appearance.ViewerEffect.EffectData();
                d.AgentID = TestUUID;
                d.ID = TestUUID;
                d.Type = 5;
                d.Duration = 1;
                d.EffectColor = new ColorAlpha(0.25, 0.5, 0.75, 1);
                d.TypeData = new byte[0];
                m.Effects.Add(d);

                d = new Msg.Appearance.ViewerEffect.EffectData();
                d.AgentID = TestUUID;
                d.ID = TestUUID;
                d.Type = 6;
                d.Duration = 1;
                d.EffectColor = new ColorAlpha(0.25, 0.5, 0.75, 1);
                d.TypeData = new byte[0];
                m.Effects.Add(d);

                sendMessage(m);
            }

            {
                Msg.Avatar.AvatarAnimation m = new Msg.Avatar.AvatarAnimation();
                m.Sender = TestUUID;
                Msg.Avatar.AvatarAnimation.AnimationData ad;

                ad = new Msg.Avatar.AvatarAnimation.AnimationData();
                ad.AnimID = TestUUID;
                ad.AnimSequenceID = 1;
                m.AnimationList.Add(ad);

                ad = new Msg.Avatar.AvatarAnimation.AnimationData();
                ad.AnimID = TestUUID;
                ad.AnimSequenceID = 2;
                m.AnimationList.Add(ad);

                Msg.Avatar.AvatarAnimation.AnimationSourceData asd;

                asd = new Msg.Avatar.AvatarAnimation.AnimationSourceData();
                asd.ObjectID = TestUUID;
                m.AnimationSourceList.Add(asd);

                asd = new Msg.Avatar.AvatarAnimation.AnimationSourceData();
                asd.ObjectID = TestUUID;
                m.AnimationSourceList.Add(asd);

                Msg.Avatar.AvatarAnimation.PhysicalAvatarEventData pae;

                pae = new Msg.Avatar.AvatarAnimation.PhysicalAvatarEventData();
                pae.TypeData = new byte[] { 1, 2, 3, 4 };
                m.PhysicalAvatarEventList.Add(pae);

                pae = new Msg.Avatar.AvatarAnimation.PhysicalAvatarEventData();
                pae.TypeData = new byte[] { 1, 2, 3, 4 };
                m.PhysicalAvatarEventList.Add(pae);

                sendMessage(m);
            }

            {
                Msg.Avatar.AvatarAppearance m = new Msg.Avatar.AvatarAppearance();
                m.Sender = TestUUID;
                m.IsTrial = true;
                Msg.Avatar.AvatarAppearance.ObjData od;

                od = new Msg.Avatar.AvatarAppearance.ObjData();
                od.TextureEntry = new byte[] {1,2 ,3, 4};
                m.ObjectData.Add(od);

                od = new Msg.Avatar.AvatarAppearance.ObjData();
                od.TextureEntry = new byte[] { 1, 2, 3, 4 };
                m.ObjectData.Add(od);

                m.VisualParams = new byte[218];
                m.VisualParams[0] = 1;
                m.VisualParams[1] = 2;

                sendMessage(m);
            }

            {
                Msg.Avatar.AvatarSitResponse m = new Msg.Avatar.AvatarSitResponse();
                m.ForceMouselook = true;
                m.IsAutoPilot = true;
                m.CameraAtOffset = new Vector3(1, 2, 3);
                m.CameraEyeOffset = new Vector3(1, 2, 3);
                m.SitObject = TestUUID;
                m.SitPosition = new Vector3(1, 2, 3);
                m.SitRotation = Quaternion.Identity;

                sendMessage(m);
            }

            {
                Msg.Camera.ClearFollowCamProperties m = new Msg.Camera.ClearFollowCamProperties();
                m.ObjectID = TestUUID;

                sendMessage(m);
            }

            {
                Msg.Camera.SetFollowCamProperties m = new Msg.Camera.SetFollowCamProperties();
                m.ObjectID = TestUUID;
                Msg.Camera.SetFollowCamProperties.CameraProperty d;

                d = new Msg.Camera.SetFollowCamProperties.CameraProperty();
                d.Type = 1;
                d.Value = 2;
                m.CameraProperties.Add(d);

                d = new Msg.Camera.SetFollowCamProperties.CameraProperty();
                d.Type = 2;
                d.Value = 2;
                m.CameraProperties.Add(d);

                sendMessage(m);
            }

            {
                Msg.Chat.ChatFromSimulator m = new Msg.Chat.ChatFromSimulator();
                m.Audible = 1;
                m.ChatType = 1;
                m.FromName = "From Name";
                m.Message = "Message";
                m.OwnerID = TestUUID;
                m.Position = new Vector3(1, 2, 3);
                m.SourceID = TestUUID;
                m.SourceType = 5;

                sendMessage(m);
            }

            {
                Msg.Circuit.AgentMovementComplete m = new Msg.Circuit.AgentMovementComplete();
                m.AgentID = TestUUID;
                m.ChannelVersion = "ChannelVersion";
                m.LookAt = new Vector3(1, 2, 3);
                m.Position = new Vector3(1, 2, 3);
                m.GridPosition = new GridVector(256, 256);
                m.SessionID = TestUUID;
                m.Timestamp = 100;

                sendMessage(m);
            }

            {
                Msg.Circuit.LogoutReply m = new Msg.Circuit.LogoutReply();
                m.AgentID = TestUUID;
                m.SessionID = TestUUID;

                sendMessage(m);
            }

            {
                Msg.Common.FeatureDisabled m = new Msg.Common.FeatureDisabled();
                m.AgentID = TestUUID;
                m.ErrorMessage = "Error Message";
                m.TransactionID = TestUUID;

                sendMessage(m);
            }
        }
    }
}
