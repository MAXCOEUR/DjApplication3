using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DjApplication3.Infrastructure
{
    public static class SupportedAudioFormats
    {
        private static readonly string[] _extensions =
        {
            ".mp3",
            ".aiff",
            ".aif",
            ".aifc",
            ".aac",
            ".m4a",
            ".flac"
        };

        private static readonly string[] _downloadCachePreference =
        {
            ".m4a",
            ".aac",
            ".mp3",
            ".flac",
            ".aiff",
            ".aif",
            ".aifc"
        };

        private static readonly HashSet<string> _extensionSet = new(_extensions, StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<string> Extensions => _extensions;

        public static bool IsSupported(string? pathOrExtension)
        {
            if (string.IsNullOrWhiteSpace(pathOrExtension))
            {
                return false;
            }

            var extension = pathOrExtension.StartsWith(".", StringComparison.Ordinal)
                ? pathOrExtension
                : Path.GetExtension(pathOrExtension);

            return _extensionSet.Contains(extension);
        }

        public static string? FindExistingAudioFile(string directory, string baseName)
        {
            if (!Directory.Exists(directory))
            {
                return null;
            }

            return _downloadCachePreference
                .Select(extension => Path.Combine(directory, baseName + extension))
                .FirstOrDefault(path => File.Exists(path) && new FileInfo(path).Length > 0);
        }

        public static bool IsM4aContainer(string path)
        {
            var extension = Path.GetExtension(path);
            return extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase);
        }
    }
}
