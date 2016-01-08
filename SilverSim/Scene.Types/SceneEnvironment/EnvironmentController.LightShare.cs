// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Messages.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        readonly ReaderWriterLock m_LightShareLock = new ReaderWriterLock();
        WindlightSkyData m_SkyWindlight = WindlightSkyData.Defaults;
        WindlightWaterData m_WaterWindlight = WindlightWaterData.Defaults;
        readonly RwLockedDictionary<UUID, bool> m_OverrideLightSharePerAgent = new RwLockedDictionary<UUID, bool>();
        bool m_WindlightValid;

        public struct WLVector4
        {
            public double X;
            public double Y;
            public double Z;
            public double W;

            public WLVector4(Quaternion q)
            {
                X = q.X;
                Y = q.Y;
                Z = q.Z;
                W = q.W;
            }

            public WLVector4(double x, double y, double z, double w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }

            public static implicit operator Quaternion(WLVector4 v)
            {
                return new Quaternion(v.X, v.Y, v.Z, v.W);
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
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

            public static WindlightSkyData Defaults
            {
                get
                {
                    WindlightSkyData skyData = new WindlightSkyData();
                    skyData.Horizon = new WLVector4(0.25, 0.25, 0.32, 0.32);
                    skyData.HazeHorizon = 0.19;
                    skyData.BlueDensity = new WLVector4(0.12, 0.22, 0.38, 0.38);
                    skyData.HazeDensity = 0.7;
                    skyData.DensityMultiplier = 0.18;
                    skyData.DistanceMultiplier = 0.8;
                    skyData.MaxAltitude = 1605;
                    skyData.SunMoonPosition = 0.317;
                    skyData.SunMoonColor = new WLVector4(0.24, 0.26, 0.30, 0.30);
                    skyData.Ambient = new WLVector4(0.35, 0.35, 0.35, 0.35);
                    skyData.EastAngle = 0;
                    skyData.SunGlowFocus = 0.1;
                    skyData.SunGlowSize = 1.75;
                    skyData.SceneGamma = 1.0;
                    skyData.StarBrightness = 0;
                    skyData.CloudColor = new WLVector4(0.41, 0.41, 0.41, 0.41);
                    skyData.CloudXYDensity = new Vector3(1.0, 0.53, 1.0);
                    skyData.CloudCoverage = 0.27;
                    skyData.CloudScale = 0.42;
                    skyData.CloudDetailXYDensity = new Vector3(1.0, 0.52, 0.12);
                    skyData.CloudScrollX = 0.2;
                    skyData.CloudScrollY = 0.01;
                    skyData.DrawClassicClouds = true;
                    skyData.CloudScrollXLock = false;
                    skyData.CloudScrollYLock = false;

                    return skyData;
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
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

            public static WindlightWaterData Defaults
            {
                get
                {
                    WindlightWaterData waterData = new WindlightWaterData();
                    waterData.Color = new Color(4 / 255f, 38 / 255f, 64 / 255f);
                    waterData.FogDensityExponent = 4;
                    waterData.UnderwaterFogModifier = 0.25;
                    waterData.ReflectionWaveletScale = new Vector3(2.0, 2.0, 2.0);
                    waterData.FresnelScale = 0.4;
                    waterData.FresnelOffset = 0.5;
                    waterData.RefractScaleAbove = 0.03;
                    waterData.RefractScaleBelow = 0.2;
                    waterData.BlurMultiplier = 0.04;
                    waterData.BigWaveDirection = new Vector3(1.05, -0.42, 0);
                    waterData.LittleWaveDirection = new Vector3(1.11, -1.16, 0);
                    waterData.NormalMapTexture = new UUID("822ded49-9a6c-f61c-cb89-6df54f42cdf4");

                    return waterData;
                }
            }
        }

        #region Client-specific update of Windlight Data
        public void SendTargetedWindlightProfile(UUID agentID, WindlightSkyData skyData, WindlightWaterData waterData)
        {
            m_OverrideLightSharePerAgent[agentID] = true;
            IAgent agent;
            if (m_Scene.RootAgents.TryGetValue(agentID, out agent))
            {
                agent.SendMessageIfRootAgent(CompileWindlightSettings(SkyData, waterData), m_Scene.ID);
            }
        }

        public void ResetTargetedWindlightProfile(UUID agentID)
        {
            m_OverrideLightSharePerAgent[agentID] = false;
            IAgent agent;
            if (m_Scene.RootAgents.TryGetValue(agentID, out agent))
            {
                UpdateWindlightProfileToClient(agent);
            }
        }
        #endregion

        #region Update of Windlight Data
        private void UpdateWindlightProfileToClients()
        {
            GenericMessage m;

            m = (m_WindlightValid) ?
                CompileWindlightSettings(m_SkyWindlight, m_WaterWindlight) :
                CompileResetWindlightSettings();

            foreach (IAgent agent in m_Scene.Agents)
            {
                bool overrideLs;
                if (m_OverrideLightSharePerAgent.TryGetValue(agent.Owner.ID, out overrideLs) && overrideLs)
                {
                    continue;
                }
                agent.SendMessageIfRootAgent(m, m_Scene.ID);
            }
        }

        public void UpdateWindlightProfileToClient(IAgent agent)
        {
            GenericMessage m;

            m = (m_WindlightValid) ?
                CompileWindlightSettings(m_SkyWindlight, m_WaterWindlight) :
                CompileResetWindlightSettings();

            agent.SendMessageAlways(m, m_Scene.ID);
        }
        #endregion

        public void ResetLightShare()
        {
            bool windlightUpdated = false;
            m_OverrideLightSharePerAgent.Clear();
            m_LightShareLock.AcquireWriterLock(-1);
            try
            {
                if (!m_WeatherConfig.EnableLightShareControl)
                {
                    m_WindlightValid = false;
                    windlightUpdated = true;
                    m_ImmediateSunUpdate = true;
                }
            }
            finally
            {
                m_LightShareLock.ReleaseWriterLock();
            }

            if (windlightUpdated)
            {
                SendToAllClients(CompileResetWindlightSettings());
            }
        }

        public bool IsWindLightValid
        {
            get
            {
                m_LightShareLock.AcquireReaderLock(-1);
                try
                {
                    return m_WindlightValid;
                }
                finally
                {
                    m_LightShareLock.ReleaseReaderLock();
                }
            }
        }

        public WindlightSkyData SkyData
        {
            get
            {
                m_LightShareLock.AcquireReaderLock(-1);
                try
                {
                    return m_WindlightValid ?
                        m_SkyWindlight :
                        WindlightSkyData.Defaults;
                }
                finally
                {
                    m_LightShareLock.ReleaseReaderLock();
                }
            }
            set
            {
                bool windlightUpdated = false;
                m_LightShareLock.AcquireWriterLock(-1);
                try
                {
                    if (!m_WeatherConfig.EnableLightShareControl)
                    {
                        m_SkyWindlight = value;
                        windlightUpdated = true;
                        m_WindlightValid = true;
                        m_ImmediateSunUpdate = true;
                    }
                }
                finally
                {
                    m_LightShareLock.ReleaseWriterLock();
                }
                if (windlightUpdated)
                {
                    UpdateWindlightProfileToClients();
                }
            }
        }

        public WindlightWaterData WaterData
        {
            get
            {
                m_LightShareLock.AcquireReaderLock(-1);
                try
                {
                    return m_WindlightValid ?
                        m_WaterWindlight :
                        WindlightWaterData.Defaults;
                }
                finally
                {
                    m_LightShareLock.ReleaseReaderLock();
                }
            }
            set
            {
                bool windlightUpdated = false;
                m_LightShareLock.AcquireWriterLock(-1);
                try
                {
                    if (!m_WeatherConfig.EnableLightShareControl)
                    {
                        m_WaterWindlight = value;
                        windlightUpdated = true;
                        m_WindlightValid = true;
                    }
                }
                finally
                {
                    m_LightShareLock.ReleaseWriterLock();
                }
                if (windlightUpdated)
                {
                    UpdateWindlightProfileToClients();
                }
            }
        }

        #region Windlight message compiler
        private GenericMessage CompileResetWindlightSettings()
        {
            GenericMessage m = new GenericMessage();
            m.Method = "WindlightReset";
            m.ParamList.Add(new byte[0]);
            return m;
        }

        private GenericMessage CompileWindlightSettings(WindlightSkyData skyWindlight, WindlightWaterData waterWindlight)
        {
            GenericMessage m = new GenericMessage();
            m.Method = "Windlight";
            byte[] mBlock = new byte[249];
            int pos = 0;
            AddToCompiledWL(waterWindlight.Color, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.FogDensityExponent, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.UnderwaterFogModifier, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.ReflectionWaveletScale, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.FresnelScale, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.FresnelOffset, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.RefractScaleAbove, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.RefractScaleBelow, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.BlurMultiplier, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.BigWaveDirection, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.LittleWaveDirection, ref mBlock, ref pos);
            AddToCompiledWL(waterWindlight.NormalMapTexture, ref mBlock, ref pos);

            AddToCompiledWL(skyWindlight.Horizon, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.HazeHorizon, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.BlueDensity, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.HazeDensity, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.DensityMultiplier, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.DistanceMultiplier, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.SunMoonColor, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.SunMoonPosition, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.Ambient, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.EastAngle, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.SunGlowFocus, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.SunGlowSize, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.SceneGamma, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.StarBrightness, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.CloudColor, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.CloudXYDensity, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.CloudCoverage, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.CloudScale, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.CloudDetailXYDensity, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.CloudScrollX, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.CloudScrollY, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.MaxAltitude, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.CloudScrollXLock, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.CloudScrollYLock, ref mBlock, ref pos);
            AddToCompiledWL(skyWindlight.DrawClassicClouds, ref mBlock, ref pos);
            m.ParamList.Add(mBlock);
            return m;
        }

        private void AddToCompiledWL(bool v, ref byte[] mBlock, ref int pos)
        {
            mBlock[pos++] = v ?
                (byte)1 :
                (byte)0;
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
            Vector3 c = v.AsVector3;
            c *= 255;
            AddToCompiledWL(c.X, ref mBlock, ref pos);
            AddToCompiledWL(c.Y, ref mBlock, ref pos);
            AddToCompiledWL(c.Z, ref mBlock, ref pos);
        }

        private void AddToCompiledWL(UUID v, ref byte[] mBlock, ref int pos)
        {
            v.ToBytes(mBlock, pos);
            pos += 16;
        }

        private void AddToCompiledWL(double v, ref byte[] mBlock, ref int pos)
        {
            byte[] b = BitConverter.GetBytes((float)v);
            if (!BitConverter.IsLittleEndian)
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
