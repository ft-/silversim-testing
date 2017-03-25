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
            m.Add("Owner", estate.Owner.ToMap());
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
            m.Add("Location", region.Location.GridLocation);
            m.Add("Size", region.Size.GridLocation);
            m.Add("Name", region.Name);
            m.Add("ServerIP", region.ServerIP);
            m.Add("ServerHttpPort", (int)region.ServerHttpPort);
            m.Add("ServerURI", region.ServerURI);
            m.Add("ServerPort", (int)region.ServerPort);
            m.Add("RegionMapTexture", region.ParcelMapTexture.ToString());
            m.Add("ProductName", region.ProductName);
            switch(region.Access)
            {
                case RegionAccess.PG:
                    m.Add("Access", "pg");
                    break;

                case RegionAccess.Mature:
                    m.Add("Access", "mature");
                    break;

                case RegionAccess.Adult:
                    m.Add("Access", "adult");
                    break;

                default:
                    m.Add("Access", "unknown");
                    break;
            }

            m.Add("Owner", region.Owner.ToMap());
            m.Add("Flags", ((uint)region.Flags).ToString());
            return m;
        }

        public static Map ToJsonMap(this IAgent agent, SceneInterface scene)
        {
            Map m = new Map();
            m.Add("ID", agent.Owner.ID);
            m.Add("FullName", agent.Owner.FullName);
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
