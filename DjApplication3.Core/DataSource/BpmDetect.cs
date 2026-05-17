using DjApplication3.Infrastructure;
using DjApplication3.outils;
using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace DjApplication3.DataSource
{
    struct BPMGroup
    {
        public int Count;
        public short Tempo;
    }

    internal class BpmDetect
    {
        private BPMGroup[] groups = Array.Empty<BPMGroup>();
        private int sampleRate;
        private int channels;

        public BPMGroup[] Groups => groups;

        private struct Peak
        {
            public int Position;
            public float Volume;
        }

        public int getBpm(string filePath)
        {
            try
            {
                CalculateGroups(filePath);
                if (Groups.Length > 0)
                {
                    return Groups[0].Tempo;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning(ex, $"Direct BPM analysis failed for {Path.GetFileName(filePath)}");
                Console.WriteLine(ex.ToString());
            }

            return getBpmWithFfmpegFallback(filePath);
        }

        private int getBpmWithFfmpegFallback(string filePath)
        {
            string? temporaryWave = null;
            try
            {
                AppPaths.EnsureRuntimeDirectories();
                temporaryWave = Path.Combine(AppPaths.TempMusicDirectory, $"analysis-bpm-{Guid.NewGuid():N}.wav");
                FFmpegGestion.ConvertAudioToWave(filePath, temporaryWave).GetAwaiter().GetResult();
                CalculateGroups(temporaryWave);
                return Groups.Length > 0 ? Groups[0].Tempo : 0;
            }
            catch (Exception ex)
            {
                AppLogger.Warning(ex, $"FFmpeg BPM fallback failed for {Path.GetFileName(filePath)}");
                Console.WriteLine(ex.ToString());
                return 0;
            }
            finally
            {
                if (temporaryWave != null && File.Exists(temporaryWave))
                {
                    try
                    {
                        File.Delete(temporaryWave);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Warning(ex, $"Temporary BPM file cleanup failed for {Path.GetFileName(temporaryWave)}");
                    }
                }
            }
        }

        private Peak[] getPeaks(float[] data)
        {
            int partSize = sampleRate / 2;
            int parts = data.Length / channels / partSize;
            Peak[] peaks = new Peak[parts];

            for (int i = 0; i < parts; ++i)
            {
                Peak max = new Peak
                {
                    Position = -1,
                    Volume = 0.0F
                };
                for (int j = 0; j < partSize; ++j)
                {
                    float vol = 0.0F;
                    for (int k = 0; k < channels; ++k)
                    {
                        float v = data[i * channels * partSize + j * channels + k];
                        if (vol < v)
                        {
                            vol = v;
                        }
                    }
                    if (max.Position == -1 || max.Volume < vol)
                    {
                        max.Position = i * partSize + j;
                        max.Volume = vol;
                    }
                }
                peaks[i] = max;
            }

            Array.Sort(peaks, (x, y) => y.Volume.CompareTo(x.Volume));
            Array.Resize(ref peaks, peaks.Length / 2);
            Array.Sort(peaks, (x, y) => x.Position.CompareTo(y.Position));

            return peaks;
        }

        private BPMGroup[] getIntervals(Peak[] peaks)
        {
            List<BPMGroup> groups = new List<BPMGroup>();

            for (int index = 0; index < peaks.Length; ++index)
            {
                Peak peak = peaks[index];
                for (int i = 1; index + i < peaks.Length && i < 10; ++i)
                {
                    float tempo = 60.0F * sampleRate / (peaks[index + i].Position - peak.Position);
                    while (tempo < 90.0F)
                    {
                        tempo *= 2.0F;
                    }
                    while (tempo > 180.0F)
                    {
                        tempo /= 2.0F;
                    }
                    BPMGroup group = new BPMGroup
                    {
                        Count = 1,
                        Tempo = (short)Math.Round(tempo)
                    };
                    int j;
                    for (j = 0; j < groups.Count && groups[j].Tempo != group.Tempo; ++j) { }
                    if (j < groups.Count)
                    {
                        group.Count = groups[j].Count + 1;
                        groups[j] = group;
                    }
                    else
                    {
                        groups.Add(group);
                    }
                }
            }
            return groups.ToArray();
        }

        public void CalculateGroups(string audioFile, int start = 0, int length = 0)
        {
            using (MediaFoundationReader reader = new MediaFoundationReader(audioFile))
            {
                sampleRate = reader.WaveFormat.SampleRate;
                channels = reader.WaveFormat.Channels;

                int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                if (bytesPerSample == 0)
                {
                    bytesPerSample = 2;
                }

                int sampleCount = (int)reader.Length / bytesPerSample;

                start *= channels * sampleRate;
                length *= channels * sampleRate;
                if (start >= sampleCount)
                {
                    groups = Array.Empty<BPMGroup>();
                    return;
                }
                if (length == 0 || start + length >= sampleCount)
                {
                    length = sampleCount - start;
                }

                length = (int)(length / channels) * channels;

                ISampleProvider sampleReader = reader.ToSampleProvider();
                float[] samples = new float[length];
                sampleReader.Read(samples, start, length);

                for (int ch = 0; ch < channels; ++ch)
                {
                    BiQuadFilter lowpass = BiQuadFilter.LowPassFilter(sampleRate, 150.0F, 1.0F);
                    BiQuadFilter highpass = BiQuadFilter.HighPassFilter(sampleRate, 100.0F, 1.0F);

                    for (int i = ch; i < length; i += channels)
                    {
                        samples[i] = highpass.Transform(lowpass.Transform(samples[i]));
                    }
                }

                Peak[] peaks = getPeaks(samples);
                BPMGroup[] allGroups = getIntervals(peaks);
                Array.Sort(allGroups, (x, y) => y.Count.CompareTo(x.Count));

                if (allGroups.Length > 5)
                {
                    Array.Resize(ref allGroups, 5);
                }

                groups = allGroups;
            }
        }
    }
}
