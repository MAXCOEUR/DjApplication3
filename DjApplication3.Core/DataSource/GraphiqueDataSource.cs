using DjApplication3.model;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DjApplication3.DataSource
{
    public class GraphiqueDataSource
    {
        public sbyte[] getWaveForme(Musique musique)
        {
            const int bufferSize = 8192;
            const int targetPoints = 8192;

            using AudioFileReader lecteurAudio = new AudioFileReader(musique.url);

            var buffer = new float[bufferSize];
            var waveform = new List<sbyte>();
            var bytesPerSample = lecteurAudio.WaveFormat.BitsPerSample / 8;
            var totalSamples = bytesPerSample > 0
                ? Math.Max(1, lecteurAudio.Length / bytesPerSample)
                : Math.Max(1, lecteurAudio.WaveFormat.SampleRate * lecteurAudio.WaveFormat.Channels);
            var samplesPerPoint = Math.Max(1.0, totalSamples / (double)targetPoints);
            var nextPointAt = samplesPerPoint;
            long samplePosition = 0;
            double absoluteSum = 0;
            double absolutePeak = 0;
            var bucketSamples = 0;

            int read;
            while ((read = lecteurAudio.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    var absolute = Math.Abs(buffer[i]);
                    absoluteSum += absolute;
                    absolutePeak = Math.Max(absolutePeak, absolute);
                    bucketSamples++;
                    samplePosition++;

                    if (samplePosition >= nextPointAt)
                    {
                        waveform.Add(GetEnvelopeValue(absoluteSum, absolutePeak, bucketSamples));
                        absoluteSum = 0;
                        absolutePeak = 0;
                        bucketSamples = 0;
                        nextPointAt += samplesPerPoint;
                    }
                }
            }

            if (bucketSamples > 0)
            {
                waveform.Add(GetEnvelopeValue(absoluteSum, absolutePeak, bucketSamples));
            }

            return waveform.ToArray();
        }

        private static sbyte GetEnvelopeValue(double absoluteSum, double absolutePeak, int sampleCount)
        {
            if (sampleCount <= 0)
            {
                return 0;
            }

            var average = absoluteSum / sampleCount;
            var envelope = (average * 0.85) + (absolutePeak * 0.15);
            return (sbyte)Math.Clamp((int)Math.Round(envelope * 100), 0, 100);
        }
    }
}
