using EgyptEGS.ApiClient;
using EgyptEGS.ApiClient.Exceptions;
using EgyptEGS.ApiClient.Model;
using EgyptEGS.Exceptions;  // Add this line
using EgyptEGS.Models;
using EgyptEGS.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace EgyptEGS.Controllers
{
    public class RelayController : Controller
    {

        private readonly ILogger<RelayController> _logger;

        public RelayController(ILogger<RelayController> logger)
        {
            _logger = logger;
        }

        [HttpGet("relay")]
        public IActionResult Index()
        {
            return View("Error", new ErrorViewModel
            {
                ErrorMessage = "Direct access to this page is not allowed. Please submit your request through the proper form.",
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [HttpPost("relay")]
        public async Task<IActionResult> ReceiveData([FromForm] Dictionary<string, string> formData, CancellationToken cancellationToken)
        {
            if (formData == null || formData.Count == 0)
            {
                return View("Error", new ErrorViewModel
                {
                    ErrorMessage = "No data was received. Please try submitting the form again.",
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }

            try
            {
                string Data = Utils.GetValue(formData, "Data");
                string BusinessDetailJson = RelayDataHelper.GetValueJson(Data, "BusinessDetails");
                (ApplicationConfig AppConfig, long egsversion) = Utils.InitializeBusinessData(BusinessDetailJson);

                if (string.IsNullOrEmpty(AppConfig?.ClientId))
                {
                    SetupViewModel setupViewModel = new()
                    {
                        Referrer = Utils.GetValue(formData, "Referrer"),
                        Api = Utils.GetValue(formData, "Api"),
                        Token = Utils.GetValue(formData, "Token"),
                        BusinessDetailJson = BusinessDetailJson,
                        EGSVersion = egsversion,
                        AppConfig = new ApplicationConfig()
                    };

                    TempData["SetupViewModel"] = JsonConvert.SerializeObject(setupViewModel);
                    return RedirectToAction("Register", "Setup");
                }

                RelayData relayData = new(formData);

                await Task.Run(() =>
                    _logger.LogInformation("API: {Api} - IP: {IpAddress} - Integration: {IntegrationType}",
                        relayData.Api,
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        relayData.AppConfig.IntegrationType),
                    cancellationToken);

                InvoiceTransformer invoiceTransformer = new();
                (string EgyptianInvoiceJson, string SerializedInvoice) = await Task.Run(() =>
                    invoiceTransformer.Transform(relayData),
                    cancellationToken);

                relayData.EgyptianInvoiceJson = EgyptianInvoiceJson;
                relayData.SerializedInvoice = SerializedInvoice;

                RelayDataViewModel viewModel = relayData.GetRelayDataViewModel();

                return View("Index", viewModel);

            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Request was cancelled");
                return View("Error", new ErrorViewModel
                {
                    Referrer = formData.GetValueOrDefault("referrer", ""),
                    ErrorMessage = "The request was cancelled. Please try again.",
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
            catch (JsonSerializationException ex)
            {
                _logger.LogError(ex, "Error processing form data");
                return View("Error", new ErrorViewModel
                {
                    Referrer = formData.GetValueOrDefault("referrer", ""),
                    ErrorMessage = "There was an error processing the invoice data. Please check if all information is filled correctly.",
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Validation errors: {@Errors}", ex.ValidationErrors);
                return View("Error", new ErrorViewModel
                {
                    Referrer = formData.GetValueOrDefault("Referrer", ""),
                    ValidationErrors = ex.ValidationErrors,  // Make sure ValidationErrors is passed
                    ErrorMessage = "Please check the following fields:"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing relay data");
                List<string> validationErrors = new();

                // Check if it's ArgumentException with validation message
                if (ex is ArgumentException && ex.Message.Contains("Required fields are missing"))
                {
                    validationErrors = ex.Message
                        .Replace("Required fields are missing: ", "")
                        .Split(", ")
                        .ToList();
                }

                return View("Error", new ErrorViewModel
                {
                    Referrer = formData.GetValueOrDefault("Referrer", ""),
                    ValidationErrors = validationErrors,
                    ErrorMessage = "Please check the following fields:",
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
                });
            }
        }

        [HttpPost("AjaxSubmitInvoice")]
        public async Task<IActionResult> AjaxSubmitInvoice([FromForm] RelayDataViewModel model)
        {
            try
            {
                _logger.LogInformation("Received AjaxSubmitInvoice request with model: {@Model}", model);

                if (model.EgyptianInvoiceJson == null)
                {
                    var modelError = new
                    {
                        ApiResponse = new Error
                        {
                            Code = "ValidationError",
                            Message = "Invalid EgyptianInvoice data"
                        }
                    };
                    return Ok(JsonConvert.SerializeObject(modelError, Formatting.Indented));
                }

                using HttpClient client = new();
                using ApiHelper apiHelper = new(client, model.ClientID, model.ClientSecret, model.IntegrationType);

                string signedInvoiceJson = model.EgyptianInvoiceJson;

                if (model.DocumentTypeVersion == "1.0")
                {
                    // 1. Generate signature
                    string signatureValue = model.InvoiceSignature; //GenerateSignature(model.EgyptianInvoiceJson);

                    // 2. Sisipkan signature

                    signedInvoiceJson = model.EgyptianInvoiceJson.TrimEnd('}') +
                        $",\"signatures\":[{{\"signatureType\":\"I\",\"value\":\"{signatureValue}\"}}]}}";
                }

                // 3. Bungkus ke dalam "documents"
                string wrappedJson = $"{{\"documents\":[{signedInvoiceJson}]}}";

                wrappedJson = wrappedJson.CleanseJson(true);

                _logger.LogInformation("Signed JSON: {Json}", wrappedJson);

                // 4. Kirim ke API
                DocumentSubmitResponse submitResponse = await apiHelper.SubmitDocument(wrappedJson);

                if (string.IsNullOrEmpty(submitResponse.SubmissionId))
                {
                    var submitError = new
                    {
                        ApiResponse = submitResponse,
                    };

                    return Ok(Utils.SerializeObject(submitError));
                }

                InvoiceSummary updatedSummary = new()
                {
                    DocumentIssueDate = model.IssueDate,
                    SubmissionId = submitResponse.SubmissionId,
                    SubmissionDate = DateTime.UtcNow,
                    DocumentUUID = submitResponse.AcceptedDocuments[0].UUID,
                    DocumentLogId = submitResponse.AcceptedDocuments[0].LongId,
                    DocumentStatus = "Submitted",
                    HashKey = submitResponse.AcceptedDocuments[0].HashKey,
                    PublicUrl = ""
                };

                model.InvoiceJson = UpdateInvoiceWithResponse(model, updatedSummary);

                string apiUrl = Utils.ConstructInvoiceApiUrl(model.Referrer, model.FormKey);

                var combinedApiObject = new
                {
                    ApiResponse = submitResponse,
                    InvoiceSummary = updatedSummary,
                    PublicURL = updatedSummary?.PublicUrl ?? "",
                    ApiInvoice = new
                    {
                        ApiUrl = apiUrl,
                        SecretKey = model.Token,
                        Payload = model.InvoiceJson
                    }
                };

                return Ok(Utils.SerializeObject(combinedApiObject));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error during Invoice submission");
                var errorObject = new
                {
                    ApiResponse = new Error
                    {
                        Code = ex.StatusCode.ToString(),
                        Message = ex.Error?.Message ?? "API Error occurred"
                    }
                };
                return Ok(JsonConvert.SerializeObject(errorObject, Formatting.Indented));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Invoice submission");
                var errorObject = new
                {
                    ApiResponse = new Error
                    {
                        Code = "SystemError",
                        Message = ex.Message
                    }
                };
                return Ok(JsonConvert.SerializeObject(errorObject, Formatting.Indented));
            }
        }


        private string UpdateInvoiceWithResponse(RelayDataViewModel model, InvoiceSummary updateSummary)
        {
            string invoiceJson = model.InvoiceJson;

            invoiceJson = RelayDataHelper.UpdateOrCreateField(invoiceJson, model.DocumentType.ToLower() == "i" ? "IssueDate" : "Date", updateSummary.DocumentIssueDate.ToString("yyyy-MM-dd"));

            invoiceJson = RelayDataHelper.ModifyStringCustomFields2(invoiceJson, ManagerCustomField.SubmissionIdGuid, updateSummary.SubmissionId);
            invoiceJson = RelayDataHelper.ModifyStringCustomFields2(invoiceJson, ManagerCustomField.SubmissionDateGuid, updateSummary.SubmissionDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            invoiceJson = RelayDataHelper.ModifyStringCustomFields2(invoiceJson, ManagerCustomField.DocumentUUIDGuid, updateSummary.DocumentUUID);
            invoiceJson = RelayDataHelper.ModifyStringCustomFields2(invoiceJson, ManagerCustomField.DocumentIssuedDateGuid, updateSummary.DocumentIssueDate.ToString("yyyy-MM-dd HH:mm:ss"));
            invoiceJson = RelayDataHelper.ModifyStringCustomFields2(invoiceJson, ManagerCustomField.DoucmentStatusGuid, updateSummary.DocumentStatus);
            invoiceJson = RelayDataHelper.ModifyStringCustomFields2(invoiceJson, ManagerCustomField.DocumentLongIdGuid, updateSummary.DocumentLogId);
            invoiceJson = RelayDataHelper.ModifyStringCustomFields2(invoiceJson, ManagerCustomField.PublicUrlGuid, updateSummary.PublicUrl);

            return invoiceJson;
        }


        [HttpPost("AjaxUpdateStatus")]
        public async Task<IActionResult> AjaxUpdateStatus([FromForm] RelayDataViewModel model)
        {
            try
            {
                _logger.LogInformation("Received AjaxUpdateStatus request with model: {@Model}", model);

                InvoiceSummary invoiceSummary = JsonConvert.DeserializeObject<InvoiceSummary>(model.InvoiceSummaryJson);

                if (string.IsNullOrEmpty(invoiceSummary?.SubmissionId))
                {
                    var modelError = new
                    {
                        ApiResponse = new Error
                        {
                            Code = "ValidationError",
                            Message = "Both DocumentUUID and SubmissionUUID are invalid"
                        }
                    };
                    return Ok(JsonConvert.SerializeObject(modelError, Formatting.Indented));
                }

                using HttpClient client = new();
                using ApiHelper apiHelper = new(client, model.ClientID, model.ClientSecret, model.IntegrationType);

                GetSubmissionResponse submissionResponse = await apiHelper.GetSubmission(invoiceSummary.SubmissionId);

                invoiceSummary.DocumentIssueDate = submissionResponse.DocumentSummary[0].DateTimeIssued.ToLocalTime();
                invoiceSummary.SubmissionDate = submissionResponse.DateTimeReceived;
                invoiceSummary.DocumentStatus = submissionResponse.DocumentSummary[0].Status;
                invoiceSummary.PublicUrl = submissionResponse.DocumentSummary[0].PublicUrl;

                model.InvoiceJson = UpdateInvoiceWithResponse(model, invoiceSummary);

                string apiUrl = Utils.ConstructInvoiceApiUrl(model.Referrer, model.FormKey);

                var combinedApiObject = new
                {
                    ApiResponse = submissionResponse,
                    InvoiceSummary = invoiceSummary,
                    ApiInvoice = new
                    {
                        ApiUrl = apiUrl,
                        SecretKey = model.Token,
                        Payload = model.InvoiceJson
                    }
                };

                return Ok(Utils.SerializeObject(combinedApiObject));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "API Error during document status retrieval");
                var errorObject = new
                {
                    ApiResponse = new Error
                    {
                        Code = ex.StatusCode.ToString(),
                        Message = ex.Error?.Message ?? "API Error occurred"
                    }
                };
                return Ok(JsonConvert.SerializeObject(errorObject, Formatting.Indented));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document status retrieval");
                var errorObject = new
                {
                    ApiResponse = new Error
                    {
                        Code = "SystemError",
                        Message = ex.Message
                    }
                };
                return Ok(JsonConvert.SerializeObject(errorObject, Formatting.Indented));
            }
        }


    }
}
