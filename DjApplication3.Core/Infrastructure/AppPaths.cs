using System;
using System.IO;

namespace DjApplication3.Infrastructure
{
    public static class AppPaths
    {
        public static string BaseDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        public static string MusicDirectory => Path.Combine(BaseDirectory, "musique");

        public static string TempMusicDirectory => Path.Combine(MusicDirectory, "tmp");

        public static string PreviewMusicDirectory => Path.Combine(TempMusicDirectory, "preview");

        public static string ExternalToolsDirectory => Path.Combine(BaseDirectory, "outilsExtern");

        public static string FfmpegDirectory => Path.Combine(ExternalToolsDirectory, "ffmpeg");

        public static string SessionCookieFile => Path.Combine(ExternalToolsDirectory, "session_cookies.txt");

        public static string YtDlpCookieFile => Path.Combine(ExternalToolsDirectory, "ytdlp_cookies.txt");

        public static string SettingsFile => Path.Combine(ExternalToolsDirectory, "settings.json");

        public static string PlayedMusicFile => Path.Combine(ExternalToolsDirectory, "played_music.json");

        public static string LogDirectory => Path.Combine(ExternalToolsDirectory, "logs");

        public static string ErrorLogFile => Path.Combine(LogDirectory, "errors.log");

        public static void EnsureRuntimeDirectories()
        {
            Directory.CreateDirectory(MusicDirectory);
            Directory.CreateDirectory(TempMusicDirectory);
            Directory.CreateDirectory(PreviewMusicDirectory);
            Directory.CreateDirectory(ExternalToolsDirectory);
            Directory.CreateDirectory(LogDirectory);
        }

        public static void CleanupTempMusicDirectory()
        {
            if (!Directory.Exists(TempMusicDirectory))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(TempMusicDirectory, "*", SearchOption.AllDirectories))
            {
                TryDeleteFile(file);
            }

            foreach (var directory in Directory.GetDirectories(TempMusicDirectory, "*", SearchOption.AllDirectories))
            {
                TryDeleteDirectory(directory);
            }
        }

        private static void TryDeleteFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
            }
        }

        private static void TryDeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0)
                {
                    Directory.Delete(directory);
                }
            }
            catch
            {
            }
        }
    }
}
