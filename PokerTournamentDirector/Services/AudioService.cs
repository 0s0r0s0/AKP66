using System;
using System.IO;
using System.Windows.Media;

namespace PokerTournamentDirector.Services
{
    /// <summary>
    /// Service pour jouer les sons MP3 personnalisés du tournoi
    /// Les sons sont lus directement depuis le dossier "Sounds" dans le répertoire d'exécution
    /// </summary>
    public class AudioService : IDisposable
    {
        private readonly MediaPlayer _mediaPlayer;
        private readonly string _soundsFolder;
        private bool _isInitialized;
        private bool _disposed;

        // Noms des fichiers sons
        public const string SOUND_START = "start.mp3";
        public const string SOUND_PAUSE = "pause.mp3";
        public const string SOUND_60S = "60s.mp3";
        public const string SOUND_COUNTDOWN = "countdown.mp3";
        public const string SOUND_LEVEL = "level.mp3";

        public AudioService()
        {
            _mediaPlayer = new MediaPlayer
            {
                Volume = 0.8 // Volume par défaut raisonnable (0.0 à 1.0)
            };

            // Chemin du dossier Sounds dans le répertoire d'exécution (bin/Debug ou bin/Release)
            _soundsFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Sounds");

            EnsureSoundsFolderExists();
        }

        private void EnsureSoundsFolderExists()
        {
            try
            {
                // On ne crée pas le dossier ici car il doit exister via la copie des fichiers
                // Mais on vérifie quand même
                if (Directory.Exists(_soundsFolder))
                {
                    _isInitialized = true;
                    Console.WriteLine($"Dossier sons trouvé : {_soundsFolder}");
                }
                else
                {
                    Console.WriteLine($"Dossier sons NON trouvé : {_soundsFolder}");
                    _isInitialized = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur vérification dossier sons : {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Joue un son si le fichier existe, sinon fallback sur son système
        /// </summary>
        public void PlaySound(string soundFileName)
        {
            if (!_isInitialized || _disposed) return;

            try
            {
                var soundPath = Path.Combine(_soundsFolder, soundFileName);

                if (File.Exists(soundPath))
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Open(new Uri(soundPath, UriKind.Absolute));
                    _mediaPlayer.Play();
                    Console.WriteLine($"Lecture du son : {soundPath}");
                }
                else
                {
                    Console.WriteLine($"Son non trouvé : {soundPath} → fallback système");
                    PlayFallbackSound(soundFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lecture son {soundFileName} : {ex.Message}");
                PlayFallbackSound(soundFileName);
            }
        }

        private void PlayFallbackSound(string soundFileName)
        {
            try
            {
                switch (soundFileName)
                {
                    case SOUND_START:
                    case SOUND_LEVEL:
                        System.Media.SystemSounds.Asterisk.Play();
                        break;
                    case SOUND_PAUSE:
                        System.Media.SystemSounds.Hand.Play();
                        break;
                    case SOUND_60S:
                        System.Media.SystemSounds.Beep.Play();
                        break;
                    case SOUND_COUNTDOWN:
                        System.Media.SystemSounds.Exclamation.Play();
                        break;
                    default:
                        System.Media.SystemSounds.Beep.Play();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur fallback son système : {ex.Message}");
            }
        }

        /// <summary>
        /// Vérifie si un son existe dans le dossier Sounds
        /// </summary>
        public bool SoundExists(string soundFileName)
        {
            if (!_isInitialized) return false;
            var soundPath = Path.Combine(_soundsFolder, soundFileName);
            return File.Exists(soundPath);
        }

        /// <summary>
        /// Retourne le chemin du dossier des sons
        /// </summary>
        public string GetSoundsFolderPath() => _soundsFolder;

        /// <summary>
        /// Liste les sons disponibles (chemins complets)
        /// </summary>
        public string[] GetAvailableSounds()
        {
            if (!_isInitialized || !Directory.Exists(_soundsFolder))
                return Array.Empty<string>();

            return Directory.GetFiles(_soundsFolder, "*.mp3");
        }

        /// <summary>
        /// Arrête le son en cours
        /// </summary>
        public void Stop()
        {
            if (_disposed) return;
            _mediaPlayer.Stop();
        }

        /// <summary>
        /// Définit le volume (0.0 à 1.0)
        /// </summary>
        public void SetVolume(double volume)
        {
            if (_disposed) return;
            _mediaPlayer.Volume = Math.Clamp(volume, 0.0, 1.0);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _mediaPlayer.Stop();
            _mediaPlayer.Close();
            _disposed = true;
        }
    }
}