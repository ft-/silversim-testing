﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;
using MultipleObjectUpdate = SilverSim.Viewer.Messages.Object.MultipleObjectUpdate;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Types.Inventory;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        [PacketHandler(MessageType.RequestPayPrice)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleRequestPayPrice(Message m)
        {
            RequestPayPrice req = (RequestPayPrice)m;

            IAgent agent;
            if (!Agents.TryGetValue(req.CircuitAgentID, out agent))
            {
                return;
            }

            ObjectPart part;
            if(Primitives.TryGetValue(req.ObjectID, out part))
            {
                PayPriceReply rep = new PayPriceReply();
                rep.ObjectID = req.ObjectID;
                Object.ObjectGroup grp = part.ObjectGroup;
                rep.ButtonData.Add(grp.PayPrice0);
                rep.ButtonData.Add(grp.PayPrice0);
                rep.ButtonData.Add(grp.PayPrice0);
                rep.ButtonData.Add(grp.PayPrice0);
                rep.ButtonData.Add(grp.PayPrice0);

                agent.SendMessageAlways(rep, ID);
            }
        }

        [PacketHandler(MessageType.ObjectSpinStart)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectSpinStart(Message m)
        {
            ObjectSpinStart req = (ObjectSpinStart)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectSpinUpdate)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectSpinUpdate(Message m)
        {
            ObjectSpinUpdate req = (ObjectSpinUpdate)m;
            if(req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectSpinStop)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectSpinStop(Message m)
        {
            ObjectSpinStop req = (ObjectSpinStop)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectShape)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectShape(Message m)
        {
            ObjectShape req = (ObjectShape)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectShape.Data d in req.ObjectData)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }

                if (!CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }

                ObjectPart.PrimitiveShape shape = prim.Shape;
                shape.PathCurve = d.PathCurve;
                shape.ProfileCurve = d.ProfileCurve;
                shape.PathBegin = d.PathBegin;
                shape.PathEnd = d.PathEnd;
                shape.PathScaleX = d.PathScaleX;
                shape.PathScaleY = d.PathScaleY;
                shape.PathShearX = d.PathShearX;
                shape.PathShearY = d.PathShearY;
                shape.PathTwist = d.PathTwist;
                shape.PathTwistBegin = d.PathTwistBegin;
                shape.PathRadiusOffset = d.PathRadiusOffset;
                shape.PathTaperX = d.PathTaperX;
                shape.PathTaperY = d.PathTaperY;
                shape.PathRevolutions = d.PathRevolutions;
                shape.PathSkew = d.PathSkew;
                shape.ProfileBegin = d.ProfileBegin;
                shape.ProfileEnd = d.ProfileEnd;
                shape.ProfileHollow = d.ProfileHollow;

                prim.Shape = shape;
            }
        }

        [PacketHandler(MessageType.ObjectSaleInfo)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectSaleInfo(Message m)
        {
            ObjectSaleInfo req = (ObjectSaleInfo)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectSaleInfo.Data d in req.ObjectData)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }

                if (!CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }

                prim.ObjectGroup.SalePrice = d.SalePrice;
                prim.ObjectGroup.SaleType = (InventoryItem.SaleInfoData.SaleType)d.SaleType;
            }
        }

        [PacketHandler(MessageType.MultipleObjectUpdate)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleMultipleObjectUpdate(Message m)
        {
            MultipleObjectUpdate req = (MultipleObjectUpdate)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if(!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach(MultipleObjectUpdate.ObjectDataEntry d in req.ObjectData)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }

                int datapos = 0;
                int mindatalength = 0;
                if((d.Flags & MultipleObjectUpdate.UpdateFlags.UpdatePosition) != 0)
                {
                    mindatalength += 12;
                }
                if ((d.Flags & MultipleObjectUpdate.UpdateFlags.UpdateRotation) != 0)
                {
                    mindatalength += 12;
                }
                if ((d.Flags & MultipleObjectUpdate.UpdateFlags.UpdateScale) != 0)
                {
                    mindatalength += 12;
                }
                if(mindatalength > d.Data.Length)
                {
                    continue;
                }

                if ((d.Flags & MultipleObjectUpdate.UpdateFlags.UpdatePosition) != 0)
                {
                    Vector3 pos = new Vector3();
                    pos.FromBytes(d.Data, datapos);
                    datapos += 12;
                    if (prim.ObjectGroup.RootPart != prim && CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                    {
                        prim.Position = pos;
                    }
                    if (prim.ObjectGroup.RootPart == prim && CanMove(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                    {
                        prim.Position = pos;
                    }
                }

                if ((d.Flags & MultipleObjectUpdate.UpdateFlags.UpdateRotation) != 0)
                {
                    Quaternion rot = new Quaternion();
                    rot.FromBytes(d.Data, datapos, true);
                    datapos += 12;
                    if (prim.ObjectGroup.RootPart != prim && CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                    {
                        prim.Rotation = rot;
                    }
                    if (prim.ObjectGroup.RootPart == prim && CanMove(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                    {
                        prim.Rotation = rot;
                    }
                }

                if ((d.Flags & MultipleObjectUpdate.UpdateFlags.UpdateScale) != 0)
                {
                    Vector3 pos = new Vector3();
                    pos.FromBytes(d.Data, datapos);
                    datapos += 12;
                    if(CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                    {
                        prim.Size = pos;
                    }
                }
            }
        }

        [PacketHandler(MessageType.ObjectRotation)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectRotation(Message m)
        {
            ObjectRotation req = (ObjectRotation)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectRotation.Data d in req.ObjectData)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }

                if (prim.ObjectGroup.RootPart != prim && !CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                if (prim.ObjectGroup.RootPart == prim && !CanMove(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.Rotation = d.Rotation;
            }
        }

        [PacketHandler(MessageType.ObjectPosition)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectPosition(Message m)
        {
            ObjectPosition req = (ObjectPosition)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectPosition.ObjectDataEntry d in req.ObjectData)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }

                if (prim.ObjectGroup.RootPart != prim && !CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                if (prim.ObjectGroup.RootPart == prim && !CanMove(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.Position = d.Position;
            }
        }

        [PacketHandler(MessageType.ObjectScale)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectScale(Message m)
        {
            ObjectScale req = (ObjectScale)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectScale.ObjectDataEntry d in req.ObjectData)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }

                if (prim.ObjectGroup.RootPart != prim && !CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                if (prim.ObjectGroup.RootPart == prim && !CanMove(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.Size = d.Size;
            }
        }

        [PacketHandler(MessageType.ObjectPermissions)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectPermissions(Message m)
        {
            ObjectPermissions req = (ObjectPermissions)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectOwner)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectOwner(Message m)
        {
            ObjectOwner req = (ObjectOwner)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectName)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectName(Message m)
        {
            ObjectName req = (ObjectName)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectName.Data d in req.ObjectData)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }

                if (!CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.Name = d.Name;
            }
        }

        [PacketHandler(MessageType.ObjectLink)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectLink(Message m)
        {
            ObjectLink req = (ObjectLink)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectDelink)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectDelink(Message m)
        {
            ObjectDelink req = (ObjectDelink)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectGroup)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectGroup(Message m)
        {
            Viewer.Messages.Object.ObjectGroup req = (Viewer.Messages.Object.ObjectGroup)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach(UInt32 d in req.ObjectList)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d, out prim))
                {
                    continue;
                }

                if (!CanChangeGroup(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.ObjectGroup.Group = new UGI(req.GroupID);
                prim.SendObjectUpdate();
            }
        }

        [PacketHandler(MessageType.ObjectIncludeInSearch)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectIncludeInSearch(Message m)
        {
            ObjectIncludeInSearch req = (ObjectIncludeInSearch)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectFlagUpdate)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectFlagUpdate(Message m)
        {
            ObjectFlagUpdate req = (ObjectFlagUpdate)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectMaterial)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectMaterial(Message m)
        {
            ObjectMaterial req = (ObjectMaterial)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach(ObjectMaterial.Data d in req.ObjectData)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }

                if (!CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.Material = d.Material;
            }
        }

        [PacketHandler(MessageType.ObjectExtraParams)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectExtraParams(Message m)
        {
            ObjectExtraParams req = (ObjectExtraParams)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach(ObjectExtraParams.Data data in req.ObjectData)
            {
                ObjectPart part;
                if(!Primitives.TryGetValue(data.ObjectLocalID, out part))
                {
                    continue;
                }
                if(!CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
                {
                    continue;
                }

            }
        }

        [PacketHandler(MessageType.ObjectExportSelected)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectExportSelected(Message m)
        {
            ObjectExportSelected req = (ObjectExportSelected)m;
            if (req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectSelect)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectSelect(Message m)
        {
            ObjectSelect req = (ObjectSelect)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ObjectPart part;
            int bytelen = 0;
            ObjectProperties props = null;
            foreach(uint primLocalID in req.ObjectData)
            {
                if (!Primitives.TryGetValue(primLocalID, out part))
                {
                    continue;
                }

                byte[] propUpdate = part.PropertiesUpdateData;
                if(null == propUpdate)
                {
                    continue;
                }
                if(bytelen + propUpdate.Length > 1400)
                {
                    agent.SendMessageAlways(props, ID);
                    bytelen = 0;
                }
                props = new ObjectProperties();
                props.ObjectData.Add(propUpdate);
                bytelen += propUpdate.Length;
            }

            if(null != props)
            {
                agent.SendMessageAlways(props, ID);
            }
        }

        [PacketHandler(MessageType.ObjectDrop)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectDrop(Message m)
        {
            ObjectDrop req = (ObjectDrop)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectAttach)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectAttach(Message m)
        {
            ObjectAttach req = (ObjectAttach)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectDescription)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectDescription(Message m)
        {
            ObjectDescription req = (ObjectDescription)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectDescription.Data d in req.ObjectData)
            {
                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }

                if (!CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.Description = d.Description;
            }
        }

        [PacketHandler(MessageType.ObjectDeselect)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectDeselect(Message m)
        {
            ObjectDeselect req = (ObjectDeselect)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            ObjectPart part;
            int bytelen = 0;
            ObjectProperties props = null;
            foreach (uint primLocalID in req.ObjectData)
            {
                if(!Primitives.TryGetValue(primLocalID, out part))
                {
                    continue;
                }

                byte[] propUpdate = part.PropertiesUpdateData;
                if (null == propUpdate)
                {
                    continue;
                }
                if (bytelen + propUpdate.Length > 1400)
                {
                    agent.SendMessageAlways(props, ID);
                    bytelen = 0;
                }
                props = new ObjectProperties();
                props.ObjectData.Add(propUpdate);
                bytelen += propUpdate.Length;
            }

            if (null != props)
            {
                agent.SendMessageAlways(props, ID);
            }
        }

        [PacketHandler(MessageType.ObjectClickAction)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectClickAction(Message m)
        {
            ObjectClickAction req = (ObjectClickAction)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectClickAction.Data data in req.ObjectData)
            {
                ObjectPart part;
                if (Primitives.TryGetValue(data.ObjectLocalID, out part))
                {
                    if (!CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
                    {
                        continue;
                    }

                    part.ClickAction = data.ClickAction;
                }
            }
        }

        [PacketHandler(MessageType.ObjectCategory)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectCategory(Message m)
        {
            ObjectCategory req = (ObjectCategory)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            foreach (ObjectCategory.Data data in req.ObjectData)
            {
                ObjectPart part;
                if (Primitives.TryGetValue(data.ObjectLocalID, out part))
                {
                    if (!CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
                    {
                        continue;
                    }

                    part.ObjectGroup.Category = data.Category;
                }
            }
        }

        [PacketHandler(MessageType.ObjectBuy)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectBuy(Message m)
        {
            ObjectBuy req = (ObjectBuy)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.BuyObjectInventory)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleBuyObjectInventory(Message m)
        {
            BuyObjectInventory req = (BuyObjectInventory)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectGrab)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectGrab(Message m)
        {
            ObjectGrab req = (ObjectGrab)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectGrabUpdate)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectGrabUpdate(Message m)
        {
            ObjectGrabUpdate req = (ObjectGrabUpdate)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectDeGrab)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleObjectDeGrab(Message m)
        {
            ObjectDeGrab req = (ObjectDeGrab)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.RequestObjectPropertiesFamily)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleRequestObjectPropertiesFamily(Message m)
        {
            RequestObjectPropertiesFamily req = (RequestObjectPropertiesFamily)m;
            if(req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }
            ObjectPart part;
            if(!Primitives.TryGetValue(req.ObjectID, out part))
            {
                return;
            }
            ObjectPropertiesFamily res = part.PropertiesFamily;
            res.RequestFlags = req.RequestFlags;
            IAgent agent;
            if (Agents.TryGetValue(req.AgentID, out agent))
            {
                agent.SendMessageAlways(res, ID);
            }
        }
    }
}
