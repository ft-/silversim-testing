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

using log4net;
using Nini.Config;
using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.LL.Messages.Land;
using SilverSim.LL.Messages.LayerData;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilverSim.LL.TerrainEdit
{
    public class ViewerTerrainEdit : IPlugin, IPacketHandlerExtender
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL TERRAIN EDIT");

        public enum StandardTerrainEffect
        {
            Flatten = 0,
            Raise = 1,
            Lower = 2,
            Smooth = 3,
            Noise = 4,
            Revert = 5,

            Erode = 255,
            Weather = 254,
            Olsen = 253
        }
        
        [AttributeUsage(AttributeTargets.Method, Inherited = false)]
        class PaintEffect : Attribute
        {
            public StandardTerrainEffect Effect;

            public PaintEffect(StandardTerrainEffect effect)
            {
                Effect = effect;
            }
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false)]
        class FloodEffect : Attribute
        {
            public StandardTerrainEffect Effect;

            public FloodEffect(StandardTerrainEffect effect)
            {
                Effect = effect;
            }
        }

        public delegate void ModifyLandEffect(LLAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data);

        Dictionary<StandardTerrainEffect, ModifyLandEffect> m_PaintEffects = new Dictionary<StandardTerrainEffect, ModifyLandEffect>();
        Dictionary<StandardTerrainEffect, ModifyLandEffect> m_FloodEffects = new Dictionary<StandardTerrainEffect, ModifyLandEffect>();

        public ViewerTerrainEdit()
        {
            foreach(MethodInfo mi in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
            {
                PaintEffect pe = (PaintEffect)Attribute.GetCustomAttribute(mi, typeof(PaintEffect));
                if(pe == null)
                {
                    continue;
                }
                else if(m_PaintEffects.ContainsKey(pe.Effect))
                {
                    m_Log.FatalFormat("Method {0} defines duplicate paint effect {1}", mi.Name, pe.Effect.ToString());
                }
                else if(mi.ReturnType != typeof(void))
                {
                    m_Log.FatalFormat("Method {0} does not have return type void", mi.Name);
                }
                else if(mi.GetParameters().Length != 4)
                {
                    m_Log.FatalFormat("Method {0} does not match in parameter count", mi.Name);
                }
                else if(mi.GetParameters()[0].ParameterType != typeof(LLAgent))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else if (mi.GetParameters()[1].ParameterType != typeof(SceneInterface))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else if (mi.GetParameters()[2].ParameterType != typeof(ModifyLand))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else if (mi.GetParameters()[3].ParameterType != typeof(ModifyLand.Data))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else
                {
                    m_PaintEffects.Add(pe.Effect, (ModifyLandEffect)Delegate.CreateDelegate(typeof(ModifyLandEffect), this, mi));
                }
                FloodEffect fe = (FloodEffect)Attribute.GetCustomAttribute(mi, typeof(FloodEffect));
                if (fe == null)
                {
                    continue;
                }
                else if (m_PaintEffects.ContainsKey(fe.Effect))
                {
                    m_Log.FatalFormat("Method {0} defines duplicate flood effect {1}", mi.Name, fe.Effect.ToString());
                }
                else if (mi.ReturnType != typeof(void))
                {
                    m_Log.FatalFormat("Method {0} does not have return type void", mi.Name);
                }
                else if (mi.GetParameters().Length != 4)
                {
                    m_Log.FatalFormat("Method {0} does not match in parameter count", mi.Name);
                }
                else if (mi.GetParameters()[0].ParameterType != typeof(LLAgent))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else if (mi.GetParameters()[1].ParameterType != typeof(SceneInterface))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else if (mi.GetParameters()[2].ParameterType != typeof(ModifyLand))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else if (mi.GetParameters()[3].ParameterType != typeof(ModifyLand.Data))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else
                {
                    m_PaintEffects.Add(fe.Effect, (ModifyLandEffect)Delegate.CreateDelegate(typeof(ModifyLandEffect), this, mi));
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        [PacketHandler(MessageType.ModifyLand)]
        void HandleMessage(LLAgent agent, Circuit circuit, Message m)
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

            ModifyLandEffect modifier;
            
            foreach (ModifyLand.Data data in req.ParcelData)
            {
                if (data.South == data.North && data.West == data.East)
                {
                    if (m_PaintEffects.TryGetValue((StandardTerrainEffect)req.Action, out modifier))
                    {
                        modifier(agent, scene, req, data);
                    }
                }
                else
                {
                    if (m_FloodEffects.TryGetValue((StandardTerrainEffect)req.Action, out modifier))
                    {
                        modifier(agent, scene, req, data);
                    }
                }
            }
        }

        [PaintEffect(StandardTerrainEffect.Raise)]
        void RaiseSphere(LLAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();
            int s = (int)(Math.Pow(2, modify.BrushSize) + 0.5);

            int x;
            int rx = (int)(data.West + 0.5);
            int ry = (int)(data.South + 0.5);
            int xFrom = (int)(rx - s + 0.5);
            int xTo = (int)(rx + s + 0.5) + 1;
            int yFrom = (int)(ry - s + 0.5);
            int yTo = (int)(ry + s + 0.5) + 1;

            if (xFrom < 0)
            {
                xFrom = 0;
            }

            if (yFrom < 0)
            {
                yFrom = 0;
            }

            if (xTo > scene.RegionData.Size.X)
            {
                xTo = (int)scene.RegionData.Size.X;
            }

            if (yTo > scene.RegionData.Size.Y)
            {
                yTo = (int)scene.RegionData.Size.Y;
            }

            for (x = xFrom; x < xTo; x++)
            {
                int y;
                for (y = yFrom; y < yTo; y++)
                {
                    Vector3 pos = new Vector3(x, y, 0);
                    if (!scene.CanTerraform(agent, pos))
                    {
                        continue;
                    }

                    // Calculate a cos-sphere and add it to the heighmap
                    double r = Math.Sqrt((x - rx) * (x - rx) + ((y - ry) * (y - ry)));
                    double z = Math.Cos(r * Math.PI / (s * 2));
                    if (z > 0.0)
                    {
                        LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, z * modify.Seconds);
                        if (lp != null && !changed.Contains(lp))
                        {
                            changed.Add(lp);
                        }
                    }
                }
            }

            if (changed.Count != 0)
            {
                foreach (LayerPatch lp in changed)
                {
                    lp.IncrementSerial();
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [PaintEffect(StandardTerrainEffect.Lower)]
        void LowerSphere(LLAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();
            int s = (int)(Math.Pow(2, modify.BrushSize) + 0.5);

            int x;
            int rx = (int)(data.West + 0.5);
            int ry = (int)(data.South + 0.5);
            int xFrom = (int)(rx - s + 0.5);
            int xTo = (int)(rx + s + 0.5) + 1;
            int yFrom = (int)(ry - s + 0.5);
            int yTo = (int)(ry + s + 0.5) + 1;

            if (xFrom < 0)
            {
                xFrom = 0;
            }

            if (yFrom < 0)
            {
                yFrom = 0;
            }

            if (xTo > scene.RegionData.Size.X)
            {
                xTo = (int)scene.RegionData.Size.X;
            }

            if (yTo > scene.RegionData.Size.Y)
            {
                yTo = (int)scene.RegionData.Size.Y;
            }

            for (x = xFrom; x < xTo; x++)
            {
                int y;
                for (y = yFrom; y < yTo; y++)
                {
                    Vector3 pos = new Vector3(x, y, 0);
                    if (!scene.CanTerraform(agent, pos))
                    {
                        continue;
                    }

                    // Calculate a cos-sphere and add it to the heighmap
                    double r = Math.Sqrt((x - rx) * (x - rx) + ((y - ry) * (y - ry)));
                    double z = Math.Cos(r * Math.PI / (s * 2));
                    if (z > 0.0)
                    {
                        LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, -z * modify.Seconds);
                        if (lp != null && !changed.Contains(lp))
                        {
                            changed.Add(lp);
                        }
                    }
                }
            }

            if (changed.Count != 0)
            {
                foreach (LayerPatch lp in changed)
                {
                    lp.IncrementSerial();
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [FloodEffect(StandardTerrainEffect.Raise)]
        void RaiseArea(LLAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();
            int x;
            for (x = (int)data.West; x < (int)data.East; x++)
            {
                int y;
                for (y = (int)data.South; y < (int)data.North; y++)
                {
                    if (scene.CanTerraform(agent, new Vector3(x, y, 0)))
                    {
                        LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, modify.BrushSize);
                        if (lp != null && !changed.Contains(lp))
                        {
                            changed.Add(lp);
                        }
                    }
                }
            }

            if (changed.Count != 0)
            {
                foreach (LayerPatch lp in changed)
                {
                    lp.IncrementSerial();
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [FloodEffect(StandardTerrainEffect.Lower)]
        void LowerArea(LLAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();
            int x;
            for (x = (int)data.West; x < (int)data.East; x++)
            {
                int y;
                for (y = (int)data.South; y < (int)data.North; y++)
                {
                    if (scene.CanTerraform(agent, new Vector3(x, y, 0)))
                    {
                        LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, -modify.BrushSize);
                        if (lp != null && !changed.Contains(lp))
                        {
                            changed.Add(lp);
                        }
                    }
                }
            }

            if (changed.Count != 0)
            {
                foreach (LayerPatch lp in changed)
                {
                    lp.IncrementSerial();
                }
                scene.Terrain.UpdateTerrainDataToClients();
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
