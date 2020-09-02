using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Globalization;
using System.Linq;
using System.Threading;

using Glacie.Data.Arc.Infrastructure;
using Glacie.Data.Compression;

namespace Glacie.Tools.Arc
{
    // TODO: [ ] List - extended

    // TODO: (VeryLow) (gx-arc) How to pass custom objects like ProgressConsole? into commands? We should register it and as service, or similar. (But command line is weird.)
    // TODO: (Medium) (gx-arc) Internal options: header-area, chunk-size / etc...
    // TODO: (Medium) (gx-arc) Glob support. Glob might be useful in other projects too (ArzDatabase/Context), so it is probably better to have it as library.
    // TODO: (gx-arc) Add use-libdeflate global option (by default is auto/false/true - enumeration needed?).

    internal static class Program
    {
        private static int Main(string[] args)
        {
            var rootCommand = new RootCommand("Glacie Archive Tool")
            {
                // Keep list sorted.
                CreateListCommand(),
                CreateInfoCommand(),
                CreateVerifyCommand(),

                CreateExtractCommand(),

                CreateOptimizeCommand(),
                CreateRebuildCommand(),

                CreateAddCommand(),
                CreateReplaceCommand(),
                CreateUpdateCommand(),
                CreateRemoveMissingCommand(),
                CreateRemoveCommand(),

            };
            return rootCommand.Invoke(args);
        }

        private static Command CreateListCommand()
        {
            var command = new Command("list", "Lists contents of archive.")
            {
                new Argument<string>("archive", "Path to ARC file.")
                  // TODO: Use validator like ExistingOnly like for FileInfo?
            };
            command.AddAlias("ls");
            command.Handler = CommandHandler.Create((ListCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateExtractCommand()
        {
            var command = new Command("extract", "Extract contents of archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Option<string>("--output-path", () => ".", "Path to destination directory."),
                new Option<bool>("--set-last-write-time", () => false, "Restore last write time file attribute from archive."),
            };
            command.Handler = CommandHandler.Create((ExtractCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateInfoCommand()
        {
            var command = new Command("info", "Technical information about archive.")
            {
                new Argument<string>("archive", "Path to ARC file.")
            };
            command.Handler = CommandHandler.Create((InfoCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateVerifyCommand()
        {
            var command = new Command("verify", "Test integrity of archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
            };
            command.AddAlias("test");
            command.Handler = CommandHandler.Create((VerifyCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateOptimizeCommand()
        {
            var command = new Command("optimize", "Optimize archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                // TODO: Use common options
                new Option<bool>("--repack", () => false,  "Recompress data chunks. This doesn't turn uncompressed entries into compressed."),
                new Option<CompressionLevel>("--compression-level",
                    ParseCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--defragment", () => true,  "Defragment archive."),
                new Option<bool>("--safe-write", () => true,  "When enabled, perform all operations over archive in-memory, and write archive content to disk only when done. This requires more memory, but archive will not be corrupted if you break or cancel operation.\nWhen disabled - perform in-place archive updates."),
            };
            command.Handler = CommandHandler.Create((OptimizeCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateRebuildCommand()
        {
            var command = new Command("rebuild", "Rebuild archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Option<ArcFileFormat>("--format",
                    ParseFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--preserve-store", () => true, "Preserve uncompressed entries."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((RebuildCommand cmd) => cmd.Run());
            return command;
        }


        private static Command CreateAddCommand()
        {
            var command = new Command("add", "Add a file or directory. If a file is already in the archive it will not be added.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("input", "Input files or directories.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<string>("--relative-to", () => ".", "Specifies base directory (entry names will be generated relative to this path)."),
                new Option<ArcFileFormat>("--format",
                    ParseFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive. This option enables creating archives with preserved case."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((AddCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateReplaceCommand()
        {
            var command = new Command("replace", "Replace a file or directory. If a file is already in the archive it will be overwritten.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("input", "Input files or directories.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<string>("--relative-to", () => ".", "Specifies base directory (entry names will be generated relative to this path)."),
                new Option<ArcFileFormat>("--format",
                    ParseFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive. This option enables creating archives with preserved case."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((ReplaceCommand cmd) => cmd.Run());
            return command;
        }


        private static Command CreateUpdateCommand()
        {
            var command = new Command("update", "Update a file or directory. Files will only be added if they are newer than those already in the archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("input", "Input files or directories.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<string>("--relative-to", () => ".", "Specifies base directory (entry names will be generated relative to this path)."),
                new Option<ArcFileFormat>("--format",
                    ParseFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive. This option enables creating archives with preserved case."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((UpdateCommand cmd) => cmd.Run());
            return command;
        }

        // TODO: It accept only existing archive, so remove unrelated options, and enfroce this in command.
        private static Command CreateRemoveMissingCommand()
        {
            var command = new Command("remove-missing", "Remove the files that are not in the specified inputs.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("input", "Input files or directories.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<string>("--relative-to", () => ".", "Specifies base directory (entry names will be generated relative to this path)."),
                new Option<ArcFileFormat>("--format",
                    ParseFileFormat, isDefault: true,
                    "Archive file format. Non-automatic value required when you create new archive. Valid values are 1 or 3 or use game type tags.")
                    .AddSuggestions("auto", "1", "tq", "tqit", "tqae", "3", "gd"),
                new Option<CompressionLevel>("--compression-level",
                    ParseCompressionLevel, isDefault: true,
                    description: "Compression level. Valid values from 0 or 'no' (no compression), 1..12 from 'fastest' to 'maximum'."),
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive. This option enables creating archives with preserved case."),
                new Option<int>("--header-area-size", "Size of header area. Default is 2048."),
                new Option<int>("--chunk-size", "Chunk length. Default is 262144."),
            };
            command.Handler = CommandHandler.Create((RemoveMissingCommand cmd) => cmd.Run());
            return command;
        }

        private static Command CreateRemoveCommand()
        {
            var command = new Command("remove", "Remove a file from the archive.")
            {
                new Argument<string>("archive", "Path to ARC file."),
                new Argument<List<string>>("entry", "Entry names to remove.")
                {
                    Arity = ArgumentArity.OneOrMore,
                },
                new Option<bool>("--safe-write", () => true, "When enabled, avoid to perform destructive operations."),
                new Option<bool>("--preserve-case", () => false, "Entry names by default is case-insensitive. This option enables creating archives with preserved case."),
            };
            command.Handler = CommandHandler.Create((RemoveCommand cmd) => cmd.Run());
            return command;
        }

        private static ArcFileFormat ParseFileFormat(ArgumentResult result)
        {
            if (result.Tokens.Count == 0) return default;

            var tokenValue = result.Tokens.Single().Value;
            switch (tokenValue.ToLowerInvariant())
            {
                case "auto":
                    return default;

                case "1":
                case "tq":
                case "tqit":
                case "tqae":
                    return ArcFileFormat.FromVersion(1);

                case "3":
                case "gd":
                    return ArcFileFormat.FromVersion(3);

                default:
                    result.ErrorMessage = "Cannot parse argument '{0}' for option '{1}'.".FormatWith(tokenValue, result.Argument.Name);
                    return default;
            }
        }

        private static CompressionLevel ParseCompressionLevel(ArgumentResult result)
        {
            if (result.Tokens.Count == 0) return CompressionLevel.Maximum;

            var tokenValue = result.Tokens.Single().Value;
            if (int.TryParse(tokenValue, NumberStyles.None, CultureInfo.InvariantCulture, out var intValue))
            {
                if (0 <= intValue && intValue <= (int)CompressionLevel.Maximum)
                {
                    return (CompressionLevel)intValue;
                }
                else
                {
                    result.ErrorMessage = "Cannot parse argument '{0}' for option '{1}'.".FormatWith(tokenValue, result.Argument.Name);
                    return default;
                }
            }

            switch (tokenValue.ToLowerInvariant())
            {
                case "none":
                case "no":
                case "nocompression":
                case "store":
                    return CompressionLevel.NoCompression;

                case "fastest":
                    return CompressionLevel.Fastest;

                case "max":
                case "maximum":
                    return CompressionLevel.Maximum;

                default:
                    result.ErrorMessage = "Cannot parse argument '{0}' for option '{1}'.".FormatWith(tokenValue, result.Argument.Name);
                    return default;
            }
        }
    }
}
