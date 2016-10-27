// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace SilverSim.Main.Common.HttpServer
{
    public static class SslSelfSignCertUtil
    {
        static Action<string, string> m_SelfSignCertFunc;

        public static void GenerateSelfSignedServiceCertificate(string filename, string hostname)
        {
            /* Mono.Security is providing some subtle issues when trying to load 4.0.0.0 on Mono 4.4 to 4.6.
             * So, we make our dependency being loaded by an assembly that allows preloading the assembly on Win. 
             */
            if(null == m_SelfSignCertFunc)
            {
                Assembly selfSignCert = Assembly.Load("SilverSim.SelfSignCert");
                Type selfSignCertType = selfSignCert.GetType("SilverSim.SelfSignCert.SslSelfSignCertUtil");
                m_SelfSignCertFunc = (Action<string,string>) selfSignCertType.GetMethod("GenerateSelfSignedServiceCertificate", new Type[] { typeof(string), typeof(string) }).CreateDelegate(typeof(Action<string,string>));
            }
            m_SelfSignCertFunc(filename, hostname);
        }
    }
}
