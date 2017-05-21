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

using SilverSim.Threading;
using System;
using System.Globalization;
using System.IO;
using System.Resources;

namespace SilverSim.Types
{
    public static class MultiLanguageSupport
    {
        private static readonly RwLockedDictionary<string, ResourceSet> m_LoadedAssemblyResources = new RwLockedDictionary<string, ResourceSet>();
        private static readonly object m_LoadAssemblyLock = new object();
        private static readonly string InstallBinPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly CultureInfo EnUsCulture = new CultureInfo("en-US");

        public static ResourceSet GetLanguageResourceSet(
            this object o,
            CultureInfo selectedCulture)
        {
            var type = o.GetType();
            var assembly = type.Assembly;
            var assemblyName = assembly.GetName().Name;
            ResourceSet res = null;
            var culture = selectedCulture ?? EnUsCulture;
            var cultureName = culture.Name;
            var cultureGroup = cultureName.Split('-')[0];

            if(!m_LoadedAssemblyResources.TryGetValue(cultureName + ":" + assemblyName, out res) &&
                !m_LoadedAssemblyResources.TryGetValue(cultureGroup + ":" + assemblyName, out res))
            {
                lock (m_LoadAssemblyLock)
                {
                    if (!m_LoadedAssemblyResources.TryGetValue(cultureName + ":" + assemblyName, out res))
                    {
                        var fName = Path.Combine(InstallBinPath, "languages/" + cultureName + "/" + assemblyName + "." + cultureName + ".resources");
                        if (File.Exists(fName))
                        {
                            using (var reader = new ResourceReader(fName))
                            {
                                res = new ResourceSet(reader);
                                m_LoadedAssemblyResources.Add(cultureName + ":" + assemblyName, res);
                            }
                        }
                    }
                    else if (cultureGroup != cultureName &&
                        !m_LoadedAssemblyResources.TryGetValue(cultureGroup + ":" + assemblyName, out res))
                    {
                        var fName = Path.Combine(InstallBinPath, "languages/" + cultureGroup + "/" + assemblyName + "." + cultureGroup +  ".resources");
                        if (File.Exists(fName))
                        {
                            using (var reader = new ResourceReader(fName))
                            {
                                res = new ResourceSet(reader);
                                m_LoadedAssemblyResources.Add(cultureGroup + ":" + assemblyName, res);
                            }
                        }
                    }
                }
            }

            return res;
        }

        public static string GetLanguageString(
            this object o,
            CultureInfo selectedCulture,
            string name,
            string defvalue)
        {
            var culture = selectedCulture ?? EnUsCulture;
            var res = o.GetLanguageResourceSet(culture);
            if(res == null)
            {
                return defvalue;
            }
            try
            {
                var str = res.GetString(name);
                if(string.IsNullOrEmpty(str))
                {
                    str = defvalue;
                }
                return str;
            }
            catch(InvalidOperationException)
            {
                return defvalue;
            }
        }
    }
}
