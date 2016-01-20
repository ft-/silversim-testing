// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.IO;
using System.Threading;

namespace SilverSim.Scripting.Common
{
    public static class ScriptLoader
    {
        static readonly ReaderWriterLock m_CompilerLock = new ReaderWriterLock();
        static readonly RwLockedDictionary<UUID, AppDomain> m_LoadedDomains = new RwLockedDictionary<UUID, AppDomain>();
        static readonly RwLockedDictionary<UUID, IScriptAssembly> m_LoadedAssemblies = new RwLockedDictionary<UUID, IScriptAssembly>();
        static readonly RwLockedDictionary<UUID, RwLockedList<ScriptInstance>> m_LoadedInstances = new RwLockedDictionary<UUID, RwLockedList<ScriptInstance>>();

        static ScriptLoader()
        {

        }

        public static void Remove(UUID assetID, ScriptInstance instance)
        {
            m_CompilerLock.AcquireWriterLock(-1);
            try
            {
                if(m_LoadedInstances.RemoveIf(assetID, delegate(RwLockedList<ScriptInstance> list)
                {
                    list.Remove(instance);
                    return list.Count == 0;
                }))
                {
                    m_LoadedAssemblies.Remove(assetID);
                    AppDomain appDom;
                    if (m_LoadedDomains.Remove(assetID, out appDom))
                    {
                        AppDomain.Unload(appDom);
                    }
                }
            }
            finally
            {
                m_CompilerLock.ReleaseWriterLock();
            }
        }

        internal static void RegisterAppDomain(UUID assetID, AppDomain appDom)
        {
            m_LoadedDomains.Add(assetID, appDom);
        }

        public static ScriptInstance Load(ObjectPart part, ObjectPartInventoryItem item, UUI user, AssetData data)
        {
            ScriptInstance instance;
            m_CompilerLock.AcquireReaderLock(-1);
            try
            {
                IScriptAssembly assembly;
                assembly = m_LoadedAssemblies.GetOrAddIfNotExists(data.ID, delegate()
                {
                    using (TextReader reader = new StreamReader(data.InputStream))
                    {
                        return CompilerRegistry.ScriptCompilers.Compile(AppDomain.CurrentDomain, user, data.ID, reader);
                    }
                });
                m_LoadedAssemblies[data.ID] = assembly;
                instance = assembly.Instantiate(part, item);
                if(!m_LoadedInstances.ContainsKey(data.ID))
                {
                    m_LoadedInstances.Add(data.ID, new RwLockedList<ScriptInstance>());
                }
                m_LoadedInstances[data.ID].Add(instance);
            }
            finally
            {
                m_CompilerLock.ReleaseReaderLock();
            }
            return instance;
        }

        public static void SyntaxCheck(UUI user, AssetData data)
        {
            using(TextReader reader = new StreamReader(data.InputStream))
            {
                CompilerRegistry.ScriptCompilers.SyntaxCheck(user, data.ID, reader);
            }
        }
    }
}
