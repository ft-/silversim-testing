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

using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Parcel;
using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System;
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

        ParcelProperties ParcelInfo2ParcelProperties(UUID agentID, ParcelInfo pinfo, int sequenceId, int requestResult)
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
            prop.MusicURL = pinfo.MusicURI;
            prop.MediaURL = pinfo.MediaURI;
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

        void HandleParcelPropertiesRequestByID(Message m)
        {
            ParcelPropertiesRequestByID req = (ParcelPropertiesRequestByID)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
            ParcelProperties res = new ParcelProperties();
            try
            {
                ParcelInfo pinfo = Parcels[req.LocalID];

                ParcelProperties props = ParcelInfo2ParcelProperties(req.AgentID, pinfo, req.SequenceID, 0);
                Agents[req.AgentID].SendMessageAlways(props, ID);
            }
            catch
            {

            }
        }
    }
}
