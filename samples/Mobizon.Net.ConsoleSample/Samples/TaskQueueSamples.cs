using System;
using System.Threading.Tasks;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class TaskQueueSamples
    {
        // GET /service/TaskQueue/GetStatus
        public static async Task GetStatusAsync(MobizonClient client, long taskId)
        {
            Console.WriteLine("=== TaskQueue.GetStatus ===");
            var result = await client.TaskQueue.GetStatusAsync(taskId);
            Console.WriteLine($"Id      : {result.Id}");
            Console.WriteLine($"Status  : {result.Status}");
            Console.WriteLine($"Progress: {result.Progress}%");
        }
    }
}
