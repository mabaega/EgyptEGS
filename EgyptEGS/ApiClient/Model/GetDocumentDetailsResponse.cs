using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class GetDocumentDetailsResponse
    {
        [JsonProperty("submissionUUID")]
        public string SubmissionUUID { get; set; }

        [JsonProperty("dateTimeRecevied")]
        public DateTime DateTimeReceived { get; set; }

        [JsonProperty("validationResults")]
        public ValidationResults ValidationResults { get; set; }

        [JsonProperty("transformationStatus")]
        public string TransformationStatus { get; set; }

        [JsonProperty("statusId")]
        public int StatusId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

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

        [JsonProperty("submissionChannel")]
        public int SubmissionChannel { get; set; }

        [JsonProperty("freezeStatus")]
        public FreezeStatus FreezeStatus { get; set; }

        [JsonProperty("serviceDeliveryDate")]
        public DateTime? ServiceDeliveryDate { get; set; }

        [JsonProperty("customsClearanceDate")]
        public DateTime? CustomsClearanceDate { get; set; }

        [JsonProperty("customsDeclarationNumber")]
        public string CustomsDeclarationNumber { get; set; }

        [JsonProperty("ePaymentNumber")]
        public string EPaymentNumber { get; set; }

        [JsonProperty("additionalMetadata")]
        public List<Metadata> AdditionalMetadata { get; set; }

        [JsonProperty("alertDetails")]
        public object AlertDetails { get; set; }

        [JsonProperty("uuid")]
        public string UUID { get; set; }

        [JsonProperty("publicUrl")]
        public string PublicUrl { get; set; }

        [JsonProperty("purchaseOrderDescription")]
        public string PurchaseOrderDescription { get; set; }

        [JsonProperty("totalItemsDiscountAmount")]
        public decimal TotalItemsDiscountAmount { get; set; }

        [JsonProperty("delivery")]
        public Delivery Delivery { get; set; }

        [JsonProperty("payment")]
        public Payment Payment { get; set; }

        [JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonProperty("taxTotals")]
        public List<TaxTotal> TaxTotals { get; set; }

        [JsonProperty("netAmount")]
        public decimal NetAmount { get; set; }

        [JsonProperty("totalDiscount")]
        public decimal TotalDiscount { get; set; }

        [JsonProperty("totalSales")]
        public decimal TotalSales { get; set; }

        [JsonProperty("invoiceLines")]
        public List<InvoiceLine> InvoiceLines { get; set; }

        [JsonProperty("documentLinesTotalCount")]
        public int DocumentLinesTotalCount { get; set; }

        [JsonProperty("references")]
        public object References { get; set; }

        [JsonProperty("salesOrderDescription")]
        public string SalesOrderDescription { get; set; }

        [JsonProperty("salesOrderReference")]
        public string SalesOrderReference { get; set; }

        [JsonProperty("proformaInvoiceNumber")]
        public string ProformaInvoiceNumber { get; set; }

        [JsonProperty("signatures")]
        public List<object> Signatures { get; set; }

        [JsonProperty("purchaseOrderReference")]
        public string PurchaseOrderReference { get; set; }

        [JsonProperty("internalID")]
        public string InternalID { get; set; }

        [JsonProperty("taxpayerActivityCode")]
        public string TaxpayerActivityCode { get; set; }

        [JsonProperty("dateTimeIssued")]
        public DateTime DateTimeIssued { get; set; }

        [JsonProperty("documentTypeVersion")]
        public string DocumentTypeVersion { get; set; }

        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        [JsonProperty("documentTypeNamePrimaryLang")]
        public string DocumentTypeNamePrimaryLang { get; set; }

        [JsonProperty("documentTypeNameSecondaryLang")]
        public string DocumentTypeNameSecondaryLang { get; set; }

        [JsonProperty("receiver")]
        public Party Receiver { get; set; }

        [JsonProperty("issuer")]
        public Party Issuer { get; set; }

        [JsonProperty("extraDiscountAmount")]
        public decimal ExtraDiscountAmount { get; set; }

        [JsonProperty("maxPercision")]
        public int MaxPrecision { get; set; }

        [JsonProperty("currenciesSold")]
        public string CurrenciesSold { get; set; }

        [JsonProperty("currencySegments")]
        public List<CurrencySegment> CurrencySegments { get; set; }

        [JsonProperty("lateSubmissionRequestNumber")]
        public string LateSubmissionRequestNumber { get; set; }
    }

    public class CurrencySegment
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("currencyExchangeRate")]
        public decimal? CurrencyExchangeRate { get; set; }

        [JsonProperty("totalItemsDiscountAmount")]
        public decimal TotalItemsDiscountAmount { get; set; }

        [JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonProperty("taxTotals")]
        public List<TaxTotal> TaxTotals { get; set; }

        [JsonProperty("netAmount")]
        public decimal NetAmount { get; set; }

        [JsonProperty("totalDiscount")]
        public decimal TotalDiscount { get; set; }

        [JsonProperty("totalSales")]
        public decimal TotalSales { get; set; }

        [JsonProperty("extraDiscountAmount")]
        public decimal ExtraDiscountAmount { get; set; }

        [JsonProperty("totalTaxableFees")]
        public decimal TotalTaxableFees { get; set; }
    }
}