using EgyptEGS.SignerService;
using System.Text;

class XProgram
{
    static void XMain(string[] args)
    {
        try
        {
           string filePath = Path.Combine(Directory.GetCurrentDirectory(), "OfficialData", "other-test-I.json");
           //string filePath = Path.Combine(Directory.GetCurrentDirectory(), "OfficialData", "one-doc-I.json");
            Console.WriteLine($"Reading file from: {filePath}");
            
            // Read the JSON file
            string jsonContent = File.ReadAllText(filePath);
            
            // Create an instance of SignatureAnalyzer
            var analyzer = new SignatureAnalyzer();
            
            // Analyze the signed JSON
            analyzer.AnalyzeSignedJson(jsonContent);
            
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
