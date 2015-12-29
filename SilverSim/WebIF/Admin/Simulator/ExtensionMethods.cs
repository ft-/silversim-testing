// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;

namespace SilverSim.WebIF.Admin.Simulator
{
    public static class ExtensionMethods
    {
        public static Map ToJsonMap(this EstateInfo estate)
        {
            Map m = new Map();
            m.Add("ID", (int)estate.ID);
            m.Add("ParentEstateID", (int)estate.ParentEstateID);
            m.Add("Name", estate.Name);
            m.Add("Flags", ((uint)estate.Flags).ToString());
            m.Add("Owner", estate.Owner.ToString());
            m.Add("PricePerMeter", estate.PricePerMeter);
            m.Add("BillableFactor", estate.BillableFactor);
            m.Add("SunPosition", estate.SunPosition);
            m.Add("AbuseEmail", estate.AbuseEmail);
            m.Add("UseGlobalTime", estate.UseGlobalTime);
            return m;
        }

        public static Map ToJsonMap(this RegionInfo region)
        {
            Map m = new Map();
            m.Add("ID", region.ID);
            m.Add("Location", region.Location.ToString());
            m.Add("Size", region.Size.ToString());
            m.Add("Name", region.Name);
            m.Add("ServerIP", region.ServerIP);
            m.Add("ServerHttpPort", (int)region.ServerHttpPort);
            m.Add("ServerURI", region.ServerURI);
            m.Add("ServerPort", (int)region.ServerPort);
            m.Add("RegionMapTexture", region.ParcelMapTexture.ToString());
            m.Add("Access", (int)region.Access);
            m.Add("Owner", region.Owner.ToString());
            m.Add("Flags", ((uint)region.Flags).ToString());
            return m;
        }

        public static Map ToJsonMap(this IAgent agent, SceneInterface scene)
        {
            Map m = new Map();
            m.Add("ID", agent.Owner.ID);
            m.Add("FirstName", agent.Owner.FirstName);
            m.Add("LastName", agent.Owner.LastName);
            m.Add("HomeURI", agent.Owner.HomeURI != null ? agent.Owner.HomeURI.ToString() : string.Empty);
            m.Add("Type", agent.IsNpc ? "Npc" : "User");
            m.Add("IsRoot", agent.IsInScene(scene));
            m.Add("Position", agent.GlobalPosition.ToString());
            m.Add("Health", agent.Health);
            return m;
        }
    }
}
