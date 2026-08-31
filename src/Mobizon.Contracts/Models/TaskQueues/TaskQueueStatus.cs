namespace Mobizon.Contracts
{
    /// <summary>Progress of a background task, as returned by <c>taskqueue/getStatus</c>.</summary>
    public class TaskQueueStatus
    {
        /// <summary>Current task status.</summary>
        public BackgroundTaskStatus Status { get; set; }

        /// <summary>Completion percentage, 0–100.</summary>
        public int Progress { get; set; }
    }
}
