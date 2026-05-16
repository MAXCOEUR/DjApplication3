using CSCore;
using CSCore.Codecs;
using CSCore.Streams;
using DjApplication3.Infrastructure;
using DjApplication3.model;
using DjApplication3.outils;
using System;
using System.Collections.Generic;
using System.IO;

namespace DjApplication3.DataSource
{
    public class GraphiqueDataSource
    {
        public sbyte[] getWaveForme(Musique musique)
        {
            try
            {
                return ReadWaveFormeWithCsCore(musique.url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Waveform directe impossible: {ex.Message}");
                return getWaveFormeWithFfmpegFallback(musique.url);
            }
        }

        private sbyte[] getWaveFormeWithFfmpegFallback(string filePath)
        {
            string? temporaryWave = null;
            try
            {
                AppPaths.EnsureRuntimeDirectories();
                temporaryWave = Path.Combine(AppPaths.TempMusicDirectory, $"analysis-wave-{Guid.NewGuid():N}.wav");
                FFmpegGestion.ConvertAudioToWave(filePath, temporaryWave).GetAwaiter().GetResult();
                return ReadWaveFormeWithCsCore(temporaryWave);
            }
            finally
            {
                if (temporaryWave != null && File.Exists(temporaryWave))
                {
                    try
                    {
                        File.Delete(temporaryWave);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private sbyte[] ReadWaveFormeWithCsCore(string filePath)
        {
            const int bufferSize = 8192;
            const int targetPoints = 8192;

            using IWaveSource waveSource = CodecFactory.Instance.GetCodec(filePath);
            using var sampleSource = waveSource.ToSampleSource();

            var buffer = new float[bufferSize];
            var waveform = new List<sbyte>();
            var totalSamples = Math.Max(1, waveSource.Length / Math.Max(1, waveSource.WaveFormat.BytesPerSample));
            var samplesPerPoint = Math.Max(1.0, totalSamples / (double)targetPoints);
            var nextPointAt = samplesPerPoint;
            long samplePosition = 0;
            double absoluteSum = 0;
            double absolutePeak = 0;
            var bucketSamples = 0;

            int read;
            while ((read = sampleSource.Read(buffer, 0, buffer.Length)) > 0)
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

            if (waveform.Count == 0)
            {
                throw new InvalidOperationException("Aucun sample lisible pour generer la waveform.");
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
