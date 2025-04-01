using System.Text.Json.Nodes;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text;

public class TestSignedDocument
{
    public static void Main()
    {
        try
        {
            // Read the signed JSON file
            string signedJson = File.ReadAllText("C:\\Users\\Incredible One\\source\\repos\\EgyptEGS\\SignService\\OfficialData\\one-doc-I.json");
            
            // Parse the JSON into JsonNode
            JsonNode documentStructure = JsonNode.Parse(signedJson);
            
            // Create a copy without signatures for serialization
            if (documentStructure is JsonObject docObj)
            {
                docObj.Remove("signatures");
            }
            
            // Serialize using our method
            string serializedContent = JsonExtensions.SerializeJsonNode(documentStructure);
            
            // Output the serialized content for verification
            Console.WriteLine("Serialized content (without signatures):");
            Console.WriteLine(serializedContent);
            
            // Get the signatures from the original document
            if (documentStructure is JsonObject originalDoc && 
                originalDoc.TryGetPropertyValue("signatures", out JsonNode? signaturesNode) && 
                signaturesNode is JsonArray signatures)
            {
                foreach (JsonNode? signatureNode in signatures)
                {
                    if (signatureNode is JsonObject signatureObj &&
                        signatureObj.TryGetPropertyValue("value", out JsonNode? signatureValue))
                    {
                        // Decode the CMS/PKCS#7 signature
                        byte[] signatureBytes = Convert.FromBase64String(signatureValue.GetValue<string>());
                        SignedCms signedCms = new SignedCms();
                        
                        try
                        {
                            // Decode the CMS structure
                            signedCms.Decode(signatureBytes);
                            
                            // Get the signed content
                            string signedContent = Encoding.UTF8.GetString(signedCms.ContentInfo.Content);
                            
                            // Compare with our serialized content
                            bool isValid = signedContent.Equals(serializedContent, StringComparison.Ordinal);
                            Console.WriteLine($"\nSignature validation result: {(isValid ? "VALID" : "INVALID")}");
                            
                            if (!isValid)
                            {
                                Console.WriteLine("\nSigned content:");
                                Console.WriteLine(signedContent);
                            }
                        }
                        catch (CryptographicException ex)
                        {
                            Console.WriteLine($"\nSignature validation failed: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("\nNo signatures found in the document.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }
    }
}