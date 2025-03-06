using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class DocumentSubmitRequest
    {
        [JsonProperty("documents")]
        public List<EgyptianInvoice> Documents { get; set; } = new List<EgyptianInvoice>();
    }
}
