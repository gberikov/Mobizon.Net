using System;
using System.IO;

namespace Mobizon.Net.ApiCapture
{
    public static class SanitizeRunner
    {
        /// <summary>
        /// Reads each *.json file from <paramref name="inDir"/>, passes it through
        /// <see cref="Sanitizer.Scrub"/>, and writes the result to
        /// <paramref name="outDir"/> under the same file name.
        /// </summary>
        /// <returns>Number of files written, or 0 if <paramref name="inDir"/> does not exist.</returns>
        public static int Run(string inDir, string outDir)
        {
            if (!Directory.Exists(inDir))
            {
                Console.WriteLine($"[sanitize] Input directory not found: {inDir}");
                return 0;
            }

            Directory.CreateDirectory(outDir);

            var files = Directory.GetFiles(inDir, "*.json");
            int count = 0;

            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var text = File.ReadAllText(file);
                var scrubbed = Sanitizer.Scrub(text);
                File.WriteAllText(Path.Combine(outDir, name), scrubbed);
                Console.WriteLine($"[sanitize] {name}");
                count++;
            }

            return count;
        }
    }
}
