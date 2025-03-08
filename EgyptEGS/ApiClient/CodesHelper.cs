using EgyptEGS.ApiClient.Exceptions;
using EgyptEGS.ApiClient.Extensions;
using EgyptEGS.ApiClient.Model;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace EgyptEGS.ApiClient
{
    public class CodesHelper : IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _clientID;
        private readonly string _clientSecret;
        private readonly IntegrationType _integrationType;
        private readonly string _taxPayerRIN;
        private readonly Dictionary<string, CodeTypeItem> _codeCache = new();
        private TokenResponse _tokenResponse;
        private bool _disposed;

        public CodesHelper(HttpClient client, string clientID, string clientSecret, IntegrationType integrationType, string taxPayerRIN)
        {
            _client = client;
            _clientID = clientID;
            _clientSecret = clientSecret;
            _integrationType = integrationType;
            _taxPayerRIN = taxPayerRIN;
        }

        private string GetTaxPayerRINFilePath()
        {
            // Check if the path is already a full path
            if (Path.IsPathFullyQualified(_taxPayerRIN))
            {
                return _taxPayerRIN;
            }

            string baseDirectory = Environment.GetEnvironmentVariable("HOME") != null
                ? Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Data", "TaxPayerCodes") // Azure
                : Path.Combine(AppContext.BaseDirectory, "Data", "TaxPayerCodes"); // Local

            // Ensure directory exists
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            // Just use the filename part if a path was provided
            string fileName = Path.GetFileName(_taxPayerRIN);
            if (!fileName.EndsWith(".json"))
            {
                fileName += ".json";
            }

            return Path.Combine(baseDirectory, fileName);
        }

        private async Task EnsureValidToken()
        {
            if (_tokenResponse == null || _tokenResponse.IsExpired)
            {
                await GetAuthenticationToken();
            }
        }

        public async Task<CodeTypeItem> GetCodeUsageByItemCode(string itemCode)
        {
            try
            {
                // Check memory cache first
                if (_codeCache.TryGetValue(itemCode, out var cachedCode))
                {
                    return cachedCode;
                }

                var localCodes = await GetLocalCodeTypes();
                
                // Check in loaded JSON
                var localCode = localCodes.Result.FirstOrDefault(c => c.ItemCode == itemCode);
                if (localCode != null)
                {
                    _codeCache[itemCode] = localCode;
                    return localCode;
                }

                // Get from server
                var serverCode = await GetCodeFromServer(itemCode);
                if (serverCode != null)
                {
                    _codeCache[itemCode] = serverCode;
                    localCodes.Result.Add(serverCode);
                    await SaveLocalCodeTypes(localCodes);
                    return serverCode;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get code usage by item code: {ex.Message}", ex);
            }
        }

        private async Task<CodeTypeItem> GetCodeFromServer(string itemCode)
        {
            await EnsureValidToken();

            string baseUrl = _integrationType.GetApiBaseUrl();
            string requestUrl = $"{baseUrl}/api/v1.0/codetypes/requests/my?ItemCode={itemCode}&Active=true&Status=Approved";

            using HttpRequestMessage request = new(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);
            request.Headers.Add("Accept-Language", "en");

            HttpResponseMessage response = await _client.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // Get retry-after value
                if (response.Headers.TryGetValues("x-rate-limit-reset", out var resetValues))
                {
                    if (DateTimeOffset.TryParse(resetValues.FirstOrDefault(), out var resetTime))
                    {
                        var delay = resetTime - DateTimeOffset.UtcNow;
                        if (delay.TotalMilliseconds > 0)
                        {
                            await Task.Delay(delay);
                            return await GetCodeFromServer(itemCode); // Retry after delay
                        }
                    }
                }
                await Task.Delay(5000); // Default 5 second delay if no reset time
                return await GetCodeFromServer(itemCode);
            }

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponse(response);
            }

            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<CodeTypeResponse>(content);
            
            return result?.Result?.FirstOrDefault();
        }

        public async Task<CodeTypeResponse> GetAllCodeUsage()
        {
            try
            {
                if (_tokenResponse == null || _tokenResponse.IsExpired)
                {
                    await GetAuthenticationToken();
                }

                string baseUrl = _integrationType.GetApiBaseUrl();
                string requestUrl = $"{baseUrl}/api/v1.0/codetypes/requests/my?Active=true&Status=Approved&Ps=5000&Pn=1";

                using HttpRequestMessage request = new(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);
                request.Headers.Add("Accept-Language", "en");

                // Remove any custom headers that might be causing issues
                HttpResponseMessage response = await _client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }

                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<CodeTypeResponse>(content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get all code usage: {ex.Message}", ex);
            }
        }

        private async Task<CodeTypeResponse> GetLocalCodeTypes()
        {
            string filePath = GetTaxPayerRINFilePath();
            
            if (!File.Exists(filePath))
            {
                // First time: Get all codes and save
                var allCodes = await GetAllCodeUsage();
                await SaveLocalCodeTypes(allCodes);
                return allCodes;
            }

            string json = await File.ReadAllTextAsync(filePath);
            return JsonConvert.DeserializeObject<CodeTypeResponse>(json) ?? new CodeTypeResponse();
        }

        private async Task SaveLocalCodeTypes(CodeTypeResponse codeTypes)
        {
            string filePath = GetTaxPayerRINFilePath();
            await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(codeTypes, Formatting.Indented));
        }

        private async Task<TokenResponse> GetAuthenticationToken()
        {
            try
            {
                FormUrlEncodedContent authContent = new(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", _clientID),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("scope", "InvoicingAPI")
                });

                string baseUrl = _integrationType.GetIdSrvBaseUrl();
                HttpResponseMessage authResponse = await _client.PostAsync($"{baseUrl}/connect/token", authContent);

                if (!authResponse.IsSuccessStatusCode)
                {
                    string error = await authResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Authentication failed: {error}");
                }

                string content = await authResponse.Content.ReadAsStringAsync();
                _tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(content);
                _tokenResponse.ExpiryDate = DateTime.UtcNow.AddSeconds(_tokenResponse.ExpiresIn);

                return _tokenResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get authentication token: {ex.Message}", ex);
            }
        }

        private async Task HandleErrorResponse(HttpResponseMessage response)
        {
            string content = await response.Content.ReadAsStringAsync();
            Error? error = JsonConvert.DeserializeObject<Error>(content);
            error.RequestStatus = response.StatusCode.ToString();
            
            int retryAfter = 0;
            
            if (response.Headers.Contains("Retry-After"))
            {
                int.TryParse(response.Headers.GetValues("Retry-After").FirstOrDefault(), out retryAfter);
            }

            throw new ApiException(response.StatusCode, error, retryAfter);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _client?.Dispose();
            }

            _disposed = true;
        }

        ~CodesHelper()
        {
            Dispose(false);
        }

    }
}