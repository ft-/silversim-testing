/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Types;
using System.Collections.Generic;

namespace ArribaSim.Scene.Types.Terrain
{
    public class TerrainMap
    {
        public struct TerrainPatch
        {
            public uint X;
            public uint Y;
            public float Height;

            public TerrainPatch(uint x, uint y, float height)
            {
                X = x;
                Y = y;
                Height = height;
            }
        }

        public const uint TERRAIN_PATCH_SIZE = 4;
        public uint SizeX { get; private set; }
        public uint SizeY { get; private set; }
        private float[] m_Map;
        private uint m_PatchCountX;

        public TerrainMap(uint sizeX, uint sizeY)
        {
            SizeX = sizeY;
            SizeY = sizeY;
            m_PatchCountX = sizeX / TERRAIN_PATCH_SIZE;
            m_Map = new float[(sizeX / TERRAIN_PATCH_SIZE) * (sizeY / TERRAIN_PATCH_SIZE)];
        }

        public double this[uint x, uint y]
        {
            get
            {
                if(x >= SizeX || y >= SizeY)
                {
                    throw new KeyNotFoundException();
                }
                x /= TERRAIN_PATCH_SIZE;
                y /= TERRAIN_PATCH_SIZE;
                return m_Map[y * m_PatchCountX + x];
            }
            set
            {
                if (x >= SizeX || y >= SizeY)
                {
                    throw new KeyNotFoundException();
                }

                x /= TERRAIN_PATCH_SIZE;
                y /= TERRAIN_PATCH_SIZE;
                m_Map[y * m_PatchCountX + x] = (float)value;
            }
        }

        public double this[Vector3 pos]
        {
            get
            {
                if(pos.X < 0 || pos.Y < 0 || pos.X >= SizeX || pos.Y >= SizeY)
                {
                    throw new KeyNotFoundException();
                }

                uint x = (uint)pos.X;
                uint y = (uint)pos.Y;
                x /= TERRAIN_PATCH_SIZE;
                y /= TERRAIN_PATCH_SIZE;

                return m_Map[y * m_PatchCountX + x];
            }
            set
            {
                if (pos.X < 0 || pos.Y < 0 || pos.X >= SizeX || pos.Y >= SizeY)
                {
                    throw new KeyNotFoundException();
                }

                uint x = (uint)pos.X;
                uint y = (uint)pos.Y;
                x /= TERRAIN_PATCH_SIZE;
                y /= TERRAIN_PATCH_SIZE;

                m_Map[y * m_PatchCountX + x] = (float)value;
            }
        }

        public IList<TerrainPatch> GetTerrainDistanceSorted(Vector3 v)
        {
            SortedList<int, TerrainPatch> sorted = new SortedList<int, TerrainPatch>();
            uint x;
            uint y;

            if(v.X < 0)
            {
                x = 0;
            }
            else if(v.X >= SizeX)
            {
                x = SizeX - 1;
            }
            else
            {
                x = (uint)v.X / TERRAIN_PATCH_SIZE;
            }

            if (v.Y < 0)
            {
                y = 0;
            }
            else if(v.Y >= SizeY)
            {
                y = SizeY - 1;
            }
            else
            {
                y = (uint)v.Y / TERRAIN_PATCH_SIZE;
            }

            int distance;

            for(uint py = 0; py < SizeY / TERRAIN_PATCH_SIZE; ++py)
            {
                for(uint px = 0; px < SizeX / TERRAIN_PATCH_SIZE; ++px)
                {
                    distance = ((int)px - (int)x) * ((int)px - (int)x) + ((int)py - (int)y) * ((int)py - (int)y);
                    sorted.Add(distance, new TerrainPatch(px, py, m_Map[py * m_PatchCountX + px]));
                }
            }

            return sorted.Values;
        }
    }
}
