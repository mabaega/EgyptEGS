using EgyptEGS.ApiClient.Model;
using EgyptEGS.Models;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using EgyptEGS.Exceptions;

namespace EgyptEGS.Utilities
{
    public class TaxComponent
    {
        public string Name { get; set; }
        public decimal ComponentRate { get; set; }
    }

    public class InvoiceTransformer
    {
        // https://sdk.invoicing.eta.gov.eg/document-validation-rules/
        //
        public (string, string) Transform(RelayData relayData)
        {
            // parse string relayData.LocalIssueDate "2025-03-06T13:48:56Z" into UTC DateTime
            DateTime utcDateTime;
            
            if (!DateTime.TryParseExact(relayData.LocalIssueDate, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.AdjustToUniversal, out utcDateTime))
            {
                throw new FormatException($"Invalid date format: {relayData.LocalIssueDate}");
            }


            EgyptianInvoice egyptianInvoice = new()
            {
                // Core invoice data
                Issuer = relayData.AppConfig.Issuer,
                Receiver = relayData.Receiver,

                DocumentType = relayData.DocumentType,
                DocumentTypeVersion = relayData.DocumentTypeVersion,

                DateTimeIssued = utcDateTime,
                TaxpayerActivityCode = relayData.AppConfig.ActivityCode,

                InternalID = relayData.ManagerInvoice.Reference,
                PurchaseOrderReference = null,
                PurchaseOrderDescription = null,
                SalesOrderReference = null,
                SalesOrderDescription = null,
                ProformaInvoiceNumber = null,
                Payment = null,
                Delivery = null,
                Signatures = null,
                ServiceDeliveryDate = null,
                InvoiceLines = TransformInvoiceLines(relayData)
            };

            if (!relayData.DocumentType.Equals("I", StringComparison.CurrentCultureIgnoreCase))
            {
                egyptianInvoice.References = relayData.CNDNReferences;
            }
            
            // Calculate invoice level totals according to validation rules
            egyptianInvoice.TotalSalesAmount = egyptianInvoice.InvoiceLines.Sum(l => l.SalesTotal);
            egyptianInvoice.TotalDiscountAmount = egyptianInvoice.InvoiceLines.Sum(l => l.Discount?.Amount ?? 0);
            egyptianInvoice.NetAmount = egyptianInvoice.InvoiceLines.Sum(l => l.NetTotal);
            egyptianInvoice.TotalItemsDiscountAmount = egyptianInvoice.InvoiceLines.Sum(l => l.ItemsDiscount);

            // Set extra discount amount from the value provided
            egyptianInvoice.ExtraDiscountAmount = 0; //relayData.ExtraDiscountAmount;

            // Calculate taxable fees (T1-T12) - sum all regardless of subtypes
            decimal taxableFees = egyptianInvoice.InvoiceLines
                .SelectMany(l => l.TaxableItems)
                .Where(t => t.TaxType.CompareTo("T13") < 0)
                .Sum(t => t.Amount);

            // Calculate non-taxable fees (T13-T20)
            decimal nonTaxableFees = egyptianInvoice.InvoiceLines
                .SelectMany(l => l.TaxableItems)
                .Where(t => t.TaxType.CompareTo("T13") >= 0 && t.TaxType.CompareTo("T20") <= 0)
                .Sum(t => t.Amount);

            // Calculate tax totals grouped by tax type
            egyptianInvoice.TaxTotals = egyptianInvoice.InvoiceLines
                .SelectMany(l => l.TaxableItems)
                .GroupBy(t => new { t.TaxType, t.SubType })
                .Select(g => new TaxTotal
                {
                    TaxType = g.Key.TaxType,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToList();

            // Calculate final total amount
            egyptianInvoice.TotalAmount = egyptianInvoice.InvoiceLines.Sum(l => l.Total)
                - (egyptianInvoice.ExtraDiscountAmount ?? 0);

            string egyptianInvoiceJson = JsonConvert.SerializeObject(egyptianInvoice, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            egyptianInvoiceJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(egyptianInvoiceJson));

            if (relayData.DocumentType.Equals("I", StringComparison.CurrentCultureIgnoreCase))
            {
                egyptianInvoiceJson = egyptianInvoiceJson.Replace("\"references\":[],", string.Empty);
            }

            egyptianInvoiceJson = egyptianInvoiceJson.Replace("\"signatures\":null,", string.Empty);

            JsonNode jNode = JsonNode.Parse(egyptianInvoiceJson);
            string serializedData = JsonExtensions.SerializeJsonNode(jNode);

            return (egyptianInvoiceJson, serializedData);
        }

        private static List<InvoiceLine> TransformInvoiceLines(RelayData relayData)
        {
            if (relayData?.ManagerInvoice?.Lines == null || relayData?.ManagerInvoice?.Lines?.Count == 0)
            {
                return new List<InvoiceLine>();
            }

            ManagerInvoice mi = relayData.ManagerInvoice;
            decimal exchangeRate = mi.ExchangeRate;

            bool hasDiscount = mi.Discount;
            int discountType = mi.DiscountType;

            bool hasWHT = mi.WithholdingTax;
            int whtType = mi.WithholdingTaxType;

            // Calculate WHT percentage if WHT is enabled
            decimal whtPercentage = CalculateWHTPercentage(mi, hasDiscount, discountType, exchangeRate);

            return mi.Lines.Select(line =>
            {
                // 1. Calculate Sales Total (Quantity * Amount)
                decimal qty = line.Qty;
                decimal unitPrice = line.UnitPrice / exchangeRate;
                decimal salesTotal = qty * unitPrice;

                // 2. Calculate Discount (Discount.Rate * Sales Total)
                decimal discountRate = hasDiscount && discountType != 1 ? line.DiscountPercentage : 0;
                decimal discountAmount = hasDiscount && discountRate > 0 ?
                    (salesTotal * discountRate / 100) :
                    (discountType == 1 ? (line.DiscountAmount / exchangeRate) : 0);

                // 3. Calculate Net Total (Sales Total - Discount Amount)
                decimal netTotal = salesTotal - discountAmount;
                decimal valueDifference = 0;
                decimal itemsDiscount = 0;

                List<TaxableItem> taxableItems = new();
                decimal totalTaxableFees = 0;
                decimal totalNonTaxableFees = 0;


                // Process tax components from TaxCode
                IEnumerable<TaxComponent> components;
                if (line.TaxCode?.Type == 1 && line.TaxCode?.Components != null)
                {
                    components = line.TaxCode.Components
                        .Where(c => !string.IsNullOrEmpty(c.Name))
                        .Select(c => new TaxComponent
                        {
                            Name = c.Name,
                            ComponentRate = c.ComponentRate
                        });
                }
                else if (line.TaxCode?.Type == 0 && !string.IsNullOrEmpty(line.TaxCode?.Name))
                {
                    components = new[]
                    {
                            new TaxComponent
                            {
                                Name = line.TaxCode.Name,
                                ComponentRate = line.TaxCode.Rate
                            }
                    };
                }
                else
                {
                    components = Array.Empty<TaxComponent>();
                }

                // Process tax components from WithHoldingTax
                if (hasWHT && whtPercentage > 0)
                {
                    components = components.Concat(new[]
                    {
                            new TaxComponent
                            {
                                Name = "T4-" + relayData.WHTSubType.ToUpper(),  // Using the same format as other tax components
                                ComponentRate = -whtPercentage  // Negative since WHT reduces total
                            }
                    });
                }

                // Process tax components in correct order: T3/T6 (fixed) -> T2 -> T1 -> T4 -> T5-T12 -> T13-T20
                components = components.OrderBy(c =>
                {
                    string type = c.Name.Split('-')[0].Trim();
                    return type switch
                    {
                        "T3" or "T6" => 1, // Fixed amount taxes first
                        "T2" => 2,         // Table tax second
                        "T1" => 3,         // VAT third
                        "T4" => 4,         // WHT fourth
                        var t when t.CompareTo("T5") >= 0 && t.CompareTo("T12") <= 0 => 5,
                        _ => 6
                    };
                });

                foreach (TaxComponent component in components)
                {
                    string taxType = component.Name.Split('-')[0].Trim();
                    string subType = component.Name.Split('-').Length > 1 ? component.Name.Split('-')[1].Trim() : taxType;
                    decimal rate = Math.Abs(component.ComponentRate);
                    decimal taxAmount = 0;

                    // Calculate tax amount based on type
                    switch (taxType)
                    {
                        case "T3": // Fixed Amount Table Tax
                        case "T6": // Fixed Amount Stamping Tax
                            taxAmount = rate;
                            rate = 0; // Rate must be 0 for fixed amounts
                            break;

                        case "T2": // Percentage Table Tax
                            decimal t3Amount = taxableItems.FirstOrDefault(t => t.TaxType == "T3")?.Amount ?? 0;
                            taxAmount = (netTotal + totalTaxableFees + valueDifference + t3Amount) * (rate / 100);
                            break;

                        case "T1": // VAT
                            decimal t2Amount = taxableItems.FirstOrDefault(t => t.TaxType == "T2")?.Amount ?? 0;
                            decimal t3AmountForVat = taxableItems.FirstOrDefault(t => t.TaxType == "T3")?.Amount ?? 0;
                            taxAmount = (netTotal + totalTaxableFees + valueDifference + t2Amount + t3AmountForVat) * (rate / 100);
                            break;

                        case "T4": // WHT
                            taxAmount = (netTotal - itemsDiscount) * (rate / 100);
                            break;

                        case var t when t.CompareTo("T5") >= 0 && t.CompareTo("T12") <= 0:
                            // Taxable fees
                            taxAmount = netTotal * (rate / 100);
                            totalTaxableFees += taxAmount;
                            break;

                        case var t when t.CompareTo("T13") >= 0 && t.CompareTo("T20") <= 0:
                            // Non-taxable fees
                            taxAmount = netTotal * (rate / 100);
                            totalNonTaxableFees += taxAmount;
                            break;
                    }

                    if (!string.IsNullOrEmpty(taxType))
                    {
                        taxableItems.Add(new TaxableItem
                        {
                            TaxType = taxType,
                            SubType = subType,
                            Amount = Math.Abs(taxAmount),
                            Rate = rate,
                            IsNegatif = component.ComponentRate < 0
                        });
                    }
                }

                // Calculate total WHT amount
                decimal totalWHT = taxableItems.Where(t => t.TaxType == "T4")
                    .Sum(t => t.Amount);

                // Calculate final total according to specification
                decimal taxAmountTotal = taxableItems
                    .Where(t => t.TaxType != "T4") // Exclude WHT from total
                    .Sum(t => t.Amount * (t.IsNegatif ? -1 : 1));

                decimal total = netTotal + taxAmountTotal + totalTaxableFees + totalNonTaxableFees - itemsDiscount - totalWHT;

                return new InvoiceLine
                {
                    Description = GetFirstNonNullOrEmpty(line.LineDescription, line.Item?.Name ?? "", line.Item?.ItemName ?? ""),
                    ItemType = line.Item?.CustomFields2?.Strings?.GetValueOrDefault(ManagerCustomField.ItemTypeGuid) ?? string.Empty,
                    ItemCode = line.Item?.CustomFields2?.Strings?.GetValueOrDefault(ManagerCustomField.ItemCodeGuid) ?? string.Empty,
                    InternalCode = line.Item?.ItemCode,
                    UnitType = line.Item?.CustomFields2?.Strings?.GetValueOrDefault(ManagerCustomField.UnitTypeGuid) ??  line.Item?.UnitName ?? string.Empty,
                    Quantity = qty,
                    UnitValue = new UnitValue
                    {
                        CurrencySold = relayData.CurrencyCode,
                        AmountEGP = unitPrice,
                        AmountSold = relayData.CurrencyCode == "EGP" ? null : line.UnitPrice,
                        CurrencyExchangeRate = relayData.CurrencyCode == "EGP" ? null : exchangeRate
                    },
                    SalesTotal = salesTotal,
                    Discount = new Discount
                    {
                        Rate = hasDiscount ? (discountType == 1 ? null : discountRate) : 0,
                        Amount = discountAmount
                    },
                    NetTotal = netTotal,
                    ValueDifference = valueDifference,
                    TaxableItems = taxableItems,
                    TotalTaxableFees = totalTaxableFees,
                    ItemsDiscount = itemsDiscount,
                    Total = total
                };
            }).ToList();
        }

        private static decimal CalculateWHTPercentage(ManagerInvoice mi, bool hasDiscount, int discountType, decimal exchangeRate)
        {
            if (!mi.WithholdingTax)
            {
                return 0;
            }

            if (mi.WithholdingTaxType == 0) // Use Percentage
            {
                return mi.WithholdingTaxPercentage;
            }

            // Calculate rate from amount
            decimal totalNetAmount = mi.Lines.Sum(l =>
            {
                // Calculate line's sales total
                decimal salesTotal = l.Qty * (l.UnitPrice / exchangeRate);

                // Apply line-specific discount
                if (hasDiscount)
                {
                    if (discountType == 0) // Each line has its own percentage
                    {
                        salesTotal -= (salesTotal * l.DiscountPercentage / 100);
                    }
                    else // Each line has its own amount
                    {
                        salesTotal -= (l.DiscountAmount / exchangeRate);
                    }
                }

                return salesTotal;
            });

            return totalNetAmount > 0 ? (mi.WithholdingTaxAmount / totalNetAmount) * 100 : 0;
        }

        private static string GetFirstNonNullOrEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(value?.Trim()))
                {
                    return value;
                }
            }
            return string.Empty;
        }

    }

}