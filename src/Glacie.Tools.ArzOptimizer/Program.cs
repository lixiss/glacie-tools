using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Glacie.Abstractions;
using Glacie.Data.Arz;

namespace Glacie.Tools
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var options = ParseArguments(args);
            if (options.Invalid)
            {
                Console.Error.WriteLine("[ERROR] Invalid command line arguments.");
                return 1;
            }
            else if (options.Help)
            {
                return WriteHelp();
            }
            else if (options.SourcePath == null)
            {
                Console.Error.WriteLine("[ERROR] Missing required arguments.");
                return WriteHelp();
            }

            var sourcePath = options.SourcePath;
            var targetPath = options.TargetPath ?? "out.arz";
            int compressionLevel = options.CompressionLevel ?? 12;

            if (File.Exists(sourcePath))
            {
                ProcessFile(sourcePath, targetPath, compressionLevel);
                return 0;
            }
            else
            {
                Console.Error.WriteLine("[ERROR] Source file \"{0}\" not found.", sourcePath);
                return 1;
            }
        }

        private static int WriteHelp()
        {
            Console.WriteLine("Usage: gx-arz-optimizer [options] <path-to-source-arz-file>");
            Console.WriteLine();
            Console.WriteLine("options:");
            Console.WriteLine("  -h|--help                     Show command line help.");
            Console.WriteLine("  --target=<path>               Write output to specified file (default: out.arz).");
            Console.WriteLine("  --compression-level=<number>  Compression level (integer, 1..12) (default: 12, maximum).");
            Console.WriteLine();
            return 1;
        }

        private sealed class Options
        {
            public bool Help;
            public string? TargetPath;
            public int? CompressionLevel;
            public string? SourcePath;
            public bool Invalid;
        }

        private static Options ParseArguments(string[] args)
        {
            var result = new Options();

            foreach (var arg in args)
            {
                if (arg == "--help" || arg == "-h")
                {
                    result.Help = true;
                    break;
                }
                else if (arg.StartsWith("--target=") && result.TargetPath == null)
                {
                    result.TargetPath = arg.Substring("--target=".Length);
                }
                else if (arg.StartsWith("--compression-level=") && result.CompressionLevel == null)
                {
                    var v = arg.Substring("--compression-level=".Length);
                    result.CompressionLevel = int.Parse(v);
                }
                else if (result.SourcePath == null)
                {
                    result.SourcePath = arg;
                }
                else
                {
                    result.Invalid = true;
                    break;
                }
            }

            return result;
        }

        private static void ProcessFile(string sourcePath, string targetPath, int compressionLevel)
        {
            var sourceFileInfo = new FileInfo(sourcePath);

            Console.WriteLine("[ INFO ] Reading: {0}", sourcePath);

            var sw1 = Stopwatch.StartNew();
            using var database = ArzDatabase.Open(sourcePath,
                new ArzReaderOptions { Mode = ArzReadingMode.Full });
            sw1.Stop();

            Console.WriteLine("[ INFO ] Done In: {0:N0}ms", sw1.ElapsedMilliseconds);

            Console.WriteLine("[ INFO ] Optimizing...");

            var optimizeResult = ArzOptimizer.Optimize(database);

            Console.WriteLine("[ INFO ] Optimization Result:");
            Console.WriteLine("  Completed In: {0:N0}ms", optimizeResult.CompletedIn.TotalMilliseconds);
            Console.WriteLine("  # of Remapped Strings: {0}", optimizeResult.NumberOfRemappedStrings);
            Console.WriteLine("  Estimated Size Reduction: {0} bytes", optimizeResult.EstimatedSizeReduction);
            Console.WriteLine("  Estimated File Size: {0} ({1:N1}%)",
                (sourceFileInfo.Length - optimizeResult.EstimatedSizeReduction),
                (sourceFileInfo.Length - optimizeResult.EstimatedSizeReduction) * 100.0 / sourceFileInfo.Length
                );

            var writerOptions = new ArzWriterOptions(true) { CompressionLevel = compressionLevel };

            Console.WriteLine("[ INFO ] Writing To: {0}", targetPath);
            Console.WriteLine("[ INFO ] Compression Level: {0}", writerOptions.CompressionLevel);

            var sw2 = Stopwatch.StartNew();
            ArzWriter.Write(targetPath, database, writerOptions);
            sw2.Stop();
            Console.WriteLine("[ INFO ] Written In: {0:N0}ms", sw2.ElapsedMilliseconds);

            var targetFileInfo = new FileInfo(targetPath);
            Console.WriteLine("[ INFO ] Source File Length: {0}", sourceFileInfo.Length);
            Console.WriteLine("[ INFO ] Target File Length: {0} ({1:N1}%)", targetFileInfo.Length,
                targetFileInfo.Length * 100.0 / sourceFileInfo.Length
                );
            Console.WriteLine();
        }
    }
}
