// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public void DetermineInitialAgentLocation(IAgent agent, TeleportFlags teleportFlags, Vector3 destinationLocation, Vector3 destinationLookAt)
        {
            GridVector size = RegionData.Size;
            if (destinationLocation.X < 0 || destinationLocation.X >= size.X)
            {
                destinationLocation.X = size.X / 2f;
            }
            if (destinationLocation.Y < 0 || destinationLocation.Y >= size.X)
            {
                destinationLocation.Y = size.Y / 2f;
            }

            ParcelInfo p = Parcels[destinationLocation];
            switch(p.LandingType)
            {
                case TeleportLandingType.None:
                    break;

                case TeleportLandingType.Direct:
                    break;

                case TeleportLandingType.LandingPoint:
                    destinationLocation = p.LandingPosition;
                    destinationLookAt = p.LandingLookAt;
                    break;
            }

            if (destinationLocation.X < 0 || destinationLocation.X >= size.X)
            {
                destinationLocation.X = size.X / 2f;
            }
            if (destinationLocation.Y < 0 || destinationLocation.Y >= size.X)
            {
                destinationLocation.Y = size.Y / 2f;
            }

            agent.Rotation = destinationLookAt.AgentLookAtToQuaternion();

            double t0_0 = Terrain[(uint)Math.Floor(destinationLocation.X), (uint)Math.Floor(destinationLocation.Y)];
            double t0_1 = Terrain[(uint)Math.Floor(destinationLocation.X), (uint)Math.Ceiling(destinationLocation.Y)];
            double t1_0 = Terrain[(uint)Math.Ceiling(destinationLocation.X), (uint)Math.Floor(destinationLocation.Y)];
            double t1_1 = Terrain[(uint)Math.Ceiling(destinationLocation.X), (uint)Math.Ceiling(destinationLocation.Y)];
            double t_x = agent.Position.X - Math.Floor(destinationLocation.X);
            double t_y = agent.Position.Y - Math.Floor(destinationLocation.Y);

            double t0 = t0_0.Lerp(t0_1, t_y);
            double t1 = t1_0.Lerp(t1_1, t_y);

            destinationLocation.Z = t0.Lerp(t1, t_x) + 1;

            agent.Position = destinationLocation;
        }
    }
}
