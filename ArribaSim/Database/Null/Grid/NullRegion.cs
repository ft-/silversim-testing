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

using ArribaSim.Main.Common;
using ArribaSim.ServiceInterfaces.Grid;
using ArribaSim.Types;
using ArribaSim.Types.Grid;
using log4net;
using Nini.Config;
using System.Reflection;
using System.Collections.Generic;
using ThreadedClasses;

namespace ArribaSim.Database.Null.Grid
{
    #region Service Implementation
    class NullGridService : GridServiceInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private bool m_DeleteOnUnregister;
        private bool m_AllowDuplicateRegionNames;
        private RwLockedDictionary<UUID, RegionInfo> m_RegionList = new RwLockedDictionary<UUID, RegionInfo>();

        #region Constructor
        public NullGridService(bool deleteOnUnregister, bool allowDuplicateRegionNames)
        {
            m_DeleteOnUnregister = deleteOnUnregister;
            m_AllowDuplicateRegionNames = allowDuplicateRegionNames;
        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        #region Accessors
        public override RegionInfo this[UUID ScopeID, UUID regionID]
        {
            get
            {
                try
                {
                    m_RegionList.ForEach(delegate(RegionInfo ri)
                    {
                        if (ri.ID.Equals(regionID) && ri.ScopeID.Equals(ScopeID))
                        {
                            throw new ReturnValueException<RegionInfo>(ri);
                        }
                    });
                }
                catch (ReturnValueException<RegionInfo> e)
                {
                    return e.Value;
                }
                throw new KeyNotFoundException();
            }
        }

        public override RegionInfo this[UUID ScopeID, uint gridX, uint gridY]
        {
            get
            {
                try
                {
                    m_RegionList.ForEach(delegate(RegionInfo ri)
                    {
                        if(ri.Location.X <= gridX && ri.Location.Y <= gridY &&
                            ri.Location.X + ri.Size.X > gridX &&
                            ri.Location.Y + ri.Size.Y > gridY)
                        {
                            throw new ReturnValueException<RegionInfo>(ri);
                        }
                    });
                }
                catch(ReturnValueException<RegionInfo> e)
                {
                    return e.Value;
                }
                throw new KeyNotFoundException();
            }
        }

        public override RegionInfo this[UUID ScopeID, string regionName]
        {
            get
            {
                try
                {
                    m_RegionList.ForEach(delegate(RegionInfo ri)
                    {
                        if (ri.Name == regionName && ri.ScopeID.Equals(ScopeID))
                        {
                            throw new ReturnValueException<RegionInfo>(ri);
                        }
                    });
                }
                catch (ReturnValueException<RegionInfo> e)
                {
                    return e.Value;
                }
                throw new KeyNotFoundException();
            }
        }

        public override RegionInfo this[UUID regionID]
        {
            get
            {
                return m_RegionList[regionID];
            }
        }

        #endregion

        #region Region Registration
        public override void RegisterRegion(RegionInfo regionInfo)
        {
            lock(this)
            {
                if (!m_AllowDuplicateRegionNames)
                {
                    try
                    {
                        RegionInfo test = this[regionInfo.ScopeID, regionInfo.Name];
                        throw new GridRegionUpdateFailedException("Duplicate region name " + test.Name);
                    }
                    catch
                    {

                    }
                }

                m_RegionList.ForEach(delegate(RegionInfo ri)
                {
                    if(!ri.ScopeID.Equals(regionInfo.ScopeID))
                    {
                        return;
                    }
                    else if(ri.ID.Equals(regionInfo.ID))
                    {
                        return;
                    }
                    else if ((ri.Location.X >= regionInfo.Location.X && ri.Location.Y >= regionInfo.Location.Y &&
                            ri.Location.X < regionInfo.Location.X + regionInfo.Size.X &&
                            ri.Location.Y < regionInfo.Location.Y + regionInfo.Size.Y) ||
                        (ri.Location.X + ri.Size.X > regionInfo.Location.X &&
                        ri.Location.Y + ri.Size.Y > regionInfo.Location.Y &&
                        ri.Location.X + ri.Size.X < regionInfo.Location.X + regionInfo.Size.X &&
                        ri.Location.Y + ri.Size.Y < regionInfo.Location.Y + regionInfo.Size.Y))
                    {
                        throw new GridRegionUpdateFailedException("Overlapping regions");
                    }
                });
                m_RegionList[regionInfo.ID] = regionInfo;
            }
        }

        public override void UnregisterRegion(UUID ScopeID, UUID RegionID)
        {
            if(m_DeleteOnUnregister)
            {
                m_RegionList.RemoveIf(RegionID, delegate(RegionInfo ri) { return (ri.Flags & (uint)RegionFlags.Persistent) == 0; });
            }

            m_RegionList.ForEach(delegate(RegionInfo ri)
            {
                if(ri.ScopeID.Equals(ScopeID) &&
                    ri.ID.Equals(RegionID))
                {
                    ri.Flags &= (~((uint)RegionFlags.RegionOnline));
                }
            });
        }

        public override void DeleteRegion(UUID scopeID, UUID regionID)
        {
            m_RegionList.Remove(regionID);
        }

        #endregion

        #region List accessors
        public override List<RegionInfo> GetDefaultRegions(UUID ScopeID)
        {
            List<RegionInfo> result = new List<RegionInfo>();
            m_RegionList.ForEach(delegate(RegionInfo ri)
            {
                if((ri.Flags & (uint)RegionFlags.DefaultRegion) != 0)
                {
                    result.Add(ri);
                }
            });

            return result;
        }

        public override List<RegionInfo> GetOnlineRegions(UUID ScopeID)
        {
            List<RegionInfo> result = new List<RegionInfo>();
            m_RegionList.ForEach(delegate(RegionInfo ri)
            {
                if ((ri.Flags & (uint)RegionFlags.RegionOnline) != 0)
                {
                    result.Add(ri);
                }
            });

            return result;
        }

        public override List<RegionInfo> GetFallbackRegions(UUID ScopeID)
        {
            List<RegionInfo> result = new List<RegionInfo>();
            m_RegionList.ForEach(delegate(RegionInfo ri)
            {
                if ((ri.Flags & (uint)RegionFlags.FallbackRegion) != 0)
                {
                    result.Add(ri);
                }
            });

            return result;
        }

        public override List<RegionInfo> GetDefaultHypergridRegions(UUID ScopeID)
        {
            List<RegionInfo> result = new List<RegionInfo>();
            m_RegionList.ForEach(delegate(RegionInfo ri)
            {
                if ((ri.Flags & (uint)RegionFlags.DefaultHGRegion) != 0)
                {
                    result.Add(ri);
                }
            });

            return result;
        }

        public override List<RegionInfo> GetRegionsByRange(UUID ScopeID, GridVector min, GridVector max)
        {
            List<RegionInfo> result = new List<RegionInfo>();
            m_RegionList.ForEach(delegate(RegionInfo ri)
            {
                if (!ri.ScopeID.Equals(ScopeID))
                {
                    return;
                }
                else if(ri.Location.X >= min.X && ri.Location.Y >= min.Y && ri.Location.X <= max.X && ri.Location.Y <= max.Y)
                {

                }
                else if(ri.Location.X + ri.Size.X >= min.X && ri.Location.Y + ri.Size.Y >= min.Y &&
                    ri.Location.X + ri.Size.X <= max.X && ri.Location.Y + ri.Size.Y <= max.Y)
                {

                }
                else if(ri.Location.X >= min.X && ri.Location.Y >= min.Y && 
                    ri.Location.X + ri.Size.X > min.X && ri.Location.Y + ri.Size.Y > min.Y)
                {

                }
                else if(ri.Location.X >= max.X && ri.Location.Y >= max.Y && 
                    ri.Location.X + ri.Size.X > max.X && ri.Location.Y + ri.Size.Y > max.Y)
                {

                }
                else
                {
                    return;
                }
                result.Add(ri);
            });

            return result;
        }

        public override List<RegionInfo> GetNeighbours(UUID ScopeID, UUID RegionID)
        {
            List<RegionInfo> result = new List<RegionInfo>();
            RegionInfo own = this[ScopeID, RegionID];

            m_RegionList.ForEach(delegate(RegionInfo ri)
            {
                if (!ri.ScopeID.Equals(ScopeID))
                {
                    return;
                }
                else if((ri.Location.X == own.Location.X + own.Size.X ||
                            ri.Location.X + ri.Size.X == own.Location.X) &&
                        ri.Location.Y <= own.Location.X + own.Size.X && 
                        ri.Location.Y + ri.Size.Y >= own.Location.Y)
                {

                }
                else if((ri.Location.Y == own.Location.Y + own.Size.Y ||
                        ri.Location.Y + ri.Size.Y == own.Location.Y) &&
                        ri.Location.X <= own.Location.X + own.Size.X &&
                        ri.Location.X + ri.Size.X >= own.Location.X)
                {

                }
                else
                {
                    return;
                }
                result.Add(ri);
            });

            return result;
        }

        public override List<RegionInfo> GetAllRegions(UUID ScopeID)
        {
            return new List<RegionInfo>(m_RegionList.Values);
        }

        public override List<RegionInfo> SearchRegionsByName(UUID ScopeID, string searchString)
        {
            List<RegionInfo> result = new List<RegionInfo>();
            m_RegionList.ForEach(delegate(RegionInfo ri)
            {
                if (ri.ScopeID.Equals(ScopeID) && ri.Name.StartsWith(searchString))
                {
                    result.Add(ri);
                }
            });

            return result;
        }
        #endregion

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "uuid CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "regionName VARCHAR(128)," +
                "regionSecret VARCHAR(128)," +
                "serverIP VARCHAR(64)," +
                "serverPort INT(10) UNSIGNED," +
                "serverURI VARCHAR(255)," +
                "locX INT(10) UNSIGNED," +
                "locY INT(10) UNSIGNED," +
                "regionMapTexture CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "serverHttpPort INT(10) UNSIGNED," +
                "owner VARCHAR(255) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "access INT(10) UNSIGNED DEFAULT '1'," +
                "ScopeID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "sizeX INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "sizeY INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "flags INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "last_seen BIGINT(20) UNSIGNED NOT NULL DEFAULT '0'," +
                "AuthenticatingToken VARCHAR(255) NOT NULL DEFAULT ''," +
                "AuthenticatingPrincipalID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "parcelMapTexture CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "PRIMARY KEY(uuid)," +
                "KEY regionName (regionName)," +
                "KEY ScopeID (ScopeID)," +
                "KEY flags (flags))"
        };
    }
    #endregion

    #region Factory
    class NullGridServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public NullGridServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new NullGridService(ownSection.GetBoolean("DeleteOnUnregister", false),
                                        ownSection.GetBoolean("AllowDuplicateRegionNames", false));
        }
    }
    #endregion

}
