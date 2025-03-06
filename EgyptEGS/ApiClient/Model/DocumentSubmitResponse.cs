using EgyptEGS.ApiClient.Model;
using Newtonsoft.Json;

public class DocumentSubmitResponse
{
    [JsonProperty("submissionId")]
    public string SubmissionId { get; set; }

    [JsonProperty("acceptedDocuments")]
    public List<DocumentAccepted> AcceptedDocuments { get; set; } = new();

    [JsonProperty("rejectedDocuments")]
    public List<DocumentRejected> RejectedDocuments { get; set; } = new();
}

public class DocumentAccepted
{
    [JsonProperty("uuid")]
    public string UUID { get; set; }

    [JsonProperty("longId")]
    public string LongId { get; set; }

    [JsonProperty("internalId")]
    public string InternalId { get; set; }

    [JsonProperty("hashKey")]
    public string HashKey { get; set; }
}

public class DocumentRejected
{
    [JsonProperty("internalId")]
    public string InternalId { get; set; }

    [JsonProperty("error")]
    public Error Error { get; set; }
}
