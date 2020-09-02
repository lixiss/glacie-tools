using System;
using System.Buffers;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO;

using Glacie.ChecksumAlgorithms;
using Glacie.Data.Arc;
using Glacie.Data.Compression;
using Glacie.ExtendedConsole;

namespace Glacie.Tools.Arc
{
    internal abstract class ProcessArchiveCommand : CommandBase
    {
        public string ArchivePath { get; }

        public bool SafeWrite { get; }

        protected ProcessArchiveCommand(InvocationContext context,
            string archive,
            bool safeWrite)
            : base(context)
        {
            ArchivePath = archive;
            SafeWrite = safeWrite;
        }

        public int Run()
        {
            if (File.Exists(ArchivePath))
            {
                ProcessArchive(ArchivePath);

                // Console.Out.WriteLine(string.Format("{0} {1}", "[done]", Path));
                return 0;
            }
            else
            {
                Console.Error.WriteLine("File does not exist: " + ArchivePath);
                return 1;
            }
        }

        private void ProcessArchive(string path)
        {
            if (SafeWrite)
            {
                MemoryStream archiveStream;
                {
                    using var progressBar = CreateProgressBar();
                    progressBar.Title = "Reading...";
                    //progressBar.Message = path;
                    archiveStream = Utilities.ReadFile(path, progressBar);
                }

                var inputLength = archiveStream.Length;


                // TODO: use short Open method with only mode as parameter
                bool wasModified;
                {
                    using var progressBar = CreateProgressBar();
                    using var archive = ArcArchive.Open(archiveStream, new ArcArchiveOptions { Mode = ArcArchiveMode.Update });
                    ProcessArchive(archive, progressBar);
                    wasModified = archive.Modified;
                }

                var outputLength = archiveStream.Length;

                // Save...
                if (wasModified)
                {
                    // TODO: Write to the temp file, and then rename/replace file.
                    using var progressBar = CreateProgressBar();
                    progressBar.Title = "Writing...";
                    //progressBar.Message = path;
                    Utilities.ReplaceFile(path, archiveStream, progressBar);
                }

                // TODO: Customize output.
                var percentageWin = (double)outputLength / inputLength * 100.0;
                Console.Out.WriteLine($"[done] Processed: {path}");
                Console.Out.WriteLine($"    {outputLength - inputLength:N0} bytes, {percentageWin:N1}%");
            }
            else
            {
                // TODO: Open stream
                using var progressBar = CreateProgressBar();
                using var archive = ArcArchive.Open(path, new ArcArchiveOptions { Mode = ArcArchiveMode.Update });
                ProcessArchive(archive, progress: null);

                // TODO: InputLength, OutputLength properties
            }
        }

        protected abstract void ProcessArchive(ArcArchive archive, IProgress? progress);
    }
}
