using EgyptEGS.Models;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace EgyptEGS.Utilities
{
    public static class Utils
    {
        public static string ConstructInvoiceApiUrl(string referrer, string invoiceUUID)
        {
            Uri uri = new(referrer);
            string baseUrl = $"{uri.Scheme}://{uri.Host}";

            if (uri.Port is not 80 and not 443)
            {
                baseUrl += $":{uri.Port}";
            }

            if (referrer.Contains("purchase-invoice-view"))
            {
                return $"{baseUrl}/api2/purchase-invoice-form/{invoiceUUID}";
            }
            else if (referrer.Contains("sales-invoice-view"))
            {
                return $"{baseUrl}/api2/sales-invoice-form/{invoiceUUID}";
            }
            else if (referrer.Contains("debit-note-view"))
            {
                return $"{baseUrl}/api2/debit-note-form/{invoiceUUID}";
            }
            else if (referrer.Contains("credit-note-view"))
            {
                return $"{baseUrl}/api2/credit-note-form/{invoiceUUID}";
            }

            throw new ArgumentException("Invalid referrer URL");
        }

        public static string ReadEmbeddedResource(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Nama lengkap resource: {Namespace}.{FileName}
            string fullResourceName = $"{assembly.GetName().Name}.{resourceName}";

            using Stream stream = assembly.GetManifestResourceStream(fullResourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Resource '{fullResourceName}' not found.");
            }

            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        public static string SerializeObject<T>(T value)
        {
            StringBuilder sb = new(256);
            StringWriter sw = new(sb, CultureInfo.InvariantCulture);

            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
            using (JsonTextWriter jsonWriter = new(sw))
            {
                jsonWriter.Formatting = Newtonsoft.Json.Formatting.Indented;
                jsonWriter.IndentChar = ' ';
                jsonWriter.Indentation = 4;

                jsonSerializer.Serialize(jsonWriter, value, typeof(T));
            }

            return sw.ToString();
        }

        public static (ApplicationConfig, long) InitializeBusinessData(string jsonBusiness)
        {
            ApplicationConfig AppConfig = new();
            long EGSVersion = 0;

            if (string.IsNullOrEmpty(jsonBusiness))
            {
                return (AppConfig, EGSVersion);
            }

            string base64CerInfo = RelayDataHelper.GetStringCustomField2Value(jsonBusiness, ManagerCustomField.AppConfigGuid);

            if (!string.IsNullOrEmpty(base64CerInfo))
            {
                try
                {
                    AppConfig = ObjectCompressor.DeserializeFromBase64String<ApplicationConfig>(base64CerInfo)
                        ?? throw new InvalidOperationException("Failed to deserialize ApplicationConfig");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to initialize business data", ex);
                }
            }

            EGSVersion = VersionHelper.GetNumberOnly(RelayDataHelper.GetStringCustomField2Value(jsonBusiness, ManagerCustomField.AppVersionGuid));

            return (AppConfig, EGSVersion);
        }

        public static string GetValue(Dictionary<string, string> formData, string key)
        {
            return formData.GetValueOrDefault(key) ?? string.Empty;
        }

    }
}
