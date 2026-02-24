using System;
using System.Threading.Tasks;
using Mobizon.Net;

namespace Mobizon.Net.ConsoleSample.Samples
{
    static class UserSamples
    {
        // GET /service/user/getownbalance
        public static async Task GetBalanceAsync(MobizonClient client)
        {
            Console.WriteLine("=== User.GetOwnBalance ===");
            var result = await client.User.GetOwnBalanceAsync();
            Console.WriteLine($"Balance : {result.Data.Balance} {result.Data.Currency}");
        }
    }
}
