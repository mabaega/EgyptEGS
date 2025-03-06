using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Ess;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using SignServices.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BCAttribute = Org.BouncyCastle.Asn1.Cms.Attribute;
using BCContentInfo = Org.BouncyCastle.Asn1.Cms.ContentInfo;
using BCIssuerAndSerialNumber = Org.BouncyCastle.Asn1.Cms.IssuerAndSerialNumber;
using BCSignedData = Org.BouncyCastle.Asn1.Cms.SignedData;
using BCSignerInfo = Org.BouncyCastle.Asn1.Cms.SignerInfo;

namespace SignServices.Services
{
    public class TokenService
    {
        private string DllLibPath;
        private string TokenPin;
        private string TokenCertificate = "Egypt Trust Sealing CA";
        private readonly ILogger<TokenService> _logger;

        public TokenService(string pkcs11LibraryPath, string tokenPin, ILogger<TokenService> logger)
        {
            if (string.IsNullOrEmpty(pkcs11LibraryPath))
            {
                throw new ArgumentException("PKCS#11 library path cannot be empty", nameof(pkcs11LibraryPath));
            }
            DllLibPath = pkcs11LibraryPath;
            TokenPin = tokenPin;
            _logger = logger;

            //ListCertificates();
        }

        private static bool ValidateDriverPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                string extension = Path.GetExtension(path).ToLower();
                return extension == ".dll";
            }
            catch
            {
                return false;
            }
        }

        public async Task<X509Certificate2?> GetPublicCertificateOnly()
        {
            try
            {
                return await Task.Run(() =>
                {
                    using IPkcs11Library pkcs11 = GetPkcs11Library();
                    ISlot? slot = GetTokenSlot(pkcs11);
                    if (slot == null)
                    {
                        _logger.LogError("No token found in slots");
                        return null;
                    }

                    using var session = slot.OpenSession(SessionType.ReadOnly);
                    return GetSigningCertificate(session);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving public certificate");
                return null;
            }
        }

        public async Task<TokenInfo?> InitializeToken(string driverPath, string pin)
        {
            DllLibPath = driverPath;
            if (!ValidateDriverPath(DllLibPath))
            {
                _logger.LogError("Invalid driver path: {DriverPath}", DllLibPath);
                throw new InvalidOperationException("Invalid driver path. Please ensure the file exists and has a .dll extension.");
            }

            try
            {
                TokenPin = pin;
                _logger.LogInformation("Initializing PKCS#11 library");
                return await Task.Run(() =>
                {
                    using IPkcs11Library pkcs11 = GetPkcs11Library();
                    ISlot? slot = GetTokenSlot(pkcs11);
                    if (slot == null)
                    {
                        _logger.LogError("No token found in slots");
                        return null;
                    }

                    ITokenInfo tokenInfo = slot.GetTokenInfo();
                    using Net.Pkcs11Interop.HighLevelAPI.ISession session = slot.OpenSession(SessionType.ReadWrite);
                    session.Login(CKU.CKU_USER, Encoding.UTF8.GetBytes(TokenPin));

                    X509Certificate2 cert = GetSigningCertificate(session);

                    return new TokenInfo
                    {
                        TokenSerial = tokenInfo.SerialNumber.Trim(),
                        TokenType = tokenInfo.Model.Trim(),
                        CertificateInfo = cert.Subject,
                        CertificateValidFrom = cert.NotBefore.ToString("yyyy-MM-dd HH:mm:ss"),
                        CertificateValidTo = cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss"),
                        IsReadyForSigning = cert.NotBefore <= DateTime.Now && cert.NotAfter >= DateTime.Now
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing token");
                throw;
            }
        }

        public async Task<SignResponse> SignDocument(string SerializedInvoice)
        {
            try
            {
                _logger.LogInformation("Initializing signing process");
                return await Task.Run(() =>
                {
                    using IPkcs11Library pkcs11 = GetPkcs11Library();
                    using Net.Pkcs11Interop.HighLevelAPI.ISession session = GetTokenSlot(pkcs11)?.OpenSession(SessionType.ReadWrite)
                        ?? throw new InvalidOperationException("No token found in slots");

                    session.Login(CKU.CKU_USER, Encoding.UTF8.GetBytes(TokenPin));

                    X509Certificate2 cert = GetSigningCertificate(session);
                    IObjectHandle key = GetSigningKey(session);

                    BCContentInfo contentInfo = new(
                        new DerObjectIdentifier("1.2.840.113549.1.7.1"),
                        new DerOctetString(Encoding.UTF8.GetBytes(SerializedInvoice)));

                    _logger.LogInformation("Creating signed attributes");
                    DerSet signedAttrs = GetSignedAttributes(cert, SerializedInvoice);

                    _logger.LogInformation("Creating signer info");
                    BCSignerInfo signerInfo = new(
                        new SignerIdentifier(new BCIssuerAndSerialNumber(
                            X509Name.GetInstance(cert.IssuerName.RawData),
                            new DerInteger(BitConverter.ToInt64(Convert.FromHexString(cert.SerialNumber))))),
                        new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1")),
                        signedAttrs,
                        new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(new DerObjectIdentifier("1.2.840.113549.1.1.11")),
                        new DerOctetString(SignWithToken(session, key, signedAttrs)),
                        null);

                    BCSignedData signedData = new(
                        new DerSet(new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1"))),
                        contentInfo,
                        new DerSet(new DerSequence(new Asn1Encodable[] { new DerOctetString(cert.RawData) })),
                        null,
                        new DerSet(signerInfo));

                    BCContentInfo finalContentInfo = new(
                        new DerObjectIdentifier("1.2.840.113549.1.7.2"),
                        signedData);

                    _logger.LogInformation("Signature created successfully");
                    return new SignResponse
                    {
                        Signature = Convert.ToBase64String(finalContentInfo.GetEncoded())
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data");
                throw;
            }
        }

        // ...
        public async Task<SignResponse> SignDocument2(string canonicalizedJson)
        {
            // Convert canonicalized invoice to byte array
            byte[] documentBytes = Encoding.UTF8.GetBytes(canonicalizedJson);

            // Initialize PKCS#11 factories
            Pkcs11InteropFactories factories = new Pkcs11InteropFactories();

            using (IPkcs11Library pkcs11Library = factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, DllLibPath, AppType.MultiThreaded))
            {
                // Find token slot
                ISlot slot = pkcs11Library.GetSlotList(SlotsType.WithTokenPresent).FirstOrDefault();
                if (slot == null)
                    throw new Exception("No token slots found");

                using (Net.Pkcs11Interop.HighLevelAPI.ISession session = slot.OpenSession(SessionType.ReadWrite))
                {
                    // Login to token
                    session.Login(CKU.CKU_USER, Encoding.UTF8.GetBytes(TokenPin));

                    // Search for signing certificate
                    var certificateSearchAttributes = new List<IObjectAttribute>
                    {
                        session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, (ulong)CKO.CKO_CERTIFICATE),
                        session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true),
                        session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CERTIFICATE_TYPE, (ulong)CKC.CKC_X_509)
                    };

                    // Find certificate
                    IObjectHandle certificateHandle = session.FindAllObjects(certificateSearchAttributes).FirstOrDefault();
                    if (certificateHandle == null)
                        throw new Exception("Signing certificate not found");

                    // Retrieve certificate data
                    byte[] certificateBytes = session.GetAttributeValue(certificateHandle, new List<CKA> { CKA.CKA_VALUE }).FirstOrDefault()?.GetValueAsByteArray();

                    if (certificateBytes == null)
                        throw new Exception("Certificate data extraction failed");

                    // Convert to X509Certificate2
                    X509Certificate2 signingCertificate = new X509Certificate2(certificateBytes);

                    // Prepare SignedCms
                    System.Security.Cryptography.Pkcs.ContentInfo contentInfo = new System.Security.Cryptography.Pkcs.ContentInfo(documentBytes);
                    SignedCms signedCms = new SignedCms(contentInfo, true);

                    // Create CmsSigner
                    CmsSigner signer = new CmsSigner(signingCertificate)
                    {
                        DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"), // SHA-256
                    };

                    // Add signing time attribute
                    signer.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.UtcNow));

                    // Prepare SigningCertificateV2 attribute
                    byte[] certificateHash;
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        certificateHash = sha256.ComputeHash(signingCertificate.RawData);
                    }

                    // Create SigningCertificateV2
                    var signingCertOid = new DerObjectIdentifier("1.2.840.113549.1.9.16.2.47");
                    EssCertIDv2 essCertID = new EssCertIDv2(
                        new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(signingCertOid),
                        certificateHash
                    );
                    SigningCertificateV2 signerCertificateV2 = new SigningCertificateV2(new EssCertIDv2[] { essCertID });

                    // Add SigningCertificateV2 attribute
                    signer.SignedAttributes.Add(new AsnEncodedData(new Oid("1.2.840.113549.1.9.16.2.47"), signerCertificateV2.GetEncoded()));

                    // Compute signature
                    signedCms.ComputeSignature(signer);

                    // Encode signature
                    byte[] encodedSignature = signedCms.Encode();

                    _logger.LogInformation("Signature created successfully");
                    return new SignResponse
                    {
                        Signature = Convert.ToBase64String(encodedSignature)
                    };

                }
            }
        }

        private static DerSet GetSignedAttributes(X509Certificate2 cert, string data)
        {
            byte[] messageDigest = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            byte[] certHash = SHA256.HashData(cert.RawData);

            DerSequence signingCertificateV2 = new(
                new DerSequence(
                    new DerSequence(
                        new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1")),
                        new DerOctetString(certHash),
                        new IssuerSerial(
                            new GeneralNames(new GeneralName(X509Name.GetInstance(cert.IssuerName.RawData))),
                            new DerInteger(BitConverter.ToInt64(Convert.FromHexString(cert.SerialNumber))))
                    )
                )
            );

            return new DerSet(
                new BCAttribute(CmsAttributes.ContentType,
                    new DerSet(new DerObjectIdentifier("1.2.840.113549.1.7.1"))),
                new BCAttribute(CmsAttributes.SigningTime,
                    new DerSet(new DerUtcTime(DateTime.UtcNow))),
                new BCAttribute(CmsAttributes.MessageDigest,
                    new DerSet(new DerOctetString(messageDigest))),
                new BCAttribute(new DerObjectIdentifier("1.2.840.113549.1.9.16.2.47"),
                    new DerSet(signingCertificateV2)));
        }

        private static byte[] SignWithToken(Net.Pkcs11Interop.HighLevelAPI.ISession session, IObjectHandle key, DerSet signedAttrs)
        {
            IMechanism mechanism = session.Factories.MechanismFactory.Create(CKM.CKM_SHA256_RSA_PKCS);
            byte[] signedAttrsEncoded = signedAttrs.GetDerEncoded();
            return session.Sign(mechanism, key, signedAttrsEncoded);
        }

        private IPkcs11Library GetPkcs11Library()
        {
            if (!File.Exists(DllLibPath))
            {
                _logger.LogError("PKCS#11 library not found at path: {Path}", DllLibPath);
                throw new FileNotFoundException($"PKCS#11 library not found at path: {DllLibPath}");
            }
            Pkcs11InteropFactories factories = new();
            return factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, DllLibPath, AppType.MultiThreaded);
        }

        private static ISlot? GetTokenSlot(IPkcs11Library pkcs11)
        {
            return pkcs11.GetSlotList(SlotsType.WithTokenPresent).FirstOrDefault();
        }

        private X509Certificate2 GetSigningCertificate(Net.Pkcs11Interop.HighLevelAPI.ISession session)
        {
            IObjectHandle? certHandle = session.FindAllObjects(new List<IObjectAttribute>
            {
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_CERTIFICATE),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CERTIFICATE_TYPE, CKC.CKC_X_509),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true)
            }).FirstOrDefault();

            if (certHandle == null)
            {
                _logger.LogError("No certificate found in token");
                throw new InvalidOperationException("No certificate found in token");
            }

            byte[] certValue = session.GetAttributeValue(certHandle, new List<CKA> { CKA.CKA_VALUE })[0].GetValueAsByteArray();
            if (certValue == null || certValue.Length == 0)
            {
                _logger.LogError("Certificate data is empty");
                throw new InvalidOperationException("Certificate data is empty");
            }

            _logger.LogInformation("Certificate found with length: {Length} bytes", certValue.Length);
            return new X509Certificate2(certValue);
        }

        private static IObjectHandle GetSigningKey(Net.Pkcs11Interop.HighLevelAPI.ISession session)
        {
            IObjectHandle? keyHandle = session.FindAllObjects(new List<IObjectAttribute>
            {
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_KEY_TYPE, CKK.CKK_RSA),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true)
            }).FirstOrDefault();

            return keyHandle ?? throw new InvalidOperationException("No private key found in token");
        }
    }
}