using System;
using System.IO;

namespace Mobizon.Net.Tests
{
    public static class Fixtures
    {
        public static string Load(string fileName) =>
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Payloads", fileName));
    }
}
