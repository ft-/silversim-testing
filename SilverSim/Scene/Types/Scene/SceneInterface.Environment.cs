using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Scene.Types.Agent;
using SilverSim.LL.Messages.Generic;
using SilverSim.LL.Messages.LayerData;
using SilverSim.LL.Messages.Region;
using SilverSim.LL.Messages;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public abstract class EnvironmentProcessor : IDisposable
        {
            public struct WLVector4
            {
                public double X;
                public double Y;
                public double Z;
                public double W;
            }

            public struct WindlightSkyData
            {
                public WLVector4 Ambient;
                public WLVector4 CloudColor;
                public double CloudCoverage;
                public WLVector4 BlueDensity;
                public Vector3 CloudDetailXYDensity;
                public double CloudScale;
                public double CloudScrollX;
                public bool CloudScrollXLock;
                public double CloudScrollY;
                public bool CloudScrollYLock;
                public Vector3 CloudXYDensity;
                public double DensityMultiplier;
                public double DistanceMultiplier;
                public bool DrawClassicClouds;
                public double EastAngle;
                public double HazeDensity;
                public double HazeHorizon;
                public WLVector4 Horizon;
                public int MaxAltitude;
                public double SceneGamma;
                public double StarBrightness;
                public double SunGlowFocus;
                public double SunGlowSize;
                public WLVector4 SunMoonColor;
                public double SunMoonPosition;
            }

            public struct WindlightWaterData
            {
                public Vector3 BigWaveDirection;
                public Vector3 LittleWaveDirection;
                public double BlurMultiplier;
                public double FresnelScale;
                public double FresnelOffset;
                public UUID NormalMapTexture;
                public Vector3 ReflectionWaveletScale;
                public double RefractScaleAbove;
                public double RefractScaleBelow;
                public double UnderwaterFogModifier;
                public Color Color;
                public double FogDensityExponent;
            }

            public struct SunData
            {
                public UInt64 UsecSinceStart;
                public UInt32 SecPerDay;
                public UInt32 SecPerYear;
                public Vector3 SunDirection;
                public double SunPhase;
                public Vector3 SunAngVelocity;
            }

            public struct WindVector
            {
                public double X;
                public double Y;
            }
            public struct WindData
            {
                public WindVector[,] Speeds;
            }

            public struct CloudData
            {
                public double[,] CloudCoverages;
            }

            bool m_WindlightValid = false;
            WindlightSkyData m_SkyWindlight = new WindlightSkyData();
            WindlightWaterData m_WaterWindlight = new WindlightWaterData();
            SunData m_SunData = new SunData();
            WindData m_WindData = new WindData();
            CloudData m_CloudData = new CloudData();
            SceneInterface m_Scene;

            public EnvironmentProcessor(SceneInterface scene)
            {
                m_Scene = scene;
                m_WindData.Speeds = new WindVector[scene.RegionData.Size.X / 4, scene.RegionData.Size.Y / 4];
                m_CloudData.CloudCoverages = new double[scene.RegionData.Size.X / 4, scene.RegionData.Size.Y / 4];
                m_SunData.SunDirection = new Vector3();
            }

            public void Dispose()
            {
                m_Scene = null;
            }

            private void UpdateWindlightProfileToClients()
            {
                GenericMessage m;

                if (m_WindlightValid)
                {
                    m = compileWindlightSettings();
                }
                else
                {
                    m = compileResetWindlightSettings();
                }

                SendToAllClients(m);
            }

            #region Viewer time message compiler
            private void SendSimulatorTimeMessageToAllClients()
            {
                SimulatorViewerTimeMessage m = new SimulatorViewerTimeMessage();
                m.SunPhase = m_SunData.SunPhase;
                m.UsecSinceStart = m_SunData.UsecSinceStart;
                m.SunDirection = m_SunData.SunDirection;
                m.SunAngVelocity = m_SunData.SunAngVelocity;
                m.SecPerYear = m_SunData.SecPerYear;
                m.SecPerDay = m_SunData.SecPerDay;
                SendToAllClients(m);
            }
            #endregion

            private void SendToAllClients(Message m)
            {
                foreach (IAgent agent in m_Scene.Agents)
                {
                    agent.SendMessageIfRootAgent(m, m_Scene.ID);
                }
            }


            #region Windlight message compiler
            private GenericMessage compileResetWindlightSettings()
            {
                GenericMessage m = new GenericMessage();
                m.Method = "WindlightReset";
                m.ParamList = new byte[0];
                return m;
            }

            private GenericMessage compileWindlightSettings()
            {
                GenericMessage m = new GenericMessage();
                m.Method = "Windlight";
                byte[] mBlock = new byte[249];
                int pos = 0;
                AddToCompiledWL(m_WaterWindlight.Color, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.FogDensityExponent, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.UnderwaterFogModifier, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.ReflectionWaveletScale, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.FresnelScale, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.FresnelOffset, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.RefractScaleAbove, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.RefractScaleBelow, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.BlurMultiplier, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.BigWaveDirection, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.LittleWaveDirection, ref mBlock, ref pos);
                AddToCompiledWL(m_WaterWindlight.NormalMapTexture, ref mBlock, ref pos);

                AddToCompiledWL(m_SkyWindlight.Horizon, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.HazeHorizon, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.BlueDensity, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.HazeDensity, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.DensityMultiplier, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.DistanceMultiplier, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.SunMoonColor, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.SunMoonPosition, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.Ambient, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.EastAngle, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.SunGlowFocus, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.SunGlowSize, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.SceneGamma, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.StarBrightness, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.CloudColor, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.CloudXYDensity, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.CloudCoverage, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.CloudScale, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.CloudDetailXYDensity, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.CloudScrollX, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.CloudScrollY, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.MaxAltitude, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.CloudScrollXLock, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.CloudScrollYLock, ref mBlock, ref pos);
                AddToCompiledWL(m_SkyWindlight.DrawClassicClouds, ref mBlock, ref pos);
                m.ParamList = mBlock;
                return m;
            }

            private void AddToCompiledWL(bool v, ref byte[] mBlock, ref int pos)
            {
                if (v)
                {
                    mBlock[pos] = 1;
                }
                else
                {
                    mBlock[pos] = 0;
                }
                ++pos;
            }

            private void AddToCompiledWL(Vector3 v, ref byte[] mBlock, ref int pos)
            {
                AddToCompiledWL(v.X, ref mBlock, ref pos);
                AddToCompiledWL(v.Y, ref mBlock, ref pos);
                AddToCompiledWL(v.Z, ref mBlock, ref pos);
            }

            private void AddToCompiledWL(WLVector4 v, ref byte[] mBlock, ref int pos)
            {
                AddToCompiledWL(v.X, ref mBlock, ref pos);
                AddToCompiledWL(v.Y, ref mBlock, ref pos);
                AddToCompiledWL(v.Z, ref mBlock, ref pos);
                AddToCompiledWL(v.W, ref mBlock, ref pos);
            }

            private void AddToCompiledWL(Color v, ref byte[] mBlock, ref int pos)
            {
                AddToCompiledWL(v.R, ref mBlock, ref pos);
                AddToCompiledWL(v.G, ref mBlock, ref pos);
                AddToCompiledWL(v.B, ref mBlock, ref pos);
            }

            private void AddToCompiledWL(UUID v, ref byte[] mBlock, ref int pos)
            {
                v.ToBytes(mBlock, pos);
                pos += 16;
            }

            private void AddToCompiledWL(double v, ref byte[] mBlock, ref int pos)
            {
                byte[] b = BitConverter.GetBytes((float)v);
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                Buffer.BlockCopy(b, 0, mBlock, pos, b.Length);
                pos += b.Length;
            }

            private void AddToCompiledWL(int v, ref byte[] mBlock, ref int pos)
            {
                byte[] b = BitConverter.GetBytes(v);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                Buffer.BlockCopy(b, 0, mBlock, pos, b.Length);
                pos += b.Length;
            }
            #endregion
        }
    }
}
