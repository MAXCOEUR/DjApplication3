using CSCore;
using System;
using System.Threading;

namespace DjApplication3.Services
{
    internal sealed class VariablePlaybackRateSampleSource : ISampleSource
    {
        private readonly ISampleSource _source;
        private readonly int _channels;
        private readonly float[] _previousFrame;
        private readonly float[] _nextFrame;
        private readonly float[] _readBuffer;
        private readonly object _positionLock = new();
        private int _playbackRateBits;
        private bool _hasPreviousFrame;
        private bool _hasNextFrame;
        private double _fraction;

        public VariablePlaybackRateSampleSource(ISampleSource source, float playbackRate)
        {
            _source = source;
            _channels = Math.Max(1, source.WaveFormat.Channels);
            _previousFrame = new float[_channels];
            _nextFrame = new float[_channels];
            _readBuffer = new float[_channels];
            PlaybackRate = playbackRate;
        }

        public float PlaybackRate
        {
            get => BitConverter.Int32BitsToSingle(Volatile.Read(ref _playbackRateBits));
            set
            {
                var bounded = Math.Clamp(value, 0.75f, 1.25f);
                Volatile.Write(ref _playbackRateBits, BitConverter.SingleToInt32Bits(bounded));
            }
        }

        public bool CanSeek => _source.CanSeek;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public long Position
        {
            get => _source.Position;
            set
            {
                lock (_positionLock)
                {
                    _source.Position = value;
                    ResetInterpolation();
                }
            }
        }

        public long Length => _source.Length;

        public int Read(float[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count <= 0)
            {
                return 0;
            }

            lock (_positionLock)
            {
                var samplesWritten = 0;
                var requestedFrames = count / _channels;

                for (var frame = 0; frame < requestedFrames; frame++)
                {
                    if (!EnsureFrames())
                    {
                        break;
                    }

                    var frameOffset = offset + samplesWritten;
                    WriteInterpolatedFrame(buffer, frameOffset);
                    samplesWritten += _channels;

                    AdvanceSourcePosition(PlaybackRate);
                }

                return samplesWritten;
            }
        }

        public void Dispose()
        {
            _source.Dispose();
        }

        private bool EnsureFrames()
        {
            if (!_hasPreviousFrame)
            {
                _hasPreviousFrame = ReadFrame(_previousFrame);
                if (!_hasPreviousFrame)
                {
                    return false;
                }
            }

            if (!_hasNextFrame)
            {
                _hasNextFrame = ReadFrame(_nextFrame);
            }

            return true;
        }

        private void WriteInterpolatedFrame(float[] buffer, int offset)
        {
            if (!_hasNextFrame)
            {
                Array.Copy(_previousFrame, 0, buffer, offset, _channels);
                return;
            }

            for (var channel = 0; channel < _channels; channel++)
            {
                var previous = _previousFrame[channel];
                buffer[offset + channel] = previous + (float)((_nextFrame[channel] - previous) * _fraction);
            }
        }

        private void AdvanceSourcePosition(float playbackRate)
        {
            _fraction += playbackRate;

            if (!_hasNextFrame)
            {
                _hasPreviousFrame = false;
                _fraction = 0;
                return;
            }

            while (_fraction >= 1.0 && _hasNextFrame)
            {
                _fraction -= 1.0;
                Array.Copy(_nextFrame, _previousFrame, _channels);
                _hasNextFrame = ReadFrame(_nextFrame);
            }
        }

        private bool ReadFrame(float[] frame)
        {
            var samplesRead = 0;
            while (samplesRead < _channels)
            {
                var read = _source.Read(_readBuffer, samplesRead, _channels - samplesRead);
                if (read <= 0)
                {
                    break;
                }

                samplesRead += read;
            }

            if (samplesRead == 0)
            {
                return false;
            }

            for (var i = 0; i < _channels; i++)
            {
                frame[i] = i < samplesRead ? _readBuffer[i] : 0f;
            }

            return true;
        }

        private void ResetInterpolation()
        {
            _fraction = 0;
            _hasPreviousFrame = false;
            _hasNextFrame = false;
        }
    }
}
