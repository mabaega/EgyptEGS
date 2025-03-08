using EgyptEGS.ApiClient.Extensions;
using Newtonsoft.Json;

namespace EgyptEGS.Models
{
    public class InvoiceSummary
    {
        public string DocumentIssueDate { get; set; }
        public string SubmissionId { get; set; } = string.Empty;

        [JsonProperty("SubmissionDate")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime SubmissionDate { get; set; }
        public string DocumentUUID { get; set; } = string.Empty;
        public string DocumentLogId { get; set; } = string.Empty;
        public string DocumentStatus { get; set; } = string.Empty;
        public string PublicUrl { get; set; } = string.Empty;
        public string HashKey { get; set; } = string.Empty;

    }
}
