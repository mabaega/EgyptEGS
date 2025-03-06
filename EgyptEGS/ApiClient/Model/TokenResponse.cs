using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonIgnore]
        public DateTime ExpiryDate { get; set; }

        [JsonIgnore]
        public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
    }
}
