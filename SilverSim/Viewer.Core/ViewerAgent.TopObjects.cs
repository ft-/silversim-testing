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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.Land;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent
    {
        [PacketHandler(MessageType.LandStatRequest)]
        public void HandleLandStatRequest(Message m)
        {
            var req = (LandStatRequest)m;
            if (req.CircuitAgentID != req.AgentID ||
                req.CircuitSessionID != req.SessionID)
            {
                return;
            }

            AgentCircuit circuit;
            if (!Circuits.TryGetValue(req.CircuitSceneID, out circuit))
            {
                return;
            }

            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                return;
            }

            UUI agentID = circuit.Agent.Owner;
            if(!scene.IsRegionOwner(agentID) && !scene.IsEstateOwner(agentID) && !scene.IsEstateManager(agentID))
            {
                return;
            }

            switch (req.ReportType)
            {
                case LandStatReportEnum.TopScripts:
                    ProcessTopScripts(circuit, scene, req.RequestFlags, req.Filter);
                    break;

                case LandStatReportEnum.TopColliders:
                    ProcessTopColliders(circuit, scene, req.RequestFlags, req.Filter);
                    break;

                default:
                    break;
            }
        }

        private void EstateOwner_Colliders(AgentCircuit circuit, EstateOwnerMessage req)
        {
            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                return;
            }

            ProcessTopColliders(circuit, scene, LandStatFilterFlags.None, string.Empty);
        }

        private void ProcessTopColliders(AgentCircuit circuit, SceneInterface scene, LandStatFilterFlags flags, string filter)
        { 
            Dictionary<uint, double> colliderScores = scene.PhysicsScene.GetTopColliders();
            var reply = new LandStatReply
            {
                ReportType = LandStatReportEnum.TopColliders,
                RequestFlags = 0,
                TotalObjectCount = (uint)colliderScores.Count
            };

            int allocedlength = 0;

            /* make top objects go first */
            foreach (KeyValuePair<uint, double> kvp in colliderScores.OrderByDescending(x => x.Value))
            {
                if (reply.ReportData.Count == 100)
                {
                    break;
                }
                ObjectPart p;
                try
                {
                    if (!scene.Primitives.TryGetValue(kvp.Key, out p))
                    {
                        continue;
                    }

                    if ((flags & LandStatFilterFlags.FilterByObject) != 0 && !p.Name.Contains(filter))
                    {
                        continue;
                    }

                    if ((flags & LandStatFilterFlags.FilterByOwner) != 0 && !p.Owner.FullName.Contains(filter))
                    {
                        continue;
                    }

                    ParcelInfo pinfo;
                    if((flags & LandStatFilterFlags.FilterByParcelName) != 0 && !scene.Parcels.TryGetValue(p.GlobalPosition, out pinfo) && !pinfo.Name.Equals(filter, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    var entry = new LandStatReply.ReportDataEntry
                    {
                        Location = p.GlobalPosition,
                        Score = kvp.Value,
                        TaskID = p.ID,
                        TaskLocalID = kvp.Key,
                        TaskName = p.Name,
                        OwnerName = p.Owner.FullName
                    };

                    if (allocedlength + entry.MessageLength > 1400)
                    {
                        circuit.SendMessage(reply);
                        reply = new LandStatReply
                        {
                            ReportType = LandStatReportEnum.TopColliders,
                            RequestFlags = 0,
                            TotalObjectCount = (uint)colliderScores.Count
                        };
                    }

                    reply.ReportData.Add(entry);
                    allocedlength += entry.MessageLength;
                }
                catch
                {
                    /* ignore the report */
                }
            }
            circuit.SendMessage(reply);
        }

        private void EstateOwner_Scripts(AgentCircuit circuit, EstateOwnerMessage req)
        {
            SceneInterface scene = circuit.Scene;
            if (scene == null)
            {
                return;
            }

            ProcessTopScripts(circuit, scene, LandStatFilterFlags.None, string.Empty);
        }

        private void ProcessTopScripts(AgentCircuit circuit, SceneInterface scene, LandStatFilterFlags flags, string filter)
        {
            RwLockedDictionary<uint, ScriptReportData> execTimes = scene.ScriptThreadPool.GetExecutionTimes();
            var reply = new LandStatReply
            {
                ReportType = LandStatReportEnum.TopScripts,
                RequestFlags = 0,
                TotalObjectCount = (uint)execTimes.Count
            };

            int allocedlength = 0;

            /* make top objects go first */
            foreach (KeyValuePair<uint, ScriptReportData> kvp in execTimes.OrderByDescending(x => x.Value.Score))
            {
                if (reply.ReportData.Count == 100)
                {
                    break;
                }
                ObjectPart p;
                try
                {
                    if (!scene.Primitives.TryGetValue(kvp.Key, out p))
                    {
                        continue;
                    }

                    if((flags & LandStatFilterFlags.FilterByObject) != 0 && !p.Name.Contains(filter))
                    {
                        continue;
                    }

                    if ((flags & LandStatFilterFlags.FilterByOwner) != 0 && !p.Owner.FullName.Contains(filter))
                    {
                        continue;
                    }

                    ParcelInfo pinfo;
                    if ((flags & LandStatFilterFlags.FilterByParcelName) != 0 && !scene.Parcels.TryGetValue(p.GlobalPosition, out pinfo) && !pinfo.Name.Equals(filter, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    var entry = new LandStatReply.ReportDataEntry
                    {
                        Location = p.GlobalPosition,
                        Score = kvp.Value.Score,
                        TaskID = p.ID,
                        TaskLocalID = kvp.Key,
                        TaskName = p.Name,
                        OwnerName = p.Owner.FullName
                    };

                    if (allocedlength + entry.MessageLength > 1400)
                    {
                        circuit.SendMessage(reply);
                        reply = new LandStatReply
                        {
                            ReportType = 0,
                            RequestFlags = 0,
                            TotalObjectCount = (uint)execTimes.Count
                        };
                    }

                    reply.ReportData.Add(entry);
                    allocedlength += entry.MessageLength;
                }
                catch
                {
                    /* ignore the report */
                }
            }
            circuit.SendMessage(reply);
        }
    }
}
