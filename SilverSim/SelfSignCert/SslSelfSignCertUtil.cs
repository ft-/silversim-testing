// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Mono.Security.X509;
using Mono.Security.X509.Extensions;
using System;
using System.Collections;
using System.Security.Cryptography;

namespace SilverSim.SelfSignCert
{
    public static class SslSelfSignCertUtil
    {
        public static void GenerateSelfSignedServiceCertificate(string filename, string hostname)
        {
            X509CertificateBuilder builder = new X509CertificateBuilder(3);
            byte[] sn = Guid.NewGuid().ToByteArray();
            if ((sn[0] & 0x80) == 0x80)
            {
                sn[0] -= 0x80;
            }

            RSA rsaKey = (RSA)RSA.Create();
            ExtendedKeyUsageExtension eku = new ExtendedKeyUsageExtension();
            eku.KeyPurpose.Add("1.3.6.1.5.5.7.3.1"); /* SSL Server */

            builder.SerialNumber = sn;
            builder.IssuerName = "CN=" + hostname;
            builder.NotBefore = DateTime.Now;
            DateTime notAfter = DateTime.Now;
            builder.NotAfter = notAfter.AddYears(1000);
            builder.SubjectName = "CN=" + hostname;
            builder.SubjectPublicKey = rsaKey;
            builder.Hash = "SHA512";
            builder.Extensions.Add(eku);

            byte[] rawCert = builder.Sign(rsaKey);

            PKCS12 p12 = new PKCS12();
            p12.Password = string.Empty;
            ArrayList list = new ArrayList();

            list.Add(new byte[4] { 1, 0, 0, 0 });
            Hashtable attributes = new Hashtable(1);
            attributes.Add(PKCS9.localKeyId, list);

            p12.AddCertificate(new X509Certificate(rawCert), attributes);
            p12.AddPkcs8ShroudedKeyBag(rsaKey, attributes);
            p12.SaveToFile(filename);
        }
    }
}
