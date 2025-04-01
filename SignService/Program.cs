using EgyptEGS.SignerService;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Read test document
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "OfficialData", "other-test.json");
            string jsonContent = File.ReadAllText(jsonPath);
            
            // Sign document using certificate from Referensi folder
            string certPath = Path.Combine(Directory.GetCurrentDirectory(), "Referensi", "test_cert.pfx");
            var signer = new JsonSigner(certPath, "test123");
            string signedJson = signer.SignDocument(jsonContent);
            
            // Save signed document
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "signed-output.json");
            File.WriteAllText(outputPath, signedJson);
            Console.WriteLine($"Signed document saved to: {outputPath}");
            
            // Verify signature
            Console.WriteLine("\nVerifying signature:");
            var analyzer = new SignatureAnalyzer();
            analyzer.AnalyzeSignedJson(signedJson);
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}