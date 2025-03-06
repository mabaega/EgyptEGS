using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class ValidationResults
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("validationSteps")]
        public List<ValidationStep> ValidationSteps { get; set; }
    }

    public class ValidationStep
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("error")]
        public Error Error { get; set; }
    }

}