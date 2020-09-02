using System.CommandLine.Invocation;

using Glacie.Data.Arc;
using Glacie.Data.Compression;

namespace Glacie.Tools.Arc
{
    internal sealed class OptimizeCommand : ProcessArchiveCommand
    {
        private readonly bool _defragment;
        private readonly bool _repack;
        private readonly CompressionLevel _compressionLevel;

        public OptimizeCommand(InvocationContext context,
            string archive,
            bool repack,
            CompressionLevel compressionLevel,
            bool defragment,
            bool safeWrite)
            : base(context, archive, safeWrite)
        {
            _repack = repack;
            _compressionLevel = compressionLevel;
            _defragment = defragment;
        }

        protected override void ProcessArchive(ArcArchive archive, IProgress? progress)
        {
            if (_defragment)
            {
                progress?.SetTitle("Defragmenting...");
                archive.Defragment(progress);
            }

            if (_repack)
            {
                progress?.SetTitle("Repacking...");
                archive.Repack(_compressionLevel, progress);
            }
            else
            {
                progress?.SetTitle("Compacting...");
                archive.Compact(progress);
            }
        }
    }
}
