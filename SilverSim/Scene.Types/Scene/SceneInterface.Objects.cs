// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System;
using MultipleObjectUpdate = SilverSim.Viewer.Messages.Object.MultipleObjectUpdate;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        [PacketHandler(MessageType.RequestPayPrice)]
        void HandleRequestPayPrice(Message m)
        {
            SilverSim.Viewer.Messages.Object.RequestPayPrice req = (SilverSim.Viewer.Messages.Object.RequestPayPrice)m;
        }

        [PacketHandler(MessageType.ObjectSpinStart)]
        void HandleObjectSpinStart(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectSpinStart req = (SilverSim.Viewer.Messages.Object.ObjectSpinStart)m;
        }

        [PacketHandler(MessageType.ObjectSpinUpdate)]
        void HandleObjectSpinUpdate(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectSpinUpdate req = (SilverSim.Viewer.Messages.Object.ObjectSpinUpdate)m;
            if(req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectSpinStop)]
        void HandleObjectSpinStop(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectSpinStop req = (SilverSim.Viewer.Messages.Object.ObjectSpinStop)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectShape)]
        void HandleObjectShape(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectShape req = (SilverSim.Viewer.Messages.Object.ObjectShape)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectSaleInfo)]
        void HandleObjectSaleInfo(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectSaleInfo req = (SilverSim.Viewer.Messages.Object.ObjectSaleInfo)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.MultipleObjectUpdate)]
        void HandleMultipleObjectUpdate(Message m)
        {
            SilverSim.Viewer.Messages.Object.MultipleObjectUpdate req = (SilverSim.Viewer.Messages.Object.MultipleObjectUpdate)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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


            foreach(MultipleObjectUpdate.ObjectDataEntry d in req.ObjectData)
            {
                ObjectPart prim;
                try
                {
                    prim = Primitives[d.ObjectLocalID];
                }
                catch
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

                prim.SendObjectUpdate();
            }
        }

        [PacketHandler(MessageType.ObjectRotation)]
        void HandleObjectRotation(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectRotation req = (SilverSim.Viewer.Messages.Object.ObjectRotation)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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

            foreach (SilverSim.Viewer.Messages.Object.ObjectRotation.Data d in req.ObjectData)
            {
                ObjectPart prim;
                try
                {
                    prim = Primitives[d.ObjectLocalID];
                }
                catch
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
                prim.SendObjectUpdate();
            }
        }

        [PacketHandler(MessageType.ObjectPosition)]
        void HandleObjectPosition(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectPosition req = (SilverSim.Viewer.Messages.Object.ObjectPosition)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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

            foreach (SilverSim.Viewer.Messages.Object.ObjectPosition.ObjectDataEntry d in req.ObjectData)
            {
                ObjectPart prim;
                try
                {
                    prim = Primitives[d.ObjectLocalID];
                }
                catch
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
                prim.SendObjectUpdate();
            }
        }

        [PacketHandler(MessageType.ObjectScale)]
        void HandleObjectScale(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectScale req = (SilverSim.Viewer.Messages.Object.ObjectScale)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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

            foreach (SilverSim.Viewer.Messages.Object.ObjectScale.ObjectDataEntry d in req.ObjectData)
            {
                ObjectPart prim;
                try
                {
                    prim = Primitives[d.ObjectLocalID];
                }
                catch
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
                prim.SendObjectUpdate();
            }
        }

        [PacketHandler(MessageType.ObjectPermissions)]
        void HandleObjectPermissions(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectPermissions req = (SilverSim.Viewer.Messages.Object.ObjectPermissions)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectOwner)]
        void HandleObjectOwner(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectOwner req = (SilverSim.Viewer.Messages.Object.ObjectOwner)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectName)]
        void HandleObjectName(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectName req = (SilverSim.Viewer.Messages.Object.ObjectName)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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

            foreach (SilverSim.Viewer.Messages.Object.ObjectName.Data d in req.ObjectData)
            {
                ObjectPart prim;
                try
                {
                    prim = Primitives[d.ObjectLocalID];
                }
                catch
                {
                    continue;
                }

                if (!CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.Name = d.Name;
                prim.SendObjectUpdate();
            }
        }

        [PacketHandler(MessageType.ObjectLink)]
        void HandleObjectLink(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectLink req = (SilverSim.Viewer.Messages.Object.ObjectLink)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectDelink)]
        void HandleObjectDelink(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectDelink req = (SilverSim.Viewer.Messages.Object.ObjectDelink)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectGroup)]
        void HandleObjectGroup(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectGroup req = (SilverSim.Viewer.Messages.Object.ObjectGroup)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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

            foreach(UInt32 d in req.ObjectList)
            {
                ObjectPart prim;
                try
                {
                    prim = Primitives[d];
                }
                catch
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
        void HandleObjectIncludeInSearch(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectIncludeInSearch req = (SilverSim.Viewer.Messages.Object.ObjectIncludeInSearch)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectFlagUpdate)]
        void HandleObjectFlagUpdate(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectFlagUpdate req = (SilverSim.Viewer.Messages.Object.ObjectFlagUpdate)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectMaterial)]
        void HandleObjectMaterial(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectMaterial req = (SilverSim.Viewer.Messages.Object.ObjectMaterial)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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

            foreach(SilverSim.Viewer.Messages.Object.ObjectMaterial.Data d in req.ObjectData)
            {
                ObjectPart prim;
                try
                {
                    prim = Primitives[d.ObjectLocalID];
                }
                catch
                {
                    continue;
                }

                if (!CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.Material = d.Material;
                prim.SendObjectUpdate();
            }
        }

        [PacketHandler(MessageType.ObjectExtraParams)]
        void HandleObjectExtraParams(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectExtraParams req = (SilverSim.Viewer.Messages.Object.ObjectExtraParams)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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
        }

        [PacketHandler(MessageType.ObjectExportSelected)]
        void HandleObjectExportSelected(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectExportSelected req = (SilverSim.Viewer.Messages.Object.ObjectExportSelected)m;
            if (req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectSelect)]
        void HandleObjectSelect(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectSelect req = (SilverSim.Viewer.Messages.Object.ObjectSelect)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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

            ObjectPart part;
            int bytelen = 0;
            ObjectProperties props = null;
            foreach(uint primLocalID in req.ObjectData)
            {
                try
                {
                    part = Primitives[primLocalID];
                }
                catch
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
        void HandleObjectDrop(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectDrop req = (SilverSim.Viewer.Messages.Object.ObjectDrop)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectAttach)]
        void HandleObjectAttach(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectAttach req = (SilverSim.Viewer.Messages.Object.ObjectAttach)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectDescription)]
        void HandleObjectDescription(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectDescription req = (SilverSim.Viewer.Messages.Object.ObjectDescription)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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

            foreach (SilverSim.Viewer.Messages.Object.ObjectDescription.Data d in req.ObjectData)
            {
                ObjectPart prim;
                try
                {
                    prim = Primitives[d.ObjectLocalID];
                }
                catch
                {
                    continue;
                }

                if (!CanEdit(agent, prim.ObjectGroup, prim.ObjectGroup.GlobalPosition))
                {
                    continue;
                }
                prim.Description = d.Description;
                prim.SendObjectUpdate();
            }
        }

        [PacketHandler(MessageType.ObjectDeselect)]
        void HandleObjectDeselect(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectDeselect req = (SilverSim.Viewer.Messages.Object.ObjectDeselect)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
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

            ObjectPart part;
            int bytelen = 0;
            ObjectProperties props = null;
            foreach (uint primLocalID in req.ObjectData)
            {
                try
                {
                    part = Primitives[primLocalID];
                }
                catch
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
        void HandleObjectClickAction(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectClickAction req = (SilverSim.Viewer.Messages.Object.ObjectClickAction)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectCategory)]
        void HandleObjectCategory(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectCategory req = (SilverSim.Viewer.Messages.Object.ObjectCategory)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectBuy)]
        void HandleObjectBuy(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectBuy req = (SilverSim.Viewer.Messages.Object.ObjectBuy)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.BuyObjectInventory)]
        void HandleBuyObjectInventory(Message m)
        {
            SilverSim.Viewer.Messages.Object.BuyObjectInventory req = (SilverSim.Viewer.Messages.Object.BuyObjectInventory)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectGrab)]
        void HandleObjectGrab(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectGrab req = (SilverSim.Viewer.Messages.Object.ObjectGrab)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectGrabUpdate)]
        void HandleObjectGrabUpdate(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectGrabUpdate req = (SilverSim.Viewer.Messages.Object.ObjectGrabUpdate)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.ObjectDeGrab)]
        void HandleObjectDeGrab(Message m)
        {
            SilverSim.Viewer.Messages.Object.ObjectDeGrab req = (SilverSim.Viewer.Messages.Object.ObjectDeGrab)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
        }

        [PacketHandler(MessageType.RequestObjectPropertiesFamily)]
        void HandleRequestObjectPropertiesFamily(Message m)
        {
            SilverSim.Viewer.Messages.Object.RequestObjectPropertiesFamily req = (SilverSim.Viewer.Messages.Object.RequestObjectPropertiesFamily)m;
            if(req.AgentID != req.CircuitAgentID ||
                req.SessionID != req.CircuitSessionID)
            {
                return;
            }
            ObjectPart part;
            try
            {
                part = Primitives[req.ObjectID];
            }
            catch
            {
                return;
            }
            ObjectPropertiesFamily res = part.PropertiesFamily;
            res.RequestFlags = req.RequestFlags;
            try
            {
                IAgent agent = Agents[req.AgentID];
                agent.SendMessageAlways(res, ID);
            }
            catch
            {

            }
        }
    }
}
