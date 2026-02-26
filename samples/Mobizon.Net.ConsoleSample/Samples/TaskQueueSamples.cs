using System;
using System.Threading.Tasks;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class TaskQueueSamples
    {
        // GET /service/TaskQueue/GetStatus
        public static async Task GetStatusAsync(MobizonClient client, int taskId)
        {
            Console.WriteLine("=== TaskQueue.GetStatus ===");
            var result = await client.TaskQueue.GetStatusAsync(taskId);
            Console.WriteLine($"Id      : {result.Data.Id}");
            Console.WriteLine($"Status  : {result.Data.Status}");
            Console.WriteLine($"Progress: {result.Data.Progress}%");
        }
    }
}
