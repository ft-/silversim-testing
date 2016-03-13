// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Land;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilverSim.Scene.Types.Scene
{
    public static class Terraforming
    {
        private static readonly ILog m_Log = LogManager.GetLogger("TERRAFORMING");

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
        sealed class PaintEffectAttribute : Attribute
        {
            public StandardTerrainEffect Effect { get; private set; }

            public PaintEffectAttribute(StandardTerrainEffect effect)
            {
                Effect = effect;
            }
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false)]
        sealed class FloodEffectAttribute : Attribute
        {
            public StandardTerrainEffect Effect { get; private set; }

            public FloodEffectAttribute(StandardTerrainEffect effect)
            {
                Effect = effect;
            }
        }

        public static readonly Dictionary<StandardTerrainEffect, Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data>> PaintEffects = new Dictionary<StandardTerrainEffect, Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data>>();
        public static readonly Dictionary<StandardTerrainEffect, Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data>> FloodEffects = new Dictionary<StandardTerrainEffect, Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data>>();

        static Terraforming()
        {
            foreach (MethodInfo mi in typeof(Terraforming).GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                PaintEffectAttribute pe = (PaintEffectAttribute)Attribute.GetCustomAttribute(mi, typeof(PaintEffectAttribute));
                if (pe == null)
                {
                    continue;
                }
                else if (PaintEffects.ContainsKey(pe.Effect))
                {
                    m_Log.FatalFormat("Method {0} defines duplicate paint effect {1}", mi.Name, pe.Effect.ToString());
                }
                else if (mi.ReturnType != typeof(void))
                {
                    m_Log.FatalFormat("Method {0} does not have return type void", mi.Name);
                }
                else if (mi.GetParameters().Length != 4)
                {
                    m_Log.FatalFormat("Method {0} does not match in parameter count", mi.Name);
                }
                else if (mi.GetParameters()[0].ParameterType != typeof(UUI) ||
                        mi.GetParameters()[1].ParameterType != typeof(SceneInterface) ||
                        mi.GetParameters()[2].ParameterType != typeof(ModifyLand) ||
                        mi.GetParameters()[3].ParameterType != typeof(ModifyLand.Data))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else
                {
                    PaintEffects.Add(pe.Effect, (Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data>)
                        Delegate.CreateDelegate(typeof(Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data>), null, mi));
                }
                FloodEffectAttribute fe = (FloodEffectAttribute)Attribute.GetCustomAttribute(mi, typeof(FloodEffectAttribute));
                if (fe == null)
                {
                    continue;
                }
                else if (PaintEffects.ContainsKey(fe.Effect))
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
                else if (mi.GetParameters()[0].ParameterType != typeof(UUI) ||
                        mi.GetParameters()[1].ParameterType != typeof(SceneInterface) ||
                        mi.GetParameters()[2].ParameterType != typeof(ModifyLand) ||
                        mi.GetParameters()[3].ParameterType != typeof(ModifyLand.Data))
                {
                    m_Log.FatalFormat("Method {0} does not match in parameters", mi.Name);
                }
                else
                {
                    PaintEffects.Add(fe.Effect, (Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data>)
                        Delegate.CreateDelegate(typeof(Action<UUI, SceneInterface, ModifyLand, ModifyLand.Data>), null, mi));
                }
            }
        }

        #region Paint Effects
        [PaintEffect(StandardTerrainEffect.Raise)]
        public static void RaiseSphere(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
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

            if (xTo >= scene.SizeX)
            {
                xTo = (int)scene.SizeX - 1;
            }

            if (yTo >= scene.SizeY)
            {
                yTo = (int)scene.SizeY - 1;
            }

#if DEBUG
            m_Log.DebugFormat("Terraforming {0},{1} RaiseSphere {2}-{3} / {4}-{5}", data.West, data.South, xFrom, xTo, yFrom, yTo);
#endif

            for (int x = xFrom; x <= xTo; x++)
            {
                for (int y = yFrom; y <= yTo; y++)
                {
                    Vector3 pos = new Vector3(x, y, 0);
                    if (!scene.CanTerraform(agentOwner, pos))
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
        public static void LowerSphere(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
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

            if (xTo >= scene.SizeX)
            {
                xTo = (int)scene.SizeX - 1;
            }

            if (yTo >= scene.SizeY)
            {
                yTo = (int)scene.SizeY - 1;
            }

            for (int x = xFrom; x <= xTo; x++)
            {
                for (int y = yFrom; y <= yTo; y++)
                {
                    Vector3 pos = new Vector3(x, y, 0);
                    if (!scene.CanTerraform(agentOwner, pos))
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
        public static void FlattenSphere(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            double strength = MetersToSphericalStrength(data.BrushSize);

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

            if (xTo >= scene.SizeX)
            {
                xTo = (int)scene.SizeX - 1;
            }

            if (yTo > scene.SizeY)
            {
                yTo = (int)scene.SizeY - 1;
            }

            for (int x = xFrom; x <= xTo; x++)
            {
                for (int y = yFrom; y <= yTo; y++)
                {
                    Vector3 pos = new Vector3(x, y, 0);
                    if (!scene.CanTerraform(agentOwner, pos))
                    {
                        continue;
                    }

                    double z;
                    z = (modify.Seconds < 4.0) ?
                        SphericalFactor(x, y, data.West, data.South, strength) * modify.Seconds * 0.25f :
                        1;

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

                    if (Math.Abs(delta) >= Double.Epsilon) // add in non-zero amount
                    {
                        LayerPatch lp = scene.Terrain.AdjustTerrain((uint)x, (uint)y, delta);
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

        [PaintEffect(StandardTerrainEffect.Smooth)]
        public static void SmoothSphere(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
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
                    if (x >= 0 && y >= 0 && x < scene.SizeX && y < scene.SizeY)
                    {
                        Vector3 pos = new Vector3(x, y, 0);
                        if (!scene.CanTerraform(agentOwner, pos))
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
        public static void NoiseSphere(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
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
                    if (x >= 0 && y >= 0 && x < scene.SizeX && y < scene.SizeY)
                    {
                        Vector3 pos = new Vector3(x, y, 0);
                        if (!scene.CanTerraform(agentOwner, pos))
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
        public static void RaiseArea(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();
            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (scene.CanTerraform(agentOwner, new Vector3(x, y, 0)))
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
        public static void LowerArea(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();
            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (scene.CanTerraform(agentOwner, new Vector3(x, y, 0)))
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
        public static void FlattenArea(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();


            double sum = 0;
            double steps = 0;

            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (!scene.CanTerraform(agentOwner, new Vector3(x, y, 0)))
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
                    if (scene.CanTerraform(agentOwner, new Vector3(x, y, 0)))
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
        public static void SmoothArea(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            double area = modify.Size;
            double step = area / 4;

            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (!scene.CanTerraform(agentOwner, new Vector3(x, y, 0)))
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
        public static void NoiseArea(UUI agentOwner, SceneInterface scene, ModifyLand modify, ModifyLand.Data data)
        {
            List<LayerPatch> changed = new List<LayerPatch>();

            for (int x = (int)data.West; x < (int)data.East; x++)
            {
                for (int y = (int)data.South; y < (int)data.North; y++)
                {
                    if (!scene.CanTerraform(agentOwner, new Vector3(x, y, 0)))
                    {
                        continue;
                    }

                    double noise = PerlinNoise2D((double)x / scene.SizeX,
                                                            (double)y / scene.SizeY, 8, 1);

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
            int w = (int)scene.SizeX;
            int h = (int)scene.SizeY;

            if (x > w - 2)
            {
                x = w - 2;
            }
            if (y > h - 2)
            {
                y = h - 2;
            }

            x = x.Clamp(1f, scene.SizeX - 2);
            y = y.Clamp(1f, scene.SizeY - 2);

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
}
