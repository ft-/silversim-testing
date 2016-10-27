// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.IO;
using System.Reflection;

namespace SilverSim.Main.Common.HttpServer
{
    public static class SslSelfSignCertUtil
    {
        static Action<string, string> m_SelfSignCertFunc;

        static Assembly m_MonoSecurity;

        static Assembly ResolveMonoSecurityEventHandler(object sender, ResolveEventArgs args)
        {
            AssemblyName aName = new AssemblyName(args.Name);
            if (aName.Name == "Mono.Security")
            {
                return m_MonoSecurity;
            }
            return null;
        }


        public static void GenerateSelfSignedServiceCertificate(string filename, string hostname)
        {
            /* Mono.Security is providing some subtle issues when trying to load 4.0.0.0 on Mono 4.4 to 4.6.
             * So, we make our dependency being loaded by an assembly that allows preloading the assembly on Win. 
             */
            try
            {
                m_MonoSecurity = Assembly.Load("Mono.Security");
            }
            catch
            {
                m_MonoSecurity = Assembly.LoadFile(Path.GetFullPath("platform-libs/Mono.Security.dll"));
                AppDomain.CurrentDomain.AssemblyResolve += ResolveMonoSecurityEventHandler;
            }

            if (null == m_SelfSignCertFunc)
            {
                Assembly selfSignCert = Assembly.Load("SilverSim.SelfSignCert");
                Type selfSignCertType = selfSignCert.GetType("SilverSim.SelfSignCert.SslSelfSignCertUtil");
                m_SelfSignCertFunc = (Action<string,string>) selfSignCertType.GetMethod("GenerateSelfSignedServiceCertificate", new Type[] { typeof(string), typeof(string) }).CreateDelegate(typeof(Action<string,string>));
            }
            m_SelfSignCertFunc(filename, hostname);
        }
    }
}
