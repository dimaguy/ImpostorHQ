using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Impostor.Commands.Core.DashBoard
{
    public class CertificateAuthority
    {
        /// <summary>
        /// This class is used to generate self-signed certificates.
        /// </summary>
        public class CertificateSynthesizer
        {
            public const string Allowed = "qwertyuiopasdfghjklzxcvbnm1234567890";
            public readonly Random PseudoRng = new Random();
            /// <summary>
            /// This is used to generate a certificate that can be used with antiHttps.
            /// </summary>
            /// <param name="ipDns">The target host name. Can be your IP address or domain name.</param>
            /// <returns>A fully function self signed certificate.</returns>
            public X509Certificate2 GetHttpsCert(string ipDns)
            {
                if (!File.Exists("antiHttps.pfx"))
                {
                    var ssc = MakeCert(ipDns);
                    return ssc.GetHttpsCert();
                }
                return new X509Certificate2("antiHttps.pfx");

            }
            private SelfSignedCert MakeCert(string cn)
            {
                var asymmetricPair = ECDsa.Create(ECCurve.NamedCurves.nistP384);
                var request = new CertificateRequest($"CN={cn}", asymmetricPair, HashAlgorithmName.SHA256);
                var ssc = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(900));
                return new SelfSignedCert()
                {
                    Certificate = ssc,
                    PfxPrivateBytes = ssc.Export(X509ContentType.Pfx),
                };
            }
            private class SelfSignedCert
            {
                /// <summary>
                /// The business.
                /// </summary>
                public X509Certificate2 Certificate { get; set; }
                /// <summary>
                /// The PFX private key.
                /// </summary>
                public byte[] PfxPrivateBytes { get; set; }
                /// <summary>
                /// The Pfx password.
                /// </summary>
                /// <summary>
                /// This will save the certificate to the disk and load it.
                /// </summary>
                /// <returns>A valid certificate.</returns>
                public X509Certificate2 GetHttpsCert()
                {
                    using (BinaryWriter binWriter = new BinaryWriter(
                        File.Open(@"antiHttps.pfx", FileMode.Create)))
                    {
                        binWriter.Write(PfxPrivateBytes);
                    }

                    File.WriteAllText("add-to-browser.cer",
                        $"-----BEGIN CERTIFICATE-----\r\n{Convert.ToBase64String(Certificate.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)}\r\n-----END CERTIFICATE-----");
                    return new X509Certificate2(@"antiHttps.pfx");
                }
            }
        }
    }
}
