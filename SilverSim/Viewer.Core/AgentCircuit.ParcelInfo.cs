// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Parcel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        [PacketHandler(MessageType.ParcelPropertiesUpdate)]
        public void HandleParcelPropertiesUpdate(Message m)
        {
            ParcelPropertiesUpdate req = (ParcelPropertiesUpdate)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            ParcelInfo pInfo;
            if (Scene.Parcels.TryGetValue(req.LocalID, out pInfo))
            {
                if (Scene.CanEditParcelDetails(Agent.Owner, pInfo))
                {
                    pInfo.Flags = req.ParcelFlags;
                    pInfo.SalePrice = req.SalePrice;
                    pInfo.Name = req.Name;
                    pInfo.Description = req.Description;
                    pInfo.MusicURI = (req.MusicURL.Length != 0) ?
                         new URI(req.MusicURL) : null;

                    pInfo.MediaURI = (req.MediaURL.Length != 0) ?
                        new URI(req.MediaURL) : null;
                    pInfo.MediaAutoScale = req.MediaAutoScale;
                    UGI ugi;
                    if (req.GroupID == UUID.Zero)
                    {
                        ugi = UGI.Unknown;
                    }
                    else if (Scene.GroupsNameService.TryGetValue(req.GroupID, out ugi))
                    {
                        pInfo.Group = ugi;
                    }
                    else
                    {
                        pInfo.Group = UGI.Unknown;
                    }

                    pInfo.PassPrice = req.PassPrice;
                    pInfo.PassHours = req.PassHours;
                    pInfo.Category = req.Category;
                    UUI uui;
                    if (req.AuthBuyerID == UUID.Zero)
                    {
                        pInfo.AuthBuyer = UUI.Unknown;
                    }
                    else if (Scene.AvatarNameService.TryGetValue(req.AuthBuyerID, out uui))
                    {
                        pInfo.AuthBuyer = uui;
                    }
                    else
                    {
                        pInfo.AuthBuyer = UUI.Unknown;
                    }

                    pInfo.SnapshotID = req.SnapshotID;
                    pInfo.LandingPosition = req.UserLocation;
                    pInfo.LandingLookAt = req.UserLookAt;
                    pInfo.LandingType = req.LandingType;
                    Scene.Parcels.Store(pInfo.ID);
                }
            }
        }

        [PacketHandler(MessageType.ParcelDwellRequest)]
        public void HandleParcelDwellRequest(Message m)
        {
            ParcelDwellRequest req = (ParcelDwellRequest)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            ParcelInfo pInfo;
            if (Scene.Parcels.TryGetValue(req.LocalID, out pInfo))
            {
                ParcelDwellReply reply = new ParcelDwellReply();
                reply.AgentID = req.AgentID;
                reply.LocalID = req.LocalID;
                reply.ParcelID = pInfo.ID;
                reply.Dwell = 0;
                SendMessage(reply);
            }
        }

        [PacketHandler(MessageType.ParcelAccessListRequest)]
        public void HandleParcelAccessListRequest(Message m)
        {
            ParcelAccessListRequest req = (ParcelAccessListRequest)m;
            if (req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }

            ParcelInfo pInfo;
            if (Scene.Parcels.TryGetValue(req.LocalID, out pInfo))
            {
                ParcelAccessListReply reply = new ParcelAccessListReply();
                reply.AgentID = req.AgentID;
                reply.SequenceID = req.SequenceID;
                reply.Flags = req.Flags;
                reply.LocalID = req.LocalID;

#warning add parcel access list
                SendMessage(reply);
            }
        }
    }
}
