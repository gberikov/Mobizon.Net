namespace Mobizon.Contracts.Models
{
    /// <summary>
    /// Represents the response codes returned by the Mobizon API.
    /// </summary>
    public enum MobizonResponseCode
    {
        /// <summary>The request completed successfully.</summary>
        Success = 0,

        /// <summary>One or more request parameters are invalid or missing.</summary>
        InvalidData = 1,

        /// <summary>Authentication failed, typically due to an invalid API key.</summary>
        AuthFailed = 2,

        /// <summary>The requested resource was not found.</summary>
        NotFound = 3,

        /// <summary>The authenticated user does not have permission to perform the operation.</summary>
        AccessDenied = 4,

        /// <summary>An internal server error occurred on the Mobizon platform.</summary>
        InternalError = 5,

        /// <summary>The operation was accepted and queued as a background task.</summary>
        BackgroundTask = 100
    }
}
