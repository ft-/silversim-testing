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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.StructuredData.Llsd;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public sealed class AttachmentResources : ICapabilityInterface
    {
        private readonly SceneInterface m_Scene;
        private readonly ViewerAgent m_Agent;
        private readonly string m_RemoteIP;

        private static readonly Dictionary<AttachmentPoint, string> LocationStrings = new Dictionary<AttachmentPoint, string>
        {
            [AttachmentPoint.Chest] = "Chest",
            [AttachmentPoint.Head] = "Head",
            [AttachmentPoint.LeftShoulder] = "Left Shoulder",
            [AttachmentPoint.RightShoulder] = "Right Shoulder",
            [AttachmentPoint.LeftHand] = "Left Hand",
            [AttachmentPoint.RightHand] = "Right Hand",
            [AttachmentPoint.LeftFoot] = "Left Foot",
            [AttachmentPoint.RightFoot] = "Right Foot",
            [AttachmentPoint.Back] = "Back",
            [AttachmentPoint.Pelvis] = "Pelvis",
            [AttachmentPoint.Mouth] = "Mouth",
            [AttachmentPoint.Chin] = "Chin",
            [AttachmentPoint.LeftEar] = "Left Ear",
            [AttachmentPoint.RightEar] = "Right Ear",
            [AttachmentPoint.LeftEye] = "Left Eye",
            [AttachmentPoint.RightEye] = "Right Eye",
            [AttachmentPoint.Nose] = "Nose",
            [AttachmentPoint.RightUpperArm] = "R Upper Arm",
            [AttachmentPoint.RightLowerArm] = "R Lower Arm",
            [AttachmentPoint.LeftUpperArm] = "L Upper Arm",
            [AttachmentPoint.LeftLowerArm] = "L Lower Arm",
            [AttachmentPoint.RightHip] = "Right Hip",
            [AttachmentPoint.RightUpperLeg] = "R Upper Leg",
            [AttachmentPoint.RightLowerLeg] = "R Lower Leg",
            [AttachmentPoint.LeftHip] = "Left Hip",
            [AttachmentPoint.LeftUpperLeg] = "L Upper Leg",
            [AttachmentPoint.LeftLowerLeg] = "L Lower Leg",
            [AttachmentPoint.Belly] = "Belly",
            [AttachmentPoint.RightPec] = "Right Pec",
            [AttachmentPoint.LeftPec] = "Left Pec",
            [AttachmentPoint.HudCenter2] = "Center 2",
            [AttachmentPoint.HudTopRight] = "Top Right",
            [AttachmentPoint.HudTopCenter] = "Top",
            [AttachmentPoint.HudTopLeft] = "Top Left",
            [AttachmentPoint.HudCenter1] = "Center",
            [AttachmentPoint.HudBottomLeft] = "Bottom Left",
            [AttachmentPoint.HudBottom] = "Bottom",
            [AttachmentPoint.HudBottomRight] = "Bottom Right",
            [AttachmentPoint.Neck] = "Neck",
            [AttachmentPoint.AvatarCenter] = "Avatar Center",
            [AttachmentPoint.LeftHandRing1] = "Left Ring Finger",
            [AttachmentPoint.RightHandRing1] = "Right Ring Finger",
            [AttachmentPoint.TailBase] = "Tail Base",
            [AttachmentPoint.TailTip] = "Tail Tip",
            [AttachmentPoint.LeftWing] = "Left Wing",
            [AttachmentPoint.RightWing] = "Right Wing",
            [AttachmentPoint.FaceJaw] = "Jaw",
            [AttachmentPoint.FaceLeftEar] = "Alt Left Ear",
            [AttachmentPoint.FaceRightEar] = "Alt Right Ear",
            [AttachmentPoint.FaceLeftEye] = "Alt Left Eye",
            [AttachmentPoint.FaceRightEye] = "Alt Right Eye",
            [AttachmentPoint.FaceTongue] = "Tongue",
            [AttachmentPoint.Groin] = "Groin",
            [AttachmentPoint.HindLeftFoot] = "Left Hind Foot",
            [AttachmentPoint.HindRightFoot] = "Right Hind Foot"
        };

        public string CapabilityName => "AttachmentResources";

        public AttachmentResources(ViewerAgent agent, SceneInterface scene, string remoteip)
        {
            m_Agent = agent;
            m_Scene = scene;
            m_RemoteIP = remoteip;
        }

        public void HttpRequestHandler(HttpRequest httpreq)
        {
            if (httpreq.CallerIP != m_RemoteIP)
            {
                httpreq.ErrorResponse(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }

            if(httpreq.Method != "GET")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            var available_summary = new AnArray();
            var used_summary = new AnArray();
            var attachments = new AnArray();

            foreach (ObjectGroup grp in m_Agent.Attachments.All)
            {
                var objects = new AnArray();
                string locationString;
                LocationStrings.TryGetValue(grp.AttachPoint, out locationString);
                attachments.Add(new Map
                {
                    { "location", locationString },
                    { "objects", objects }
                });
                foreach(ObjectPart part in grp.ValuesByKey1)
                {
                    objects.Add(new Map
                    {
                        { "id", part.ID },
                        { "name", part.Name }
                        // urls
                        // memory
                    });
                }
            }

            available_summary.Add(new Map
            {
                { "type", "urls" },
                { "amount", 0 }
            });
            available_summary.Add(new Map
            {
                { "type", "memory" },
                { "amount", 0 }
            });

            used_summary.Add(new Map
            {
                { "type", "urls" },
                { "amount", 0 }
            });
            used_summary.Add(new Map
            {
                { "type", "memory" },
                { "amount", 0 }
            });

            var resdata = new Map
            {
                { "summary", new Map { { "available", available_summary }, { "used", used_summary } } },
                { "attachments", attachments }
            };
            using (HttpResponse res = httpreq.BeginResponse("application/llsd+xml"))
            {
                using (Stream s = res.GetOutputStream())
                {
                    LlsdXml.Serialize(resdata, s);
                }
            }
        }
    }
}
