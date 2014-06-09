/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.BackendConnectors.Robust.Common;
using ArribaSim.ServiceInterfaces.Grid;
using ArribaSim.Types;
using ArribaSim.Types.Grid;
using HttpClasses;
using System;
using System.Collections.Generic;

namespace ArribaSim.BackendConnectors.Robust.Grid
{
    #region Service Implementation
    public class RobustGridConnector : GridServiceInterface
    {
        string m_GridURI;
        public int TimeoutMs { get; set; }

        #region Constructor
        public RobustGridConnector(string uri)
        {
            TimeoutMs = 20000;
            if(!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "grid";
            m_GridURI = uri;
        }
        #endregion

        #region Accessors
        public override RegionInfo this[UUID ScopeID, UUID regionID]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["SCOPEID"] = ScopeID;
                post["REGIONID"] = regionID.ToString();
                post["METHOD"] = "get_region_by_uuid";
                return DeserializeRegion(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
            }
        }

        public override RegionInfo this[UUID ScopeID, GridVector position]
        {
            get
            {
                return this[ScopeID, position.X, position.Y];
            }
        }

        public override RegionInfo this[UUID ScopeID, uint gridX, uint gridY]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["SCOPEID"] = ScopeID;
                post["X"] = gridX.ToString();
                post["Y"] = gridY.ToString();
                post["METHOD"] = "get_region_by_position";
                return DeserializeRegion(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
            }
        }

        public override RegionInfo this[UUID ScopeID, string regionName]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["SCOPEID"] = ScopeID;
                post["NAME"] = regionName;
                post["METHOD"] = "get_region_by_name";
                return DeserializeRegion(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
            }
        }

        public override RegionInfo this[UUID regionID]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["SCOPEID"] = UUID.Zero;
                post["REGIONID"] = regionID.ToString();
                post["METHOD"] = "get_region_by_uuid";
                return DeserializeRegion(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
            }
        }

        #endregion

        private void checkResult(Map map)
        {
            if (!map.ContainsKey("Result"))
            {
                throw new GridRegionUpdateFailedException();
            }
            if (map["Result"].ToString().ToLower() != "success")
            {
                throw new GridRegionUpdateFailedException();
            }
        }

        #region Region Registration
        public override void RegisterRegion(RegionInfo regionInfo)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["METHOD"] = "register";
            checkResult(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));

        }

        public override void UnregisterRegion(UUID ScopeID, UUID RegionID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["SCOPEID"] = ScopeID;
            post["REGIONID"] = RegionID;
            post["METHOD"] = "deregister";
            checkResult(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
        }
        #endregion

        #region List accessors
        private List<RegionInfo> DeserializeList(Map map)
        {
            List<RegionInfo> rl = new List<RegionInfo>();
            foreach(IValue i in map.Values)
            {
                if(i is Map)
                {
                    Map m = (Map)i;
                    rl.Add(Deserialize(m));
                }
            }
            return rl;
        }

        private RegionInfo DeserializeRegion(Map map)
        {
            IValue i = map["result"];
            if(i is Map)
            {
                return Deserialize((Map)i);
            }
            else
            {
                throw new GridServiceInaccessibleException();
            }
        }

        public override List<RegionInfo> GetDefaultRegions(UUID ScopeID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["SCOPEID"] = ScopeID;
            post["METHOD"] = "get_default_regions";
            return DeserializeList(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
        }

        public override List<RegionInfo> GetFallbackRegions(UUID ScopeID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["SCOPEID"] = ScopeID;
            post["METHOD"] = "get_fallback_regions";
            return DeserializeList(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
        }

        public override List<RegionInfo> GetDefaultHypergridRegions(UUID ScopeID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["SCOPEID"] = ScopeID;
            post["METHOD"] = "get_default_hypergrid_regions";
            return DeserializeList(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
        }

        public override List<RegionInfo> GetRegionsByRange(UUID ScopeID, GridVector min, GridVector max)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["SCOPEID"] = ScopeID;
            post["XMIN"] = min.X.ToString();
            post["YMIN"] = min.Y.ToString();
            post["XMAX"] = max.X.ToString();
            post["YMAX"] = max.Y.ToString();
            post["METHOD"] = "get_region_range";
            return DeserializeList(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
        }

        public override List<RegionInfo> GetNeighbours(UUID ScopeID, UUID RegionID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["SCOPEID"] = ScopeID;
            post["REGIONID"] = RegionID;
            post["METHOD"] = "get_neighbours";
            return DeserializeList(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
        }

        public override List<RegionInfo> GetAllRegions(UUID ScopeID)
        {
            throw new NotSupportedException();
        }

        public override List<RegionInfo> SearchRegionsByName(UUID ScopeID, string searchString)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["SCOPEID"] = ScopeID;
            post["NAME"] = searchString;
            post["METHOD"] = "get_regions_by_name";
            return DeserializeList(OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_GridURI, null, post, false, TimeoutMs)));
        }

        private RegionInfo Deserialize(Map map)
        {
            RegionInfo r = new RegionInfo();
            r.ID = map["uuid"].ToString();
            r.Location.X = map["locX"].AsUInt;
            r.Location.Y = map["locY"].AsUInt;
            r.Size.X = map["sizeX"].AsUInt;
            r.Size.Y = map["sizeY"].AsUInt;
            r.Name = map["regionName"].ToString();
            r.ServerIP = map["serverIP"].ToString();
            r.ServerHttpPort = map["serverHttpPort"].AsUInt;
            r.ServerURI = map["serverURI"].ToString();
            r.ServerPort = map["serverPort"].AsUInt;
            r.RegionMapTexture = map["regionMapTexture"].AsUUID;
            r.ParcelMapTexture = map["parcelMapTexture"].AsUUID;
            r.Access = map["access"].AsUInt;
            r.RegionSecret = map["regionSecret"].ToString();
            r.Owner.ID = map["owner_uuid"].AsUUID;
            return r;
        }
        #endregion
    }
    #endregion
}
