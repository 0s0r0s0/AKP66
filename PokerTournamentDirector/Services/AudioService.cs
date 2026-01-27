using System;
using System.IO;
using System.Windows.Media;

/// v2.0 - Gestion des sons de tournoi 
namespace PokerTournamentDirector.Services
{
    /// <summary>
    /// Service responsable de la lecture des sons MP3 personnalisés du tournoi.
    /// Les fichiers sons sont chargés depuis le dossier "Sounds" situé dans le répertoire d'exécution.
    /// </summary>
    public class AudioService : IDisposable
    {
        private readonly MediaPlayer _mediaPlayer;
        private readonly string _soundsFolderPath;
        private bool _isInitialized;
        private bool _disposed;

        #region Constantes - Noms des fichiers sons

        public const string SOUND_START = "start.mp3";
        public const string SOUND_PAUSE = "pause.mp3";
        public const string SOUND_60S = "60s.mp3";
        public const string SOUND_10S = "10s.mp3";
        public const string SOUND_COUNTDOWN = "countdown.mp3";
        public const string SOUND_LEVEL = "level.mp3";
        public const string SOUND_BRAVO = "bravo.mp3";
        public const string SOUND_BREAK = "break.mp3";    
        public const string SOUND_KILL = "kill.mp3";
        public const string SOUND_REBUY = "rebuy.mp3";
        public const string SOUND_TEST = "test.mp3";
        public const string SOUND_UNDO = "undo.mp3";

        #endregion

        #region Constructeur et initialisation

        public AudioService()
        {
            _mediaPlayer = new MediaPlayer
            {
                Volume = 0.8 // Volume par défaut (0.0 à 1.0)
            };

            _soundsFolderPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Sounds");

            InitializeSoundsFolder();
        }

        private void InitializeSoundsFolder()
        {
            try
            {
                _isInitialized = Directory.Exists(_soundsFolderPath);

                if (_isInitialized)
                {
                    Console.WriteLine($"Dossier sons chargé : {_soundsFolderPath}");
                }
                else
                {
                    Console.WriteLine($"Dossier sons introuvable : {_soundsFolderPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la vérification du dossier sons : {ex.Message}");
                _isInitialized = false;
            }
        }

        #endregion

        #region Lecture des sons

        /// <summary>
        /// Joue le fichier son spécifié s'il existe dans le dossier Sounds.
        /// En cas d'échec, joue un son système de secours.
        /// </summary>
        /// <param name="soundFileName">Nom du fichier son (ex: "start.mp3")</param>
        public void PlaySound(string soundFileName)
        {
            if (!_isInitialized || _disposed) return;

            try
            {
                string fullPath = Path.Combine(_soundsFolderPath, soundFileName);

                if (File.Exists(fullPath))
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.Open(new Uri(fullPath, UriKind.Absolute));
                    _mediaPlayer.Play();
                    Console.WriteLine($"Son joué : {fullPath}");
                }
                else
                {
                    Console.WriteLine($"Fichier son introuvable : {fullPath} → fallback système");
                    PlaySystemFallback(soundFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la lecture de {soundFileName} : {ex.Message}");
                PlaySystemFallback(soundFileName);
            }
        }

        private void PlaySystemFallback(string soundFileName)
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

        #endregion

        #region Contrôles du lecteur

        /// <summary>
        /// Arrête immédiatement la lecture en cours.
        /// </summary>
        public void Stop()
        {
            if (_disposed) return;
            _mediaPlayer.Stop();
        }

        /// <summary>
        /// Définit le volume du lecteur (valeur entre 0.0 et 1.0).
        /// </summary>
        /// <param name="volume">Volume désiré (clampé entre 0 et 1)</param>
        public void SetVolume(double volume)
        {
            if (_disposed) return;
            _mediaPlayer.Volume = Math.Clamp(volume, 0.0, 1.0);
        }

        #endregion

        #region Informations et diagnostics

        /// <summary>
        /// Vérifie si un fichier son existe dans le dossier Sounds.
        /// </summary>
        /// <param name="soundFileName">Nom du fichier à vérifier</param>
        /// <returns>true si le fichier existe</returns>
        public bool SoundExists(string soundFileName)
        {
            if (!_isInitialized) return false;
            string fullPath = Path.Combine(_soundsFolderPath, soundFileName);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// Retourne le chemin complet du dossier contenant les sons.
        /// </summary>
        public string GetSoundsFolderPath() => _soundsFolderPath;

        /// <summary>
        /// Liste tous les fichiers MP3 disponibles dans le dossier Sounds.
        /// </summary>
        /// <returns>Tableau des chemins complets des fichiers MP3</returns>
        public string[] GetAvailableSounds()
        {
            if (!_isInitialized || !Directory.Exists(_soundsFolderPath))
                return Array.Empty<string>();

            return Directory.GetFiles(_soundsFolderPath, "*.mp3");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _mediaPlayer.Stop();
            _mediaPlayer.Close();
            _disposed = true;
        }

        #endregion
    }
}