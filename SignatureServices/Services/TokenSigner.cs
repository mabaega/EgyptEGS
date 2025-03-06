using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.X509;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TokenServices.Models;
using BCAttribute = Org.BouncyCastle.Asn1.Cms.Attribute;
using BCContentInfo = Org.BouncyCastle.Asn1.Cms.ContentInfo;
using BCIssuerAndSerialNumber = Org.BouncyCastle.Asn1.Cms.IssuerAndSerialNumber;
using BCSignedData = Org.BouncyCastle.Asn1.Cms.SignedData;
using BCSignerInfo = Org.BouncyCastle.Asn1.Cms.SignerInfo;

namespace TokenServices.Services
{
    public class TokenSigner
    {
        private readonly string DllLibPath;
        private readonly string TokenPin;
        private readonly ILogger<TokenSigner> _logger;

        public TokenSigner(string pkcs11LibraryPath, string tokenPin, ILogger<TokenSigner> logger)
        {
            DllLibPath = pkcs11LibraryPath;
            TokenPin = tokenPin;
            _logger = logger;
        }

        public TokenInfo? GetTokenInfo()
        {
            try
            {
                _logger.LogInformation("Initializing PKCS#11 library");
                using IPkcs11Library pkcs11 = GetPkcs11Library();
                ISlot slot = GetTokenSlot(pkcs11);
                if (slot == null)
                {
                    return null;
                }

                ITokenInfo tokenInfo = slot.GetTokenInfo();
                using Net.Pkcs11Interop.HighLevelAPI.ISession session = slot.OpenSession(SessionType.ReadWrite);
                session.Login(CKU.CKU_USER, Encoding.UTF8.GetBytes(TokenPin));

                X509Certificate2 cert = GetSigningCertificate(session);

                // Ensure we're getting actual values from the token
                //_logger.LogInformation("Token Label: {Label}", tokenInfo.Label);
                //_logger.LogInformation("Token Model: {Model}", tokenInfo.Model);
                //_logger.LogInformation("Token Serial: {Serial}", tokenInfo.SerialNumber);

                return new TokenInfo
                {
                    Label = tokenInfo.Label.Trim(),
                    Model = tokenInfo.Model.Trim(),
                    SerialNumber = tokenInfo.SerialNumber.Trim(),
                    CertificateSubject = cert.Subject,
                    CertificateIssuer = cert.Issuer,
                    ValidUntil = cert.NotAfter
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token information");
                throw;
            }
        }

        public string Sign(string data)
        {
            try
            {
                _logger.LogInformation("Initializing signing process");
                using IPkcs11Library pkcs11 = GetPkcs11Library();
                using Net.Pkcs11Interop.HighLevelAPI.ISession session = GetTokenSlot(pkcs11).OpenSession(SessionType.ReadWrite);

                session.Login(CKU.CKU_USER, Encoding.UTF8.GetBytes(TokenPin));

                X509Certificate2 cert = GetSigningCertificate(session);
                IObjectHandle key = GetSigningKey(session);

                BCContentInfo contentInfo = new(
                    new DerObjectIdentifier("1.2.840.113549.1.7.1"),
                    new DerOctetString(Encoding.UTF8.GetBytes(data)));

                _logger.LogInformation("Creating signed attributes");
                DerSet signedAttrs = GetSignedAttributes(session, cert, data);

                _logger.LogInformation("Creating signer info");
                BCSignerInfo signerInfo = new(
                    new SignerIdentifier(new BCIssuerAndSerialNumber(
                        X509Name.GetInstance(cert.IssuerName.RawData),
                        new DerInteger(BitConverter.ToInt64(Convert.FromHexString(cert.SerialNumber))))),
                    new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1")),
                    signedAttrs,
                    new AlgorithmIdentifier(new DerObjectIdentifier("1.2.840.113549.1.1.11")),
                    new DerOctetString(SignWithToken(session, key, signedAttrs)),
                    null);

                _logger.LogInformation("Creating signed data");
                BCSignedData signedData = new(
                    new DerSet(new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1"))),
                    contentInfo,
                    new DerSet(new DerSequence(new Asn1Encodable[] { new DerOctetString(cert.RawData) })),
                    null,
                    new DerSet(signerInfo));

                BCContentInfo finalContentInfo = new(
                    new DerObjectIdentifier("1.2.840.113549.1.7.2"),
                    signedData);

                _logger.LogInformation("Signature created successfully");
                return Convert.ToBase64String(finalContentInfo.GetEncoded());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing data");
                throw;
            }
        }

        private DerSet GetSignedAttributes(Net.Pkcs11Interop.HighLevelAPI.ISession session, X509Certificate2 cert, string data)
        {
            byte[] messageDigest = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            byte[] certHash = SHA256.HashData(cert.RawData);

            DerSequence signingCertificateV2 = new(
                new DerSequence(
                    new DerSequence(
                        new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1")),
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

        private byte[] SignWithToken(Net.Pkcs11Interop.HighLevelAPI.ISession session, IObjectHandle key, DerSet signedAttrs)
        {
            IMechanism mechanism = session.Factories.MechanismFactory.Create(CKM.CKM_SHA256_RSA_PKCS);
            byte[] signedAttrsEncoded = signedAttrs.GetDerEncoded();
            return session.Sign(mechanism, key, signedAttrsEncoded);
        }

        private IPkcs11Library GetPkcs11Library()
        {
            Pkcs11InteropFactories factories = new();
            return factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, DllLibPath, AppType.MultiThreaded);
        }

        private ISlot GetTokenSlot(IPkcs11Library pkcs11)
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

        private IObjectHandle GetSigningKey(Net.Pkcs11Interop.HighLevelAPI.ISession session)
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