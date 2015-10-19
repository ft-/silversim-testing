// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.ServiceInterfaces.Asset;
using ThreadedClasses;
using SilverSim.Types;
using System.Timers;

namespace SilverSim.Scene.Types.Scene
{
    public class NotecardCache
    {
        private SceneInterface m_Scene;
        private RwLockedDictionary<UUID, Notecard> m_Notecards = new RwLockedDictionary<UUID,Notecard>();
        private RwLockedDictionary<UUID, int> m_LastAccessed = new RwLockedDictionary<UUID,int>();
        private Timer m_Timer = new Timer(1);

        public NotecardCache(SceneInterface scene)
        {
            m_Scene = scene;
            m_Timer.Elapsed += OnTimer;
            m_Timer.Interval = 1000;
            m_Timer.Enabled = true;
        }

        ~NotecardCache()
        {
            m_Timer.Enabled = false;
            m_Timer.Elapsed -= OnTimer;
        }

        public void OnTimer(object source, ElapsedEventArgs e)
        {
            foreach(KeyValuePair<UUID, int> kvp in m_LastAccessed)
            {
                if(Environment.TickCount - kvp.Value > 30000)
                {
                    /* no need for locking here since we take out the last accessed first */
                    m_LastAccessed.Remove(kvp.Key);
                    m_Notecards.Remove(kvp.Key);
                }
            }
        }

        public Notecard this[UUID assetid]
        {
            get
            {
                Notecard v;
                if(m_Notecards.TryGetValue(assetid, out v))
                {
                    return v;
                }
                AssetData asset = m_Scene.AssetService[assetid];
                if(asset.Type != AssetType.Notecard)
                {
                    throw new NotANotecardFormatException();
                }

                m_LastAccessed[assetid] = Environment.TickCount;
                return m_Notecards.GetOrAddIfNotExists(assetid, delegate()
                {
                    return new Notecard(asset);
                });
            }
        }
    }
}
