using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class GetSubmissionResponse
    {
        [JsonProperty("submissionId")]
        public string SubmissionId { get; set; }

        [JsonProperty("documentCount")]
        public int DocumentCount { get; set; }

        [JsonProperty("dateTimeReceived")]
        public DateTime DateTimeReceived { get; set; }

        [JsonProperty("overallStatus")]
        public string OverallStatus { get; set; }

        [JsonProperty("documentSummary")]
        public DocumentSummary[] DocumentSummary { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }

    public class DocumentSummary
    {
        [JsonProperty("publicUrl")]
        public string PublicUrl { get; set; }

        [JsonProperty("uuid")]
        public string UUID { get; set; }

        [JsonProperty("longId")]
        public string LongId { get; set; }

        [JsonProperty("internalId")]
        public string InternalId { get; set; }

        [JsonProperty("typeName")]
        public string TypeName { get; set; }

        [JsonProperty("typeVersionName")]
        public string TypeVersionName { get; set; }

        [JsonProperty("issuerId")]
        public string IssuerId { get; set; }

        [JsonProperty("issuerName")]
        public string IssuerName { get; set; }

        [JsonProperty("receiverId")]
        public string ReceiverId { get; set; }

        [JsonProperty("receiverName")]
        public string ReceiverName { get; set; }

        [JsonProperty("receiverType")]
        public int ReceiverType { get; set; }

        [JsonProperty("issuerType")]
        public int IssuerType { get; set; }

        [JsonProperty("dateTimeIssued")]
        public DateTime DateTimeIssued { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("submissionUUID")]
        public string SubmissionUUID { get; set; }

        [JsonProperty("dateTimeReceived")]
        public DateTime DateTimeReceived { get; set; }

        [JsonProperty("totalSales")]
        public decimal TotalSales { get; set; }

        [JsonProperty("totalDiscount")]
        public decimal TotalDiscount { get; set; }

        [JsonProperty("netAmount")]
        public decimal NetAmount { get; set; }

        [JsonProperty("documentStatusReason")]
        public string DocumentStatusReason { get; set; }

        [JsonProperty("cancelRequestDate")]
        public DateTime? CancelRequestDate { get; set; }

        [JsonProperty("rejectRequestDate")]
        public DateTime? RejectRequestDate { get; set; }

        [JsonProperty("cancelRequestDelayedDate")]
        public DateTime? CancelRequestDelayedDate { get; set; }

        [JsonProperty("rejectRequestDelayedDate")]
        public DateTime? RejectRequestDelayedDate { get; set; }

        [JsonProperty("declineCancelRequestDate")]
        public DateTime? DeclineCancelRequestDate { get; set; }

        [JsonProperty("declineRejectRequestDate")]
        public DateTime? DeclineRejectRequestDate { get; set; }

        [JsonProperty("canbeCancelledUntil")]
        public DateTime CanbeCancelledUntil { get; set; }

        [JsonProperty("canbeRejectedUntil")]
        public DateTime CanbeRejectedUntil { get; set; }

        [JsonProperty("freezeStatus")]
        public FreezeStatus FreezeStatus { get; set; }

        [JsonProperty("lateSubmissionRequestNumber")]
        public string LateSubmissionRequestNumber { get; set; }
    }

    public class FreezeStatus
    {
        [JsonProperty("frozen")]
        public bool Frozen { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("actionDate")]
        public DateTime? ActionDate { get; set; }

        [JsonProperty("auCode")]
        public string AuCode { get; set; }

        [JsonProperty("auName")]
        public string AuName { get; set; }
    }
}