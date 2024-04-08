using System;

namespace ScrumBoard.Services
{
    public enum RequestFailure
    {
        ConnectionFailed,
        NotFound,
        Forbidden,
        Unauthorized,
        BadHttpStatus,
        InvalidPayload,
    }
    public class GitlabRequestFailedException : Exception
    {
        public RequestFailure FailureType { get; private set; }

        public GitlabRequestFailedException(RequestFailure failureType, Exception source = null) : base(
            $"Gitlab request failed due to {failureType}", source)
        {
            FailureType = failureType;
        }
    }
}
