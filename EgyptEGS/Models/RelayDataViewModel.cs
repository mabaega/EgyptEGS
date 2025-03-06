using EgyptEGS.ApiClient.Model;

namespace EgyptEGS.Models
{
    public class RelayDataViewModel
    {
        public string Referrer { get; set; } = string.Empty;
        public string FormKey { get; set; } = string.Empty;
        public string Api { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public IntegrationType IntegrationType { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string DocumentType { get; set; }
        public string DocumentTypeVersion { get; set; }
        public List<string> DocumentReference { get; set; } = new List<string>();
        public DateTime ServiceDeliveryDate { get; set; }
        public string IssueDate { get; set; }
        public string CurrencyCode { get; set; } = "EGP";
        public decimal TotalTaxAmount { get; internal set; }
        public string InvoiceJson { get; set; }
        public string EgyptianInvoiceJson { get; set; }
        public string SerializedInvoice { get; set; }
        public string InvoiceSignature { get; set; }
        public string ServerResponseJson { get; set; } = string.Empty;
        public string InvoiceSummaryJson { get; set; }
        public string SignServiceUrl { get; set; }
    }
}