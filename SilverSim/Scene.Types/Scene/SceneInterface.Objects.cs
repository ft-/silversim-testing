// SilverSim is distributed under the terms of the
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
using SilverSim.Scene.Types.Script.Events;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        [PacketHandler(MessageType.RequestPayPrice)]
        public void HandleRequestPayPrice(Message m)
        {
            RequestPayPrice req = (RequestPayPrice)m;

            IAgent agent;
            if (!Agents.TryGetValue(req.CircuitAgentID, out agent))
            {
                return;
            }

#if DEBUG
            m_Log.DebugFormat("PayPrice localid={0}", req.ObjectID);
#endif

            ObjectPart part;
            if(Primitives.TryGetValue(req.ObjectID, out part))
            {
                PayPriceReply rep = new PayPriceReply();
                rep.ObjectID = req.ObjectID;
                Object.ObjectGroup grp = part.ObjectGroup;
                rep.ButtonData.Add(grp.PayPrice0);
                rep.ButtonData.Add(grp.PayPrice1);
                rep.ButtonData.Add(grp.PayPrice2);
                rep.ButtonData.Add(grp.PayPrice3);
                rep.ButtonData.Add(grp.PayPrice4);

                agent.SendMessageAlways(rep, ID);
            }
        }

        [PacketHandler(MessageType.ObjectSpinStart)]
        public void HandleObjectSpinStart(Message m)
        {
            ObjectSpinStart req = (ObjectSpinStart)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

#if DEBUG
            m_Log.DebugFormat("ObjectSpinStart localid={0}", req.ObjectID);
#endif
        }

        [PacketHandler(MessageType.ObjectSpinUpdate)]
        public void HandleObjectSpinUpdate(Message m)
        {
            ObjectSpinUpdate req = (ObjectSpinUpdate)m;
            if(req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

#if DEBUG
            m_Log.DebugFormat("ObjectSpinUpdate localid={0}", req.ObjectID);
#endif
        }

        [PacketHandler(MessageType.ObjectSpinStop)]
        public void HandleObjectSpinStop(Message m)
        {
            ObjectSpinStop req = (ObjectSpinStop)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

#if DEBUG
            m_Log.DebugFormat("ObjectSpinStop localid={0}", req.ObjectID);
#endif
        }

        [PacketHandler(MessageType.ObjectShape)]
        public void HandleObjectShape(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectShape localid={0}", d.ObjectLocalID);
#endif

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
        public void HandleObjectSaleInfo(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectSaleInfo localid={0}", d.ObjectLocalID);
#endif

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
        public void HandleMultipleObjectUpdate(Message m)
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
#if DEBUG
                m_Log.DebugFormat("MultipleObjectUpdate localid={0}", d.ObjectLocalID);
#endif

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
                    if(CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                    {
                        prim.Size = pos;
                    }
                }
            }
        }

        [PacketHandler(MessageType.ObjectRotation)]
        public void HandleObjectRotation(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectRotation localid={0}", d.ObjectLocalID);
#endif

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
        public void HandleObjectPosition(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectPosition localid={0}", d.ObjectLocalID);
#endif

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
        public void HandleObjectScale(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectScale localid={0}", d.ObjectLocalID);
#endif

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
        public void HandleObjectPermissions(Message m)
        {
            ObjectPermissions req = (ObjectPermissions)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if (!RootAgents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            bool isGod = agent.IsActiveGod && agent.IsInScene(this);

            foreach (ObjectPermissions.Data d in req.ObjectData)
            {
#if DEBUG
                m_Log.DebugFormat("ObjectPermissions localid={0} field={1} change={2} mask=({3})", d.ObjectLocalID, d.Field.ToString(), d.ChangeType.ToString(), d.Mask.ToString());
#endif

                ObjectPart prim;
                if (!Primitives.TryGetValue(d.ObjectLocalID, out prim))
                {
                    continue;
                }
                Object.ObjectGroup grp = prim.ObjectGroup;
                InventoryPermissionsMask setmask = InventoryPermissionsMask.Every;
                if(!isGod)
                {
                    setmask = grp.RootPart.OwnerMask;
                }

                InventoryPermissionsMask clrmask = InventoryPermissionsMask.None;

                switch (d.ChangeType)
                {
                    case ObjectPermissions.ChangeType.Set:
                        setmask &= d.Mask;
                        break;

                    case ObjectPermissions.ChangeType.Clear:
                    default:
                        setmask = InventoryPermissionsMask.None;
                        clrmask = d.Mask;
                        break;
                }

                if (agent.IsActiveGod)
                {
                    foreach(ObjectPart part in grp.ValuesByKey1)
                    {
                        ApplyPermissions(part, d, setmask, clrmask);
                    }
                }
                else if (grp.RootPart.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Modify))
                {
                    ApplyPermissions(grp.RootPart, d, setmask, clrmask);
                }

#if DEBUG
                m_Log.DebugFormat("changed {5} => base=({0}) owner=({1}) group=({2}) everyone=({3}) nextowner=({4})", 
                    grp.RootPart.BaseMask.ToString(),
                    grp.RootPart.OwnerMask.ToString(),
                    grp.RootPart.GroupMask.ToString(),
                    grp.RootPart.EveryoneMask.ToString(),
                    grp.RootPart.NextOwnerMask.ToString(),
                    grp.RootPart.LocalID);
#endif

            }
        }

        void ApplyPermissions(ObjectPart prim, ObjectPermissions.Data d, InventoryPermissionsMask setmask, InventoryPermissionsMask clrmask)
        {
            if ((d.Field & ObjectPermissions.ChangeFieldMask.Base) != 0)
            {
                prim.SetClrBaseMask(setmask, clrmask);
            }
            if ((d.Field & ObjectPermissions.ChangeFieldMask.Everyone) != 0)
            {
                prim.SetClrEveryoneMask(setmask, clrmask);
            }
            if ((d.Field & ObjectPermissions.ChangeFieldMask.Group) != 0)
            {
                prim.SetClrGroupMask(setmask, clrmask);
            }
            if ((d.Field & ObjectPermissions.ChangeFieldMask.NextOwner) != 0)
            {
                prim.SetClrNextOwnerMask(setmask, clrmask);
            }
            if ((d.Field & ObjectPermissions.ChangeFieldMask.Owner) != 0)
            {
                prim.SetClrOwnerMask(setmask, clrmask);
            }
        }

        [PacketHandler(MessageType.ObjectOwner)]
        public void HandleObjectOwner(Message m)
        {
            ObjectOwner req = (ObjectOwner)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            IAgent agent;
            if(!RootAgents.TryGetValue(req.AgentID, out agent) || !agent.IsActiveGod)
            {
                return;
            }

            UUI owner;
            UGI group = UGI.Unknown;

            if(!AvatarNameService.TryGetValue(req.OwnerID, out owner))
            {
                return;
            }

            if(UUID.Zero != group.ID && !GroupsNameService.TryGetValue(req.GroupID, out group))
            {
                return;
            }

            foreach (uint d in req.ObjectList)
            {
#if DEBUG
                m_Log.DebugFormat("ObjectOwner localid={0}", d);
#endif

                ObjectPart prim;
                if (!Primitives.TryGetValue(d, out prim))
                {
                    continue;
                }

                prim.Owner = owner;
                prim.Group = group;
            }
        }

        [PacketHandler(MessageType.ObjectName)]
        public void HandleObjectName(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectName localid={0}", d.ObjectLocalID);
#endif

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
        public void HandleObjectLink(Message m)
        {
            ObjectLink req = (ObjectLink)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectDelink)]
        public void HandleObjectDelink(Message m)
        {
            ObjectDelink req = (ObjectDelink)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectGroup)]
        public void HandleObjectGroup(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectGroup localid={0}", d);
#endif

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
            }
        }

        [PacketHandler(MessageType.ObjectIncludeInSearch)]
        public void HandleObjectIncludeInSearch(Message m)
        {
            ObjectIncludeInSearch req = (ObjectIncludeInSearch)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectFlagUpdate)]
        public void HandleObjectFlagUpdate(Message m)
        {
            ObjectFlagUpdate req = (ObjectFlagUpdate)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectMaterial)]
        public void HandleObjectMaterial(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectMaterial localid={0}", d.ObjectLocalID);
#endif

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
        public void HandleObjectExtraParams(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectExtraParams localid={0}", data.ObjectLocalID);
#endif

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

        [PacketHandler(MessageType.ObjectImage)]
        public void HandleObjectImage(Message m)
        {
            ObjectImage req = (ObjectImage)m;
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

            foreach (ObjectImage.ObjectDataEntry data in req.ObjectData)
            {
#if DEBUG
                m_Log.DebugFormat("ObjectImage localid={0}", data.ObjectLocalID);
#endif

                ObjectPart part;
                if (!Primitives.TryGetValue(data.ObjectLocalID, out part))
                {
                    continue;
                }
                if (!CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
                {
                    continue;
                }

                part.TextureEntryBytes = data.TextureEntry;
                part.MediaURL = data.MediaURL;
            }
        }
        [PacketHandler(MessageType.ObjectExportSelected)]
        public void HandleObjectExportSelected(Message m)
        {
            ObjectExportSelected req = (ObjectExportSelected)m;
            if (req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectSelect)]
        public void HandleObjectSelect(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectSelect localid={0}", primLocalID);
#endif

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
        public void HandleObjectDrop(Message m)
        {
            ObjectDrop req = (ObjectDrop)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectAttach)]
        public void HandleObjectAttach(Message m)
        {
            ObjectAttach req = (ObjectAttach)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectDescription)]
        public void HandleObjectDescription(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectDescription localid={0}", d.ObjectLocalID);
#endif

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
        public void HandleObjectDeselect(Message m)
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
                if (!Primitives.TryGetValue(primLocalID, out part))
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
        public void HandleObjectClickAction(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectClickAction localid={0}", data.ObjectLocalID);
#endif

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
        public void HandleObjectCategory(Message m)
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
#if DEBUG
                m_Log.DebugFormat("ObjectCategory localid={0}", data.ObjectLocalID);
#endif

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
        public void HandleObjectBuy(Message m)
        {
            ObjectBuy req = (ObjectBuy)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.BuyObjectInventory)]
        public void HandleBuyObjectInventory(Message m)
        {
            BuyObjectInventory req = (BuyObjectInventory)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        void PostTouchEvent(ObjectPart part, TouchEvent e)
        {
            if ((part.Flags & SilverSim.Types.Primitive.PrimitiveFlags.Touch) != 0)
            {
                part.PostEvent(e);
            }
            else if (part.LinkNumber != Object.ObjectGroup.LINK_ROOT)
            {
                ObjectPart rootPart = part.ObjectGroup.RootPart;
                if ((rootPart.Flags & SilverSim.Types.Primitive.PrimitiveFlags.Touch) != 0 || part.IsPassTouches)
                {
                    rootPart.PostEvent(e);
                }
            }
        }

        void AddDetectAgentData(IAgent agent, DetectInfo detectdata)
        {
            detectdata.Key = agent.ID;
            detectdata.Group = agent.Group;
            detectdata.Owner = agent.Owner;
            detectdata.Name = agent.Name;
            detectdata.ObjType = agent.DetectedType;
            detectdata.Position = agent.GlobalPosition;
            detectdata.Velocity = agent.Velocity;
            detectdata.Rotation = agent.GlobalRotation;
        }

        [PacketHandler(MessageType.ObjectGrab)]
        public void HandleObjectGrab(Message m)
        {
            ObjectGrab req = (ObjectGrab)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            TouchEvent e = new TouchEvent();
            e.Type = TouchEvent.TouchType.Start;

            ObjectPart part;
            if (!Primitives.TryGetValue(req.ObjectLocalID, out part))
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            DetectInfo detectdata = new DetectInfo();
            AddDetectAgentData(agent, detectdata);
            detectdata.GrabOffset = req.GrabOffset;
            detectdata.LinkNumber = part.LinkNumber;
            if (req.ObjectData.Count > 0)
            {
                ObjectGrab.Data grabdata = req.ObjectData[0];
                detectdata.TouchBinormal = grabdata.Binormal;
                detectdata.TouchFace = grabdata.FaceIndex;
                detectdata.TouchPosition = grabdata.Position;
                detectdata.TouchST = grabdata.STCoord;
                detectdata.TouchUV = grabdata.UVCoord;
            }
            e.Detected.Add(detectdata);

            PostTouchEvent(part, e);
        }

        [PacketHandler(MessageType.ObjectGrabUpdate)]
        public void HandleObjectGrabUpdate(Message m)
        {
            ObjectGrabUpdate req = (ObjectGrabUpdate)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            TouchEvent e = new TouchEvent();
            e.Type = TouchEvent.TouchType.Continuous;

            ObjectPart part;
            if (!Primitives.TryGetValue(req.ObjectLocalID, out part))
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            DetectInfo detectdata = new DetectInfo();
            AddDetectAgentData(agent, detectdata);
            detectdata.GrabOffset = req.GrabPosition;
            detectdata.LinkNumber = part.LinkNumber;
            if (req.ObjectData.Count > 0)
            {
                ObjectGrabUpdate.Data grabdata = req.ObjectData[0];
                detectdata.TouchBinormal = grabdata.Binormal;
                detectdata.TouchFace = grabdata.FaceIndex;
                detectdata.TouchPosition = grabdata.Position;
                detectdata.TouchST = grabdata.STCoord;
                detectdata.TouchUV = grabdata.UVCoord;
            }
            e.Detected.Add(detectdata);

            PostTouchEvent(part, e);
        }

        [PacketHandler(MessageType.ObjectDeGrab)]
        public void HandleObjectDeGrab(Message m)
        {
            ObjectDeGrab req = (ObjectDeGrab)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            TouchEvent e = new TouchEvent();
            e.Type = TouchEvent.TouchType.End;

            ObjectPart part;
            if (!Primitives.TryGetValue(req.ObjectLocalID, out part))
            {
                return;
            }

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            DetectInfo detectdata = new DetectInfo();
            AddDetectAgentData(agent, detectdata);
            detectdata.GrabOffset = Vector3.Zero;
            detectdata.LinkNumber = part.LinkNumber;
            if (req.ObjectData.Count > 0)
            {
                ObjectDeGrab.Data grabdata = req.ObjectData[0];
                detectdata.TouchBinormal = grabdata.Binormal;
                detectdata.TouchFace = grabdata.FaceIndex;
                detectdata.TouchPosition = grabdata.Position;
                detectdata.TouchST = grabdata.STCoord;
                detectdata.TouchUV = grabdata.UVCoord;
            }
            e.Detected.Add(detectdata);

            PostTouchEvent(part, e);
        }

        [PacketHandler(MessageType.RequestObjectPropertiesFamily)]
        public void HandleRequestObjectPropertiesFamily(Message m)
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
