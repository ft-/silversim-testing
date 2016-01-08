// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.WindLight;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Generic;
using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Viewer.Messages.Region;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public EnvironmentController Environment;

        EnvironmentSettings m_EnvironmentSettings;

        public EnvironmentSettings EnvironmentSettings
        {
            get
            {
                EnvironmentSettings envSettings = m_EnvironmentSettings;
                if (envSettings == null)
                {
                    return null;
                }
                return new EnvironmentSettings(envSettings);
            }
            set
            {
                m_EnvironmentSettings = (null != value) ?
                    new EnvironmentSettings(m_EnvironmentSettings) :
                    null;
            }
        }

        public class EnvironmentController
        {
            private const int BASE_REGION_SIZE = 256;

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
                        waterData.Color = new Color(4/255f, 38/255f, 64/255f);
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

            [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
            public struct SunData
            {
                public UInt64 UsecSinceStart;
                public UInt32 SecPerDay;
                public UInt32 SecPerYear;
                public Vector3 SunDirection;
                public double SunPhase;
                public double FixedSunPhase;
                public Vector3 SunAngVelocity;
                public bool IsSunFixed;
            }

            public IWindModel Wind
            {
                get; private set;
            }

            bool m_WindlightValid;
            struct WeatherConfig
            {
                public bool EnableLightShareControl;
            }

            WeatherConfig m_WeatherConfig = new WeatherConfig();
            readonly ReaderWriterLock m_LightShareLock = new ReaderWriterLock();
            WindlightSkyData m_SkyWindlight = new WindlightSkyData();
            WindlightWaterData m_WaterWindlight = new WindlightWaterData();
            SunData m_SunData = new SunData();
            readonly SceneInterface m_Scene;
            readonly System.Timers.Timer m_Timer = new System.Timers.Timer(1000 / 60f);
            readonly RwLockedDictionary<UUID, bool> m_OverrideLightSharePerAgent = new RwLockedDictionary<UUID, bool>();

            int m_SunUpdateEveryMsecs = 10000;
            uint m_SendSimTimeAfterNSunUpdates = 10 - 1;
            int m_UpdateWindModelEveryMsecs = 10000;

            uint m_SunUpdatesUntilSendSimTime;

            public int SunUpdateEveryMsecs
            {
                get
                {
                    lock(this)
                    {
                        return m_SunUpdateEveryMsecs;
                    }
                }
                set
                {
                    lock(this)
                    {
                        m_SunUpdateEveryMsecs = value;
                    }
                }
            }

            public uint SendSimTimeEveryNthSunUpdate
            {
                get
                {
                    lock(this)
                    {
                        return m_SendSimTimeAfterNSunUpdates + 1;
                    }
                }
                set
                {
                    lock(this)
                    {
                        if (value >= 1)
                        {
                            m_SendSimTimeAfterNSunUpdates = value - 1;
                        }
                    }
                }
            }

            public int UpdateWindModelEveryMsecs
            {
                get
                {
                    lock(this)
                    {
                        return m_UpdateWindModelEveryMsecs;
                    }
                }
                set
                {
                    lock(this)
                    {
                        if (value >= 1)
                        {
                            m_UpdateWindModelEveryMsecs = value;
                        }
                    }
                }
            }

            public Vector3 SunDirection
            {
                get
                {
                    lock(this)
                    {
                        return m_SunData.SunDirection;
                    }
                }
                set
                {
                    lock(this)
                    {
                        m_SunData.SunDirection = value;
                    }
                }
            }

            public EnvironmentController(SceneInterface scene)
            {
                m_Scene = scene;
                Wind = new NoWindModel();
                m_SunData.SunDirection = new Vector3();
                m_SunData.SecPerDay = 4 * 60 * 60;
                m_SunData.SecPerYear = 11 * m_SunData.SecPerDay;
            }

            public void Start()
            {
                lock(this)
                {
                    if(!m_Timer.Enabled)
                    {
                        EnvironmentTimer(this, null);
                        m_Timer.Elapsed += EnvironmentTimer;
                        m_LastFpsTickCount = System.Environment.TickCount;
                        m_LastWindModelUpdateTickCount = m_LastFpsTickCount;
                        m_LastSunUpdateTickCount = m_LastFpsTickCount;
                        m_CountedTicks = 0;
                        m_Timer.Start();
                    }
                }
            }

            public void Stop()
            {
                lock(this)
                {
                    if(m_Timer.Enabled)
                    {
                        m_Timer.Stop();
                        m_Timer.Elapsed -= EnvironmentTimer;
                    }
                }
            }

            int m_LastFpsTickCount;
            int m_LastWindModelUpdateTickCount;
            int m_LastSunUpdateTickCount;
            int m_CountedTicks;
            double m_EnvironmentFps;

            public double EnvironmentFps
            {
                get
                {
                    lock (this)
                    {
                        return m_EnvironmentFps;
                    }
                }
            }
            
            private void EnvironmentTimer(object sender, System.Timers.ElapsedEventArgs e)
            {
                ++m_CountedTicks;
                int newTickCount = System.Environment.TickCount;
                if (newTickCount - m_LastFpsTickCount >= 1000)
                {
                    int timeDiff = newTickCount - m_LastFpsTickCount;
                    m_LastFpsTickCount = System.Environment.TickCount;
                    lock(this)
                    {
                        m_EnvironmentFps = m_CountedTicks * (double)timeDiff / 1000f;
                    }
                    m_CountedTicks = 0;
                }

                if(newTickCount - m_LastSunUpdateTickCount >= m_SunUpdateEveryMsecs)
                {
                    m_LastSunUpdateTickCount = newTickCount;
                    UpdateSunDirection();
                    if(m_SunUpdatesUntilSendSimTime-- == 0)
                    {
                        m_SunUpdatesUntilSendSimTime = m_SendSimTimeAfterNSunUpdates;
                        SendSimulatorTimeMessageToAllClients();
                    }
                }
                if (null != Wind)
                {
                    if(newTickCount - m_LastWindModelUpdateTickCount >= m_UpdateWindModelEveryMsecs)
                    {
                        m_LastWindModelUpdateTickCount = m_UpdateWindModelEveryMsecs;
                        Wind.UpdateModel(m_SunData);
                    }
                }
            }

            #region Update of sun direction
            /* source of algorithm is secondlifescripters mailing list */
            double AverageSunTilt = -0.25 * Math.PI;
            double SeasonalSunTilt = 0.03 * Math.PI;
            double SunNormalizedOffset = 0.45;

            public void SetSunDurationParams(uint secperday, uint daysperyear)
            {
                lock(this)
                {
                    m_SunData.SecPerDay = secperday;
                    m_SunData.SecPerYear = secperday * daysperyear;
                }
            }

            public void GetSunDurationParams(out uint secperday, out uint daysperyear)
            {
                lock(this)
                {
                    secperday = m_SunData.SecPerDay;
                    daysperyear = m_SunData.SecPerYear / m_SunData.SecPerDay;
                }
            }

            public double TimeOfDay
            {
                get
                {
                    ulong utctime = Date.GetUnixTime();
                    return m_SunData.IsSunFixed ?
                        utctime :
                        utctime % (m_SunData.SecPerDay);
                }
            }

            public double ActualSunPosition
            {
                get
                {
                    lock(this)
                    {
                        return m_SunData.SunPhase * 12 / Math.PI;
                    }
                }
            }

            public double FixedSunPosition
            {
                get
                {
                    lock(this)
                    {
                        return m_SunData.FixedSunPhase * 12 / Math.PI;
                    }
                }
                set
                {
                    lock(this)
                    {
                        m_SunData.FixedSunPhase = value * Math.PI / 12;
                    }
                }
            }

            public bool IsSunFixed
            {
                get
                {
                    lock(this)
                    {
                        return m_SunData.IsSunFixed;
                    }
                }
                set
                {
                    lock(this)
                    {
                        m_SunData.IsSunFixed = value;
                    }
                }
            }

            public void UpdateSunDirection()
            {
                double DailyOmega;
                double YearlyOmega;
                lock (this)
                {
                    DailyOmega = 2 / m_SunData.SecPerDay;
                    YearlyOmega = 2 / (m_SunData.SecPerYear);
                }
                ulong utctime = Date.GetUnixTime();
                bool sunFixed = m_SunData.IsSunFixed;
                if(sunFixed)
                {
                    utctime = 0;
                }

                double daily_phase = DailyOmega * utctime;
                double sun_phase = daily_phase % (2 * Math.PI);
                double yearly_phase = YearlyOmega * utctime;
                double tilt = AverageSunTilt + SeasonalSunTilt * Math.Sin(yearly_phase);

                m_SunData.SunPhase = sun_phase;
                Vector3 sunDirection = new Vector3(Math.Cos(-sun_phase), Math.Sin(-sun_phase), 0);
                Quaternion tiltRot = new Quaternion(tilt, 1, 0, 0);

                sunDirection *= tiltRot;
                Vector3 sunVelocity = new Vector3(0, 0, DailyOmega);
                if(sunFixed)
                {
                    sunVelocity = Vector3.Zero;
                }
                sunVelocity *= tiltRot;
                sunDirection.Z += SunNormalizedOffset;
                double radius = sunDirection.Length;
                sunDirection = sunDirection.Normalize();
                sunVelocity *= (1 / radius);
                lock (this)
                {
                    m_SunData.SunDirection = sunDirection;
                    m_SunData.SunAngVelocity = sunVelocity;
                    m_SunData.UsecSinceStart = utctime * 1000000;
                }
            }
            #endregion

            #region Update of Wind Data
            private List<LayerData> CompileWindData(Vector3 basepos)
            {
                List<LayerData> mlist = new List<LayerData>();
                List<LayerPatch> patchesList = new List<LayerPatch>();
                LayerPatch patchX = new LayerPatch();
                LayerPatch patchY = new LayerPatch();

                /* round to nearest low pos */
                bool rX = basepos.X % 256 >= 128;
                bool rY = basepos.Y % 256 >= 128;
                basepos.X = Math.Floor(basepos.X / 256) * 256;
                basepos.Y = Math.Floor(basepos.Y / 256) * 256;

                for (int y = 0; y < 16; ++y)
                {
                    for(int x = 0; x < 16; ++x)
                    {
                        Vector3 actpos = basepos;
                        actpos.X += x * 4;
                        actpos.Y += y * 4;
                        if(rX && x < 8)
                        {
                            actpos.X += 128;
                        }
                        if (rY && y < 8)
                        {
                            actpos.Y += 128;
                        }
                        Vector3 w = Wind[actpos];
                        patchX[x, y] = (float)w.X;
                        patchY[x, y] = (float)w.Y;
                    }
                }

                patchesList.Add(patchX);
                patchesList.Add(patchY);

                LayerData.LayerDataType layerType = LayerData.LayerDataType.Wind;

                if (BASE_REGION_SIZE < m_Scene.SizeX || BASE_REGION_SIZE < m_Scene.SizeY)
                {
                    layerType = LayerData.LayerDataType.WindExtended;
                }
                int offset = 0;
                while (offset < patchesList.Count)
                {
                    int remaining = Math.Min(patchesList.Count - offset, LayerCompressor.MESSAGES_PER_WIND_LAYER_PACKET);
                    int actualused;
                    mlist.Add(LayerCompressor.ToLayerMessage(patchesList, layerType, offset, remaining, out actualused));
                    offset += actualused;
                }
                return mlist;
            }

            public void UpdateWindDataToSingleClient(IAgent agent)
            {
                List<LayerData> mlist = CompileWindData(agent.GlobalPosition);
                foreach (LayerData m in mlist)
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }

            private void UpdateWindDataToClients()
            {
                foreach (IAgent agent in m_Scene.Agents)
                {
                    List<LayerData> mlist = CompileWindData(agent.GlobalPosition);
                    foreach (LayerData m in mlist)
                    {
                        agent.SendMessageAlways(m, m_Scene.ID);
                    }
                }
            }
            #endregion

            #region Client-specific update of Windlight Data
            public void SendTargetedWindlightProfile(UUID agentID, WindlightSkyData skyData, WindlightWaterData waterData)
            {
                m_OverrideLightSharePerAgent[agentID] = true;
                IAgent agent;
                if(m_Scene.RootAgents.TryGetValue(agentID, out agent))
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

            #region Viewer time message update
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

            public void SendSimulatorTimeMessageToClient(IAgent agent)
            {
                SimulatorViewerTimeMessage m = new SimulatorViewerTimeMessage();
                m.SunPhase = m_SunData.SunPhase;
                m.UsecSinceStart = Date.GetUnixTime() * 1000000;
                m.SunDirection = m_SunData.SunDirection;
                m.SunAngVelocity = m_SunData.SunAngVelocity;
                m.SecPerYear = m_SunData.SecPerYear;
                m.SecPerDay = m_SunData.SecPerDay;
                agent.SendMessageAlways(m, m_Scene.ID);
            }
            #endregion

            private void SendToAllClients(Message m)
            {
                foreach (IAgent agent in m_Scene.Agents)
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }

            public enum BooleanWeatherParams
            {
                EnableLightShare,
            }

            public bool this[BooleanWeatherParams type]
            {
                get
                {
                    switch(type)
                    {
                        case BooleanWeatherParams.EnableLightShare:
                            return m_WeatherConfig.EnableLightShareControl;

                        default:
                            return false;
                    }
                }
                set
                {
                    switch(type)
                    {
                        case BooleanWeatherParams.EnableLightShare:
                            m_LightShareLock.AcquireWriterLock(-1);
                            try
                            {
                                m_WeatherConfig.EnableLightShareControl = value;
                            }
                            finally
                            {
                                m_LightShareLock.ReleaseWriterLock();
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

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
                    }
                }
                finally
                {
                    m_LightShareLock.ReleaseWriterLock();
                }

                if(windlightUpdated)
                {
                    SendToAllClients(CompileResetWindlightSettings());
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
                        }
                    }
                    finally
                    { 
                        m_LightShareLock.ReleaseWriterLock();
                    }
                    if(windlightUpdated)
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
