using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

public static class JsonExtensions
{

    public static (string, byte[]) CalucateHash(string jsonString)
    {
        try
        {
            JsonNode jNode = JsonNode.Parse(jsonString);
            string serializedData = SerializeJsonNode(jNode);

            byte[] dataToSign = Encoding.UTF8.GetBytes(serializedData);

            // Calculate SHA256 hash
            byte[] hash;
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                hash = sha256.ComputeHash(dataToSign);
            }
            return (serializedData, hash);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid JSON format", nameof(jsonString), ex);
        }
    }

    public static string SerializeJsonNode(JsonNode documentStructure)
    {
        // If it's a simple value type
        if (documentStructure is JsonValue value)
        {
            return $"\"{value}\"";
        }

        StringBuilder serializedString = new();

        // If it's an object, process each element
        if (documentStructure is JsonObject obj)
        {
            foreach (KeyValuePair<string, JsonNode?> element in obj)
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
                    foreach (JsonNode? arrayElement in array)
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

    public static string CleanseJson(this string json, bool writeIndented = false)
    {
        if (!writeIndented)
        {
            // Membersihkan whitespace berlebih tanpa mengubah data
            return Regex.Replace(json, @"\s+", " ").Trim();
        }

        // Jika indentasi diperlukan, format secara manual (basic)
        StringBuilder indentedJson = new();
        int indentLevel = 0;
        bool inQuotes = false;

        foreach (char c in json)
        {
            switch (c)
            {
                case '{':
                case '[':
                    if (!inQuotes)
                    {
                        indentedJson.Append(c);
                        indentedJson.AppendLine();
                        indentLevel++;
                        indentedJson.Append(new string(' ', indentLevel * 4));
                    }
                    else
                    {
                        indentedJson.Append(c);
                    }
                    break;
                case '}':
                case ']':
                    if (!inQuotes)
                    {
                        indentedJson.AppendLine();
                        indentLevel--;
                        indentedJson.Append(new string(' ', indentLevel * 4));
                        indentedJson.Append(c);
                    }
                    else
                    {
                        indentedJson.Append(c);
                    }
                    break;
                case '"':
                    inQuotes = !inQuotes;
                    indentedJson.Append(c);
                    break;
                case ',':
                    indentedJson.Append(c);
                    if (!inQuotes)
                    {
                        indentedJson.AppendLine();
                        indentedJson.Append(new string(' ', indentLevel * 4));
                    }
                    break;
                default:
                    indentedJson.Append(c);
                    break;
            }
        }

        return indentedJson.ToString();
    }
}
