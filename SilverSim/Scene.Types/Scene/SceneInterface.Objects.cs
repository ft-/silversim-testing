// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        private class ObjectPropertiesSendHandler : IDisposable
        {
            private ObjectProperties m_Props;
            private int m_Bytelen;
            private readonly IAgent m_Agent;
            private readonly UUID m_SceneID;

            public ObjectPropertiesSendHandler(IAgent agent, UUID sceneID)
            {
                m_Agent = agent;
                m_SceneID = sceneID;
            }

            public void Send(ObjectPart part)
            {
                var propUpdate = part.PropertiesUpdateData;
                if (propUpdate == null)
                {
                    return;
                }

                if (m_Bytelen + propUpdate.Length > 1400)
                {
                    m_Agent.SendMessageAlways(m_Props, m_SceneID);
                    m_Bytelen = 0;
                    m_Props = null;
                }

                if (m_Props == null)
                {
                    m_Props = new ObjectProperties();
                }

                m_Props.ObjectData.Add(propUpdate);
                m_Bytelen += propUpdate.Length;
            }

            public void Dispose()
            {
                if(m_Props != null)
                {
                    m_Agent.SendMessageAlways(m_Props, m_SceneID);
                    m_Bytelen = 0;
                    m_Props = null;
                }
            }
        }

        [PacketHandler(MessageType.RequestPayPrice)]
        public void HandleRequestPayPrice(Message m)
        {
            var req = (RequestPayPrice)m;

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
                var rep = new PayPriceReply()
                {
                    ObjectID = req.ObjectID
                };
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
            var req = (ObjectSpinUpdate)m;
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
            var req = (ObjectSpinStop)m;
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
            var req = (ObjectShape)m;
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

        [PacketHandler(MessageType.MultipleObjectUpdate)]
        public void HandleMultipleObjectUpdate(Message m)
        {
            var req = (MultipleObjectUpdate)m;
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
                    var pos = new Vector3();
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
                    var rot = new Quaternion();
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
            var req = (ObjectRotation)m;
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
            var req = (ObjectPosition)m;
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
            var req = (ObjectScale)m;
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
            var req = (ObjectPermissions)m;
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
            using (ObjectPropertiesSendHandler propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
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
                    var grp = prim.ObjectGroup;
                    var setmask = InventoryPermissionsMask.Every;
                    if (!isGod)
                    {
                        setmask = grp.RootPart.OwnerMask;
                    }

                    var clrmask = InventoryPermissionsMask.None;

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
                        foreach (ObjectPart part in grp.ValuesByKey1)
                        {
                            ApplyPermissions(part, d, setmask, clrmask, propHandler);
                        }
                    }
                    else if (grp.RootPart.CheckPermissions(agent.Owner, agent.Group, InventoryPermissionsMask.Modify))
                    {
                        ApplyPermissions(grp.RootPart, d, setmask, clrmask, propHandler);
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
        }

        private void ApplyPermissions(ObjectPart prim, ObjectPermissions.Data d, InventoryPermissionsMask setmask, InventoryPermissionsMask clrmask,
            ObjectPropertiesSendHandler propHandler)
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
            propHandler.Send(prim);
        }

        [PacketHandler(MessageType.ObjectOwner)]
        public void HandleObjectOwner(Message m)
        {
            var req = (ObjectOwner)m;
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

            using (ObjectPropertiesSendHandler propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
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

                    propHandler.Send(prim);
                }
            }
        }

        [PacketHandler(MessageType.ObjectName)]
        public void HandleObjectName(Message m)
        {
            var req = (ObjectName)m;
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

            using (var propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
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

                    propHandler.Send(prim);
                }
            }
        }

        [PacketHandler(MessageType.ObjectLink)]
        public void HandleObjectLink(Message m)
        {
            var req = (ObjectLink)m;
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

            List<UUID> primids = new List<UUID>();
            foreach (uint id in req.ObjectList)
            {
                ObjectPart part;
                if (Primitives.TryGetValue(id, out part) &&
                    CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
                {
                    primids.Add(part.ID);
                }
            }

            LinkObjects(primids);
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

            IAgent agent;
            if (!Agents.TryGetValue(req.AgentID, out agent))
            {
                return;
            }

            var primids = new List<UUID>();
            foreach(uint id in req.ObjectList)
            {
                ObjectPart part;
                if(Primitives.TryGetValue(id, out part) &&
                    CanEdit(agent, part.ObjectGroup, part.ObjectGroup.GlobalPosition))
                {
                    primids.Add(part.ID);
                }
            }

            UnlinkObjects(primids);
        }

        [PacketHandler(MessageType.ObjectGroup)]
        public void HandleObjectGroup(Message m)
        {
            var req = (Viewer.Messages.Object.ObjectGroup)m;
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

            using (var propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
                foreach (UInt32 d in req.ObjectList)
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
                    propHandler.Send(prim);
                }
            }
        }

        [PacketHandler(MessageType.ObjectIncludeInSearch)]
        public void HandleObjectIncludeInSearch(Message m)
        {
            var req = (ObjectIncludeInSearch)m;
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

            using (var propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
                foreach (ObjectIncludeInSearch.Data d in req.ObjectData)
                {
                    ObjectPart part;
                    if (!Primitives.TryGetValue(d.ObjectLocalID, out part))
                    {
                        continue;
                    }

                    part.ObjectGroup.IsIncludedInSearch = d.IncludeInSearch;
                    propHandler.Send(part);
                }
            }
        }

        [PacketHandler(MessageType.ObjectFlagUpdate)]
        public void HandleObjectFlagUpdate(Message m)
        {
            var req = (ObjectFlagUpdate)m;
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
            if (!Primitives.TryGetValue(req.ObjectLocalID, out part))
            {
                return;
            }

            Object.ObjectGroup grp = part.ObjectGroup;
            if(grp == null)
            {
                return;
            }

#if DEBUG
            m_Log.DebugFormat("ObjectFlagUpdate localid={0} isphantom={1} istemporary={2} usephysics={3}", req.ObjectLocalID, req.IsPhantom, req.IsTemporary, req.UsePhysics);
#endif

            grp.IsPhantom = req.IsPhantom;
            grp.IsTempOnRez = req.IsTemporary;
            grp.IsPhysics = req.UsePhysics;
            if(!req.IsTemporary && grp.IsTemporary)
            {
                grp.IsTemporary = false;
            }
            if(req.ExtraPhysics.Count != 0)
            {
                ObjectFlagUpdate.ExtraPhysicsData d = req.ExtraPhysics[0];
                part.PhysicsShapeType = d.PhysicsShapeType;
                part.PhysicsDensity = d.Density;
                part.PhysicsFriction = d.Friction;
                part.PhysicsRestitution = d.Restitution;
                part.PhysicsGravityMultiplier = d.GravityMultiplier;
            }
        }

        [PacketHandler(MessageType.ObjectMaterial)]
        public void HandleObjectMaterial(Message m)
        {
            var req = (ObjectMaterial)m;
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

            foreach (ObjectMaterial.Data d in req.ObjectData)
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
            const ushort FlexiEP = 0x10;
            const ushort LightEP = 0x20;
            const ushort SculptEP = 0x30;
            const ushort ProjectionEP = 0x40;

            var req = (ObjectExtraParams)m;
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

            foreach(var data in req.ObjectData)
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

                switch(data.ParamType)
                {
                    case FlexiEP:
                        if(!data.ParamInUse)
                        {
                            ObjectPart.FlexibleParam flexi = part.Flexible;
                            flexi.IsFlexible = false;
                            part.Flexible = flexi;
                        }
                        else
                        {
                            part.Flexible = ObjectPart.FlexibleParam.FromUdpDataBlock(data.ParamData);
                        }
                        break;

                    case LightEP:
                        if(!data.ParamInUse)
                        {
                            ObjectPart.PointLightParam light = part.PointLight;
                            light.IsLight = false;
                            part.PointLight = light;
                        }
                        else
                        {
                            part.PointLight = ObjectPart.PointLightParam.FromUdpDataBlock(data.ParamData);
                        }
                        break;

                    case SculptEP:
                        if(data.ParamInUse && data.ParamData.Length >= 17)
                        {
                            byte[] param = data.ParamData;
                            ObjectPart.PrimitiveShape shape = part.Shape;
                            shape.SculptMap = new UUID(param, 0);
                            shape.IsSculptInverted = (param[16] & 0x40) != 0;
                            shape.IsSculptMirrored = (param[16] & 0x80) != 0;
                            part.Shape = shape;
                        }
                        break;

                    case ProjectionEP:
                        if(!data.ParamInUse)
                        {
                            ObjectPart.ProjectionParam proj = part.Projection;
                            proj.IsProjecting = false;
                            part.Projection = proj;
                        }
                        else
                        {
                            part.Projection = ObjectPart.ProjectionParam.FromUdpDataBlock(data.ParamData);
                        }
                        break;
                }
            }
        }

        [PacketHandler(MessageType.ObjectImage)]
        public void HandleObjectImage(Message m)
        {
            var req = (ObjectImage)m;
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
            var req = (ObjectExportSelected)m;
            if (req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectSelect)]
        public void HandleObjectSelect(Message m)
        {
            var req = (ObjectSelect)m;
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
            var selectedObjects = agent.SelectedObjects(ID);

            foreach (uint primLocalID in req.ObjectData)
            {
#if DEBUG
                m_Log.DebugFormat("ObjectSelect localid={0}", primLocalID);
#endif

                if (!Primitives.TryGetValue(primLocalID, out part))
                {
                    continue;
                }

                if (!selectedObjects.Contains(part.ID))
                {
                    selectedObjects.Add(part.ID);
                    agent.ScheduleUpdate(part.UpdateInfo, ID);
                }
            }
        }

        [PacketHandler(MessageType.ObjectDrop)]
        public void HandleObjectDrop(Message m)
        {
            var req = (ObjectDrop)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
#warning Implement attachment drop
        }

        [PacketHandler(MessageType.ObjectAttach)]
        public void HandleObjectAttach(Message m)
        {
            var req = (ObjectAttach)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
#warning Implement attach object from region
        }

        [PacketHandler(MessageType.ObjectDescription)]
        public void HandleObjectDescription(Message m)
        {
            var req = (ObjectDescription)m;
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

            using (var propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
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
                    propHandler.Send(prim);
                }
            }
        }

        [PacketHandler(MessageType.ObjectDeselect)]
        public void HandleObjectDeselect(Message m)
        {
            var req = (ObjectDeselect)m;
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
            RwLockedList<UUID> selectedObjects = agent.SelectedObjects(ID);

            foreach (uint primLocalID in req.ObjectData)
            {
                if (!Primitives.TryGetValue(primLocalID, out part))
                {
                    continue;
                }

                selectedObjects.Remove(part.ID);
                agent.ScheduleUpdate(part.UpdateInfo, ID);
            }
        }

        [PacketHandler(MessageType.ObjectClickAction)]
        public void HandleObjectClickAction(Message m)
        {
            var req = (ObjectClickAction)m;
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

            using (var propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
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
                        propHandler.Send(part);
                    }
                }
            }
        }

        [PacketHandler(MessageType.ObjectCategory)]
        public void HandleObjectCategory(Message m)
        {
            var req = (ObjectCategory)m;
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

            using (var propHandler = new ObjectPropertiesSendHandler(agent, ID))
            {
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
                        propHandler.Send(part);
                    }
                }
            }
        }

        [PacketHandler(MessageType.RequestObjectPropertiesFamily)]
        public void HandleRequestObjectPropertiesFamily(Message m)
        {
            var req = (RequestObjectPropertiesFamily)m;
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
