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
            p.Flush();
            socket.SendTo(p.Data, p.DataLength, SocketFlags.None, DestinationEndPoint);
        }
        private static void sendMessage(Msg.Message m, bool zeroFlag)
        {
            Msg.UDPPacket p = new Msg.UDPPacket();
            p.IsZeroEncoded = zeroFlag;
            m.Serialize(p);
            p.Flush();
            socket.SendTo(p.Data, p.DataLength, SocketFlags.None, DestinationEndPoint);
        }
        private static readonly UUID TestUUID = new UUID("00112233-1122-1122-1122-001122334455");
        private static IPEndPoint DestinationEndPoint = new IPEndPoint(new IPAddress(new byte[] {192,168,99,4}), 9300);
        public static void Main(string[] args)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(new IPAddress(0), 9300));

            {
                Msg.Inventory.InventoryDescendents m = new Msg.Inventory.InventoryDescendents();
                Msg.Inventory.InventoryDescendents.FolderDataEntry f;
                
                f = new Msg.Inventory.InventoryDescendents.FolderDataEntry();
                f.Name = "Folder 1";
                f.Type = SilverSim.Types.Inventory.InventoryType.Bodypart;
                f.ParentID = TestUUID;
                f.FolderID = TestUUID;
                m.FolderData.Add(f);

                f = new Msg.Inventory.InventoryDescendents.FolderDataEntry();
                f.Name = "Folder 2";
                f.Type = SilverSim.Types.Inventory.InventoryType.Animation;
                f.ParentID = TestUUID;
                f.FolderID = TestUUID;
                m.FolderData.Add(f);

                Msg.Inventory.InventoryDescendents.ItemDataEntry i;
                
                i = new Msg.Inventory.InventoryDescendents.ItemDataEntry();
                i.ItemID = TestUUID;
                i.FolderID = TestUUID;
                i.CreatorID = TestUUID;
                i.OwnerID = TestUUID;
                i.GroupID = TestUUID;
                i.BaseMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.OwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.GroupMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.EveryoneMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.NextOwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.IsGroupOwned = true;
                i.AssetID = TestUUID;
                i.Type = SilverSim.Types.Asset.AssetType.Animation;
                i.InvType = SilverSim.Types.Inventory.InventoryType.Animation;
                i.Flags = 0x55;
                i.SaleType = SilverSim.Types.Inventory.InventoryItem.SaleInfoData.SaleType.Copy;
                i.SalePrice = 55;
                i.Name = "Item 1";
                i.Description = "Desc 1";
                i.CreationDate = 100;
                m.ItemData.Add(i);

                i = new Msg.Inventory.InventoryDescendents.ItemDataEntry();
                i.ItemID = TestUUID;
                i.FolderID = TestUUID;
                i.CreatorID = TestUUID;
                i.OwnerID = TestUUID;
                i.GroupID = TestUUID;
                i.BaseMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.OwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.GroupMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.EveryoneMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.NextOwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.IsGroupOwned = true;
                i.AssetID = TestUUID;
                i.Type = SilverSim.Types.Asset.AssetType.Animation;
                i.InvType = SilverSim.Types.Inventory.InventoryType.Animation;
                i.Flags = 0x55;
                i.SaleType = SilverSim.Types.Inventory.InventoryItem.SaleInfoData.SaleType.Copy;
                i.SalePrice = 55;
                i.Name = "Item 2";
                i.Description = "Desc 2";
                i.CreationDate = 100;
                m.ItemData.Add(i);

                sendMessage(m);
            }

            {
                Msg.Inventory.FetchInventoryReply m = new Msg.Inventory.FetchInventoryReply();

                Msg.Inventory.FetchInventoryReply.ItemDataEntry i;

                i = new Msg.Inventory.FetchInventoryReply.ItemDataEntry();
                i.ItemID = TestUUID;
                i.FolderID = TestUUID;
                i.CreatorID = TestUUID;
                i.OwnerID = TestUUID;
                i.GroupID = TestUUID;
                i.BaseMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.OwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.GroupMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.EveryoneMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.NextOwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.IsGroupOwned = true;
                i.AssetID = TestUUID;
                i.Type = SilverSim.Types.Asset.AssetType.Animation;
                i.InvType = SilverSim.Types.Inventory.InventoryType.Animation;
                i.Flags = 0x55;
                i.SaleType = SilverSim.Types.Inventory.InventoryItem.SaleInfoData.SaleType.Copy;
                i.SalePrice = 55;
                i.Name = "Item 1";
                i.Description = "Desc 1";
                i.CreationDate = 100;
                m.ItemData.Add(i);

                i = new Msg.Inventory.FetchInventoryReply.ItemDataEntry();
                i.ItemID = TestUUID;
                i.FolderID = TestUUID;
                i.CreatorID = TestUUID;
                i.OwnerID = TestUUID;
                i.GroupID = TestUUID;
                i.BaseMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.OwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.GroupMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.EveryoneMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.NextOwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
                i.IsGroupOwned = true;
                i.AssetID = TestUUID;
                i.Type = SilverSim.Types.Asset.AssetType.Animation;
                i.InvType = SilverSim.Types.Inventory.InventoryType.Animation;
                i.Flags = 0x55;
                i.SaleType = SilverSim.Types.Inventory.InventoryItem.SaleInfoData.SaleType.Copy;
                i.SalePrice = 55;
                i.Name = "Item 2";
                i.Description = "Desc 2";
                i.CreationDate = 100;
                m.ItemData.Add(i);

                sendMessage(m);
            }
#if TESTED
            {
                Msg.Economy.MoneyBalanceReply m = new Msg.Economy.MoneyBalanceReply();
                m.MoneyBalance = 1000;
                m.Amount = 1000;
                sendMessage(m, true);
            }

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

            {
                Msg.Economy.EconomyData m = new Msg.Economy.EconomyData();
                m.ObjectCount = 1;
                m.ObjectCapacity = 1;
                m.PriceEnergyUnit = 2;
                m.PriceObjectClaim = 3;
                m.PricePublicObjectDecay = 4;
                m.PricePublicObjectDelete = 5;
                m.PriceParcelClaim = 6;
                m.PriceParcelClaimFactor = 7;
                m.PriceUpload = 8;
                m.PriceRentLight = 9;
                m.TeleportMinPrice = 10;
                m.TeleportPriceExponent = 11;
                m.EnergyEfficiency = 12;
                m.PriceObjectRent = 13;
                m.PriceObjectScaleFactor = 14;
                m.PriceParcelRent = 15;
                m.PriceGroupCreate = 16;

                sendMessage(m);
            }

            {
                Msg.Estate.EstateCovenantReply m = new Msg.Estate.EstateCovenantReply();
                m.CovenantID = TestUUID;
                m.CovenantTimestamp = 100;
                m.EstateName = "Estate";
                m.EstateOwnerID = TestUUID;

                sendMessage(m);
            }

            {
                Msg.Event.EventInfoReply m = new Msg.Event.EventInfoReply();
                m.AgentID = TestUUID;
                m.Amount = 10;
                m.Category = "1";
                m.Cover = 1;
                m.Creator = TestUUID;
                m.Date = "1";
                m.DateUTC = 100;
                m.Desc = "Desc";
                m.Duration = 10;
                m.EventFlags = 10;
                m.EventID = 100;
                m.Name = "Name";
                m.SimName = "SimName";

                sendMessage(m);
            }

            {
                Msg.Event.EventLocationReply m = new Msg.Event.EventLocationReply();
                m.QueryID = TestUUID;
                m.Success = true;
                m.RegionID = TestUUID;
                m.RegionPos = new Vector3(1, 2, 3);

                sendMessage(m);
            }

            {
                Msg.God.GrantGodlikePowers m = new Msg.God.GrantGodlikePowers();
                m.AgentID = TestUUID;
                m.SessionID = TestUUID;
                m.GodLevel = 255;
                m.Token = TestUUID;

                sendMessage(m);
            }

            {
                Msg.Image.ImageData m = new Msg.Image.ImageData();
                m.Codec = 1;
                m.Data = new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                m.ID = TestUUID;
                m.Packets = 10;
                m.Size = 1024;

                sendMessage(m);
            }

            {
                Msg.Image.ImagePacket m = new Msg.Image.ImagePacket();
                m.Packet = 1;
                m.Data = new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                m.ID = TestUUID;

                sendMessage(m);
            }

            {
                Msg.Land.LandStatReply m = new Msg.Land.LandStatReply();
                Msg.Land.LandStatReply.ReportDataEntry e;
                
                e = new Msg.Land.LandStatReply.ReportDataEntry();
                e.Location = new Vector3(1, 2, 3);
                e.OwnerName = "Owner Name";
                e.Score = 10;
                e.TaskID = TestUUID;
                e.TaskLocalID = 10;
                e.TaskName = "Task Name";
                m.ReportData.Add(e);

                e = new Msg.Land.LandStatReply.ReportDataEntry();
                e.Location = new Vector3(1, 2, 3);
                e.OwnerName = "Owner Name";
                e.Score = 10;
                e.TaskID = TestUUID;
                e.TaskLocalID = 10;
                e.TaskName = "Task Name";
                m.ReportData.Add(e);

                m.ReportType = 1;
                m.RequestFlags = 10;
                m.TotalObjectCount = 1000;

                sendMessage(m);
            }

            {
                Msg.Names.UUIDGroupNameReply m = new Msg.Names.UUIDGroupNameReply();
                Msg.Names.UUIDGroupNameReply.Data e;

                e = new Msg.Names.UUIDGroupNameReply.Data();
                e.GroupName = "Group Name";
                e.ID = TestUUID;

                m.UUIDNameBlock.Add(e);

                e = new Msg.Names.UUIDGroupNameReply.Data();
                e.GroupName = "Group Name 2";
                e.ID = TestUUID;

                m.UUIDNameBlock.Add(e);

                sendMessage(m);
            }

            {
                Msg.Names.UUIDNameReply m = new Msg.Names.UUIDNameReply();
                Msg.Names.UUIDNameReply.Data e;

                e = new Msg.Names.UUIDNameReply.Data();
                e.FirstName = "First Name";
                e.LastName = "Last Name";
                e.ID = TestUUID;

                m.UUIDNameBlock.Add(e);

                e = new Msg.Names.UUIDNameReply.Data();
                e.FirstName = "First Name";
                e.LastName = "Last Name";
                e.ID = TestUUID;

                m.UUIDNameBlock.Add(e);

                sendMessage(m);
            }

            {
                Msg.Region.RegionHandshake m = new Msg.Region.RegionHandshake();
                
                m.RegionFlags = 3;
                m.SimAccess = 4;
                m.SimName = "SimName";
                m.SimOwner = TestUUID;
                m.IsEstateManager = true;
                m.WaterHeight = 22f;
                m.BillableFactor = 10f;
                m.CacheID = TestUUID;
                m.TerrainBase0 = TestUUID;
                m.TerrainBase1 = TestUUID;
                m.TerrainBase2 = TestUUID;
                m.TerrainBase3 = TestUUID;
                m.TerrainDetail0 = TestUUID;
                m.TerrainDetail1 = TestUUID;
                m.TerrainDetail2 = TestUUID;
                m.TerrainDetail3 = TestUUID;
                m.TerrainStartHeight00 = 0f;
                m.TerrainStartHeight01 = 1f;
                m.TerrainStartHeight10 = 2f;
                m.TerrainStartHeight11 = 3f;
                m.TerrainHeightRange00 = 0f;
                m.TerrainHeightRange01 = 1f;
                m.TerrainHeightRange10 = 2f;
                m.TerrainHeightRange11 = 3f;

                m.RegionID = TestUUID;

                m.CPUClassID = 9;
                m.CPURatio = 1;
                m.ColoName = "ColoName";
                m.ProductSKU = "ProductSKU";
                m.ProductName = "ProductName";

                sendMessage(m);
            }

            {
                Msg.Region.RegionInfo m = new Msg.Region.RegionInfo();

                m.AgentID = TestUUID;
                m.SessionID = TestUUID;
                m.SimName = "SimName";
                m.EstateID = 100;
                m.ParentEstateID = 1;
                m.RegionFlags = 10;
                m.SimAccess = 3;
                m.MaxAgents = 40;
                m.BillableFactor = 1;
                m.ObjectBonusFactor = 1;
                m.WaterHeight = 20;
                m.TerrainLowerLimit = -100;
                m.TerrainRaiseLimit = 100;
                m.PricePerMeter = 100;
                m.RedirectGridX = 101;
                m.RedirectGridY = 102;
                m.UseEstateSun = true;
                m.SunHour = 10;
                m.ProductSKU = "ProductSKU";
                m.ProductName = "ProductName";
                m.HardMaxAgents = 40;
                m.HardMaxObjects = 100;
                m.RegionFlagsExtended.Add(55);

                sendMessage(m);
            }

            {
                Msg.Region.SimulatorViewerTimeMessage m = new Msg.Region.SimulatorViewerTimeMessage();

                m.SecPerDay = 10;
                m.SecPerYear = 100;
                m.SunAngVelocity = new Vector3(1, 2, 3);
                m.SunDirection = new Vector3(1, 2, 3);
                m.SunPhase = 100;
                m.UsecSinceStart = 100;

                sendMessage(m);
            }

            {
                Msg.Script.LoadURL m = new Msg.Script.LoadURL();

                m.ObjectID = TestUUID;
                m.ObjectName = "Primitive";
                m.OwnerID = TestUUID;
                m.OwnerIsGroup = false;
                m.URL = "http://example.com/";

                sendMessage(m);
            }

            {
                Msg.Script.ScriptDialog m = new Msg.Script.ScriptDialog();

                m.Buttons.Add("1");
                m.Buttons.Add("2");
                m.Buttons.Add("3");
                m.ChatChannel = 10;
                m.FirstName = "First";
                m.ImageID = TestUUID;
                m.LastName = "Last";
                m.Message = "Message";
                m.ObjectID = TestUUID;
                m.ObjectName = "Object";
                m.OwnerData.Add(TestUUID);
                m.OwnerData.Add(TestUUID);

                sendMessage(m);
            }

            {
                Msg.Script.ScriptQuestion m = new Msg.Script.ScriptQuestion();

                m.TaskID = TestUUID;
                m.ItemID = TestUUID;
                m.ObjectName = "Object";
                m.ObjectOwner = TestUUID;
                m.Questions = 55;

                sendMessage(m);
            }

            {
                Msg.Script.ScriptRunningReply m = new Msg.Script.ScriptRunningReply();

                m.IsRunning = true;
                m.ItemID = TestUUID;
                m.ObjectID = TestUUID;

                sendMessage(m);
            }

            {
                Msg.Simulator.SimStats m = new Msg.Simulator.SimStats();

                m.ObjectCapacity = 10;
                m.PID = 10;
                m.RegionFlags = 10;
                m.RegionX = 10;
                m.RegionY = 10;
                Msg.Simulator.SimStats.Data d;

                d = new Msg.Simulator.SimStats.Data();
                d.StatID = 10;
                d.StatValue = 10;
                m.Stat.Add(d);

                d = new Msg.Simulator.SimStats.Data();
                d.StatID = 20;
                d.StatValue = 20;
                m.Stat.Add(d);

                sendMessage(m);
            }

            {
                Msg.TaskInventory.ReplyTaskInventory m = new Msg.TaskInventory.ReplyTaskInventory();

                m.TaskID = TestUUID;
                m.Serial = 10;
                m.Filename = "Filename";

                sendMessage(m);
            }

            {
                Msg.Generic.EstateOwnerMessage m = new Msg.Generic.EstateOwnerMessage();

                m.AgentID = TestUUID;
                m.Invoice = TestUUID;
                m.Method = "Estate Owner";
                m.ParamList = new byte[10];
                m.SessionID = TestUUID;

                sendMessage(m);
            }

            {
                Msg.Generic.GodlikeMessage m = new Msg.Generic.GodlikeMessage();

                m.AgentID = TestUUID;
                m.Invoice = TestUUID;
                m.Method = "Godlike";
                m.ParamList = new byte[10];
                m.SessionID = TestUUID;

                sendMessage(m);
            }

            {
                Msg.Generic.GenericMessage m = new Msg.Generic.GenericMessage();

                m.AgentID = TestUUID;
                m.Invoice = TestUUID;
                m.Method = "Generic";
                m.ParamList = new byte[10];
                m.SessionID = TestUUID;

                sendMessage(m);
            }
#endif
        }
    }
}
