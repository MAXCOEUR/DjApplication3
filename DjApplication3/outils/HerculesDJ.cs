using DjApplication3.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib.WavPack;

namespace DjApplication3.outils
{
    internal class HerculesDJ
    {
        public event EventHandler eventPlayPauseLeft;
        public event EventHandler eventPlayPauseRight;
        public event EventHandler eventCasqueLeft;
        public event EventHandler eventCasqueRight;
        public event EventHandler<int> eventPisteLeft;
        public event EventHandler<int> eventPisteRight;
        public event EventHandler<float> eventVolumeLeft;
        public event EventHandler<float> eventVolumeRight;
        public event EventHandler<float> eventMixe;
        public event EventHandler<int> eventScratchLeft;
        public event EventHandler<int> eventScratchRight;
        Process process;

        bool isPressSratchLeft = false;
        bool isPressSratchRight = false;
        public async void start()
        {
            if (!System.IO.File.Exists(SettingsManager.Instance.pathTShark))
            {
                return;
            }
            // Créer un processus pour exécuter tshark
            process = new Process();
            process.StartInfo.FileName = SettingsManager.Instance.pathTShark;
            process.StartInfo.Arguments = $"-i USBPcap1 -Y \"usb.src contains {SettingsManager.Instance.numeroUSB}\" -x -l";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            // Rediriger la sortie standard pour la lecture
            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                {
                    return;
                }
                string[] split = e.Data.Split(' ');
                if (split.Length == 0 || split[0] != "0010")
                {
                    return;
                }

                string param1 = split[14];
                string param2 = split[15];
                string param3 = split[16];

                if (param1 != "90" && param1 != "b0")
                {
                    return;
                }
                buttonHercules(param1, param2, param3);
            };

            // Démarrer le processus et commencer à lire la sortie
            process.Start();
            process.BeginOutputReadLine();
        }
        public void Dispose()
        {
            process?.Dispose();
        }


        private void buttonHercules(string param1,string param2,string param3)
        {
            int resultat = Convert.ToInt32(param3, 16);

            switch (param1)
            {
                case "90":
                    switch (param2)
                    {
                        case "30":
                            if(resultat == 127)
                            {
                                eventPlayPauseRight?.Invoke(this, EventArgs.Empty);
                            }
                            Console.WriteLine("Droite playPause " + resultat);
                            break;
                        case "32":
                            if (resultat == 127)
                            {
                                eventCasqueRight?.Invoke(this, EventArgs.Empty);
                            }
                            Console.WriteLine("Droite casque "+ resultat);
                            break;
                        case "23":
                            if (resultat == 127)
                            {
                                eventPisteRight?.Invoke(this, 1);
                            }
                            Console.WriteLine("Droite piste1 "+ resultat);
                            break;
                        case "24":
                            if (resultat == 127)
                            {
                                eventPisteRight?.Invoke(this, 2);
                            }
                            Console.WriteLine("Droite piste2 "+ resultat);
                            break;
                        case "25":
                            if (resultat == 127)
                            {
                                eventPisteRight?.Invoke(this, 3);
                            }
                            Console.WriteLine("Droite piste3 "+ resultat);
                            break;
                        case "26":
                            if (resultat == 127)
                            {
                                eventPisteRight?.Invoke(this, 4);
                            }
                            Console.WriteLine("Droite piste4 "+ resultat);
                            break;
                        case "34":
                            if (resultat == 127)
                            {
                                isPressSratchRight = true;
                            }
                            else
                            {
                                isPressSratchRight = false;
                            }
                            Console.WriteLine("Droite scrachbutton " + resultat);
                            break;

                        case "16":
                            if (resultat == 127)
                            {
                                eventPlayPauseLeft?.Invoke(this, EventArgs.Empty);
                            }
                            Console.WriteLine("Gauche playPause " + resultat);
                            break;
                        case "18":
                            if (resultat == 127)
                            {
                                eventCasqueLeft?.Invoke(this, EventArgs.Empty);
                            }
                            Console.WriteLine("Gauche casque " + resultat);
                            break;
                        case "09":
                            if (resultat == 127)
                            {
                                eventPisteLeft?.Invoke(this, 1);
                            }
                            Console.WriteLine("Gauche piste1 " + resultat);
                            break;
                        case "0a":
                            if (resultat == 127)
                            {
                                eventPisteLeft?.Invoke(this, 2);
                            }
                            Console.WriteLine("Gauche piste2 " + resultat);
                            break;
                        case "0b":
                            if (resultat == 127)
                            {
                                eventPisteLeft?.Invoke(this, 3);
                            }
                            Console.WriteLine("Gauche piste3 " + resultat);
                            break;
                        case "0c":
                            if (resultat == 127)
                            {
                                eventPisteLeft?.Invoke(this, 4);
                            }
                            Console.WriteLine("Gauche piste4 " + resultat);
                            break;
                        case "1a":
                            if (resultat == 127)
                            {
                                isPressSratchLeft = true;
                            }
                            else
                            {
                                isPressSratchLeft = false;
                            }
                            Console.WriteLine("Gauche scrachbutton " + resultat);
                            break;
                    }
                    break;
                case "b0":
                    switch (param2)
                    {
                        case "31":
                            eventScratchRight?.Invoke(this, resultat);
                            Console.WriteLine("Droite scrach " + resultat);
                            break;
                        case "3b":
                            eventVolumeRight?.Invoke(this, resultat/127.0F);
                            Console.WriteLine("Droite volume " + resultat / 127.0F);
                            break;

                        case "30":
                            eventScratchLeft?.Invoke(this, resultat);
                            Console.WriteLine("Gauche scrach " + resultat);
                            break;
                        case "36":
                            eventVolumeLeft?.Invoke(this, resultat / 127.0F);
                            Console.WriteLine("Gauche volume " + resultat / 127.0F);
                            break;

                        case "3a":
                            eventMixe?.Invoke(this, resultat / 127.0F);
                            Console.WriteLine("mixage " + resultat / 127.0F);
                            break;
                    }
                    break;
            }
        }
    }
}
