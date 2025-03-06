using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class Delivery
    {
        [JsonProperty("approach")]
        public string? Approach { get; set; }

        [JsonProperty("packaging")]
        public string? Packaging { get; set; }

        [JsonProperty("dateValidity")]
        public string? DateValidity { get; set; }

        [JsonProperty("exportPort")]
        public string? ExportPort { get; set; }

        [JsonProperty("countryOfOrigin")]
        public string? CountryOfOrigin { get; set; }

        [JsonProperty("grossWeight")]
        public decimal? GrossWeight { get; set; }

        [JsonProperty("netWeight")]
        public decimal? NetWeight { get; set; }

        [JsonProperty("terms")]
        public string? Terms { get; set; }
    }
}
