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

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;

namespace SilverSim.WebIF.Admin.Simulator
{
    public static class ExtensionMethods
    {
        public static Map ToJsonMap(this EstateInfo estate, IAdminWebIF webif) => new Map
        {
            { "ID", (int)estate.ID },
            { "ParentEstateID", (int)estate.ParentEstateID },
            { "Name", estate.Name },
            { "Flags", ((uint)estate.Flags).ToString() },
            { "Owner", webif.ResolveName(estate.Owner).ToMap() },
            { "PricePerMeter", estate.PricePerMeter },
            { "BillableFactor", estate.BillableFactor },
            { "SunPosition", estate.SunPosition },
            { "AbuseEmail", estate.AbuseEmail },
            { "UseGlobalTime", estate.UseGlobalTime }
        };

        public static Map ToJsonMap(this RegionInfo region, IAdminWebIF webif)
        {
            var m = new Map
            {
                { "ID", region.ID },
                { "Location", region.Location.GridLocation },
                { "Size", region.Size.GridLocation },
                { "Name", region.Name },
                { "ServerIP", region.ServerIP },
                { "ServerHttpPort", (int)region.ServerHttpPort },
                { "ServerURI", region.ServerURI },
                { "ServerPort", (int)region.ServerPort },
                { "RegionMapTexture", region.ParcelMapTexture.ToString() },
                { "ProductName", region.ProductName }
            };
            switch (region.Access)
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

            m.Add("Owner", webif.ResolveName(region.Owner).ToMap());
            m.Add("Flags", ((uint)region.Flags).ToString());
            return m;
        }

        public static Map ToJsonMap(this IAgent agent, SceneInterface scene) => new Map
        {
            { "ID", agent.Owner.ID },
            { "FullName", agent.NamedOwner.FullName },
            { "FirstName", agent.NamedOwner.FirstName },
            { "LastName", agent.NamedOwner.LastName },
            { "HomeURI", agent.NamedOwner.HomeURI != null ? agent.NamedOwner.HomeURI.ToString() : string.Empty },
            { "Type", agent.IsNpc ? "Npc" : "User" },
            { "IsRoot", agent.IsInScene(scene) },
            { "Position", agent.GlobalPosition.ToString() },
            { "Health", agent.Health }
        };
    }
}
