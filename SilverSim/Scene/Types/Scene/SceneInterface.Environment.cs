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
using System.Threading;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public EnvironmentController Environment;

        public class EnvironmentController : IDisposable
        {
            private const int BASE_REGION_SIZE = 256;

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
            //bool m_SunFixed = false;

            public EnvironmentController(SceneInterface scene)
            {
                m_Scene = scene;
                m_WindData.Speeds = new WindVector[scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_ENTRY_WIDTH, scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_ENTRY_WIDTH];
                m_CloudData.CloudCoverages = new double[scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_ENTRY_WIDTH, scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_ENTRY_WIDTH];
                m_SunData.SunDirection = new Vector3();
            }

            public void Dispose()
            {
                m_Scene = null;
            }

            #region Update of Wind Data
            private List<LayerData> CompileWindData()
            {
                int y;
                int x;
                int NumTerrainPatchesPerY = (int)m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES / LayerCompressor.LAYER_PATCH_ENTRY_WIDTH;

                LayerPatch[] p = new LayerPatch[2 * m_Scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES * m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];

                for (y = 0; y < m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_SIM_WIDTH; ++y)
                {
                    for (x = 0; x < m_Scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_SIM_WIDTH; ++x)
                    {
                        p[2 * (y * m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES + x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES) + 0].Data[y, x] = (float)m_WindData.Speeds[y, x].X;
                        p[2 * (y * m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES + x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES) + 1].Data[y, x] = (float)m_WindData.Speeds[y, x].Y;
                    }
                }
                for (y = 0; y < m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for (x = 0; x < m_Scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        p[2 * (y * NumTerrainPatchesPerY + x) + 0].X = x * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        p[2 * (y * NumTerrainPatchesPerY + x) + 0].Y = y * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        p[2 * (y * NumTerrainPatchesPerY + x) + 1].X = x * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        p[2 * (y * NumTerrainPatchesPerY + x) + 1].Y = y * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                    }
                }

                List<LayerData> mlist = new List<LayerData>();
                if (BASE_REGION_SIZE == m_Scene.RegionData.Size.X && BASE_REGION_SIZE == m_Scene.RegionData.Size.Y)
                {
                    mlist.Add(LayerCompressor.ToLayerMessage(p, LayerData.LayerDataType.Wind));
                }
                else
                {
                    int offset = 0;
                    while (offset < p.Length)
                    {
                        if (p.Length - offset > LayerCompressor.MAX_PATCHES_PER_MESSAGE)
                        {
                            mlist.Add(LayerCompressor.ToLayerMessage(p, LayerData.LayerDataType.WindExtended, offset, LayerCompressor.MAX_PATCHES_PER_MESSAGE));
                            offset += LayerCompressor.MAX_PATCHES_PER_MESSAGE;
                        }
                        else
                        {
                            mlist.Add(LayerCompressor.ToLayerMessage(p, LayerData.LayerDataType.WindExtended, offset, p.Length - offset));
                            offset = p.Length;
                        }
                    }
                }
                return mlist;
            }

            public void UpdateWindDataToSingleClient(IAgent agent)
            {
                List<LayerData> mlist = CompileWindData();
                foreach (LayerData m in mlist)
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }

            private void UpdateWindDataToClients()
            {
                List<LayerData> mlist = CompileWindData();
                foreach (LayerData m in mlist)
                {
                    SendToAllClients(m);
                }
            }
            #endregion

            #region Update of Cloud data
            private List<LayerData> CompileCloudData()
            {
                int y;
                int x;
                LayerPatch[] p = new LayerPatch[m_Scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES * m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                int NumTerrainPatchesPerY = (int)m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES / LayerCompressor.LAYER_PATCH_ENTRY_WIDTH;

                for (y = 0; y < m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_ENTRY_WIDTH; ++y)
                {
                    for (x = 0; x < m_Scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_ENTRY_WIDTH; ++x)
                    {
                        p[y * m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES + x / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES].Data[y, x] = (float)m_CloudData.CloudCoverages[y, x];
                    }
                }
                for (y = 0; y < m_Scene.RegionData.Size.Y / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for (x = 0; x < m_Scene.RegionData.Size.X / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        p[y * NumTerrainPatchesPerY + x].X = x * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                        p[y * NumTerrainPatchesPerY + x].Y = y * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                    }
                }

                List<LayerData> mlist = new List<LayerData>();
                if (BASE_REGION_SIZE == m_Scene.RegionData.Size.X && BASE_REGION_SIZE == m_Scene.RegionData.Size.Y)
                {
                    mlist.Add(LayerCompressor.ToLayerMessage(p, LayerData.LayerDataType.Cloud));
                }
                else
                {
                    int offset = 0;
                    int MaxPatchesPerMessage = LayerCompressor.MAX_PATCHES_PER_MESSAGE;
                    if(MaxPatchesPerMessage % 2 != 0)
                    {
                        --MaxPatchesPerMessage;
                    }

                    while (offset < p.Length)
                    {
                        if (p.Length - offset > MaxPatchesPerMessage)
                        {
                            mlist.Add(LayerCompressor.ToLayerMessage(p, LayerData.LayerDataType.CloudExtended, offset, MaxPatchesPerMessage));
                            offset += MaxPatchesPerMessage;
                        }
                        else
                        {
                            mlist.Add(LayerCompressor.ToLayerMessage(p, LayerData.LayerDataType.WindExtended, offset, p.Length - offset));
                            offset = p.Length;
                        }
                    }
                }

                return mlist;
            }

            public void UpdateCloudDataToSingleClient(IAgent agent)
            {
                List<LayerData> mlist = CompileCloudData();
                foreach (LayerData m in mlist)
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }

            private void UpdateCloudDataToClients()
            {
                List<LayerData> mlist = CompileCloudData();
                foreach(LayerData m in mlist)
                {
                    SendToAllClients(m);
                }
            }
            #endregion

            #region Update of Windlight Data
            private void UpdateWindlightProfileToClients()
            {
                GenericMessage m;

                if (m_WindlightValid)
                {
                    m = compileWindlightSettings(m_SkyWindlight, m_WaterWindlight);
                }
                else
                {
                    m = compileResetWindlightSettings();
                }

                SendToAllClients(m);
            }

            public void UpdateWindlightProfileToClient(IAgent agent)
            {
                GenericMessage m;
                if (m_WindlightValid)
                {
                    m = compileWindlightSettings(m_SkyWindlight, m_WaterWindlight);
                }
                else
                {
                    m = compileResetWindlightSettings();
                }
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

            private void SendSimulatorTimeMessageToClient(IAgent agent)
            {
                SimulatorViewerTimeMessage m = new SimulatorViewerTimeMessage();
                m.SunPhase = m_SunData.SunPhase;
                m.UsecSinceStart = m_SunData.UsecSinceStart;
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

            #region Windlight message compiler
            private GenericMessage compileResetWindlightSettings()
            {
                GenericMessage m = new GenericMessage();
                m.Method = "WindlightReset";
                m.ParamList = new byte[0];
                return m;
            }

            private GenericMessage compileWindlightSettings(WindlightSkyData skyWindlight, WindlightWaterData waterWindlight)
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
