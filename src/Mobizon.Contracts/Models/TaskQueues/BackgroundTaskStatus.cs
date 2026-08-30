namespace Mobizon.Contracts
{
    /// <summary>Status of a background task in the Mobizon task queue (<c>taskqueue/getStatus</c>).</summary>
    public enum BackgroundTaskStatus
    {
        /// <summary>Task is waiting to start.</summary>
        Pending = 0,

        /// <summary>Task is currently being processed.</summary>
        InProgress = 1,

        /// <summary>Task has completed successfully.</summary>
        Completed = 2,

        /// <summary>Task was rejected.</summary>
        Rejected = 3
    }
}
