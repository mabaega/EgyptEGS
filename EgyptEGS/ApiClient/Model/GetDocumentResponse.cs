using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class GetDocumentResponse
    {
        [JsonProperty("requestStatus")]
        public string RequestStatus { get; set; }

        [JsonProperty("document")]
        public string Document { get; set; }

        [JsonProperty("transformationStatus")]
        public string TransformationStatus { get; set; }

        [JsonProperty("validationResults")]
        public ValidationResults ValidationResults { get; set; }

        [JsonProperty("maxPercision")]
        public int MaxPrecision { get; set; }

        [JsonProperty("invoiceLineItemCodes")]
        public List<InvoiceLineItemCode> InvoiceLineItemCodes { get; set; }

        [JsonProperty("documentLinesTotalCount")]
        public int DocumentLinesTotalCount { get; set; }

        [JsonProperty("additionalMetadata")]
        public List<Metadata> AdditionalMetadata { get; set; }

        [JsonProperty("lateSubmissionRequestNumber")]
        public string LateSubmissionRequestNumber { get; set; }

        [JsonProperty("serviceDeliveryDate")]
        public DateTime? ServiceDeliveryDate { get; set; }

        [JsonProperty("uuid")]
        public string UUID { get; set; }

        [JsonProperty("submissionUUID")]
        public string SubmissionUUID { get; set; }

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

        [JsonProperty("dateTimeIssued")]
        public DateTime DateTimeIssued { get; set; }

        [JsonProperty("dateTimeReceived")]
        public DateTime DateTimeReceived { get; set; }

        [JsonProperty("totalSales")]
        public decimal TotalSales { get; set; }

        [JsonProperty("totalDiscount")]
        public decimal TotalDiscount { get; set; }

        [JsonProperty("netAmount")]
        public decimal NetAmount { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
    public class InvoiceLineItemCode
    {
        [JsonProperty("codeTypeId")]
        public int CodeTypeId { get; set; }

        [JsonProperty("codeTypeNamePrimaryLang")]
        public string CodeTypeNamePrimaryLang { get; set; }

        [JsonProperty("codeTypeNameSecondaryLang")]
        public string CodeTypeNameSecondaryLang { get; set; }

        [JsonProperty("itemCode")]
        public string ItemCode { get; set; }

        [JsonProperty("codeNamePrimaryLang")]
        public string CodeNamePrimaryLang { get; set; }

        [JsonProperty("codeNameSecondaryLang")]
        public string CodeNameSecondaryLang { get; set; }
    }
}