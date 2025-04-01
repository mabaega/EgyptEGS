using System.Text.Json.Nodes;
using System;
using System.IO;

public class TestJsonSerialization
{
    public static void Main()
    {
        try
        {
            // Read the source JSON file using relative path
            string sourceJson = File.ReadAllText("OfficialData/one-doc.json");
            string expectedSerialized = File.ReadAllText("OfficialData/one-doc-serialized.json.txt");

            // Parse the JSON into JsonNode
            JsonNode documentStructure = JsonNode.Parse(sourceJson);

            // Serialize using our method
            string actualSerialized = JsonExtensions.SerializeJsonNode(documentStructure);

            // Compare results
            bool areEqual = actualSerialized == expectedSerialized;
            Console.WriteLine($"Serialization test result: {(areEqual ? "PASSED" : "FAILED")}");

            if (!areEqual)
            {
                Console.WriteLine("\nActual output:");
                Console.WriteLine(actualSerialized);
                Console.WriteLine("\nExpected output:");
                Console.WriteLine(expectedSerialized);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }
    }
}