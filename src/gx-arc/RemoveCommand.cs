using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO;

using Glacie.Data.Arc;

namespace Glacie.Tools.Arc
{
    internal sealed class RemoveCommand : CommandBase
    {
        public string ArchivePath { get; }
        public IReadOnlyList<string> EntryNames { get; }
        public bool SafeWrite { get; }
        public bool PreserveCase { get; }

        public RemoveCommand(InvocationContext context,
            string archive,
            List<string> entry,
            bool safeWrite,
            bool preserveCase)
            : base(context)
        {
            ArchivePath = archive;
            EntryNames = entry;
            SafeWrite = safeWrite;
            PreserveCase = preserveCase;
        }

        public int Run()
        {
            if (!File.Exists(ArchivePath))
            {
                // TODO: Use common error helpers, or validators.
                Console.Error.WriteLine("File does not exist: " + ArchivePath);
                return 1;
            }

            Check.That(EntryNames.Count > 0);

            {
                using var progressBar = CreateProgressBar();
                progressBar.Title = "Removing...";

                using var archive = ArcArchive.Open(ArchivePath, new ArcArchiveOptions
                {
                    Mode = ArcArchiveMode.Update,
                    SafeWrite = SafeWrite,
                });
                progressBar.SetMaxValue(EntryNames.Count);

                // TODO: Glob support needed.
                foreach (var entryName in EntryNames)
                {
                    progressBar.Message = entryName;

                    // TODO: Use common helper.
                    var normalizedEntryName = entryName.Replace('\\', '/');
                    if (!PreserveCase)
                    {
                        normalizedEntryName = normalizedEntryName.ToLowerInvariant();
                    }

                    if (archive.TryGetEntry(normalizedEntryName, out var entry))
                    {
                        entry.Remove();
                        // TODO: Log result
                    }
                }
            }

            return 0;
        }
    }
}
