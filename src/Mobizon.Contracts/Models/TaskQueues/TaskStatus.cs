namespace Mobizon.Contracts.Models.TaskQueues
{
    /// <summary>
    /// Represents the execution status of a Mobizon background task.
    /// </summary>
    public enum TaskStatus
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
