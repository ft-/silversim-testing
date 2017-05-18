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
using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
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
            var req = (ObjectGrab)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            var e = new TouchEvent()
            {
                Detected = new List<DetectInfo>(),
                Type = TouchEvent.TouchType.Start
            };
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

            var detectdata = new DetectInfo();
            AddDetectAgentData(agent, detectdata);
            detectdata.GrabOffset = req.GrabOffset;
            detectdata.LinkNumber = part.LinkNumber;
            if (req.ObjectData.Count > 0)
            {
                var grabdata = req.ObjectData[0];
                detectdata.TouchBinormal = grabdata.Binormal;
                detectdata.TouchNormal = grabdata.Normal;
                detectdata.TouchFace = grabdata.FaceIndex;
                detectdata.TouchPosition = grabdata.Position;
                detectdata.TouchST = grabdata.STCoord;
                detectdata.TouchUV = grabdata.UVCoord;
            }
            e.Detected.Add(detectdata);

            part.PostTouchEvent(e);
        }

        [PacketHandler(MessageType.ObjectGrabUpdate)]
        public void HandleObjectGrabUpdate(Message m)
        {
            var req = (ObjectGrabUpdate)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            var e = new TouchEvent()
            {
                Detected = new List<DetectInfo>(),
                Type = TouchEvent.TouchType.Continuous
            };
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

            var detectdata = new DetectInfo();
            AddDetectAgentData(agent, detectdata);
            detectdata.GrabOffset = req.GrabPosition;
            detectdata.LinkNumber = part.LinkNumber;
            if (req.ObjectData.Count > 0)
            {
                var grabdata = req.ObjectData[0];
                detectdata.TouchBinormal = grabdata.Binormal;
                detectdata.TouchNormal = grabdata.Normal;
                detectdata.TouchFace = grabdata.FaceIndex;
                detectdata.TouchPosition = grabdata.Position;
                detectdata.TouchST = grabdata.STCoord;
                detectdata.TouchUV = grabdata.UVCoord;
            }
            e.Detected.Add(detectdata);

            part.PostTouchEvent(e);
        }

        [PacketHandler(MessageType.ObjectDeGrab)]
        public void HandleObjectDeGrab(Message m)
        {
            var req = (ObjectDeGrab)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }

            var e = new TouchEvent()
            {
                Detected = new List<DetectInfo>(),
                Type = TouchEvent.TouchType.End
            };
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

            var detectdata = new DetectInfo();
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

            part.PostTouchEvent(e);
        }
    }
}
