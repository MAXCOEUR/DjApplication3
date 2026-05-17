using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DjApplication3.Infrastructure
{
    public static class AppLogger
    {
        private static readonly object SyncLock = new();

        public static void Error(Exception exception, string context)
            => Write("ERROR", context, exception);

        public static void Warning(Exception exception, string context)
            => Write("WARN", context, exception);

        public static void Info(string message)
            => Write("INFO", message, null);

        private static void Write(string level, string context, Exception? exception)
        {
            var entry = BuildEntry(level, context, exception);

            try
            {
                AppPaths.EnsureRuntimeDirectories();
                lock (SyncLock)
                {
                    File.AppendAllText(AppPaths.ErrorLogFile, entry, Encoding.UTF8);
                }
            }
            catch
            {
                try
                {
                    var fallbackPath = Path.Combine(AppContext.BaseDirectory, "djapplication3-errors.log");
                    lock (SyncLock)
                    {
                        File.AppendAllText(fallbackPath, entry, Encoding.UTF8);
                    }
                }
                catch
                {
                    Debug.WriteLine(entry);
                }
            }
        }

        private static string BuildEntry(string level, string context, Exception? exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine("--------------------------------------------------------------------------------");
            builder.AppendLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {context}");

            if (exception != null)
            {
                builder.AppendLine(exception.ToString());
            }

            builder.AppendLine();
            return builder.ToString();
        }
    }
}
