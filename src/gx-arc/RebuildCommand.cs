using System.Buffers;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO;

using Glacie.Data.Arc;
using Glacie.Data.Arc.Infrastructure;
using Glacie.Data.Compression;

namespace Glacie.Tools.Arc
{
    internal sealed class RebuildCommand : CommandBase
    {
        public string ArchivePath { get; }
        public ArcFileFormat Format { get; }
        public CompressionLevel CompressionLevel { get; }
        public bool PreserveStore { get; }
        public bool SafeWrite { get; }
        public int? HeaderAreaSize { get; }
        public int? ChunkSize { get; }

        public RebuildCommand(InvocationContext context,
            string archive,
            ArcFileFormat format,
            CompressionLevel compressionLevel,
            bool preserveStore,
            bool safeWrite,
            int? headerAreaSize = null,
            int? chunkSize = null)
            : base(context)
        {
            ArchivePath = archive;
            Format = format;
            CompressionLevel = compressionLevel;
            PreserveStore = preserveStore;
            SafeWrite = safeWrite;
            HeaderAreaSize = headerAreaSize;
            ChunkSize = chunkSize;
        }

        public int Run()
        {
            if (!File.Exists(ArchivePath))
            {
                Console.Error.WriteLine("File does not exist: " + ArchivePath);
                return 1;
            }

            RebuildArchive(ArchivePath);
            return 0;
        }

        private void RebuildArchive(string path)
        {
            var archiveOptions = new ArcArchiveOptions
            {
                Mode = ArcArchiveMode.Create,
                // Layout = Format,
                CompressionLevel = CompressionLevel,
                SafeWrite = false,
                HeaderAreaLength = HeaderAreaSize,
                ChunkLength = ChunkSize,
                // TODO: UseLibDeflate = setup from global option
            };

            using var progressBar = CreateProgressBar();
            using var inputArchive = ArcArchive.Open(ArchivePath, ArcArchiveMode.Read);

            if (Format == default)
            {
                archiveOptions.Format = inputArchive.GetFormat();
            }
            else
            {
                archiveOptions.Format = Format;
            }

            long totalLength = 0;
            foreach (var entry in inputArchive.GetEntries())
            {
                totalLength += entry.Length;
            }

            progressBar.SetMaxValue(totalLength);

            if (true || SafeWrite)
            {
                var outputStream = new MemoryStream();
                using var outputArchive = ArcArchive.Open(outputStream, archiveOptions);

                CopyEntries(inputArchive, outputArchive, progressBar);

                outputArchive.Dispose();
                inputArchive.Dispose();

                Utilities.ReplaceFile(ArchivePath, outputStream, progressBar);
            }
            else
            {
                // TODO: implement writing into different file
                throw Error.NotImplemented();
            }
        }

        private void CopyEntries(ArcArchive inputArchive, ArcArchive outputArchive, IProgress? progress)
        {
            foreach (var inputEntry in inputArchive.GetEntries())
            {
                progress?.SetMessage(inputEntry.Name);

                CompressionLevel? compressionLevel = PreserveStore && (int)inputEntry.EntryType == 1
                        ? CompressionLevel.NoCompression
                        : (CompressionLevel?)null;

                var outputEntry = outputArchive.CreateEntry(inputEntry.Name);

                {
                    using var inputStream = inputEntry.Open();
                    using var outputStream = outputEntry.OpenWrite(compressionLevel);
                    CopyStream(inputStream, outputStream, progress);
                }

                outputEntry.Timestamp = inputEntry.Timestamp;
            }
        }

        private void CopyStream(Stream inputStream, Stream outputStream, IProgress? progress)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
            while (true)
            {
                var bytesRead = inputStream.Read(buffer);
                if (bytesRead == 0) break;
                outputStream.Write(buffer, 0, bytesRead);

                progress?.AddValue(bytesRead);
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
