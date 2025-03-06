using EgyptEGS.ApiClient.Model;
using System.Net;

namespace EgyptEGS.ApiClient.Exceptions
{
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public Error Error { get; }
        public int RetryAfter { get; }

        public ApiException(HttpStatusCode statusCode, Error error, int retryAfter = 0)
            : base(error?.ToString() ?? "An API error occurred")
        {
            StatusCode = statusCode;
            Error = error;
            RetryAfter = retryAfter;
        }

        public bool ShouldRetry => StatusCode == HttpStatusCode.UnprocessableEntity
                                  && Error?.IsDuplicateSubmission == true
                                  && RetryAfter > 0;
    }
}