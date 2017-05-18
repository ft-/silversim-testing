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
            var mlist = new List<LayerData>();
            var patchesList = new List<LayerPatch>();
            var patchX = new LayerPatch();
            var patchY = new LayerPatch();

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

            var layerType = LayerData.LayerDataType.Wind;

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
            foreach (LayerData m in CompileWindData(agent.GlobalPosition))
            {
                agent.SendMessageAlways(m, m_Scene.ID);
            }
        }

        private void UpdateWindDataToClients()
        {
            foreach (IAgent agent in m_Scene.Agents)
            {
                foreach (LayerData m in CompileWindData(agent.GlobalPosition))
                {
                    agent.SendMessageAlways(m, m_Scene.ID);
                }
            }
        }
        #endregion
    }
}
