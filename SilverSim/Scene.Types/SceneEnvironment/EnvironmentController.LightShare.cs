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

        public struct WLVector2
        {
            public double X;
            public double Y;

            public WLVector2(Vector3 v)
            {
                X = v.X;
                Y = v.Y;
            }

            public WLVector2(double x, double y)
            {
                X = x;
                Y = y;
            }

            public static implicit operator Vector3(WLVector2 v) => new Vector3(v.X, v.Y, 0);
        }

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

            public static implicit operator Quaternion(WLVector4 v) => new Quaternion(v.X, v.Y, v.Z, v.W);
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
            public WLVector2 CloudScroll;
            public bool CloudScrollXLock;
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

            public static WindlightSkyData Defaults => new WindlightSkyData()
            {
                Horizon = new WLVector4(0.25, 0.25, 0.32, 0.32),
                HazeHorizon = 0.19,
                BlueDensity = new WLVector4(0.12, 0.22, 0.38, 0.38),
                HazeDensity = 0.7,
                DensityMultiplier = 0.18,
                DistanceMultiplier = 0.8,
                MaxAltitude = 1605,
                SunMoonPosition = 0.317,
                SunMoonColor = new WLVector4(0.24, 0.26, 0.30, 0.30),
                Ambient = new WLVector4(0.35, 0.35, 0.35, 0.35),
                EastAngle = 0,
                SunGlowFocus = 0.1,
                SunGlowSize = 1.75,
                SceneGamma = 1.0,
                StarBrightness = 0,
                CloudColor = new WLVector4(0.41, 0.41, 0.41, 0.41),
                CloudXYDensity = new Vector3(1.0, 0.53, 1.0),
                CloudCoverage = 0.27,
                CloudScale = 0.42,
                CloudDetailXYDensity = new Vector3(1.0, 0.52, 0.12),
                CloudScroll = new WLVector2(0.2, 0.01),
                DrawClassicClouds = true,
                CloudScrollXLock = false,
                CloudScrollYLock = false
            };
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
        public struct WindlightWaterData
        {
            public WLVector2 BigWaveDirection;
            public WLVector2 LittleWaveDirection;
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

            public static WindlightWaterData Defaults => new WindlightWaterData()
            {
                Color = new Color(4 / 255f, 38 / 255f, 64 / 255f),
                FogDensityExponent = 4,
                UnderwaterFogModifier = 0.25,
                ReflectionWaveletScale = new Vector3(2.0, 2.0, 2.0),
                FresnelScale = 0.4,
                FresnelOffset = 0.5,
                RefractScaleAbove = 0.03,
                RefractScaleBelow = 0.2,
                BlurMultiplier = 0.04,
                BigWaveDirection = new WLVector2(1.05, -0.42),
                LittleWaveDirection = new WLVector2(1.11, -1.16),
                NormalMapTexture = new UUID("822ded49-9a6c-f61c-cb89-6df54f42cdf4")
            };
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
            var m = (m_WindlightValid) ?
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
            var m = (m_WindlightValid) ?
                CompileWindlightSettings(m_SkyWindlight, m_WaterWindlight) :
                CompileResetWindlightSettings();

            agent.SendMessageAlways(m, m_Scene.ID);
        }

        public void UpdateWindlightProfileToClientNoReset(IAgent agent)
        {
            if (m_WindlightValid)
            {
                agent.SendMessageAlways(CompileWindlightSettings(m_SkyWindlight, m_WaterWindlight), m_Scene.ID);
            }
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
            var m = new GenericMessage();
            m.Method = "WindlightReset";
            m.ParamList.Add(new byte[0]);
            return m;
        }

        private GenericMessage CompileWindlightSettings(WindlightSkyData skyWindlight, WindlightWaterData waterWindlight)
        {
            var m = new GenericMessage();
            m.Method = "Windlight";
            var mBlock = new byte[249];
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
            AddToCompiledWL(skyWindlight.CloudScroll, ref mBlock, ref pos);
            AddToCompiledWL((ushort)skyWindlight.MaxAltitude, ref mBlock, ref pos);
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

        private void AddToCompiledWL(WLVector2 v, ref byte[] mBlock, ref int pos)
        {
            AddToCompiledWL(v.X, ref mBlock, ref pos);
            AddToCompiledWL(v.Y, ref mBlock, ref pos);
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

        private void AddToCompiledWL(ushort v, ref byte[] mBlock, ref int pos)
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
