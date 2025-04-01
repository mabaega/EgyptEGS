using System;
using System.IO;
using EgyptEGS.SignerService;

public class TestSignatureAnalysis
{
    public static void Main()
    {
        try
        {
            // Read the source JSON file
            string signedJson = File.ReadAllText("OfficialData/one-doc-I.json");

            // Create analyzer instance and analyze the signature
            var analyzer = new SignatureAnalyzer();
            analyzer.AnalyzeSignedJson(signedJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}