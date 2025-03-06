using Newtonsoft.Json;

namespace EgyptEGS.ApiClient.Model
{
    public class Error
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("details")]
        public List<Error> Details { get; set; }

        public bool IsBadStructure => Code == "BadStructure";
        public bool IsMaximumSizeExceeded => Code == "MaximumSizeExceeded";
        public bool IsIncorrectSubmitter => Code == "IncorrectSubmitter";
        public bool IsForbidden => Code == "Forbidden";
        public bool IsDuplicateSubmission => Code == "DuplicateSubmission";

        public static Error CreateBadStructure(string message = "Invalid document structure")
        {
            return new Error { Code = "BadStructure", Message = message };
        }

        public static Error CreateMaximumSizeExceeded(string message = "Submission size exceeds allowed limit")
        {
            return new Error { Code = "MaximumSizeExceeded", Message = message };
        }

        public static Error CreateIncorrectSubmitter(string message = "Unauthorized submitter")
        {
            return new Error { Code = "IncorrectSubmitter", Message = message };
        }

        public static Error CreateForbidden(string message = "Access forbidden")
        {
            return new Error { Code = "Forbidden", Message = message };
        }

        public static Error CreateDuplicateSubmission(string message = "Duplicate submission detected")
        {
            return new Error { Code = "DuplicateSubmission", Message = message };
        }

        public override string ToString()
        {
            string detail = Details?.Any() == true
                ? $" Details: {string.Join(", ", Details.Select(d => d.Message))}"
                : string.Empty;

            return $"Error {Code}: {Message}{detail}";
        }
    }
}