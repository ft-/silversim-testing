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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.ServiceInterfaces.Purge;
using SilverSim.ServiceInterfaces.Statistics;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Timers;

namespace SilverSim.AssetPurger
{
    [Description("Asset purging")]
    [PluginName("AssetPurger")]
    public sealed class AssetPurger : IPlugin, IPluginShutdown, IQueueStatsAccess
    {
        private static ILog m_Log = LogManager.GetLogger("ASSET PURGE");
        private List<IAssetPurgeServiceInterface> m_PurgeServices;
        private List<IAssetReferenceInfoServiceInterface> m_ReferenceServices;

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        private QueueStat GetQueueStat()
        {
            return new QueueStat(m_IsRunning ? "PROCESSING" : "IDLE", 0, (uint)m_Processed);
        }

        public IList<QueueStatAccessor> QueueStats
        {
            get
            {
                var list = new List<QueueStatAccessor>();
                list.Add(new QueueStatAccessor("AssetPurger", GetQueueStat));
                return list;
            }
        }

        private System.Timers.Timer m_Timer = new System.Timers.Timer(3600000);
        private bool m_IsRunning;
        private bool m_StopProcess;
        private uint m_Processed;

        private readonly bool m_Enabled;

        public AssetPurger(IConfig ownSection)
        {
            m_Enabled = ownSection.GetBoolean("Enabled", false);
        }

        ~AssetPurger()
        {
            m_Timer.Dispose();
        }

        public void Shutdown()
        {
            m_Timer.Stop();
            m_Timer.Elapsed -= TimerTriggered;
            m_StopProcess = true;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_ReferenceServices = loader.GetServicesByValue<IAssetReferenceInfoServiceInterface>();
            m_PurgeServices = loader.GetServicesByValue<IAssetPurgeServiceInterface>();

            if(m_Enabled && m_ReferenceServices.Count > 0 && m_PurgeServices.Count > 0)
            {
                loader.CommandRegistry.Commands.Add("purge-assets", PurgeAssetsCmd);
                m_Timer.Elapsed += TimerTriggered;
                TimerTriggered(null, null);
                m_Timer.Start();
            }
        }

        private void PurgeAssetsCmd(List<string> args, TTY io, UUID limitedToScene)
        {
            if(args[0] == "help")
            {
                io.Write("Triggers unused assets purge");
            }
            else
            {
                TimerTriggered(null, null);
                io.Write("assets purge triggered");
            }
        }

        private void TimerTriggered(object o, ElapsedEventArgs args)
        {
            if(!m_IsRunning)
            {
                m_IsRunning = true;
                ThreadPool.QueueUserWorkItem(Process);
            }
        }

        private void Process(object o)
        {
            long purged = 0;
            try
            {
                foreach (IAssetPurgeServiceInterface purge in m_PurgeServices)
                {
                    foreach (UUID id in purge.GetUnprocessedAssets())
                    {
                        purge.EnqueueAsset(id);
                        if (m_StopProcess)
                        {
                            m_IsRunning = false;
                            return;
                        }
                    }
                }

                var referencedAssets = new List<UUID>();
                foreach (IAssetReferenceInfoServiceInterface assetrefs in m_ReferenceServices)
                {
                    assetrefs.EnumerateUsedAssets((assetid) =>
                    {
                        if (!referencedAssets.Contains(assetid))
                        {
                            referencedAssets.Add(assetid);
                        }
                    });
                    if (m_StopProcess)
                    {
                        m_IsRunning = false;
                        return;
                    }
                }

                foreach (IAssetPurgeServiceInterface purge in m_PurgeServices)
                {
                    int count = referencedAssets.Count;
                    for (int i = 0; i < count; i += 200)
                    {
                        int maxCnt = count - i;
                        if (maxCnt > 200)
                        {
                            maxCnt = 200;
                        }
                        purge.MarkAssetAsUsed(referencedAssets.GetRange(i, maxCnt));
                        if (m_StopProcess)
                        {
                            m_IsRunning = false;
                            return;
                        }
                    }
                }

                foreach (IAssetPurgeServiceInterface purge in m_PurgeServices)
                {
                    purged += purge.PurgeUnusedAssets();
                    if (m_StopProcess)
                    {
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                m_Log.Debug("Exception", e);
            }

            m_Log.InfoFormat("Purged {0} assets", purged);
            m_Processed += (uint)purged;
            m_IsRunning = false;
        }
    }
}
