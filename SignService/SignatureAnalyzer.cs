using System.Security.Cryptography.Pkcs;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace EgyptEGS.SignerService;
public class SignatureAnalyzer
{
    private const string EXPECTED_CONTENT_TYPE_OID = "1.2.840.113549.1.7.5";
    private const string SHA256_OID = "2.16.840.1.101.3.4.2.1";
    private const string ESS_SIGNING_CERTIFICATE_V2_OID = "1.2.840.113549.1.9.16.2.47";
    private const string SIGNING_TIME_OID = "1.2.840.113549.1.9.5";
    private const string CONTENT_TYPE_OID = "1.2.840.113549.1.9.3";
    private const string MESSAGE_DIGEST_OID = "1.2.840.113549.1.9.4";
    private string documentIssuerId = string.Empty; // Initialize with empty string to fix CS8618
    
    public void AnalyzeSignedJson(string signedJson)
    {
        var jsonObj = JObject.Parse(signedJson);
        var signatures = jsonObj["signatures"];
        if (signatures == null || !signatures.Any())
        {
            throw new InvalidOperationException("No signatures found in the document");
        }

        // Get issuer ID from document
        documentIssuerId = jsonObj["issuer"]?["id"]?.ToString() 
            ?? throw new InvalidOperationException("Issuer ID not found in document"); // Fix CS8601
        if (string.IsNullOrEmpty(documentIssuerId))
        {
            throw new InvalidOperationException("Issuer ID not found in document");
        }

        var signature = signatures.First()["value"]?.ToString() 
            ?? throw new InvalidOperationException("Invalid signature format");
        
        // Validate Base64
        if (!IsValidBase64String(signature))
        {
            throw new InvalidOperationException("Signature is not a valid Base64 string");
        }

        // Create a copy of the JSON without signatures for content verification
        var contentObj = JObject.Parse(signedJson);
        contentObj.Remove("signatures");
        string canonicalJson = contentObj.ToString(Newtonsoft.Json.Formatting.None);
        
        // Extract and analyze signature
        byte[] signatureBytes = Convert.FromBase64String(signature);
        var signedCms = new SignedCms();
        signedCms.Decode(signatureBytes);
        
        foreach(var signerInfo in signedCms.SignerInfos)
        {
            AnalyzeCertificate(signerInfo, signedCms);
            AnalyzeSignatureDetails(signerInfo);
            VerifyMandatoryAttributes(signerInfo);
            CheckUnsignedAttributes(signerInfo);
        }
    
        // Content verification
        Console.WriteLine("\nContent Verification:");
        var signedContent = canonicalJson;
        Console.WriteLine($"Original Content Length: {canonicalJson.Length}");
        Console.WriteLine($"Signed Content Length: {signedContent.Length}");
        CompareCanonicalizations(signedContent, canonicalJson);
    }

    private void AnalyzeCertificate(SignerInfo signerInfo, SignedCms signedCms)
        {
            Console.WriteLine("\nSigner Certificate Details:");
            try
            {
                var cert = signedCms.Certificates[0];
                
                // Check RSA key usage - looking specifically for digitalSignature and nonRepudiation
                bool hasValidKeyUsage = false;
                foreach (var extension in cert.Extensions)
                {
                    if (extension.Oid?.Value == "2.5.29.15") // Key Usage
                    {
                        var keyUsage = (System.Security.Cryptography.X509Certificates.X509KeyUsageExtension)extension;
                        Console.WriteLine($"Key Usage Flags: {keyUsage.KeyUsages}");
                        
                        if ((keyUsage.KeyUsages & System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.DigitalSignature) != 0 &&
                            (keyUsage.KeyUsages & System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.NonRepudiation) != 0)
                        {
                            hasValidKeyUsage = true;
                        }
                    }
                }
            
                if (!hasValidKeyUsage)
                {
                    Console.WriteLine("Error: Certificate does not have required key usage flags (DigitalSignature and NonRepudiation)");
                }
            
                // Check if certificate is from Egypt
                var countryField = cert.Subject.Split(',').FirstOrDefault(x => x.Trim().StartsWith("C="));
                if (countryField == null || !countryField.Contains("EG"))
                {
                    Console.WriteLine("Error: Certificate is not issued in Egypt");
                }
            
                // Check VAT ID in certificate subject and verify against issuer ID
                var vatField = cert.Subject.Split(',').FirstOrDefault(x => x.Trim().StartsWith("OID.2.5.4.97="));
                if (vatField != null)
                {
                    var vatId = vatField.Split('=')[1].Trim();
                    Console.WriteLine($"VAT ID in certificate: {vatId}");
                    
                    // Extract number from certificate VAT ID (remove VATEG- prefix)
                    var certVatNumber = vatId.StartsWith("VATEG-") ? vatId.Substring(6) : vatId;
                    
                    if (certVatNumber != documentIssuerId)
                    {
                        Console.WriteLine($"Error: Certificate VAT ID number ({certVatNumber}) does not match document issuer ID ({documentIssuerId})");
                        throw new InvalidOperationException("VAT ID mismatch between certificate and document issuer");
                    }
                    else
                    {
                        Console.WriteLine("VAT ID verification successful: Certificate VAT ID matches document issuer ID");
                    }
                }
                else
                {
                    Console.WriteLine("Error: No VAT ID found in certificate subject");
                    throw new InvalidOperationException("No VAT ID found in certificate subject");
                }

                // Enhanced certificate validation
                var now = DateTime.UtcNow;
                if (cert.NotBefore > now)
                {
                    Console.WriteLine("\nError: Certificate is not yet valid!");
                    Console.WriteLine($"Current time: {now}");
                    Console.WriteLine($"Certificate becomes valid at: {cert.NotBefore}");
                }
                if (cert.NotAfter < now)
                {
                    Console.WriteLine("\nError: Certificate has expired!");
                    Console.WriteLine($"Current time: {now}");
                    Console.WriteLine($"Certificate expired at: {cert.NotAfter}");
                }
            
                // Remove extended key usage check since basic key usage flags are sufficient
                if (signedCms.Certificates.Count > 1)
                {
                    Console.WriteLine("Warning: More than one certificate found. Spec requires only signer certificate.");
                }
        
                Console.WriteLine($"Subject: {cert.Subject}");
                Console.WriteLine($"Issuer: {cert.Issuer}");
                Console.WriteLine($"Valid From: {cert.NotBefore}");
                Console.WriteLine($"Valid To: {cert.NotAfter}");
                Console.WriteLine($"Serial Number: {cert.SerialNumber}");
                Console.WriteLine($"Certificate Type: X.509 v{cert.Version}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing certificate: {ex.Message}");
                throw;
            }
        }

    private bool IsValidBase64String(string base64)
    {
        try
        {
            Convert.FromBase64String(base64);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void AnalyzeSignatureDetails(SignerInfo signerInfo)
    {
        Console.WriteLine("\nSignature Details:");
        Console.WriteLine($"Digest Algorithm: {signerInfo.DigestAlgorithm.Value}");
        Console.WriteLine($"Expected Digest Algorithm: {SHA256_OID}");
        Console.WriteLine($"Digest Algorithm Valid: {signerInfo.DigestAlgorithm.Value == SHA256_OID}");
    }

    private void VerifyMandatoryAttributes(SignerInfo signerInfo)
    {
        var signedAttrs = signerInfo.SignedAttributes;
        if (signedAttrs == null)
        {
            Console.WriteLine("Error: No signed attributes found");
            return;
        }
    
        var mandatoryOids = new Dictionary<string, bool>
        {
            { CONTENT_TYPE_OID, false },
            { MESSAGE_DIGEST_OID, false },
            { SIGNING_TIME_OID, false },
            { ESS_SIGNING_CERTIFICATE_V2_OID, false }
        };
    
        foreach (var attr in signedAttrs)
        {
            var oid = attr.Oid?.Value;
            if (oid != null && mandatoryOids.ContainsKey(oid))
            {
                mandatoryOids[oid] = true;
                Console.WriteLine($"\nFound mandatory attribute: {oid}");

                // Analyze specific attributes
                if (oid == CONTENT_TYPE_OID)
                {
                    try
                    {
                        var contentType = attr.Values[0].RawData;
                        // Skip the first byte (tag) and length byte to get the actual OID value
                        var oidBytes = new byte[contentType.Length - 2];
                        Array.Copy(contentType, 2, oidBytes, 0, oidBytes.Length);
                        var oidValue = BitConverter.ToString(oidBytes).Replace("-", "");
                        
                        // Convert hex to OID
                        var contentTypeOid = ConvertHexToOid(oidValue);
                        Console.WriteLine($"Content Type Value: {contentTypeOid}");
                        if (contentTypeOid != EXPECTED_CONTENT_TYPE_OID)
                        {
                            Console.WriteLine($"Warning: Unexpected content type value. Expected {EXPECTED_CONTENT_TYPE_OID}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing content type: {ex.Message}");
                    }
                }
                else if (oid == MESSAGE_DIGEST_OID)
                {
                    var messageDigest = BitConverter.ToString(attr.Values[0].RawData).Replace("-", "");
                    Console.WriteLine($"Message Digest: {messageDigest}");
                }
                else if (oid == SIGNING_TIME_OID)
                {
                    try
                    {
                        var timeData = attr.Values[0];
                        var utcTime = new AsnEncodedData(timeData.RawData).Format(false);
                        Console.WriteLine($"Signing Time (UTC): {utcTime}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing signing time: {ex.Message}");
                    }
                }
                else if (oid == ESS_SIGNING_CERTIFICATE_V2_OID)
                {
                    var certV2Data = attr.Values[0].RawData;
                    Console.WriteLine("ESS Signing Certificate v2:");
                    Console.WriteLine($"Raw Data Length: {certV2Data.Length} bytes");
                    
                    // The first part should be SHA256 hash (32 bytes)
                    if (certV2Data.Length >= 32)
                    {
                        var hashPart = new byte[32];
                        Array.Copy(certV2Data, hashPart, 32);
                        Console.WriteLine($"Certificate Hash (SHA256): {BitConverter.ToString(hashPart).Replace("-", "")}");
                    }
                }
            }
        }

        Console.WriteLine("\nMandatory Attributes Status:");
        foreach (var pair in mandatoryOids)
        {
            Console.WriteLine($"{pair.Key}: {(pair.Value ? "Present" : "Missing")}");
        }

        bool allMandatoryPresent = mandatoryOids.Values.All(v => v);
        Console.WriteLine($"\nAll Mandatory Attributes Present: {allMandatoryPresent}");
    }

    private void CheckUnsignedAttributes(SignerInfo signerInfo)
    {
        Console.WriteLine("\nUnsigned Attributes:");
        var unsignedAttrs = signerInfo.UnsignedAttributes;
        if (unsignedAttrs != null && unsignedAttrs.Count > 0)
        {
            Console.WriteLine("Warning: CAdES-BES should not contain unsigned attributes");
            foreach(var attr in unsignedAttrs)
            {
                Console.WriteLine($"Unexpected Attribute OID: {attr.Oid?.Value}");
            }
        }
        else
        {
            Console.WriteLine("No unsigned attributes (correct for CAdES-BES)");
        }
    }

    private void CompareCanonicalizations(string officialJson, string ourJson)
    {
        // Compare byte by byte
        byte[] official = Encoding.UTF8.GetBytes(officialJson);
        byte[] ours = Encoding.UTF8.GetBytes(ourJson);

        Console.WriteLine("\n=== Canonicalization Comparison ===");
        Console.WriteLine($"Official Length: {official.Length}");
        Console.WriteLine($"Our Length: {ours.Length}");

        if (official.Length != ours.Length)
        {
            Console.WriteLine("Different lengths!");
            return;
        }

        for (int i = 0; i < official.Length; i++)
        {
            if (official[i] != ours[i])
            {
                Console.WriteLine($"Difference at position {i}:");
                Console.WriteLine($"Official: {(char)official[i]} ({official[i]})");
                Console.WriteLine($"Ours: {(char)ours[i]} ({ours[i]})");
            }
        }
    }

    // Add this helper method to the class
    private string ConvertHexToOid(string hex)
    {
        // First two nodes are encoded in the first byte: node1 = byte / 40, node2 = byte % 40
        byte firstByte = Convert.ToByte(hex.Substring(0, 2), 16);
        int node1 = firstByte / 40;
        int node2 = firstByte % 40;
        
        List<string> nodes = new List<string> { node1.ToString(), node2.ToString() };
        
        // Process remaining bytes
        int value = 0;
        for (int i = 2; i < hex.Length; i += 2)
        {
            byte b = Convert.ToByte(hex.Substring(i, 2), 16);
            value = (value << 7) | (b & 0x7F);
            
            if ((b & 0x80) == 0)
            {
                nodes.Add(value.ToString());
                value = 0;
            }
        }
        
        return string.Join(".", nodes);
    }
}