using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class Metadata
    {
        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }
    }
}