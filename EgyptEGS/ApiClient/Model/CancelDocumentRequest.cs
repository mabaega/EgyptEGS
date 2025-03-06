using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class CancelDocumentRequest
    {
        [JsonProperty("status")]
        public string Status { get; set; } = "cancelled";

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}