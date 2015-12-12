// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Land;
using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.TerrainEdit
{
    public class ViewerTerrainEdit : IPlugin, IPacketHandlerExtender
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL TERRAIN EDIT");
        
        public ViewerTerrainEdit()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        [PacketHandler(MessageType.ModifyLand)]
        public void HandleMessage(ViewerAgent agent, AgentCircuit circuit, Message m)
        {
            ModifyLand req = (ModifyLand)m;
            if (req.CircuitSessionID != req.SessionID ||
                req.CircuitAgentID != req.AgentID)
            {
                return;
            }
            SceneInterface scene = circuit.Scene;
            if(null == scene)
            {
                return;
            }

            Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data> modifier;
            
            foreach (ModifyLand.Data data in req.ParcelData)
            {
                if (data.South == data.North && data.West == data.East)
                {
                    if (Terraforming.PaintEffects.TryGetValue((Terraforming.StandardTerrainEffect)req.Action, out modifier))
                    {
                        modifier(agent.Owner, scene, req, data);
                    }
                }
                else
                {
                    if (Terraforming.FloodEffects.TryGetValue((Terraforming.StandardTerrainEffect)req.Action, out modifier))
                    {
                        modifier(agent.Owner, scene, req, data);
                    }
                }
            }
        }
    }

    [PluginName("ViewerTerrainEdit")]
    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerTerrainEdit();
        }
    }
}
