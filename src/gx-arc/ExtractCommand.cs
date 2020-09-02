using System;
using System.Buffers;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO;

using Glacie.ChecksumAlgorithms;
using Glacie.Data.Arc;

namespace Glacie.Tools.Arc
{
    internal sealed class ExtractCommand : CommandBase
    {
        public string ArchivePath { get; }

        public string OutputPath { get; }

        public bool SetLastWriteTime { get; }

        public ExtractCommand(InvocationContext context,
            string archive, string outputPath, bool setLastWriteTime)
            : base(context)
        {
            ArchivePath = archive;
            OutputPath = outputPath;
            SetLastWriteTime = setLastWriteTime;
        }

        public int Run()
        {
            if (!File.Exists(ArchivePath))
            {
                // TODO: Glob?

                // TODO: Use common error helpers, or validators.
                Console.Error.WriteLine("File does not exist: " + ArchivePath);
                // File does not exist: ./Creatures.arc1
                return 1;
            }

            if (File.Exists(OutputPath))
            {
                // TODO: Use common error helpers, or validators.
                Console.Error.WriteLine("Output path should be a directory: " + OutputPath);
                return 1;
            }

            ExtractArchive(ArchivePath);
            return 0;
        }

        private void ExtractArchive(string path)
        {
            using var archive = ArcArchive.Open(path);
            ExtractArchive(archive);
        }

        private void ExtractArchive(ArcArchive archive)
        {
            using var progressBar = CreateProgressBar();
            progressBar.Title = "Extracting...";

            long totalLength = 0;
            foreach (var entry in archive.GetEntries())
            {
                totalLength += entry.Length;
            }
            progressBar?.SetMaxValue(totalLength);


            foreach (var entry in archive.GetEntries())
            {
                progressBar?.SetMessage(entry.Name);

                // Assuming what validated entry name is safe to use in path
                // (with combining).
                EntryNameUtilities.Validate(entry.Name);

                var outputPath = Path.Combine(OutputPath, entry.Name);

                var directoryPath = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(directoryPath);

                using var inputStream = entry.Open();

                DateTimeOffset? lastWriteTime = SetLastWriteTime ? entry.LastWriteTime : (DateTimeOffset?)null;
                WriteFile(outputPath, inputStream, lastWriteTime, progressBar);
            }
        }

        private void WriteFile(string path, Stream stream, DateTimeOffset? lastWriteTime, IProgress? progress)
        {
            {
                using var outputStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);

                while (true)
                {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    outputStream.Write(buffer, 0, bytesRead);

                    progress?.AddValue(bytesRead);
                }

                ArrayPool<byte>.Shared.Return(buffer);
            }

            if (lastWriteTime != null)
            {
                try
                {
                    new FileInfo(path).LastWriteTimeUtc = lastWriteTime.Value.UtcDateTime;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Failed to set last write time for file: " + e.Message);
                }
            }
        }
    }
}
