// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        private const int BASE_REGION_SIZE = 256;

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
                for (int x = 0; x < 16; ++x)
                {
                    Vector3 actpos = basepos;
                    actpos.X += x * 4;
                    actpos.Y += y * 4;
                    if (rX && x < 8)
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
    }
}
