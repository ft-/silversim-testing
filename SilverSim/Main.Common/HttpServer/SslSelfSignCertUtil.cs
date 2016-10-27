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

        public static void GenerateSelfSignedServiceCertificate(string filename, string hostname)
        {
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
