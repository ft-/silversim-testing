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
            var builder = new X509CertificateBuilder(3);
            var sn = Guid.NewGuid().ToByteArray();
            if ((sn[0] & 0x80) == 0x80)
            {
                sn[0] -= 0x80;
            }

            var rsaKey = RSA.Create();
            var eku = new ExtendedKeyUsageExtension();
            eku.KeyPurpose.Add("1.3.6.1.5.5.7.3.1"); /* SSL Server */

            builder.SerialNumber = sn;
            builder.IssuerName = "CN=" + hostname;
            builder.NotBefore = DateTime.Now;
            var notAfter = DateTime.Now;
            builder.NotAfter = notAfter.AddYears(1000);
            builder.SubjectName = "CN=" + hostname;
            builder.SubjectPublicKey = rsaKey;
            builder.Hash = "SHA512";
            builder.Extensions.Add(eku);

            var rawCert = builder.Sign(rsaKey);

            var p12 = new PKCS12();
            p12.Password = string.Empty;
            var list = new ArrayList();

            list.Add(new byte[4] { 1, 0, 0, 0 });
            var attributes = new Hashtable(1);
            attributes.Add(PKCS9.localKeyId, list);

            p12.AddCertificate(new X509Certificate(rawCert), attributes);
            p12.AddPkcs8ShroudedKeyBag(rsaKey, attributes);
            p12.SaveToFile(filename);
        }
    }
}
