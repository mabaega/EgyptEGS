using EgyptEGS.ApiClient.Extensions;
using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class EgyptianInvoice
    {
        // Core
        [JsonProperty("issuer")]
        public Party Issuer { get; set; }

        [JsonProperty("receiver")]
        public Party Receiver { get; set; }

        [JsonProperty("documentType")]
        public string DocumentType { get; set; } = "I";  // Default: "i" untuk invoice, "d" untuk debit note, "c" untuk credit note

        [JsonProperty("documentTypeVersion")]
        public string DocumentTypeVersion { get; set; } = "1.0"; //static

        [JsonProperty("dateTimeIssued")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime DateTimeIssued { get; set; } = DateTime.UtcNow;

        [JsonProperty("taxpayerActivityCode")]
        public string TaxpayerActivityCode { get; set; }

        [JsonProperty("internalID")]
        public string InternalID { get; set; } // Nomor faktur internal dari reference

        [JsonProperty("purchaseOrderReference")]
        public string? PurchaseOrderReference { get; set; }

        [JsonProperty("purchaseOrderDescription")]
        public string? PurchaseOrderDescription { get; set; }

        [JsonProperty("salesOrderReference")]
        public string? SalesOrderReference { get; set; }

        [JsonProperty("salesOrderDescription")]
        public string? SalesOrderDescription { get; set; }

        [JsonProperty("proformaInvoiceNumber")]
        public string? ProformaInvoiceNumber { get; set; } // isi dengan formKey

        [JsonProperty("payment")]
        public Payment? Payment { get; set; } // skip

        [JsonProperty("delivery")]
        public Delivery? Delivery { get; set; } // skip

        // Properti tambahan untuk debit note dan credit note
        [JsonProperty("references")]
        public List<string> References { get; set; } = new List<string>();

        [JsonProperty("invoiceLines")]
        public List<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();

        [JsonProperty("totalSalesAmount")]
        public decimal TotalSalesAmount { get; set; }

        [JsonProperty("totalDiscountAmount")]
        public decimal TotalDiscountAmount { get; set; }

        [JsonProperty("netAmount")]
        public decimal NetAmount { get; set; }

        [JsonProperty("taxTotals")]
        public List<TaxTotal> TaxTotals { get; set; } = new List<TaxTotal>();

        [JsonProperty("extraDiscountAmount")]
        public decimal? ExtraDiscountAmount { get; set; } // skip

        [JsonProperty("totalItemsDiscountAmount")]
        public decimal TotalItemsDiscountAmount { get; set; }

        [JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonProperty("signatures")]
        public List<Signature>? Signatures { get; set; } // = new List<Signature>();

        // Hilangkan serviceDeliveryDate karena tidak ada di debit note dan credit note
        [JsonProperty("serviceDeliveryDate")]
        public DateTime? ServiceDeliveryDate { get; set; }
    }

    public class InvoiceLine
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("itemType")]
        public string ItemType { get; set; }  // GS1/EGS

        [JsonProperty("itemCode")]
        public string ItemCode { get; set; }

        [JsonProperty("unitType")]
        public string UnitType { get; set; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("unitValue")]
        public UnitValue UnitValue { get; set; }

        [JsonProperty("salesTotal")]
        public decimal SalesTotal { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("valueDifference")]
        public decimal? ValueDifference { get; set; }

        [JsonProperty("totalTaxableFees")]
        public decimal TotalTaxableFees { get; set; }

        [JsonProperty("netTotal")]
        public decimal NetTotal { get; set; }

        [JsonProperty("itemsDiscount")]
        public decimal ItemsDiscount { get; set; }

        [JsonProperty("discount")]
        public Discount? Discount { get; set; }

        [JsonProperty("taxableItems")]
        public List<TaxableItem> TaxableItems { get; set; } = new List<TaxableItem>();

        [JsonProperty("internalCode")]
        public string? InternalCode { get; set; }
    }

    public class UnitValue
    {
        [JsonProperty("currencySold")]
        public string CurrencySold { get; set; }  // ISO 4217 (contoh: "EGP")

        [JsonProperty("amountEGP")]
        public decimal AmountEGP { get; set; }

        [JsonProperty("amountSold")]
        public decimal? AmountSold { get; set; }

        [JsonProperty("currencyExchangeRate")]
        public decimal? CurrencyExchangeRate { get; set; }
    }

    public class Discount
    {
        [JsonProperty("rate")]
        public decimal? Rate { get; set; }  // 0-100

        [JsonProperty("amount")]
        public decimal? Amount { get; set; }
    }

    public class TaxableItem
    {
        [JsonProperty("taxType")]
        public string TaxType { get; set; }  // Kode pajak (contoh: "VAT")

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("subType")]
        public string? SubType { get; set; }

        [JsonProperty("rate")]
        public decimal Rate { get; set; }  // 0-999

        [JsonIgnore]
        [JsonProperty("isNegatif")]
        public bool IsNegatif { get; set; }
    }

    public class TaxTotal
    {
        [JsonProperty("taxType")]
        public string TaxType { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }

    public class Signature
    {
        [JsonProperty("signatureType")]
        public string SignatureType { get; set; }  // "I" (Issuer) atau "S" (Service Provider)

        [JsonProperty("value")]
        public string Value { get; set; }  // Base64 encoded CADES-BES
    }
}
