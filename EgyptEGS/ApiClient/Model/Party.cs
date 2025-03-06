using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EgyptEGS.ApiClient.Model
{
    public class Party
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PartyType Type { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }  // Optional untuk P/F

        [JsonProperty("name")]
        public string? Name { get; set; }  // Optional untuk P/F

        [JsonProperty("address")]
        public Address? Address { get; set; }  // Optional untuk P/F
    }
    public class Address
    {
        [JsonProperty("branchId")]
        public string? BranchId { get; set; }  // Wajib untuk Issuer type B

        [JsonProperty("country")]
        public string Country { get; set; } = "EG";  // ISO 3166-1 alpha-2 (contoh: "EG")

        [JsonProperty("governate")]
        public string Governate { get; set; } = "Egypt";

        [JsonProperty("regionCity")]
        public string RegionCity { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("buildingNumber")]
        public string BuildingNumber { get; set; }

        [JsonProperty("postalCode")]
        public string? PostalCode { get; set; }

        [JsonProperty("floor")]
        public string? Floor { get; set; }

        [JsonProperty("room")]
        public string? Room { get; set; }

        [JsonProperty("landmark")]
        public string? Landmark { get; set; }

        [JsonProperty("additionalInformation")]
        public string? AdditionalInformation { get; set; }
    }
}
