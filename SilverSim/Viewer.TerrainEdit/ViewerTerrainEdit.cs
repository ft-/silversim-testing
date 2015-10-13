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

namespace SilverSim.Viewer.TerrainEdit
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

        public delegate void ModifyLandEffect(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data);

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
                else if(mi.GetParameters()[0].ParameterType != typeof(ViewerAgent))
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
                else if (mi.GetParameters()[0].ParameterType != typeof(ViewerAgent))
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
        void HandleMessage(ViewerAgent agent, AgentCircuit circuit, Message m)
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

        #region Paint Effects
        [PaintEffect(StandardTerrainEffect.Raise)]
        void RaiseSphere(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            int xFrom = (int)(data.West - data.BrushSize + 0.5);
            int xTo = (int)(data.West + data.BrushSize + 0.5);
            int yFrom = (int)(data.South - data.BrushSize + 0.5);
            int yTo = (int)(data.South + data.BrushSize + 0.5);

            if (xFrom < 0)
            {
                xFrom = 0;
            }

            if (yFrom < 0)
            {
                yFrom = 0;
            }

            if (xTo >= scene.RegionData.Size.X)
            {
                xTo = (int)scene.RegionData.Size.X - 1;
            }

            if (yTo >= scene.RegionData.Size.Y)
            {
                yTo = (int)scene.RegionData.Size.Y - 1;
            }

#if DEBUG
            m_Log.DebugFormat("ModifyLand {0},{1} RaiseSphere {2}-{3} / {4}-{5}", data.West, data.South, xFrom, xTo, yFrom, yTo);
#endif

            for (int x = xFrom; x <= xTo; x++)
            {
                for (int y = yFrom; y <= yTo; y++)
                {
                    Vector3 pos = new Vector3(x, y, 0);
                    if (!scene.CanTerraform(agent, pos))
                    {
                        continue;
                    }

                    // Calculate a cos-sphere and add it to the heightmap
                    double r = Math.Sqrt((x - data.West) * (x - data.West) + ((y - data.South) * (y - data.South)));
                    double z = Math.Cos(r * Math.PI / (data.BrushSize * 2));
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
                    scene.Terrain.UpdateTerrainListeners(lp);
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [PaintEffect(StandardTerrainEffect.Lower)]
        void LowerSphere(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            int xFrom = (int)(data.West - data.BrushSize + 0.5);
            int xTo = (int)(data.West + data.BrushSize + 0.5);
            int yFrom = (int)(data.South - data.BrushSize + 0.5);
            int yTo = (int)(data.South + data.BrushSize + 0.5);

            if (xFrom < 0)
            {
                xFrom = 0;
            }

            if (yFrom < 0)
            {
                yFrom = 0;
            }

            if (xTo >= scene.RegionData.Size.X)
            {
                xTo = (int)scene.RegionData.Size.X - 1;
            }

            if (yTo >= scene.RegionData.Size.Y)
            {
                yTo = (int)scene.RegionData.Size.Y - 1;
            }

            for (int x = xFrom; x <= xTo; x++)
            {
                for (int y = yFrom; y <= yTo; y++)
                {
                    Vector3 pos = new Vector3(x, y, 0);
                    if (!scene.CanTerraform(agent, pos))
                    {
                        continue;
                    }

                    // Calculate a cos-sphere and add it to the heightmap
                    double r = Math.Sqrt((x - data.West) * (x - data.West) + ((y - data.South) * (y - data.South)));
                    double z = Math.Cos(r * Math.PI / (data.BrushSize * 2));
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
                    scene.Terrain.UpdateTerrainListeners(lp);
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [PaintEffect(StandardTerrainEffect.Flatten)]
        void FlattenSphere(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            double strength = MetersToSphericalStrength(data.BrushSize);

            int x, y;

            int xFrom = (int)(data.West - data.BrushSize + 0.5);
            int xTo = (int)(data.West + data.BrushSize + 0.5);
            int yFrom = (int)(data.South - data.BrushSize + 0.5);
            int yTo = (int)(data.South + data.BrushSize + 0.5);

            if (xFrom < 0)
            {
                xFrom = 0;
            }

            if (yFrom < 0)
            {
                yFrom = 0;
            }

            if (xTo >= scene.RegionData.Size.X)
            {
                xTo = (int)scene.RegionData.Size.X - 1;
            }

            if (yTo > scene.RegionData.Size.Y)
            {
                yTo = (int)scene.RegionData.Size.Y - 1;
            }

            for (x = xFrom; x <= xTo; x++)
            {
                for (y = yFrom; y <= yTo; y++)
                {
                    Vector3 pos = new Vector3(x, y, 0);
                    if (!scene.CanTerraform(agent, pos))
                    {
                        continue;
                    }

                    double z;
                    if (modify.Seconds < 4.0)
                    {
                        z = SphericalFactor(x, y, data.West, data.South, strength) * modify.Seconds * 0.25f;
                    }
                    else
                    {
                        z = 1;
                    }

                    double delta = modify.Height - scene.Terrain[(uint)x, (uint)y];
                    if (Math.Abs(delta) > 0.1)
                    {
                        if (z > 1)
                        {
                            z = 1;
                        }
                        else if (z < 0)
                        {
                            z = 0;
                        }
                        delta *= z;
                    }

                    if (delta != 0) // add in non-zero amount
                    {
                        LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, delta);
                        if(lp != null && !changed.Contains(lp))
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

        [PaintEffect(StandardTerrainEffect.Smooth)]
        void SmoothSphere(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            int n = (int)(data.BrushSize + 0.5f);
            if (data.BrushSize > 6)
            {
                data.BrushSize = 6;
            }
            double strength = MetersToSphericalStrength(data.BrushSize);

            double area = data.BrushSize;
            double step = data.BrushSize / 4;
            double duration = modify.Seconds * 0.03f;

            int zx = (int)(data.West + 0.5);
            int zy = (int)(data.South + 0.5);

            for (int dx = -n; dx <= n; dx++)
            {
                for (int dy = -n; dy <= n; dy++)
                {
                    int x = zx + dx;
                    int y = zy + dy;
                    if (x >= 0 && y >= 0 && x < scene.RegionData.Size.X && y < scene.RegionData.Size.Y)
                    {
                        Vector3 pos = new Vector3(x, y, 0);
                        if (!scene.CanTerraform(agent, pos))
                        {
                            continue;
                        }

                        double z = SphericalFactor(x, y, data.West, data.South, strength) / (strength);
                        if (z > 0) // add in non-zero amount
                        {
                            double average = 0;
                            int avgsteps = 0;

                            for (double nn = 0 - area; nn < area; nn += step)
                            {
                                for (double l = 0 - area; l < area; l += step)
                                {
                                    avgsteps++;
                                    average += GetBilinearInterpolate(x + nn, y + l, scene);
                                }
                            }
                            double da = z;
                            double a = (scene.Terrain[(uint)x, (uint)y] - (average / avgsteps)) * da;

                            LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, -(a * duration));
                            if (lp != null && !changed.Contains(lp))
                            {
                                changed.Add(lp);
                            }
                        }
                    }
                }
            }

            if (changed.Count != 0)
            {
                foreach (LayerPatch lp in changed)
                {
                    lp.IncrementSerial();
                    scene.Terrain.UpdateTerrainListeners(lp);
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [PaintEffect(StandardTerrainEffect.Noise)]
        void NoiseSphere(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            int n = (int)(data.BrushSize + 0.5f);
            if (data.BrushSize > 6)
            {
                data.BrushSize = 6;
            }
            
            double strength = MetersToSphericalStrength(data.BrushSize);

            double area = data.BrushSize;
            double step = data.BrushSize / 4;
            double duration = modify.Seconds * 0.01f;

            int zx = (int)(data.West + 0.5);
            int zy = (int)(data.South + 0.5);

            double average = 0;
            int avgsteps = 0;

            for (double nn = 0 - area; nn < area; nn += step)
            {
                for (double l = 0 - area; l < area; l += step)
                {
                    avgsteps++;
                    average += GetBilinearInterpolate(data.West + nn, data.South + l, scene);
                }
            }

            for (int dx = -n; dx <= n; dx++)
            {
                for (int dy = -n; dy <= n; dy++)
                {
                    int x = zx + dx;
                    int y = zy + dy;
                    if (x >= 0 && y >= 0 && x < scene.RegionData.Size.X && y < scene.RegionData.Size.Y)
                    {
                        Vector3 pos = new Vector3(x, y, 0);
                        if (!scene.CanTerraform(agent, pos))
                        {
                            continue;
                        }

                        double z = SphericalFactor(x, y, data.West, data.South, strength) / (strength);
                        if (z > 0) // add in non-zero amount
                        {
                            double a = (scene.Terrain[(uint)x, (uint)y] - (average / avgsteps));

                            LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, (a * duration));
                            if (lp != null && !changed.Contains(lp))
                            {
                                changed.Add(lp);
                            }
                        }
                    }
                }
            }

            if (changed.Count != 0)
            {
                foreach (LayerPatch lp in changed)
                {
                    lp.IncrementSerial();
                    scene.Terrain.UpdateTerrainListeners(lp);
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }
        #endregion

        #region Flood Effects
        [FloodEffect(StandardTerrainEffect.Raise)]
        void RaiseArea(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();
            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (scene.CanTerraform(agent, new Vector3(x, y, 0)))
                    {
                        LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, modify.Size);
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
                    scene.Terrain.UpdateTerrainListeners(lp);
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [FloodEffect(StandardTerrainEffect.Lower)]
        void LowerArea(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();
            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (scene.CanTerraform(agent, new Vector3(x, y, 0)))
                    {
                        LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, -modify.Size);
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
                    scene.Terrain.UpdateTerrainListeners(lp);
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [FloodEffect(StandardTerrainEffect.Flatten)]
        void FlattenArea(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();


            double sum = 0;
            double steps = 0;

            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (!scene.CanTerraform(agent, new Vector3(x, y, 0)))
                    {
                        continue;
                    }
                    sum += scene.Terrain[(uint)x, (uint)y];
                    steps += 1;
                }
            }

            double avg = sum / steps;

            double str = 0.1f * modify.Size; // == 0.2 in the default client

            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (scene.CanTerraform(agent, new Vector3(x, y, 0)))
                    {
                        LayerPatch lp = scene.Terrain.BlendTerrain((uint)x, (uint)y, avg, str);
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
                    scene.Terrain.UpdateTerrainListeners(lp);
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [FloodEffect(StandardTerrainEffect.Smooth)]
        void SmoothArea(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            double area = modify.Size;
            double step = modify.Size / 4;

            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (!scene.CanTerraform(agent, new Vector3(x, y, 0)))
                    {
                        continue;
                    }

                    double average = 0;
                    int avgsteps = 0;

                    for (double n = 0 - area; n < area; n += step)
                    {
                        for (double l = 0 - area; l < area; l += step)
                        {
                            avgsteps++;
                            average += GetBilinearInterpolate(x + n, y + l, scene);
                        }
                    }

                    LayerPatch lp = scene.Terrain.BlendTerrain((uint)x, (uint)y, average / avgsteps, 1);
                    if (lp != null && !changed.Contains(lp))
                    {
                        changed.Add(lp);
                    }
                }
            }
 
            if (changed.Count != 0)
            {
                foreach (LayerPatch lp in changed)
                {
                    lp.IncrementSerial();
                    scene.Terrain.UpdateTerrainListeners(lp);
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }

        [FloodEffect(StandardTerrainEffect.Noise)]
        void NoiseArea(ViewerAgent agent, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (!scene.CanTerraform(agent, new Vector3(x, y, 0)))
                    {
                        continue;
                    }

                    double noise = PerlinNoise2D(x / scene.RegionData.Size.X,
                                                            y / scene.RegionData.Size.Y, 8, 1);

                    LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, noise * modify.Size);
                    if (lp != null && !changed.Contains(lp))
                    {
                        changed.Add(lp);
                    }
                }
            }

            if (changed.Count != 0)
            {
                foreach (LayerPatch lp in changed)
                {
                    lp.IncrementSerial();
                    scene.Terrain.UpdateTerrainListeners(lp);
                }
                scene.Terrain.UpdateTerrainDataToClients();
            }
        }
        #endregion

        #region Util Functions
        static double MetersToSphericalStrength(double size)
        {
            return (size + 1) * 1.35f;
        }

        static double SphericalFactor(double x, double y, double rx, double ry, double size)
        {
            return size * size - ((x - rx) * (x - rx) + (y - ry) * (y - ry));
        }

        static double GetBilinearInterpolate(double x, double y, SceneInterface scene)
        {
            int w = (int)scene.RegionData.Size.X;
            int h = (int)scene.RegionData.Size.Y;

            if (x > w - 2)
            {
                x = w - 2;
            }
            if (y > h - 2)
            {
                y = h - 2;
            }
            if (x < 0.0)
            {
                x = 1.0f;
            }
            if (y < 0.0)
            {
                y = 1.0f;
            }

            if (x > scene.RegionData.Size.X - 2)
            {
                x = scene.RegionData.Size.X - 2;
            }
            if (x < 0)
            {
                x = 0;
            }
            if (y > scene.RegionData.Size.Y - 2)
            {
                y = scene.RegionData.Size.Y - 2;
            }
            if (y < 0)
            {
                y = 0;
            }

            const int stepSize = 1;
            double h00 = scene.Terrain[(uint)x, (uint)y];
            double h10 = scene.Terrain[(uint)x + stepSize, (uint)y];
            double h01 = scene.Terrain[(uint)x, (uint)y + stepSize];
            double h11 = scene.Terrain[(uint)x + stepSize, (uint)y + stepSize];
            double h1 = h00;
            double h2 = h10;
            double h3 = h01;
            double h4 = h11;
            double a00 = h1;
            double a10 = h2 - h1;
            double a01 = h3 - h1;
            double a11 = h1 - h2 - h3 + h4;
            double partialx = x - (uint)x;
            double partialz = y - (uint)y;
            double hi = a00 + (a10 * partialx) + (a01 * partialz) + (a11 * partialx * partialz);
            return hi;
        }

        static double Noise(double x, double y)
        {
            int n = (int)x + (int)(y * 749);
            n = (n << 13) ^ n;
            return (1 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824);
        }

        static double SmoothedNoise1(double x, double y)
        {
            double corners = (Noise(x - 1, y - 1) + Noise(x + 1, y - 1) + Noise(x - 1, y + 1) + Noise(x + 1, y + 1)) / 16;
            double sides = (Noise(x - 1, y) + Noise(x + 1, y) + Noise(x, y - 1) + Noise(x, y + 1)) / 8;
            double center = Noise(x, y) / 4;
            return corners + sides + center;
        }

        static double Interpolate(double x, double y, double z)
        {
            return (x * (1 - z)) + (y * z);
        }

        static double InterpolatedNoise(double x, double y)
        {
            int integer_X = (int)(x);
            double fractional_X = x - integer_X;

            int integer_Y = (int)y;
            double fractional_Y = y - integer_Y;

            double v1 = SmoothedNoise1(integer_X, integer_Y);
            double v2 = SmoothedNoise1(integer_X + 1, integer_Y);
            double v3 = SmoothedNoise1(integer_X, integer_Y + 1);
            double v4 = SmoothedNoise1(integer_X + 1, integer_Y + 1);

            double i1 = Interpolate(v1, v2, fractional_X);
            double i2 = Interpolate(v3, v4, fractional_X);

            return Interpolate(i1, i2, fractional_Y);
        }

        static double PerlinNoise2D(double x, double y, int octaves, double persistence)
        {
            double total = 0;

            for (int i = 0; i < octaves; i++)
            {
                double frequency = Math.Pow(2, i);
                double amplitude = Math.Pow(persistence, i);

                total += InterpolatedNoise(x * frequency, y * frequency) * amplitude;
            }
            return total;
        }
        #endregion
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
