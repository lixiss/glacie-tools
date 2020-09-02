using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace Glacie.Tools.Arc
{
    internal static class Utilities
    {
        private const int BufferSize = 16 * 1024;

        public static MemoryStream ReadFile(string path, IProgress? progress)
        {
            using var inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

            progress?.SetMaxValue(inputStream.Length);

            // Want to free previously occupied memory by memory stream.
            CollectGarbageIf(256 * 1024 * 1024 - inputStream.Length);

            var memoryStream = new MemoryStream(checked((int)inputStream.Length));
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            while (true)
            {
                var bytesRead = inputStream.Read(buffer);
                if (bytesRead == 0) break;
                memoryStream.Write(buffer, 0, bytesRead);

                progress?.AddValue(bytesRead);
            }

            ArrayPool<byte>.Shared.Return(buffer);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public static void ReplaceFile(string path, MemoryStream stream, IProgress? progress)
        {
            var temporaryFileName = path + "." + new Random().Next(0, int.MaxValue) + ".gx-arc.tmp";
            var targetFileName = GetCanonicalFileNameCasingForPath(path);

            WriteFile(temporaryFileName, stream, progress);
            File.Replace(temporaryFileName, targetFileName, null);
        }

        public static void WriteFile(string path, MemoryStream stream, IProgress? progress)
        {
            stream.Position = 0;
            var bytesToWrite = stream.Length;
            var buffer = stream.GetBuffer();
            var position = 0;

            using var outputStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

            progress?.SetValue(0);
            progress?.SetMaxValue(bytesToWrite);

            while (bytesToWrite > 0)
            {
                var blockLength = checked((int)Math.Min(bytesToWrite, BufferSize));

                outputStream.Write(buffer, position, blockLength);
                position += blockLength;
                bytesToWrite -= blockLength;

                progress?.AddValue(blockLength);
            }
        }

        public static void CollectGarbageIf(long totalMemoryThreshold, bool waitForPendingFinalizers = false)
        {
            var totalMemory = GC.GetTotalMemory(false);
            if (totalMemory > totalMemoryThreshold)
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                if (waitForPendingFinalizers)
                {
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        private static string GetCanonicalFileNameCasingForPath(string path)
        {
            // See "Add API to get actual file casing in path #14321"
            // https://github.com/dotnet/runtime/issues/14321

            if (!File.Exists(path)) return path;

            var pathInfo = new FileInfo(path);
            var parentDirectory = pathInfo.Directory;
            var fis = parentDirectory.EnumerateFileSystemInfos(pathInfo.Name, SearchOption.TopDirectoryOnly);

            string result = path;
            foreach (var fileSystemInfo in fis)
            {
                return fileSystemInfo.FullName;
            }
            return path;
        }
    }
}
