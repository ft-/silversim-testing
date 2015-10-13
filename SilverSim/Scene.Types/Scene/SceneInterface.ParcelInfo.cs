// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Parcel;
using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        private Int32[,] m_ParcelLayer; /* initialized in constructor */
        private bool[] m_ParcelLayerDirty; /* RegionWidth / PARCEL_BLOCK_SIZE * RegionHeight / PARCEL_BLOCK_SIZE / 1024 */
        private ReaderWriterLock m_ParcelLayerRwLock = new ReaderWriterLock();
        protected readonly RwLockedDoubleDictionary<UUID, Int32, ParcelInfo> m_Parcels = new RwLockedDoubleDictionary<UUID, int, ParcelInfo>();
        private object m_ParcelOverlayUpdateLock = new object();

        private void InitializeParcelLayer()
        {
            m_ParcelLayer = new Int32[SizeY / PARCEL_BLOCK_SIZE, SizeX / PARCEL_BLOCK_SIZE];
            m_ParcelLayerDirty = new bool[(SizeY / PARCEL_BLOCK_SIZE) * (SizeX / PARCEL_BLOCK_SIZE) / 1024];
        }

        public void AddParcel(ParcelInfo p)
        {
            m_Parcels.Add(p.ID, p.LocalID, p);
        }

        public void ClearParcels()
        {
            m_Parcels.Clear();
        }

        private ParcelOverlayType GetParcelLayerByte(int x, int y, UUI agentID)
        {
            ParcelInfo pi;
            ParcelOverlayType ov = ParcelOverlayType.Public;
            Int32 parcelLocalID = m_ParcelLayer[x, y];
            if (m_Parcels.TryGetValue(parcelLocalID, out pi))
            {
                if(pi.Owner == agentID)
                {
                    ov |= ParcelOverlayType.OwnedBySelf;
                }
                else if(pi.SalePrice >= 0 &&
                    (pi.AuthBuyer.ID == UUID.Zero ||
                    pi.AuthBuyer.ID == agentID.ID))
                {
                    ov |= ParcelOverlayType.ForSale;
                }
                else if(pi.Owner.ID == UUID.Zero)
                {
                    ov |= ParcelOverlayType.Public;
                }
                else
                {
                    ov |= ParcelOverlayType.OwnedByOther;
                }

                if(x == 0)
                {
                    ov |= ParcelOverlayType.BorderWest;
                }
                else if (m_ParcelLayer[x - 1, y] != parcelLocalID && m_Parcels.ContainsKey(m_ParcelLayer[x - 1, y]))
                {
                    ov |= ParcelOverlayType.BorderWest;
                }

                if (y == 0)
                {
                    ov |= ParcelOverlayType.BorderSouth;
                }
                else if (m_ParcelLayer[x, y - 1] != parcelLocalID && m_Parcels.ContainsKey(m_ParcelLayer[x, y - 1]))
                {
                    ov |= ParcelOverlayType.BorderSouth;
                }
            }

            return ov;
        }

        public void SendAllParcelOverlaysTo(IAgent agent)
        {
            byte[] c = new byte[SizeX * SizeY / PARCEL_BLOCK_SIZE / PARCEL_BLOCK_SIZE];
            m_ParcelLayerRwLock.AcquireReaderLock(-1);
            try
            {
                int bytePos = 0;
                UUI agentID = agent.Owner;
                for (int y = 0; y < SizeY / PARCEL_BLOCK_SIZE; ++y)
                {
                    for (int x = 0; x < SizeX / PARCEL_BLOCK_SIZE; ++x)
                    {
                        c[bytePos++] = (byte)GetParcelLayerByte(x, y, agentID);
                    }
                }
            }
            finally
            {
                m_ParcelLayerRwLock.ReleaseReaderLock();
            }

            int sequenceID = 0;
            int offset;
            ParcelOverlay m;

            for(offset = 0; offset < c.Length; offset += 1024, ++sequenceID)
            {
                m = new ParcelOverlay();
                m.SequenceID = sequenceID;
                if (c.Length - offset >= 1024)
                {
                    m.Data = new byte[1024];
                }
                else
                {
                    m.Data = new byte[c.Length - offset];
                }
                Buffer.BlockCopy(c, offset, m.Data, 0, m.Data.Length);
                agent.SendMessageAlways(m, ID);
            }
        }

        private void SendParcelUpdates()
        {
            lock(m_ParcelOverlayUpdateLock)
            {
                m_ParcelLayerRwLock.AcquireReaderLock(-1);
                try
                {
                    int sequenceID = 0;
                    int totalLen = (int)(SizeX / PARCEL_BLOCK_SIZE * SizeY / PARCEL_BLOCK_SIZE);
                    int xwidth = (int)(SizeX / PARCEL_BLOCK_SIZE);
                    for (int offset = 0; offset < totalLen; offset += 1024, ++sequenceID)
                    {
                        if (m_ParcelLayerDirty[offset / 1024])
                        {
                            foreach (IAgent a in Agents)
                            {
                                ParcelOverlay m = new ParcelOverlay();
                                if (totalLen - offset >= 1024)
                                {
                                    m.Data = new byte[1024];
                                }
                                else
                                {
                                    m.Data = new byte[totalLen - offset];
                                }
                                m.SequenceID = sequenceID;
                                UUI agentID = a.Owner;
                                for (int pos = 0; pos < m.Data.Length; ++pos)
                                {
                                    m.Data[pos] = (byte)GetParcelLayerByte((offset + pos) % xwidth, (offset + pos) / xwidth, agentID);
                                }
                                a.SendMessageAlways(m, ID);
                            }
                            m_ParcelLayerDirty[offset / 1024] = false;
                        }
                    }
                }
                finally
                {
                    m_ParcelLayerRwLock.ReleaseReaderLock();
                }
            }
        }

        [PacketHandler(MessageType.ParcelInfoRequest)]
        void HandleParcelInfoRequest(Message m)
        {
            ParcelInfoRequest req = (ParcelInfoRequest)m;
            if(req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            try
            {
                ParcelInfo pinfo = Parcels[req.ParcelID];
                ParcelInfoReply reply = new ParcelInfoReply();
                reply.AgentID = req.AgentID;
                reply.OwnerID = pinfo.Owner.ID;
                reply.Name = pinfo.Name;
                reply.Description = pinfo.Description;
                reply.ActualArea = pinfo.ActualArea;
                reply.BillableArea = pinfo.BillableArea;
                reply.Flags = pinfo.Flags;
                reply.SimName = RegionData.Name;
                reply.SnapshotID = UUID.Zero;
                reply.Dwell = pinfo.Dwell;
                reply.SalePrice = pinfo.SalePrice;
                reply.AuctionID = pinfo.AuctionID;
                Agents[req.AgentID].SendMessageAlways(reply, ID);
            }
            catch
            {

            }
        }

        public ParcelProperties ParcelInfo2ParcelProperties(UUID agentID, ParcelInfo pinfo, int sequenceId, ParcelProperties.RequestResultType requestResult)
        {
            ParcelProperties prop = new ParcelProperties();
            
            prop.RequestResult = requestResult;
            prop.SequenceID = sequenceId;
            prop.SnapSelection = false;
#warning Implement user-specific counts
            prop.SelfCount = 0; /* TODO: */
            prop.OtherCount = 0;
            prop.PublicCount = 0;
            prop.LocalID = pinfo.LocalID;
            if(prop.IsGroupOwned = pinfo.GroupOwned)
            {
                prop.OwnerID = pinfo.Group.ID;
            }
            else
            {
                prop.OwnerID = pinfo.Owner.ID;
            }
            prop.AuctionID = pinfo.AuctionID;
            prop.ClaimDate = pinfo.ClaimDate;
            prop.ClaimPrice = pinfo.ClaimPrice;
            prop.RentPrice = pinfo.RentPrice;
            prop.AABBMax = pinfo.AABBMax;
            prop.AABBMin = pinfo.AABBMin;
            prop.Bitmap = pinfo.LandBitmap.Data;
            prop.Area = pinfo.Area;
            prop.Status = pinfo.Status;
            prop.SimWideMaxPrims = 15000;
            prop.SimWideTotalPrims = 15000;
            prop.MaxPrims = 15000;
            prop.TotalPrims = 15000;
            prop.OwnerPrims = 0;
            prop.GroupPrims = 0;
            prop.OtherPrims = 0;
            prop.SelectedPrims = 0;
            prop.ParcelPrimBonus = pinfo.ParcelPrimBonus;
            prop.OtherCleanTime = pinfo.OtherCleanTime;
            prop.ParcelFlags = pinfo.Flags;
            prop.SalePrice = pinfo.SalePrice;
            prop.Name = pinfo.Name;
            prop.Description = pinfo.Description;
            if (null != pinfo.MusicURI)
            {
                prop.MusicURL = pinfo.MusicURI;
            }
            else
            {
                prop.MusicURL = "";
            }
            if (null != pinfo.MediaURI)
            {
                prop.MediaURL = pinfo.MediaURI;
            }
            else
            {
                prop.MediaURL = "";
            }
            prop.MediaID = pinfo.MediaID;
            prop.MediaAutoScale = pinfo.MediaAutoScale;
            prop.GroupID = pinfo.Group.ID;
            prop.PassPrice = pinfo.PassPrice;
            prop.PassHours = pinfo.PassHours;
            prop.Category = pinfo.Category;
            prop.AuthBuyerID = pinfo.AuthBuyer.ID;
            prop.SnapshotID = pinfo.SnapshotID;
            prop.UserLocation = pinfo.LandingPosition;
            prop.UserLookAt = pinfo.LandingLookAt;
            prop.LandingType = pinfo.LandingType;
            prop.RegionPushOverride = false;
            prop.RegionDenyAnonymous = false;
            prop.RegionDenyIdentified = false;
            prop.RegionDenyTransacted = false;
            prop.RegionDenyAgeUnverified = false;
            prop.Privacy = false;
            prop.SeeAVs = true;
            prop.AnyAVSounds = true;
            prop.GroupAVSounds = true;
            prop.MediaDesc = "";
            prop.MediaHeight = 0;
            prop.MediaWidth = 0;
            prop.MediaLoop = false;
            prop.MediaType = "";
            prop.ObscureMedia = false;
            prop.ObscureMusic = false;

            return prop;
        }

        [PacketHandler(MessageType.ParcelPropertiesRequest)]
        void HandleParcelPropertiesRequest(Message m)
        {
            Dictionary<UUID, ParcelInfo> results = new Dictionary<UUID, ParcelInfo>();
            ParcelPropertiesRequest req = (ParcelPropertiesRequest)m;
            if(req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            int start_x = (int)(req.West + 0.5);
            int start_y = (int)(req.South + 0.5);
            int end_x = (int)(req.East + 0.5);
            int end_y = (int)(req.North + 0.5);
            if(start_x < 0)
            {
                start_x = 0;
            }
            if(start_y < 0)
            {
                start_y = 0;
            }
            if(end_x >= RegionData.Size.X)
            {
                end_x = (int)RegionData.Size.X - 1;
            }
            if (end_y >= RegionData.Size.Y)
            {
                end_y = (int)RegionData.Size.Y - 1;
            }
            
            for(int x = start_x; x <= end_x; ++x)
            {
                for(int y = start_y; y <= end_y; ++y)
                {
                    ParcelInfo pinfo;
                    try
                    {
                        pinfo = Parcels[new Vector3(x, y, 0)];
                    }
                    catch
                    {
                        continue;
                    }

                    if(!results.ContainsKey(pinfo.ID))
                    {
                        results.Add(pinfo.ID, pinfo);
                    }
                }
            }

            IAgent agent;
            try
            {
                agent = Agents[req.AgentID];
            }
            catch
            {
                return;
            }
            foreach(ParcelInfo pinfo in results.Values)
            {
                ParcelProperties props = ParcelInfo2ParcelProperties(req.AgentID, pinfo, req.SequenceID,
                    (results.Count > 1) ? ParcelProperties.RequestResultType.Multiple : ParcelProperties.RequestResultType.Single);
                props.SnapSelection = req.SnapSelection;
                agent.SendMessageAlways(props, ID);
            }
        }

        [PacketHandler(MessageType.ParcelPropertiesRequestByID)]
        void HandleParcelPropertiesRequestByID(Message m)
        {
            ParcelPropertiesRequestByID req = (ParcelPropertiesRequestByID)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
            try
            {
                ParcelInfo pinfo = Parcels[req.LocalID];

                ParcelProperties props = ParcelInfo2ParcelProperties(req.AgentID, pinfo, req.SequenceID, ParcelProperties.RequestResultType.Single);
                Agents[req.AgentID].SendMessageAlways(props, ID);
            }
            catch
            {

            }
        }
    }
}
