using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ess;

namespace EgyptEGS.SignerService;

public class JsonSigner
{
    private const string SHA256_OID = "2.16.840.1.101.3.4.2.1";
    private const string CONTENT_TYPE_OID = "1.2.840.113549.1.9.3";
    private const string MESSAGE_DIGEST_OID = "1.2.840.113549.1.9.4";
    private const string SIGNING_TIME_OID = "1.2.840.113549.1.9.5";
    private const string ESS_SIGNING_CERTIFICATE_V2_OID = "1.2.840.113549.1.9.16.2.47";
    private const string SIGNED_DATA_OID = "1.2.840.113549.1.7.2";

    private readonly X509Certificate2 _signingCert;

    public JsonSigner(string pfxPath, string password)
    {
#pragma warning disable SYSLIB0057
        _signingCert = new X509Certificate2(pfxPath, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
#pragma warning restore SYSLIB0057
    }

    public string SignDocument(string jsonContent)
    {
        var jsonDoc = JsonNode.Parse(jsonContent) ?? throw new ArgumentException("Invalid JSON content");
        var jsonForSigning = JsonNode.Parse(jsonContent) ?? throw new ArgumentException("Invalid JSON content");
        
        if (jsonForSigning["signatures"] != null)
        {
            ((JsonObject)jsonForSigning).Remove("signatures");
        }

        string serializedContent = JsonExtensions.SerializeJsonNode(jsonForSigning);
        byte[] contentBytes = System.Text.Encoding.UTF8.GetBytes(serializedContent);

        // Create content info with the correct OID
        var content = new ContentInfo(new Oid("1.2.840.113549.1.7.5"), contentBytes);
        var cms = new SignedCms(content, true);

        // Create ESS Signing Certificate V2
        using (var sha256 = SHA256.Create())
        {
            var certHash = sha256.ComputeHash(_signingCert.RawData);
            var bouncyCertificate = new EssCertIDv2(
                new Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier(
                    new DerObjectIdentifier("1.2.840.113549.1.9.16.2.47")), 
                certHash);

            var signerCertificateV2 = new SigningCertificateV2(new EssCertIDv2[] { bouncyCertificate });

            // Configure CMS signer
            var signer = new CmsSigner(_signingCert)
            {
                DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1")
            };

            // Add required attributes
            signer.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.UtcNow));
            signer.SignedAttributes.Add(new AsnEncodedData(
                new Oid("1.2.840.113549.1.9.16.2.47"), 
                signerCertificateV2.GetEncoded()));

            // Compute and encode the signature
            cms.ComputeSignature(signer);
            var signatureBytes = cms.Encode();

            // Add signature to JSON
            var signaturesArray = new JsonArray();
            signaturesArray.Add(new JsonObject
            {
                ["value"] = Convert.ToBase64String(signatureBytes)
            });
            ((JsonObject)jsonDoc)["signatures"] = signaturesArray;

            return jsonDoc.ToJsonString();
        }
    }
}