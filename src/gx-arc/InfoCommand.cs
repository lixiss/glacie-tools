using System;
using System.Buffers;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO;
using System.Text;

using Glacie.ChecksumAlgorithms;
using Glacie.Data.Arc;
using Glacie.Data.Arc.Infrastructure;

namespace Glacie.Tools.Arc
{
    internal sealed class InfoCommand : CommandBase
    {
        public string Path { get; }

        public InfoCommand(InvocationContext context,
            string archive)
            : base(context)
        {
            Path = archive;
        }

        public int Run()
        {
            if (File.Exists(Path))
            {
                InfoArchive(Path);
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

        private void InfoArchive(string path)
        {
            using var archive = ArcArchive.Open(path);
            InfoArchive(archive);
        }

        private void InfoArchive(ArcArchive archive)
        {
            var sb = new StringBuilder();

            var format = archive.GetFormat();
            sb.AppendFormat("                       Format: {0} ({1}) ({2})\n", format.Version, FormatTag(format), FormatCapabilities(format));
            sb.AppendFormat("\n");

            int nonCompressedEntryCount = 0;
            long totalLength = 0;
            long totalCompressedLength = 0;
            foreach (var entry in archive.GetEntries())
            {
                totalLength += entry.Length;
                totalCompressedLength += entry.CompressedLength;

                nonCompressedEntryCount += (int)entry.EntryType == 1 ? 1 : 0;
            }

            double totalCompressedLengthPercentage = 0;
            if (totalCompressedLength > 0)
            {
                totalCompressedLengthPercentage = (double)totalCompressedLength / totalLength * 100.0;
            }

            var li = archive.GetLayoutInfo();
            sb.AppendFormat("                 # of Entries: {0:N0}\n", li.EntryCount);
            sb.AppendFormat("                 Total Length: {0:N0} bytes\n", totalLength);
            sb.AppendFormat("      Total Compressed Length: {0:N0} bytes ({1:N1}%)\n",
                totalCompressedLength,
                totalCompressedLengthPercentage);
            sb.AppendFormat("  # of Non-Compressed Entries: {0:N0}\n", nonCompressedEntryCount);
            sb.AppendFormat("         # of Removed Entries: {0:N0}\n", li.RemovedEntryCount);
            sb.AppendFormat("\n");
            sb.AppendFormat("                  # of Chunks: {0:N0}\n", li.ChunkCount);
            sb.AppendFormat("             # of Used Chunks: {0:N0}\n", li.LiveChunkCount);
            sb.AppendFormat("        # of Unordered Chunks: {0:N0}\n", li.UnorderedChunkCount);
            sb.AppendFormat("\n");
            sb.AppendFormat("           # of Free Segments: {0:N0}\n", li.FreeSegmentCount);
            sb.AppendFormat("           Free Segment Space: {0:N0} bytes\n", li.FreeSegmentBytes);
            sb.AppendFormat("\n");
            sb.AppendFormat("                  Can Compact: {0}\n", li.CanCompact ? "Yes" : "No");
            sb.AppendFormat("               Can Defragment: {0}\n", li.CanDefragment ? "Yes" : "No");

            Console.Out.Write(sb.ToString());
        }

        private static string FormatTag(ArcFileFormat format) =>
            format.Version switch
            {
                1 => "tq",
                3 => "gd",
                _ => throw Error.Unreachable(),
            };

        private static string FormatCapabilities(ArcFileFormat format)
        {
            var sb = new StringBuilder();
            if (format.ZlibCompression)
            {
                sb.Append("zlib");
            }
            else if (format.Lz4Compression)
            {
                sb.Append("lz4");
            }

            if (format.SupportStoreChunks)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append("store-chunk");
            }
            return sb.ToString();
        }
    }
}
 