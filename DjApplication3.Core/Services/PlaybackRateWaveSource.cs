using CSCore;
using System;

namespace DjApplication3.Services
{
    internal sealed class PlaybackRateWaveSource : IWaveSource
    {
        private readonly IWaveSource _source;
        private readonly WaveFormat _waveFormat;

        public PlaybackRateWaveSource(IWaveSource source, float playbackRate)
        {
            _source = source;
            var sourceFormat = source.WaveFormat;
            var adjustedSampleRate = Math.Clamp(
                (int)Math.Round(sourceFormat.SampleRate * Math.Clamp(playbackRate, 0.75f, 1.25f)),
                8000,
                192000);

            _waveFormat = new WaveFormat(
                adjustedSampleRate,
                sourceFormat.BitsPerSample,
                sourceFormat.Channels,
                sourceFormat.WaveFormatTag,
                sourceFormat.ExtraSize);
        }

        public bool CanSeek => _source.CanSeek;

        public WaveFormat WaveFormat => _waveFormat;

        public long Position
        {
            get => _source.Position;
            set => _source.Position = value;
        }

        public long Length => _source.Length;

        public int Read(byte[] buffer, int offset, int count)
            => _source.Read(buffer, offset, count);

        public void Dispose()
        {
            _source.Dispose();
        }
    }
}
