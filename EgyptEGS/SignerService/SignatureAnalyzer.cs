using System.Security.Cryptography.Pkcs;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EgyptEGS.SignerService;
public class SignatureAnalyzer
{
    public void AnalyzeSignedJson(string signedJson)
    {
        var jsonObj = JObject.Parse(signedJson);
        var documents = jsonObj["documents"].First();
        var signature = documents["signatures"].First()["value"].ToString();
        
        // Extract and analyze signature
        byte[] signatureBytes = Convert.FromBase64String(signature);
        SignedCms cms = new SignedCms();
        cms.Decode(signatureBytes);
        
        // Get the original content that was signed
        string signedContent = Encoding.UTF8.GetString(cms.ContentInfo.Content);
        
        Console.WriteLine("=== Signature Analysis ===");
        Console.WriteLine($"Content Type: {cms.ContentInfo.ContentType}");
        Console.WriteLine($"Is Detached: {cms.Detached}");
        Console.WriteLine("\nSigned Content:");
        Console.WriteLine(signedContent);
        
        foreach(var signer in cms.SignerInfos)
        {
            Console.WriteLine("\nSigner Details:");
            Console.WriteLine($"Digest Algorithm: {signer.DigestAlgorithm.FriendlyName}");
            Console.WriteLine("\nSigned Attributes:");
            foreach(var attr in signer.SignedAttributes)
            {
                Console.WriteLine($"OID: {attr.Oid.Value}");
            }
        }
        
        // Remove signature and get original document
        documents["signatures"] = null;
        string originalJson = jsonObj.ToString();
        Console.WriteLine("\nOriginal Document:");
        Console.WriteLine(originalJson);
    }

    public void CompareCanonicalizations(string officialJson, string ourJson)
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
}