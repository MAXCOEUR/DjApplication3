using CSCore;
using CSCore.Codecs;
using System;
using System.IO;

namespace DjApplication3.Infrastructure
{
    public static class AudioCompatibility
    {
        public static bool CanReadSamples(string path)
        {
            try
            {
                using var source = CodecFactory.Instance.GetCodec(path);
                if (source.Length <= 0 || source.WaveFormat.Channels <= 0 || source.WaveFormat.SampleRate <= 0)
                {
                    return false;
                }

                var buffer = new byte[Math.Max(source.WaveFormat.BlockAlign, 4096)];
                return source.Read(buffer, 0, buffer.Length) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decode audio impossible pour {Path.GetFileName(path)}: {ex.Message}");
                return false;
            }
        }
    }
}
