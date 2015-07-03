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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.LL.Messages;
using SilverSim.Scene.Types.Object;
using SilverSim.LL.Messages.Object;
using SilverSim.Scene.Types.Agent;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        [PacketHandler(MessageType.RequestPayPrice)]
        void HandleRequestPayPrice(Message m)
        {
            SilverSim.LL.Messages.Object.RequestPayPrice req = (SilverSim.LL.Messages.Object.RequestPayPrice)m;
        }

        [PacketHandler(MessageType.ObjectSpinStart)]
        void HandleObjectSpinStart(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSpinStart req = (SilverSim.LL.Messages.Object.ObjectSpinStart)m;
        }

        [PacketHandler(MessageType.ObjectSpinUpdate)]
        void HandleObjectSpinUpdate(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSpinUpdate req = (SilverSim.LL.Messages.Object.ObjectSpinUpdate)m;
        }

        [PacketHandler(MessageType.ObjectSpinStop)]
        void HandleObjectSpinStop(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSpinStop req = (SilverSim.LL.Messages.Object.ObjectSpinStop)m;
        }

        [PacketHandler(MessageType.ObjectShape)]
        void HandleObjectShape(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectShape req = (SilverSim.LL.Messages.Object.ObjectShape)m;
        }

        [PacketHandler(MessageType.ObjectSaleInfo)]
        void HandleObjectSaleInfo(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSaleInfo req = (SilverSim.LL.Messages.Object.ObjectSaleInfo)m;
        }

        [PacketHandler(MessageType.ObjectRotation)]
        void HandleObjectRotation(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectRotation req = (SilverSim.LL.Messages.Object.ObjectRotation)m;
        }

        [PacketHandler(MessageType.ObjectPermissions)]
        void HandleObjectPermissions(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectPermissions req = (SilverSim.LL.Messages.Object.ObjectPermissions)m;
        }

        [PacketHandler(MessageType.ObjectOwner)]
        void HandleObjectOwner(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectOwner req = (SilverSim.LL.Messages.Object.ObjectOwner)m;
        }

        [PacketHandler(MessageType.ObjectName)]
        void HandleObjectName(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectName req = (SilverSim.LL.Messages.Object.ObjectName)m;
        }

        [PacketHandler(MessageType.ObjectLink)]
        void HandleObjectLink(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectLink req = (SilverSim.LL.Messages.Object.ObjectLink)m;
        }

        [PacketHandler(MessageType.ObjectDelink)]
        void HandleObjectDelink(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDelink req = (SilverSim.LL.Messages.Object.ObjectDelink)m;
        }

        [PacketHandler(MessageType.ObjectGroup)]
        void HandleObjectGroup(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectGroup req = (SilverSim.LL.Messages.Object.ObjectGroup)m;
        }

        [PacketHandler(MessageType.ObjectIncludeInSearch)]
        void HandleObjectIncludeInSearch(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectIncludeInSearch req = (SilverSim.LL.Messages.Object.ObjectIncludeInSearch)m;
        }

        [PacketHandler(MessageType.ObjectFlagUpdate)]
        void HandleObjectFlagUpdate(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectFlagUpdate req = (SilverSim.LL.Messages.Object.ObjectFlagUpdate)m;
        }

        [PacketHandler(MessageType.ObjectMaterial)]
        void HandleObjectMaterial(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectMaterial req = (SilverSim.LL.Messages.Object.ObjectMaterial)m;
        }

        [PacketHandler(MessageType.ObjectExtraParams)]
        void HandleObjectExtraParams(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectExtraParams req = (SilverSim.LL.Messages.Object.ObjectExtraParams)m;
        }

        [PacketHandler(MessageType.ObjectExportSelected)]
        void HandleObjectExportSelected(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectExportSelected req = (SilverSim.LL.Messages.Object.ObjectExportSelected)m;
        }

        [PacketHandler(MessageType.ObjectSelect)]
        void HandleObjectSelect(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectSelect req = (SilverSim.LL.Messages.Object.ObjectSelect)m;
            if(req.CircuitAgentID == req.AgentID &&
                req.CircuitSessionID == req.SessionID)
            {
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
        }

        [PacketHandler(MessageType.ObjectDrop)]
        void HandleObjectDrop(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDrop req = (SilverSim.LL.Messages.Object.ObjectDrop)m;
        }

        [PacketHandler(MessageType.ObjectAttach)]
        void HandleObjectAttach(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectAttach req = (SilverSim.LL.Messages.Object.ObjectAttach)m;
        }

        [PacketHandler(MessageType.ObjectDetach)]
        void HandleObjectDetach(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDetach req = (SilverSim.LL.Messages.Object.ObjectDetach)m;
        }

        [PacketHandler(MessageType.ObjectDescription)]
        void HandleObjectDescription(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDescription req = (SilverSim.LL.Messages.Object.ObjectDescription)m;
        }

        [PacketHandler(MessageType.ObjectDeselect)]
        void HandleObjectDeselect(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDeselect req = (SilverSim.LL.Messages.Object.ObjectDeselect)m;
            if (req.CircuitAgentID == req.AgentID &&
                req.CircuitSessionID == req.SessionID)
            {
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
        }

        [PacketHandler(MessageType.ObjectClickAction)]
        void HandleObjectClickAction(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectClickAction req = (SilverSim.LL.Messages.Object.ObjectClickAction)m;
        }

        [PacketHandler(MessageType.ObjectCategory)]
        void HandleObjectCategory(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectCategory req = (SilverSim.LL.Messages.Object.ObjectCategory)m;
        }

        [PacketHandler(MessageType.ObjectBuy)]
        void HandleObjectBuy(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectBuy req = (SilverSim.LL.Messages.Object.ObjectBuy)m;
        }

        [PacketHandler(MessageType.BuyObjectInventory)]
        void HandleBuyObjectInventory(Message m)
        {
            SilverSim.LL.Messages.Object.BuyObjectInventory req = (SilverSim.LL.Messages.Object.BuyObjectInventory)m;
        }

        [PacketHandler(MessageType.ObjectGrab)]
        void HandleObjectGrab(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectGrab req = (SilverSim.LL.Messages.Object.ObjectGrab)m;
        }

        [PacketHandler(MessageType.ObjectGrabUpdate)]
        void HandleObjectGrabUpdate(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectGrabUpdate req = (SilverSim.LL.Messages.Object.ObjectGrabUpdate)m;
        }

        [PacketHandler(MessageType.ObjectDeGrab)]
        void HandleObjectDeGrab(Message m)
        {
            SilverSim.LL.Messages.Object.ObjectDeGrab req = (SilverSim.LL.Messages.Object.ObjectDeGrab)m;
        }

        [PacketHandler(MessageType.RequestObjectPropertiesFamily)]
        void HandleRequestObjectPropertiesFamily(Message m)
        {
            SilverSim.LL.Messages.Object.RequestObjectPropertiesFamily req = (SilverSim.LL.Messages.Object.RequestObjectPropertiesFamily)m;
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
