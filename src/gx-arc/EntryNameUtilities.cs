using System.IO;

namespace Glacie.Tools.Arc
{
    internal static class EntryNameUtilities
    {
        public static void Validate(string value)
        {
            if (value.StartsWith("/"))
            {
                throw Error.InvalidOperation("Entry name must not be rooted. You should specify relative-to option.");
            }
            else if (value.StartsWith("./"))
            {
                throw Error.InvalidOperation("Entry name is relative.");
            }
            else if (value.StartsWith("../"))
            {
                throw Error.InvalidOperation("Entry name is relative.");
            }
            else if (value.Contains("/./"))
            {
                throw Error.InvalidOperation("Entry name contains relative segment.");
            }
            else if (value.Contains("/../"))
            {
                throw Error.InvalidOperation("Entry name contains relative segment.");
            }
            else if (value.EndsWith("/."))
            {
                throw Error.InvalidOperation("Entry name ends with special name.");
            }
            else if (value.EndsWith("/.."))
            {
                throw Error.InvalidOperation("Entry name ends with special name.");
            }
            else if (Path.IsPathFullyQualified(value))
            {
                throw Error.InvalidOperation("Entry name must not be fully qualified. You should specify relative-to option.");
            }
            else if (Path.IsPathRooted(value))
            {
                throw Error.InvalidOperation("Entry name must not be rooted. You should specify relative-to option.");
            }

            // TODO: check special characters, check non-ascii characters
        }
    }
}
