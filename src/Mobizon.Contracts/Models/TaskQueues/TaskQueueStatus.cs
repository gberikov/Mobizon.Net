namespace Mobizon.Contracts.Models.TaskQueues
{
    /// <summary>
    /// Represents the current status and progress of a Mobizon background task.
    /// </summary>
    public class TaskQueueStatus
    {
        /// <summary>
        /// Gets or sets the unique ID of the background task.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the execution status of the task.
        /// </summary>
        public TaskStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the completion progress of the task as a percentage (0–100).
        /// </summary>
        public int Progress { get; set; }
    }
}
