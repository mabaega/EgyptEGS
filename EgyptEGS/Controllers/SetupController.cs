using EgyptEGS.ApiClient.Extensions;
using EgyptEGS.ApiClient.Model;
using EgyptEGS.Models;
using EgyptEGS.Utilities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EgyptEGS.Controllers
{
    public class SetupController : Controller
    {
        private readonly ILogger<SetupController> _logger;

        public SetupController(ILogger<SetupController> logger)
        {
            _logger = logger;
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            string? setupViewModelJson = TempData["SetupViewModel"] as string;
            if (!string.IsNullOrEmpty(setupViewModelJson))
            {
                SetupViewModel? setupViewModel = JsonConvert.DeserializeObject<SetupViewModel>(setupViewModelJson);
                return View("index", setupViewModel);
            }
            return View();
        }

        [HttpPost("Update")]
        public IActionResult UpdateCustomField([FromForm] Dictionary<string, string> formData)
        {
            try
            {
                string Referrer = Utils.GetValue(formData, "Referrer");
                string FormKey = Utils.GetValue(formData, "Key");
                string Data = Utils.GetValue(formData, "Data");
                string Api = Utils.GetValue(formData, "Api");
                string Token = Utils.GetValue(formData, "Token");

                _logger.LogInformation("Setup API Diakses oleh : {0} IP address: {1}", Api, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

                string BusinessDetailJson = RelayDataHelper.GetValueJson(Data, "BusinessDetails");

                if (!string.IsNullOrEmpty(FormKey))
                {
                    SetupViewModel setupViewModel = new()
                    {
                        Referrer = Referrer,
                        Api = Api,
                        Token = Token,
                        BusinessDetailJson = BusinessDetailJson
                    };

                    return View("UpdateBusinessData", setupViewModel);
                }

                return View("index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kesalahan saat memproses setup api");
                throw new Exception(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult UpdateBusinessDetail([FromForm] SetupViewModel viewModel, [FromForm] string appConfigJson)
        {
            try
            {
                _logger.LogInformation("Received appConfigJson: {0}", appConfigJson);

                if (string.IsNullOrEmpty(appConfigJson))
                {
                    _logger.LogWarning("AppConfigJson is empty");
                    return Json(new { success = false, message = "AppConfig data is required" });
                }

                JsonSerializerSettings settings = new()
                {
                    Error = (sender, args) =>
                    {
                        _logger.LogError("Deserialization error: {0}", args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true;
                    },
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };

                try
                {
                    viewModel.AppConfig = JsonConvert.DeserializeObject<ApplicationConfig>(appConfigJson, settings);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON deserialization failed");
                    return Json(new { success = false, message = "Invalid AppConfig format" });
                }

                if (viewModel == null || viewModel.AppConfig == null)
                {
                    return Json(new { success = false, message = "Invalid data" });
                }

                ApplicationConfig appConfig = viewModel.AppConfig;
                string businessDetails = viewModel.BusinessDetailJson;

                if (string.IsNullOrEmpty(businessDetails))
                {
                    businessDetails = @"{
                        ""Name"": """",
                        ""Address"": """",
                        ""CustomFields2"": {
                            ""Decimals"": { },
                            ""Strings"": {
                                ""7351fc99-f61d-44b8-96ad-0e10c7c3fc9a"": """",
                                ""12a2b5fb-e388-4e72-94ff-8337462fb543"": """"
                            }
                        }
                    }";
                }

                businessDetails = RelayDataHelper.UpdateOrCreateField(businessDetails, "Name", appConfig.Issuer.Name ?? "");
                //businessDetails = RelayDataHelper.UpdateOrCreateField(businessDetails, "Address", appConfig.ToBusinessAddress());
                businessDetails = RelayDataHelper.ModifyStringCustomFields2(businessDetails, ManagerCustomField.AppConfigGuid, ObjectCompressor.SerializeToBase64String(appConfig));
                businessDetails = RelayDataHelper.ModifyStringCustomFields2(businessDetails, ManagerCustomField.AppVersionGuid, VersionHelper.GetVersion());

                var combinedApiObject = new
                {
                    ApiBusinessDetails = new
                    {
                        ApiUrl = $"{viewModel.Api}/business-details-form/38cf4712-6e95-4ce1-b53a-bff03edad273",
                        SecretKey = viewModel.Token,
                        Payload = businessDetails
                    }
                };

                return Ok(JsonConvert.SerializeObject(combinedApiObject, Formatting.Indented));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating business detail");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("setup/getcfdata")]
        public IActionResult CustomFieldJson()
        {
            try
            {
                string jsonData = Utils.ReadEmbeddedResource("CfData.json");
                return Content(jsonData, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to load resource: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetAccessToken([FromBody] TokenRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ClientId) || string.IsNullOrEmpty(request.ClientSecret))
                {
                    return Json(new { success = false, message = "Invalid credentials" });
                }

                using HttpClient client = new();
                FormUrlEncodedContent content = new(new[]
                {
                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                        new KeyValuePair<string, string>("client_id", request.ClientId),
                        new KeyValuePair<string, string>("client_secret", request.ClientSecret),
                        new KeyValuePair<string, string>("scope", "InvoicingAPI")
                    });

                // Parse string to enum
                IntegrationType integrationType = Enum.Parse<IntegrationType>(request.IntegrationType);
                string baseUrl = integrationType.GetIdSrvBaseUrl();

                HttpResponseMessage response = await client.PostAsync($"{baseUrl}/connect/token", content);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return Json(new { success = true, data = result });
                }

                string errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Failed to get access token. {errorContent}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}

