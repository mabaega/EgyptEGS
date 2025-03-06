using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class Payment
    {
        [JsonProperty("bankName")]
        public string? BankName { get; set; }

        [JsonProperty("bankAddress")]
        public string? BankAddress { get; set; }

        [JsonProperty("bankAccountNo")]
        public string? BankAccountNo { get; set; }

        [JsonProperty("bankAccountIBAN")]
        public string? BankAccountIBAN { get; set; }

        [JsonProperty("swiftCode")]
        public string? SwiftCode { get; set; }

        [JsonProperty("terms")]
        public string? Terms { get; set; }
    }
}
