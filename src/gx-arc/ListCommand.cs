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
    internal sealed class ListCommand : CommandBase
    {
        public string Path { get; }

        public ListCommand(InvocationContext context,
            string archive)
            : base(context)
        {
            Path = archive;
        }

        public int Run()
        {
            if (File.Exists(Path))
            {
                ListArchive(Path);
                return 0;
            }
            else
            {
                // TODO: Glob?

                // TODO: Use common error helpers, or validators.
                Console.Error.WriteLine("File does not exist: " + Path);
                // File does not exist: ./Creatures.arc1
                return 1;
            }
        }

        private void ListArchive(string path)
        {
            using var archive = ArcArchive.Open(path);
            ListArchive(archive);
        }

        private void ListArchive(ArcArchive archive)
        {
            foreach (var entry in archive.GetEntries())
            {
                Console.Out.WriteLine(entry.Name);
            }
        }
    }
}
