using EgyptEGS.ApiClient.Exceptions;
using EgyptEGS.ApiClient.Extensions;
using EgyptEGS.ApiClient.Model;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace EgyptEGS.ApiClient
{
    public class ApiHelper : IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _clientID;
        private readonly string _clientSecret;
        private readonly IntegrationType _integrationType;
        private TokenResponse _tokenResponse;
        private bool _disposed;

        public ApiHelper(HttpClient client, string clientID, string clientSecret, IntegrationType integrationType)
        {
            _client = client;
            _clientID = clientID;
            _clientSecret = clientSecret;
            _integrationType = integrationType;
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

        public async Task<DocumentSubmitResponse> SubmitDocument(string signedDocumentsJson)
        {
            try
            {
                if (_tokenResponse == null || _tokenResponse.IsExpired)
                {
                    await GetAuthenticationToken();
                }

                string baseUrl = _integrationType.GetApiBaseUrl();
                string requestUrl = $"{baseUrl}/api/v1/documentsubmissions";

                using HttpRequestMessage httpRequest = new(HttpMethod.Post, requestUrl);

                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);
                httpRequest.Content = new StringContent(signedDocumentsJson, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }

                string content = await response.Content.ReadAsStringAsync();
                DocumentSubmitResponse? result = JsonConvert.DeserializeObject<DocumentSubmitResponse>(content);
                result.RequestStatus = response.StatusCode.ToString();

                if (response.Headers.Contains("Retry-After"))
                {
                    if (int.TryParse(response.Headers.GetValues("Retry-After").FirstOrDefault(), out int retryAfter))
                    {
                        // Wait for the specified time and retry
                        await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                        return await SubmitDocument(signedDocumentsJson);
                    }
                }

                return result;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to submit document: {ex.Message}", ex);
            }
        }

        public async Task<GetSubmissionResponse> GetSubmission(string submissionUUID, int pageSize = 1, int pageNo = 1)
        {
            try
            {
                if (_tokenResponse == null || _tokenResponse.IsExpired)
                {
                    await GetAuthenticationToken();
                }

                string baseUrl = _integrationType.GetApiBaseUrl();
                string requestUrl = $"{baseUrl}/api/v1/documentSubmissions/{submissionUUID}?PageSize={pageSize}";

                using HttpRequestMessage request = new(HttpMethod.Get, requestUrl);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);
                request.Headers.Add("PageSize", pageSize.ToString());
                request.Headers.Add("PageNo", pageNo.ToString());

                HttpResponseMessage response = await _client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }

                string content = await response.Content.ReadAsStringAsync();
                GetSubmissionResponse? result = JsonConvert.DeserializeObject<GetSubmissionResponse>(content);
                result.RequestStatus = response.StatusCode.ToString();

                if (response.Headers.Contains("Retry-After"))
                {
                    if (int.TryParse(response.Headers.GetValues("Retry-After").FirstOrDefault(), out int retryAfter))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                        return await GetSubmission(submissionUUID, pageSize, pageNo);
                    }
                }

                return result;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get submission: {ex.Message}", ex);
            }
        }

        public async Task<GetDocumentResponse> GetDocument(string documentUUID)
        {
            try
            {
                if (_tokenResponse == null || _tokenResponse.IsExpired)
                {
                    await GetAuthenticationToken();
                }

                string baseUrl = _integrationType.GetApiBaseUrl();
                string requestUrl = $"{baseUrl}/api/v1/documents/{documentUUID}/raw";

                using HttpRequestMessage request = new(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                HttpResponseMessage response = await _client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }

                string content = await response.Content.ReadAsStringAsync();
                GetDocumentResponse? result = JsonConvert.DeserializeObject<GetDocumentResponse>(content);
                result.RequestStatus = response.StatusCode.ToString();

                if (response.Headers.Contains("Retry-After"))
                {
                    if (int.TryParse(response.Headers.GetValues("Retry-After").FirstOrDefault(), out int retryAfter))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                        return await GetDocument(documentUUID);
                    }
                }

                return result;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get document: {ex.Message}", ex);
            }
        }

        public async Task<GetDocumentDetailsResponse> GetDocumentDetails(string documentUUID)
        {
            try
            {
                if (_tokenResponse == null || _tokenResponse.IsExpired)
                {
                    await GetAuthenticationToken();
                }

                string baseUrl = _integrationType.GetApiBaseUrl();
                string requestUrl = $"{baseUrl}/api/v1/documents/{documentUUID}/details";

                using HttpRequestMessage request = new(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                HttpResponseMessage response = await _client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }

                string content = await response.Content.ReadAsStringAsync();
                GetDocumentDetailsResponse? result = JsonConvert.DeserializeObject<GetDocumentDetailsResponse>(content);
                result.RequestStatus = response.StatusCode.ToString();

                if (response.Headers.Contains("Retry-After"))
                {
                    if (int.TryParse(response.Headers.GetValues("Retry-After").FirstOrDefault(), out int retryAfter))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                        return await GetDocumentDetails(documentUUID);
                    }
                }

                return result;
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get document details: {ex.Message}", ex);
            }
        }

        public async Task CancelDocument(string documentUUID, string reason)
        {
            try
            {
                if (_tokenResponse == null || _tokenResponse.IsExpired)
                {
                    await GetAuthenticationToken();
                }

                string baseUrl = _integrationType.GetApiBaseUrl();
                string requestUrl = $"{baseUrl}/api/v1/documents/{documentUUID}/state";

                using HttpRequestMessage request = new(HttpMethod.Put, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                CancelDocumentRequest cancelRequest = new() { Reason = reason };
                string json = JsonConvert.SerializeObject(cancelRequest);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to cancel document: {ex.Message}", ex);
            }
        }

        public async Task DeclineDocumentRejection(string documentUUID)
        {
            try
            {
                if (_tokenResponse == null || _tokenResponse.IsExpired)
                {
                    await GetAuthenticationToken();
                }

                string baseUrl = _integrationType.GetApiBaseUrl();
                string requestUrl = $"{baseUrl}/api/v1/documents/{documentUUID}/decline/rejection";

                using HttpRequestMessage request = new(HttpMethod.Put, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenResponse.AccessToken);

                // Empty content as per API specification
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response);
                }
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to decline document rejection: {ex.Message}", ex);
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

        ~ApiHelper()
        {
            Dispose(false);
        }

    }
}