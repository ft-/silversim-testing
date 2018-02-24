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

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace SilverSim.Scripting.Common
{
    public static class ScriptLoader
    {
        private static readonly ReaderWriterLock m_CompilerLock = new ReaderWriterLock();
        private static readonly RwLockedDictionary<UUID, AppDomain> m_LoadedDomains = new RwLockedDictionary<UUID, AppDomain>();
        private static readonly RwLockedDictionary<UUID, IScriptAssembly> m_LoadedAssemblies = new RwLockedDictionary<UUID, IScriptAssembly>();
        private static readonly RwLockedDictionary<UUID, RwLockedList<ScriptInstance>> m_LoadedInstances = new RwLockedDictionary<UUID, RwLockedList<ScriptInstance>>();

        public static void Remove(UUID assetID, ScriptInstance instance) => m_CompilerLock.AcquireWriterLock(() =>
        {
            if (m_LoadedInstances.RemoveIf(assetID, (RwLockedList<ScriptInstance> list) =>
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
        });

        internal static void RegisterAppDomain(UUID assetID, AppDomain appDom)
        {
            m_LoadedDomains.Add(assetID, appDom);
        }

        public static ScriptInstance Load(ObjectPart part, ObjectPartInventoryItem item, UUI user, AssetData data, CultureInfo currentCulture, byte[] serializedState = null, Func<string, TextReader> openInclude = null)
        {
            return m_CompilerLock.AcquireReaderLock(() =>
            {
                IScriptAssembly assembly = m_LoadedAssemblies.GetOrAddIfNotExists(data.ID, () =>
                {
                    using (var reader = new StreamReader(data.InputStream))
                    {
                        return CompilerRegistry.ScriptCompilers.Compile(AppDomain.CurrentDomain, user, data.ID, reader, currentCulture, openInclude);
                    }
                });

                ScriptInstance instance = assembly.Instantiate(part, item, serializedState);
                if (!m_LoadedInstances.ContainsKey(data.ID))
                {
                    m_LoadedInstances.Add(data.ID, new RwLockedList<ScriptInstance>());
                }
                m_LoadedInstances[data.ID].Add(instance);
                return instance;
            });
        }

        public static void SyntaxCheck(UUI user, AssetData data, CultureInfo currentCulture)
        {
            using(var reader = new StreamReader(data.InputStream))
            {
                CompilerRegistry.ScriptCompilers.SyntaxCheck(user, data.ID, reader, currentCulture);
            }
        }
    }
}
