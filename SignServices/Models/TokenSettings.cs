using System.Text.Json;

namespace SignServices.Models
{
    public class TokenSettings
    {
        private readonly IConfiguration? _configuration;

        public TokenSettings(IConfiguration? configuration = null)
        {
            _configuration = configuration;
            if (_configuration != null)
            {
                DriverPath = _configuration.GetValue<string>("TokenSettings:DriverPath") ?? string.Empty;
            }
        }

        public string DriverPath { get; set; } = string.Empty;

        public bool ValidateDriverPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                // Check if the file has a valid extension for a driver based on the platform
                string extension = Path.GetExtension(path).ToLower();
                string[] validExtensions = {
                    ".dll",    // Windows
                    ".so",     // Linux
                    ".dylib"   // macOS
                };
                return validExtensions.Contains(extension);
            }
            catch
            {
                return false;
            }
        }

        public JsonSerializerOptions GetOptions()
        {
            return new JsonSerializerOptions { WriteIndented = true };
        }

        public void SaveSettings(JsonSerializerOptions options)
        {
            if (!ValidateDriverPath(DriverPath))
            {
                throw new InvalidOperationException("Invalid driver path. Please ensure the file exists and has a .dll extension.");
            }

            string existingJson = File.Exists("appsettings.json") ? File.ReadAllText("appsettings.json") : "{}";
            JsonDocument jsonDocument = JsonDocument.Parse(existingJson);
            JsonElement rootElement = jsonDocument.RootElement;

            Dictionary<string, object> newSettings = new();
            foreach (JsonProperty property in rootElement.EnumerateObject())
            {
                if (property.Name != "TokenSettings")
                {
                    object? deserializedValue = JsonSerializer.Deserialize<object>(property.Value.GetRawText());
                    if (deserializedValue != null)
                    {
                        newSettings[property.Name] = deserializedValue;
                    }
                }
            }

            newSettings["TokenSettings"] = new { this.DriverPath };
            string jsonString = JsonSerializer.Serialize(newSettings, options);
            File.WriteAllText("appsettings.json", jsonString);
        }
    }

    public class TokenInfo
    {
        public string TokenSerial { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public string CertificateInfo { get; set; } = string.Empty;
        public string CertificateValidFrom { get; set; } = string.Empty;
        public string CertificateValidTo { get; set; } = string.Empty;
        public bool IsReadyForSigning { get; set; } = false;
    }

    public class CertificateInfo
    {
        public string OrganizationIdentifier { get; set; }
        public string Email { get; set; }
        public string CommonName { get; set; }
        public string OrganizationalUnit { get; set; }
        public string Organization { get; set; }
        public string Locality { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string SerialNumber { get; set; }
        public string Thumbprint { get; set; }
    }



    public class SignRequest
    {
        public string SerializedInvoice { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

    public class SignResponse
    {
        public string Signature { get; set; } = string.Empty;
    }
}