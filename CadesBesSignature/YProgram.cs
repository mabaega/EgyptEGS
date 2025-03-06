using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BCContentInfo = Org.BouncyCastle.Asn1.Cms.ContentInfo;
using BCSignerInfo = Org.BouncyCastle.Asn1.Cms.SignerInfo;
using BCSignedData = Org.BouncyCastle.Asn1.Cms.SignedData;
using BCAttribute = Org.BouncyCastle.Asn1.Cms.Attribute;
using BCIssuerAndSerialNumber = Org.BouncyCastle.Asn1.Cms.IssuerAndSerialNumber;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Pkcs;

public class TokenSigner
{
    private readonly string DllLibPath;
    private readonly string TokenPin;

    public TokenSigner(string pkcs11LibraryPath, string tokenPin)
    {
        DllLibPath = pkcs11LibraryPath;
        TokenPin = tokenPin;
    }

    public static void Main(String[] args)
    {
        try 
        {
            string pkcs11LibraryPath = @"D:\SoftHSM2\lib\softhsm2-x64.dll";
            string tokenPin = "1234";
            string stringToSign = "Hello, World!";

            var signer = new TokenSigner(pkcs11LibraryPath, tokenPin);
            Console.WriteLine("Token Information:");
            signer.ListTokenInfo();
            
            Console.WriteLine("\nSigning data...");
            var signature = signer.Sign(stringToSign);
            Console.WriteLine($"Signature: {signature}");
        }
        catch(Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }

    public void ListTokenInfo()
    {
        using var pkcs11 = GetPkcs11Library();
        var slot = GetTokenSlot(pkcs11);
        if (slot == null) return;

        var tokenInfo = slot.GetTokenInfo();
        Console.WriteLine($"Token Label: {tokenInfo.Label}");
        Console.WriteLine($"Token Model: {tokenInfo.Model}");
        Console.WriteLine($"Token Serial: {tokenInfo.SerialNumber}");

        using var session = slot.OpenSession(SessionType.ReadWrite);
        session.Login(CKU.CKU_USER, Encoding.UTF8.GetBytes(TokenPin));

        var cert = GetSigningCertificate(session);
        if (cert != null)
        {
            Console.WriteLine($"Certificate Subject: {cert.Subject}");
            Console.WriteLine($"Certificate Issuer: {cert.Issuer}");
            Console.WriteLine($"Valid Until: {cert.NotAfter}");
        }
    }

    public string Sign(string data)
    {
        using var pkcs11 = GetPkcs11Library();
        using var session = GetTokenSlot(pkcs11).OpenSession(SessionType.ReadWrite);
        
        session.Login(CKU.CKU_USER, Encoding.UTF8.GetBytes(TokenPin));
        
        var cert = GetSigningCertificate(session);
        var key = GetSigningKey(session);
        
        // Create CAdES-BES structure
        var contentInfo = new BCContentInfo(
            new DerObjectIdentifier("1.2.840.113549.1.7.1"),
            new DerOctetString(Encoding.UTF8.GetBytes(data)));

        var signedAttrs = GetSignedAttributes(session, cert, data);  // Pass session and cert
        
        var signerInfo = new BCSignerInfo(
            new SignerIdentifier(new BCIssuerAndSerialNumber(
                X509Name.GetInstance(cert.IssuerName.RawData),
                new DerInteger(BitConverter.ToInt64(Convert.FromHexString(cert.SerialNumber))))),
            new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1")),
            signedAttrs,
            new AlgorithmIdentifier(new DerObjectIdentifier("1.2.840.113549.1.1.11")),
            new DerOctetString(SignWithToken(session, key, signedAttrs)),  // Pass signedAttrs instead of data
            null);

        // Fix SignedData creation
        var signedData = new BCSignedData(
            new DerSet(new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1"))),
            contentInfo,
            new DerSet(new DerSequence(new Asn1Encodable[] { new DerOctetString(cert.RawData) })),  // Fixed certificate encoding
            null,
            new DerSet(signerInfo));

        var finalContentInfo = new BCContentInfo(
            new DerObjectIdentifier("1.2.840.113549.1.7.2"),
            signedData);

        return Convert.ToBase64String(finalContentInfo.GetEncoded());
    }

    private DerSet GetSignedAttributes(ISession session, X509Certificate2 cert, string data)  // Add parameters
    {
        var messageDigest = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        var certHash = SHA256.HashData(cert.RawData);
        
        var signingCertificateV2 = new DerSequence(
            new DerSequence(
                new DerSequence(
                    new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1")),
                    new DerOctetString(certHash),
                    new IssuerSerial(
                        new GeneralNames(new GeneralName(X509Name.GetInstance(cert.IssuerName.RawData))),  // Fix IssuerSerial
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
            new BCAttribute(new DerObjectIdentifier("1.2.840.113549.1.9.16.2.47"), // SigningCertificateV2
                new DerSet(signingCertificateV2)));
    }

    private byte[] SignWithToken(ISession session, IObjectHandle key, DerSet signedAttrs)  // Change parameter type
    {
        var mechanism = session.Factories.MechanismFactory.Create(CKM.CKM_SHA256_RSA_PKCS);
        byte[] signedAttrsEncoded = signedAttrs.GetDerEncoded();
        return session.Sign(mechanism, key, signedAttrsEncoded);
    }
    
    private IPkcs11Library GetPkcs11Library()
    {
        var factories = new Pkcs11InteropFactories();
        return factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, DllLibPath, AppType.MultiThreaded);
    }

    private ISlot GetTokenSlot(IPkcs11Library pkcs11)
    {
        return pkcs11.GetSlotList(SlotsType.WithTokenPresent).FirstOrDefault() 
            ?? throw new InvalidOperationException("No token found");
    }

    private X509Certificate2 GetSigningCertificate(ISession session)
    {
        var certHandle = session.FindAllObjects(new List<IObjectAttribute>
        {
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_CERTIFICATE),
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CERTIFICATE_TYPE, CKC.CKC_X_509),
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true)
        }).FirstOrDefault();

        if (certHandle == null)
            throw new InvalidOperationException("No certificate found in token");

        var certValue = session.GetAttributeValue(certHandle, new List<CKA> { CKA.CKA_VALUE })[0].GetValueAsByteArray();
        return new X509Certificate2(certValue);
    }

    private IObjectHandle GetSigningKey(ISession session)
    {
        var keyHandle = session.FindAllObjects(new List<IObjectAttribute>
        {
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY),
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_KEY_TYPE, CKK.CKK_RSA),
            session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true)
        }).FirstOrDefault();

        return keyHandle ?? throw new InvalidOperationException("No private key found in token");
    }
}