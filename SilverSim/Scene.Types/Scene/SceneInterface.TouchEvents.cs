// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {

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
                detectdata.TouchNormal = grabdata.Normal;
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
                detectdata.TouchNormal = grabdata.Normal;
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
                detectdata.TouchNormal = grabdata.Normal;
                detectdata.TouchFace = grabdata.FaceIndex;
                detectdata.TouchPosition = grabdata.Position;
                detectdata.TouchST = grabdata.STCoord;
                detectdata.TouchUV = grabdata.UVCoord;
            }
            e.Detected.Add(detectdata);

            PostTouchEvent(part, e);
        }
    }
}
