// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Reflection;
using System.Text;
using ThreadedClasses;
using System.IO;

namespace SilverSim.Types
{
    public static class MultiLanguageSupport
    {
        static readonly RwLockedDictionary<string, ResourceSet> m_LoadedAssemblyResources = new RwLockedDictionary<string, ResourceSet>();
        static readonly object m_LoadAssemblyLock = new object();
        static readonly CultureInfo EnCulture = new CultureInfo("en");

        public static ResourceSet GetLanguageResourceSet(
            this object o,
            CultureInfo culture)
        {
            Type type = o.GetType();
            System.Reflection.Assembly a = type.Assembly;
            string assemblyName = a.GetName().Name;
            ResourceSet res = null;
            string cultureName = culture.Name;
            string cultureGroup = cultureName.Split('-')[0];

            if(!m_LoadedAssemblyResources.TryGetValue(cultureName + ":" + assemblyName, out res) &&
                !m_LoadedAssemblyResources.TryGetValue(cultureGroup + ":" + assemblyName, out res))
            {
                lock (m_LoadAssemblyLock)
                {
                    if (!m_LoadedAssemblyResources.TryGetValue(cultureName + ":" + assemblyName, out res))
                    {
                        string fName = "languages/" + cultureName + "/" + assemblyName + "." + cultureName + ".resources";
                        if (File.Exists(fName))
                        {
                            using (ResourceReader reader = new ResourceReader(fName))
                            {
                                res = new ResourceSet(reader);
                                m_LoadedAssemblyResources.Add(cultureName + ":" + assemblyName, res);
                            }
                        }
                    }
                    else if (cultureGroup != cultureName &&
                        !m_LoadedAssemblyResources.TryGetValue(cultureGroup + ":" + assemblyName, out res))
                    {
                        string fName = "languages/" + cultureGroup + "/" + assemblyName + "." + cultureGroup +  ".resources";
                        if (File.Exists(fName))
                        {
                            using (ResourceReader reader = new ResourceReader(fName))
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
            CultureInfo culture,
            string name,
            string defvalue)
        {
            ResourceSet res = o.GetLanguageResourceSet(culture);
            if(res == null)
            {
                return defvalue;
            }
            try
            {
                string str = res.GetString(name);
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
