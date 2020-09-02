using System.Collections.Generic;
using System.CommandLine.Invocation;

using Glacie.Data.Arc;
using Glacie.Data.Arc.Infrastructure;
using Glacie.Data.Compression;

namespace Glacie.Tools.Arc
{
    internal sealed class ReplaceCommand : ProcessFileSystemEntriesCommand
    {
        public ReplaceCommand(InvocationContext context,
            string archive,
            List<string> input,
            string relativeTo,
            ArcFileFormat format,
            CompressionLevel compressionLevel,
            bool safeWrite,
            bool preserveCase,
            int? headerAreaSize = null,
            int? chunkSize = null)
            : base(context, archive, input, relativeTo, format, compressionLevel,
                  safeWrite, preserveCase, headerAreaSize, chunkSize)
        {
        }

        protected override string GetProcessInputFilesTitle() => "Replacing files...";

        protected override void ProcessInputFile(ArcArchive archive, InputFileInfo fileInfo, IProgress? progress)
        {
            if (archive.TryGetEntry(fileInfo.EntryName, out var entry))
            {
                {
                    using var entryStream = entry.OpenWrite();
                    CopyFileToStream(fileInfo.FileName, entryStream, progress);
                }
                entry.LastWriteTime = fileInfo.LastWriteTime;

                // TODO: Console.Out.WriteLine("Added: " + inputFileInfo.EntryName);
            }
            else
            {
                entry = archive.CreateEntry(fileInfo.EntryName);
                {
                    using var entryStream = entry.OpenWrite();
                    CopyFileToStream(fileInfo.FileName, entryStream, progress);
                }
                entry.LastWriteTime = fileInfo.LastWriteTime;

                // TODO: Console.Out.WriteLine("Added: " + inputFileInfo.EntryName);
            }
        }

        protected override void PostProcessArchive(ArcArchive archive, IProgress? progress) { }
    }
}
