using System.Collections.Generic;
using System.CommandLine.Invocation;

using Glacie.Data.Arc;
using Glacie.Data.Arc.Infrastructure;
using Glacie.Data.Compression;

namespace Glacie.Tools.Arc
{
    internal sealed class RemoveMissingCommand : ProcessFileSystemEntriesCommand
    {
        private readonly HashSet<string> _entryNamesToKeep = new HashSet<string>();

        public RemoveMissingCommand(InvocationContext context,
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
        { }

        protected override string GetProcessInputFilesTitle() => "Removing missing files...";

        protected override void ProcessInputFile(ArcArchive archive, InputFileInfo fileInfo, IProgress? progress)
        {
            if (archive.Exists(fileInfo.EntryName))
            {
                _entryNamesToKeep.Add(fileInfo.EntryName);
            }
        }

        protected override void PostProcessArchive(ArcArchive archive, IProgress? progress)
        {
            progress?.SetMaxValue(archive.Count);

            foreach (var entry in archive.GetEntries())
            {
                if (!_entryNamesToKeep.Contains(entry.Name))
                {
                    entry.Remove();

                    // TODO: Write action into log
                }

                progress?.AddValue(1);
            }
        }
    }
}
