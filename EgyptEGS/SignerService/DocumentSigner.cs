using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ess;


namespace EgyptEGS.SignerService;
public class DocumentSigner
{
    private readonly X509Certificate2 _certificate;

    public DocumentSigner(string certPath, string password)
    {
        _certificate = new X509Certificate2(certPath, password);
    }

    public string SignDocument(string content)
    {
        byte[] data = Encoding.UTF8.GetBytes(content);
        
        ContentInfo contentInfo = new ContentInfo(new Oid("1.2.840.113549.1.7.5"), data);
        SignedCms cms = new SignedCms(contentInfo, true);

        EssCertIDv2 bouncyCertificate = new EssCertIDv2(
            new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(
                new DerObjectIdentifier("1.2.840.113549.1.9.16.2.47")), 
            HashBytes(_certificate.RawData)
        );
        
        SigningCertificateV2 signerCertificateV2 = new SigningCertificateV2(new[] { bouncyCertificate });
        CmsSigner signer = new CmsSigner(_certificate)
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

    private byte[] HashBytes(byte[] input)
    {
        using (var sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(input);
        }
    }
}