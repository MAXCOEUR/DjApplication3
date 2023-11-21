using DjApplication3.model;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DjApplication3.DataSource
{
    internal class GraphiqueDataSource
    {
        public float[] getWaveForme(Musique musique)
        {
            const int bufferSize = 8192;

            AudioFileReader lecteurAudio = new AudioFileReader(musique.url);

            var buffer = new float[bufferSize];
            var waveform = new List<float>();

            // Calcul du facteur de réduction

            while (lecteurAudio.Read(buffer, 0, buffer.Length) > 0)
            {
                waveform.AddRange(buffer);
            }

            lecteurAudio.Dispose(); // Assurez-vous de libérer les ressources du lecteur temporaire

            return waveform.ToArray();
        }
    }
}
