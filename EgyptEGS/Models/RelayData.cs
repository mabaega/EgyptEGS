using EgyptEGS.ApiClient.Model;
using EgyptEGS.Exceptions;
using EgyptEGS.Utilities;
using Newtonsoft.Json;

namespace EgyptEGS.Models
{
    public class RelayData
    {
        public string Referrer { get; private set; } = string.Empty;
        public string FormKey { get; private set; } = string.Empty;
        public string Api { get; private set; } = string.Empty;
        public string Token { get; private set; } = string.Empty;
        public string InvoiceJson { get; set; } = string.Empty;
        public string BusinessDetailJson { get; set; } = string.Empty;
        public ApplicationConfig AppConfig { get; set; }
        public long EGSVersion { get; private set; }
        public ManagerInvoice ManagerInvoice { get; private set; }

        public string DocumentType { get; private set; } = "I";
        public string LocalIssueDate { get; private set; }
        public string DocumentTypeVersion { get; private set; } = "0.9";
        public string WHTSubType { get; private set; } = "W002";


        public string CurrencyCode { get; private set; } = string.Empty;
        public decimal ExchangeRate { get; private set; } = 1;
        public decimal InvoiceTotal { get; private set; } = 0;

        public Party Receiver { get; private set; }

        public string EgyptianInvoiceJson { get; set; }
        public string SerializedInvoice { get; set; }
        public InvoiceSummary InvoiceSummary { get; set; } = new InvoiceSummary();
        public List<string> CNDNReferences { get; internal set; }
        public RelayData() { }

        public RelayData(Dictionary<string, string> formData)
        {
            if (formData == null)
            {
                throw new ArgumentNullException(nameof(formData));
            }

            try
            {
                List<string> validationErrors = new();

                // Continue with existing initialization
                Referrer = Utils.GetValue(formData, "Referrer");
                FormKey = Utils.GetValue(formData, "Key");
                Api = Utils.GetValue(formData, "Api");
                Token = Utils.GetValue(formData, "Token");

                string invoiceView = Utils.GetValue(formData, "View");
                InvoiceTotal = RelayDataHelper.ParseTotalValue(invoiceView);

                string Data = Utils.GetValue(formData, "Data");

                BusinessDetailJson = RelayDataHelper.GetValueJson(Data, "BusinessDetails");
                (AppConfig, EGSVersion) = Utils.InitializeBusinessData(BusinessDetailJson);

                InvoiceJson = RelayDataHelper.FindStringValueInJson(Data, FormKey) ?? "";

                string mergedJson = RelayDataHelper.GetJsonDataByGuid(Data, FormKey);

                ManagerInvoice = JsonConvert.DeserializeObject<ManagerInvoice>(mergedJson);

                if (string.IsNullOrEmpty(ManagerInvoice?.Reference))
                {
                    validationErrors.Add("Invoice Reference is required");
                }

                if (ManagerInvoice?.Lines?.Count == 0)
                {
                    validationErrors.Add("Invoice must have at least one item");
                }

                if (InvoiceTotal == 0)
                {
                    validationErrors.Add("Invoice total amount cannot be zero");
                }

                if (validationErrors.Count > 0)
                {
                    throw new ValidationException("Required fields are missing", validationErrors);
                }

                ExchangeRate = ManagerInvoice.ExchangeRate;



                InitializeInvoiceData(mergedJson);

                DetermineInvoiceType(mergedJson);

            }
            catch (ValidationException)
            {
                throw; // Re-throw ValidationException directly
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize RelayData", ex);
            }
        }

        private void InitializeInvoiceData(string jsonInvoice)
        {
            if (string.IsNullOrEmpty(jsonInvoice))
            {
                throw new ArgumentException("Invoice data is required");
            }

            try
            {
                CurrencyCode = RelayDataHelper.FindStringValueInJson(jsonInvoice, "Code", "Currency") ?? "EGP";

                LocalIssueDate = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.DocumentIssuedDateGuid);

                if(string.IsNullOrEmpty(LocalIssueDate))
                {
                    ManagerInvoice.IssueDate.ToString("yyyy-MM-dd HH:mm:ss");
                }

                WHTSubType = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.WHTSubTypeGuid) ?? "W002";

                InvoiceSummary.DocumentIssueDate = LocalIssueDate;

                InvoiceSummary.SubmissionId = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.SubmissionIdGuid) ?? "";

                string submissionDate = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.DocumentIssuedDateGuid);

                if (DateTime.TryParseExact(submissionDate, "yyyy-MM-ddTHH:mm:ssZ", null, System.Globalization.DateTimeStyles.None, out DateTime parsedSubmissionDate))
                {
                    InvoiceSummary.SubmissionDate = parsedSubmissionDate;
                }

                InvoiceSummary.DocumentUUID = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.DocumentUUIDGuid) ?? "";
                InvoiceSummary.DocumentLogId = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.DocumentLongIdGuid) ?? "";
                InvoiceSummary.DocumentStatus = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.DoucmentStatusGuid) ?? "";
                InvoiceSummary.PublicUrl = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.PublicUrlGuid) ?? "";

                List<string> validationErrors = new();

                string receiverType = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.ReceiverTypeGuid, "InvoiceParty");
                string receiverId = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.ReceiverIdGuid, "InvoiceParty");
                string receiverName = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.ReceiverNameGuid, "InvoiceParty");

                // Validate receiver fields
                if (string.IsNullOrEmpty(receiverType))
                {
                    validationErrors.Add("Receiver Type is required");
                }

                if (string.IsNullOrEmpty(receiverId))
                {
                    validationErrors.Add("Receiver ID is required");
                }

                if (string.IsNullOrEmpty(receiverName))
                {
                    validationErrors.Add("Receiver Name is required");
                }

                // Validate receiver address
                string regionCity = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.RegionCityGuid, "InvoiceParty");
                string street = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.StreetGuid, "InvoiceParty");
                string buildingNumber = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.BuildingNumberGuid, "InvoiceParty");

                if (string.IsNullOrEmpty(regionCity))
                {
                    validationErrors.Add("Receiver Region/City is required");
                }

                if (string.IsNullOrEmpty(street))
                {
                    validationErrors.Add("Receiver Street is required");
                }

                if (string.IsNullOrEmpty(buildingNumber))
                {
                    validationErrors.Add("Receiver Building Number is required");
                }

                if (validationErrors.Count > 0)
                {
                    throw new ValidationException("Invalid receiver information", validationErrors);
                }
                Receiver = new Party
                {
                    Type = ParsePartyType(receiverType),
                    Id = receiverId,
                    Name = receiverName,
                    Address = new Address
                    {
                        BranchId = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.BranchIdGuid, "InvoiceParty"),
                        Country = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.CountryCodeGuid, "InvoiceParty") ?? "EG",
                        Governate = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.GovernorateGuid, "InvoiceParty") ?? "Egypt",
                        RegionCity = regionCity ?? "",
                        Street = street ?? "",
                        BuildingNumber = buildingNumber ?? "",
                        PostalCode = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.PostalCodeGuid, "InvoiceParty"),
                        Floor = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.FloorGuid, "InvoiceParty"),
                        Room = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.RoomGuid, "InvoiceParty"),
                        Landmark = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.LandmarkGuid, "InvoiceParty"),
                        AdditionalInformation = RelayDataHelper.GetStringCustomField2Value(jsonInvoice, ManagerCustomField.AdditionalInformationGuid, "InvoiceParty")
                    }
                };
            }
            catch (ValidationException)
            {
                throw;  // Re-throw ValidationException directly
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize invoice data", ex);
            }
        }

        private PartyType ParsePartyType(string partyTypeString)
        {
            try
            {
                if (string.IsNullOrEmpty(partyTypeString))
                {
                    return PartyType.B;
                }

                string typeCode = partyTypeString.Split('—')[0].Trim();
                return (PartyType)Enum.Parse(typeof(PartyType), typeCode);
            }
            catch //(Exception ex)
            {
                //throw new InvalidOperationException("Failed to get party type", ex);
                return PartyType.B;
            }
        }

        private void DetermineInvoiceType(string jsonInvoice)
        {
            if (Referrer.Contains("credit-note-view"))
            {
                DocumentType = "C";
                string salesUnitPrice = RelayDataHelper.FindStringValueInJson(jsonInvoice, "UnitPrice");
                if (decimal.TryParse(salesUnitPrice, out decimal salesUnitPriceDecimal) && salesUnitPriceDecimal < 0)
                {
                    DocumentType = "D";
                }

                CNDNReferences = new List<string> { ManagerInvoice.RefInvoice.Reference, ManagerInvoice.RefInvoice.IssueDate.ToString("yyyy-MM-dd"), InvoiceSummary.DocumentUUID};
            }
        }
        public ApprovedInvoiceViewModel GetApprovedInvoiceViewModel()
        {
            ApprovedInvoiceViewModel viewModel = new()
            {
                SubmitDocumentRequestJson = EgyptianInvoiceJson,
                Referrer = Referrer,
                InvoiceSummary = InvoiceSummary,
            };
            return viewModel;
        }
        public RelayDataViewModel GetRelayDataViewModel()
        {
            RelayDataViewModel viewModel = new();
            EgyptianInvoice egyptianInvoice = JsonConvert.DeserializeObject<EgyptianInvoice>(EgyptianInvoiceJson);

            viewModel.Referrer = Referrer;
            viewModel.FormKey = FormKey;
            viewModel.Api = Api;
            viewModel.Token = Token;
            viewModel.IntegrationType = AppConfig.IntegrationType;
            viewModel.ClientID = AppConfig.ClientId;
            viewModel.ClientSecret = AppConfig.ClientSecret;
            viewModel.InvoiceJson = InvoiceJson;
            viewModel.EgyptianInvoiceJson = EgyptianInvoiceJson;
            viewModel.SerializedInvoice = SerializedInvoice;

            viewModel.DocumentType = DocumentType;
            viewModel.DocumentTypeVersion = DocumentTypeVersion;

            //Client local time
            viewModel.IssueDate = LocalIssueDate;

            viewModel.TotalTaxAmount = egyptianInvoice.InvoiceLines
                .SelectMany(l => l.TaxableItems)
                .Sum(t => t.Amount * (t.IsNegatif ? -1 : 1));

            viewModel.CurrencyCode = CurrencyCode;

            viewModel.InvoiceSummaryJson = JsonConvert.SerializeObject(InvoiceSummary);

            viewModel.SignServiceUrl = AppConfig.SignServiceUrl;

            return viewModel;
        }
    }
}