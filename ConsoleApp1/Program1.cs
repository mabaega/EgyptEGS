using System;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Text;

public class Program1
{
    public static void XMain(string[] args)
    {
        // Define file paths
        var inputFilePath = @"C:\Tmp\one-doc.json";
        var outputFilePath = @"C:\Tmp\SeializedOneDoc.txt";

        // Load the content from the input file
        string fileContent = File.ReadAllText(inputFilePath);

        // Parse the content into a JsonNode
        JsonNode documentStructure = JsonNode.Parse(fileContent);

        // Serialize the JsonNode using the custom method
        string serializedContent = SerializeJsonNode(documentStructure);

        // Save the serialized content to the output file
        File.WriteAllText(outputFilePath, serializedContent);
    }

    public static string SerializeJsonNode(JsonNode documentStructure)
    {
        // If it's a simple value type
        if (documentStructure is JsonValue value)
        {
            return $"\"{value.ToString()}\"";
        }

        var serializedString = new StringBuilder();

        // If it's an object, process each element
        if (documentStructure is JsonObject obj)
        {
            foreach (var element in obj)
            {
                // If element is not array type
                if (element.Value is not JsonArray)
                {
                    serializedString.Append($"\"{element.Key.ToUpperInvariant()}\"");
                    serializedString.Append(SerializeJsonNode(element.Value));
                }
                // If element is array type
                else if (element.Value is JsonArray array)
                {
                    serializedString.Append($"\"{element.Key.ToUpperInvariant()}\"");
                    foreach (var arrayElement in array)
                    {
                        // Add array property name before each element for JSON
                        serializedString.Append($"\"{element.Key.ToUpperInvariant()}\"");
                        serializedString.Append(SerializeJsonNode(arrayElement));
                    }
                }
            }
        }

        return serializedString.ToString();
    }
}
