using Mobizon.Contracts.Models.Common;

namespace Mobizon.Contracts.Models.Common
{
    /// <summary>
    /// Represents the response codes returned by the Mobizon API.
    /// </summary>
    public enum MobizonResponseCode
    {
        /// <summary>The operation completed successfully.</summary>
        Success = 0,

        /// <summary>Validation error: transmitted data contains invalid values. The <c>data</c> field describes which fields are incorrect.</summary>
        ValidationError = 1,

        /// <summary>Record not found — likely deleted, incorrect ID specified, or the user lacks access rights.</summary>
        NotFound = 2,

        /// <summary>Unknown application error. Contact support with the request details.</summary>
        UnknownError = 3,

        /// <summary>Invalid <c>module</c> parameter. Check the spelling against the API documentation.</summary>
        InvalidModule = 4,

        /// <summary>Invalid <c>method</c> parameter. Check the spelling against the API documentation.</summary>
        InvalidMethod = 5,

        /// <summary>Invalid <c>format</c> parameter. Check the spelling against the API documentation.</summary>
        InvalidFormat = 6,

        /// <summary>Login error: incorrect credentials or an expired/closed session. Details are in the <c>message</c> field.</summary>
        LoginError = 8,

        /// <summary>Access to the specified API method is denied.</summary>
        AccessDenied = 9,

        /// <summary>Server data save error during operation execution — usually caused by simultaneous access from multiple clients.</summary>
        SaveError = 10,

        /// <summary>Missing required parameters in the request. Check the documentation and add the necessary parameters.</summary>
        MissingParameters = 11,

        /// <summary>An input parameter does not satisfy the established conditions or constraints.</summary>
        InvalidParameter = 12,

        /// <summary>Request sent to an API server that does not serve this user. The correct domain is available in the <c>data</c> field.</summary>
        WrongServer = 13,

        /// <summary>The user account is blocked or deleted.</summary>
        AccountBlocked = 14,

        /// <summary>Operation error unrelated to data update. Details are in the <c>message</c> field.</summary>
        OperationError = 15,

        /// <summary>Rate limiting error: too many requests in a short timeframe. Decrease the request frequency.</summary>
        RateLimitExceeded = 30,

        /// <summary>Bulk operation partially completed: some elements failed but others succeeded. Details are in the <c>data</c> field.</summary>
        BulkPartialSuccess = 98,

        /// <summary>Bulk operation completely failed: no elements were processed. Details are in the <c>data</c> and <c>message</c> fields.</summary>
        BulkCompleteFailure = 99,

        /// <summary>The operation was accepted and queued as a background task. The <c>data</c> field contains the task ID for tracking via TaskQueue/GetStatus.</summary>
        BackgroundTask = 100,

        /// <summary>General service error. Details are in the <c>message</c> field.</summary>
        ServiceError = 999
    }
}
