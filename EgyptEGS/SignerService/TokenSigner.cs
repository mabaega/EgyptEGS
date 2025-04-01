using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace EgyptEGS.SignerService
{
    public class TokenSigner
    {
        private readonly string DllLibPath = "eps2003csp11.dll";
        private string TokenPin = "999999999";
        private string TokenCertificate = "Egypt Trust Sealing CA";

        public TokenSigner(string pin = "999999999", string certName = "Egypt Trust Sealing CA")
        {
            TokenPin = pin;
            TokenCertificate = certName;
        }

        public string SignDocument(string content)
        {
            return SignWithCMS(content);
        }

        private string SignWithCMS(string serializedText)
        {
            byte[] data = Encoding.UTF8.GetBytes(serializedText);
            Pkcs11InteropFactories factories = new Pkcs11InteropFactories();
            
            using (IPkcs11Library pkcs11Library = factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, DllLibPath, AppType.MultiThreaded))
            {
                ISlot slot = pkcs11Library.GetSlotList(SlotsType.WithTokenPresent).FirstOrDefault();
                if (slot is null) throw new Exception("No slots found");

                using (var session = slot.OpenSession(SessionType.ReadWrite))
                {
                    session.Login(CKU.CKU_USER, Encoding.UTF8.GetBytes(TokenPin));

                    X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.MaxAllowed);

                    var foundCerts = store.Certificates.Find(X509FindType.FindByIssuerName, TokenCertificate, false);
                    if (foundCerts.Count == 0) throw new Exception("No device detected");

                    var certForSigning = foundCerts[0];
                    store.Close();

                    ContentInfo content = new ContentInfo(new Oid("1.2.840.113549.1.7.5"), data);
                    SignedCms cms = new SignedCms(content, true);
                    
                    EssCertIDv2 bouncyCertificate = new EssCertIDv2(
                        new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(
                            new DerObjectIdentifier("1.2.840.113549.1.9.16.2.47")), 
                        HashBytes(certForSigning.RawData)
                    );
                    
                    SigningCertificateV2 signerCertificateV2 = new SigningCertificateV2(new[] { bouncyCertificate });
                    CmsSigner signer = new CmsSigner(certForSigning)
                    {
                        DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1") // SHA256
                    };

                    signer.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.UtcNow));
                    signer.SignedAttributes.Add(new AsnEncodedData(
                        new Oid("1.2.840.113549.1.9.16.2.47"), 
                        signerCertificateV2.GetEncoded()
                    ));

                    cms.ComputeSignature(signer);
                    return Convert.ToBase64String(cms.Encode());
                }
            }
        }

        private byte[] HashBytes(byte[] input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(input);
            }
        }

        public void ListCertificates()
        {
            using X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.MaxAllowed);
            var foundCerts = store.Certificates.Find(X509FindType.FindByIssuerName, TokenCertificate, false);
            
            foreach (X509Certificate2 cert in foundCerts)
            {
                Console.WriteLine($"Subject: {cert.Subject}");
                Console.WriteLine($"Issuer: {cert.Issuer}");
                Console.WriteLine($"Valid From: {cert.NotBefore}");
                Console.WriteLine($"Valid To: {cert.NotAfter}");
                Console.WriteLine($"Serial Number: {cert.SerialNumber}");
                Console.WriteLine($"Thumbprint: {cert.Thumbprint}");
                Console.WriteLine("---");
            }
            
            store.Close();
        }
    }
}