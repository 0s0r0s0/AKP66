using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PokerTournamentDirector.ViewModels
{
    public partial class TournamentTimerViewModel : ObservableObject
    {
        private readonly TournamentService _tournamentService;
        private readonly SettingsService _settingsService;
        private readonly AudioService _audioService;
        private readonly TournamentLogService _logService;
        private readonly PokerDbContext _context;
        private readonly DispatcherTimer _timer;
        private Tournament? _tournament;
        private AppSettings? _settings;

        // Pour la gestion correcte de la pause
        private DateTime _levelStartTime;
        private TimeSpan _elapsedBeforePause = TimeSpan.Zero;
        private int _levelDurationSeconds = 1200;

        // Pour la sauvegarde d'état
        private static readonly string StateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PokerTournamentDirector",
            "tournament_state.json");

        public int TournamentId { get; private set; }

        [ObservableProperty]
        private string _tournamentName = string.Empty;

        [ObservableProperty]
        private string _currentLevelName = string.Empty;

        [ObservableProperty]
        private int _currentLevel = 1;

        [ObservableProperty]
        private int _totalLevels = 1;

        [ObservableProperty]
        private int _smallBlind = 25;

        [ObservableProperty]
        private int _bigBlind = 50;

        [ObservableProperty]
        private int _ante = 0;

        [ObservableProperty]
        private int _nextSmallBlind = 50;

        [ObservableProperty]
        private int _nextBigBlind = 100;

        [ObservableProperty]
        private int _nextAnte = 0;

        [ObservableProperty]
        private string _timeRemaining = "20:00";

        [ObservableProperty]
        private int _totalSecondsRemaining = 1200;

        [ObservableProperty]
        private int _playersRemaining = 0;

        [ObservableProperty]
        private int _totalEntries = 0;

        [ObservableProperty]
        private int _totalRebuys = 0;

        [ObservableProperty]
        private int _averageStack = 0;

        [ObservableProperty]
        private long _totalChips = 0;

        [ObservableProperty]
        private decimal _prizePool = 0;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private bool _isPaused = false;

        [ObservableProperty]
        private bool _isBreak = false;

        [ObservableProperty]
        private string _breakName = string.Empty;

        [ObservableProperty]
        private bool _isTournamentFinished = false;

        // Cet événement sera déclenché quand le tournoi finit -> célébration
        public event EventHandler? OnVictoryCelebrationNeeded;

        // Déclenche automatiquement la célébration quand le tournoi se termine
        partial void OnIsTournamentFinishedChanged(bool oldValue, bool newValue)
        {
            if (newValue && !oldValue)
            {
                OnVictoryCelebrationNeeded?.Invoke(this, EventArgs.Empty);
            }
        }

        [ObservableProperty]
        private string _winnerName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<BlindLevel> _blindLevels = new();

        // Stats supplémentaires
        [ObservableProperty]
        private string _currentTime = DateTime.Now.ToString("HH:mm");

        [ObservableProperty]
        private string _tournamentDuration = "00:00:00";

        [ObservableProperty]
        private string _timeToNextBreak = "--:--";

        [ObservableProperty]
        private int _progressBarMaximum = 1200;

        // Pour l'ajout de joueurs en cours de tournoi
        [ObservableProperty]
        private bool _canAddLatePlayers = true;

        // NOUVEAU: Championnat et recaves
        [ObservableProperty]
        private bool _isChampionshipMatch;

        [ObservableProperty]
        private bool _canRebuy;

        [ObservableProperty]
        private bool _isLastRebuyLevel;

        public TournamentTimerViewModel(
            TournamentService tournamentService,
            SettingsService settingsService,
            AudioService audioService,
            TournamentLogService logService,
            PokerDbContext context)
        {
            _tournamentService = tournamentService;
            _settingsService = settingsService;
            _audioService = audioService;
            _logService = logService;
            _context = context;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
        }

        public async Task LoadTournamentAsync(int tournamentId)
        {
            TournamentId = tournamentId;
            _tournament = await _tournamentService.GetTournamentAsync(tournamentId);
            if (_tournament == null) return;

            // Charger les paramètres de sons
            _settings = await _settingsService.GetSettingsAsync();

            TournamentName = _tournament.Name;

            if (_tournament.BlindStructure?.Levels != null)
            {
                BlindLevels = new ObservableCollection<BlindLevel>(
                    _tournament.BlindStructure.Levels.OrderBy(l => l.LevelNumber)
                );
                TotalLevels = BlindLevels.Count;
            }

            // NOUVEAU: Vérifier si championnat
            var championshipMatch = await _context.ChampionshipMatches
                .FirstOrDefaultAsync(m => m.TournamentId == tournamentId);
            IsChampionshipMatch = championshipMatch != null;

            // NOUVEAU: Log ouverture
            await LogActionAsync("Ouverture écran timer");

            // Essayer de restaurer l'état sauvegardé
            if (await TryRestoreStateAsync())
            {
                return; // État restauré avec succès
            }

            await UpdateCurrentLevelAsync();
            await InternalRefreshStatsAsync();
        }

        [RelayCommand]
        private async Task StartTournamentAsync()
        {
            if (_tournament == null) return;

            IsRunning = true;
            IsPaused = false;
            _levelStartTime = DateTime.Now;
            _elapsedBeforePause = TimeSpan.Zero;

            if (_tournament.StartTime == null)
            {
                _tournament.StartTime = DateTime.Now;
                _tournament.Status = TournamentStatus.Running;
            }

            // Son de démarrage
            if (_settings?.EnableSounds == true)
            {
                _audioService.PlaySound(AudioService.SOUND_START);
            }

            // NOUVEAU: Log démarrage
            await LogActionAsync("Tournoi démarré", $"Niveau {CurrentLevel}, {_tournament.Players.Count} joueurs");

            _timer.Start();
            SaveStateAsync();
        }

        [RelayCommand]
        private async Task PauseTournamentAsync()
        {
            if (!IsRunning || IsPaused) return;

            IsPaused = true;
            _timer.Stop();

            // Sauvegarder le temps écoulé avant la pause
            _elapsedBeforePause += DateTime.Now - _levelStartTime;

            if (_tournament != null)
            {
                _tournament.Status = TournamentStatus.Paused;
            }

            // Son de pause (MP3)
            if (_settings?.EnableSounds == true && _settings.SoundOnPauseResume)
            {
                _audioService.PlaySound(AudioService.SOUND_PAUSE);
            }

            // NOUVEAU: Log pause
            await LogActionAsync("Pause", $"Niveau {CurrentLevel}, temps restant: {TimeRemaining}");

            SaveStateAsync();
        }

        [RelayCommand]
        private async Task ResumeTournamentAsync()
        {
            if (!IsPaused) return;

            IsPaused = false;
            // Réinitialiser le point de départ pour le calcul du temps
            _levelStartTime = DateTime.Now;

            if (_tournament != null)
            {
                _tournament.Status = TournamentStatus.Running;
            }

            // Son de reprise (même que pause)
            if (_settings?.EnableSounds == true && _settings.SoundOnPauseResume)
            {
                _audioService.PlaySound(AudioService.SOUND_PAUSE);
            }

            // NOUVEAU: Log reprise
            await LogActionAsync("Reprise", $"Niveau {CurrentLevel}");

            _timer.Start();
            SaveStateAsync();
        }

        [RelayCommand]
        private async Task NextLevelAsync()
        {
            if (_tournament == null || BlindLevels.Count == 0) return;

            var result = CustomMessageBox.ShowQuestion(
     $"Passer au niveau suivant ({CurrentLevel + 1}) ?\n\nCette action ne peut pas être annulée.",
     "Confirmer");

            if (result != MessageBoxResult.Yes)
                return;

            CurrentLevel++;

            if (CurrentLevel > BlindLevels.Count)
            {
                CurrentLevel = BlindLevels.Count;
                return;
            }

            _tournament.CurrentLevel = CurrentLevel;
            _levelStartTime = DateTime.Now;
            _elapsedBeforePause = TimeSpan.Zero;

            await UpdateCurrentLevelAsync();
            await _tournamentService.UpdateTournamentAsync(_tournament);

            // NOUVEAU: Log changement niveau
            await LogActionAsync("Niveau suivant (manuel)", $"Passage au niveau {CurrentLevel}");

            // NOUVEAU: Son break si c'est une pause
            if (IsBreakLevel())
            {
                PlayBreakSound();
            }
            else _audioService.PlaySound(AudioService.SOUND_LEVEL);

                SaveStateAsync();
        }

        [RelayCommand]
        private async Task PreviousLevelAsync()
        {
            if (_tournament == null || CurrentLevel <= 1) return;

            var result = CustomMessageBox.ShowQuestion(
    $"Revenir au niveau précédent ({CurrentLevel - 1}) ?\n\nCette action ne peut pas être annulée.",
    "Confirmer");

            if (result != MessageBoxResult.Yes)
                return;

            CurrentLevel--;
            _tournament.CurrentLevel = CurrentLevel;
            _levelStartTime = DateTime.Now;
            _elapsedBeforePause = TimeSpan.Zero;

            await UpdateCurrentLevelAsync();
            await _tournamentService.UpdateTournamentAsync(_tournament);

            // NOUVEAU: Log changement niveau
            await LogActionAsync("Niveau précédent (manuel)", $"Retour au niveau {CurrentLevel}");
            if (IsBreakLevel())
            {
                PlayBreakSound();
            }
            else _audioService.PlaySound(AudioService.SOUND_LEVEL);

            SaveStateAsync();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            if (_tournament == null || IsPaused || IsTournamentFinished) return;

            // Calculer le temps total écoulé (avant pause + depuis reprise)
            var totalElapsed = _elapsedBeforePause + (DateTime.Now - _levelStartTime);
            TotalSecondsRemaining = _levelDurationSeconds - (int)totalElapsed.TotalSeconds;

            if (TotalSecondsRemaining <= 0)
            {
                // Niveau terminé, passer au suivant automatiquement
                CurrentLevel++;

                if (CurrentLevel > BlindLevels.Count)
                {
                    CurrentLevel = BlindLevels.Count;
                    return;
                }

                _tournament.CurrentLevel = CurrentLevel;
                _levelStartTime = DateTime.Now;
                _elapsedBeforePause = TimeSpan.Zero;

                await UpdateCurrentLevelAsync();
                await _tournamentService.UpdateTournamentAsync(_tournament);

                // NOUVEAU: Log changement automatique
                await LogActionAsync("Changement automatique de niveau", $"Passage au niveau {CurrentLevel}");

                // NOUVEAU: Son break si pause
                if (IsBreakLevel())
                {
                    PlayBreakSound();
                }

                SaveStateAsync();
                return;
            }

            int minutes = TotalSecondsRemaining / 60;
            int seconds = TotalSecondsRemaining % 60;
            TimeRemaining = $"{minutes:D2}:{seconds:D2}";

            // Mettre à jour l'heure actuelle
            CurrentTime = DateTime.Now.ToString("HH:mm");

            // Mettre à jour la durée du tournoi
            if (_tournament.StartTime.HasValue)
            {
                var duration = DateTime.Now - _tournament.StartTime.Value;
                TournamentDuration = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            }

            // Calculer le temps avant la prochaine pause
            UpdateTimeToNextBreak();

            // Alertes sonores MP3 (avec vérification des paramètres)
            if (_settings?.EnableSounds == true)
            {
                switch (TotalSecondsRemaining)
                {
                    case 60:
                        if (_settings.SoundOn60Seconds)
                            _audioService.PlaySound(AudioService.SOUND_60S);
                        break;
                    case 10:
                        if (_settings.SoundOn10Seconds)
                            _audioService.PlaySound(AudioService.SOUND_10S);
                        break;
                    case 3:
                        if (_settings.SoundOnCountdown)
                            _audioService.PlaySound(AudioService.SOUND_COUNTDOWN);
                        break;
                    case 0:
                        if (_settings.SoundOnLevelChange)
                            _audioService.PlaySound(AudioService.SOUND_LEVEL);
                        break;
                }
            }
        }

        private void UpdateTimeToNextBreak()
        {
            var nextBreak = BlindLevels
                .Where(l => l.LevelNumber > CurrentLevel && l.IsBreak)
                .OrderBy(l => l.LevelNumber)
                .FirstOrDefault();

            if (nextBreak == null)
            {
                TimeToNextBreak = "--:--";
                return;
            }

            // Calculer le temps restant jusqu'à la pause
            int secondsToBreak = TotalSecondsRemaining;
            for (int level = CurrentLevel + 1; level < nextBreak.LevelNumber; level++)
            {
                var lvl = BlindLevels.FirstOrDefault(l => l.LevelNumber == level);
                if (lvl != null)
                {
                    secondsToBreak += lvl.DurationMinutes * 60;
                }
            }

            int mins = secondsToBreak / 60;
            int secs = secondsToBreak % 60;
            TimeToNextBreak = $"{mins:D2}:{secs:D2}";
        }

        private async Task UpdateCurrentLevelAsync()
        {
            var currentBlindLevel = BlindLevels.FirstOrDefault(l => l.LevelNumber == CurrentLevel);
            if (currentBlindLevel == null) return;

            SmallBlind = currentBlindLevel.SmallBlind;
            BigBlind = currentBlindLevel.BigBlind;
            Ante = currentBlindLevel.Ante;
            IsBreak = currentBlindLevel.IsBreak;
            BreakName = currentBlindLevel.BreakName ?? string.Empty;

            // Nom du niveau
            CurrentLevelName = IsBreak
                ? (string.IsNullOrEmpty(BreakName) ? "PAUSE" : BreakName)
                : $"NIVEAU {CurrentLevel}";

            // Durée du niveau
            _levelDurationSeconds = currentBlindLevel.DurationMinutes * 60;
            ProgressBarMaximum = _levelDurationSeconds;

            // Next level (trouver le prochain niveau non-pause pour les blinds)
            var nextBlindLevel = BlindLevels
                .Where(l => l.LevelNumber > CurrentLevel && !l.IsBreak)
                .OrderBy(l => l.LevelNumber)
                .FirstOrDefault();

            if (nextBlindLevel != null)
            {
                NextSmallBlind = nextBlindLevel.SmallBlind;
                NextBigBlind = nextBlindLevel.BigBlind;
                NextAnte = nextBlindLevel.Ante;
            }

            // Reset timer
            TotalSecondsRemaining = _levelDurationSeconds;
            int minutes = TotalSecondsRemaining / 60;
            int seconds = TotalSecondsRemaining % 60;
            TimeRemaining = $"{minutes:D2}:{seconds:D2}";

            // Vérifier si on peut encore ajouter des retardataires
            if (_tournament != null)
            {
                CanAddLatePlayers = CurrentLevel <= _tournament.LateRegistrationLevels;
            }

            // NOUVEAU: Mise à jour statut recaves
            UpdateRebuyStatus();

            await InternalRefreshStatsAsync();
        }

        // NOUVELLE MÉTHODE: Mise à jour statut recaves
        private void UpdateRebuyStatus()
        {
            if (_tournament == null || !_tournament.AllowRebuys || !IsRunning)
            {
                CanRebuy = false;
                IsLastRebuyLevel = false;
                return;
            }

            var lateRegLevels = _tournament.LateRegistrationLevels;

            CanRebuy = CurrentLevel <= lateRegLevels;
            IsLastRebuyLevel = CurrentLevel == lateRegLevels && CanRebuy;
        }

        // NOUVELLE MÉTHODE: Vérifier si niveau = pause
        private bool IsBreakLevel()
        {
            var level = BlindLevels.FirstOrDefault(l => l.LevelNumber == CurrentLevel);
            return level?.IsBreak ?? false;
        }

        // NOUVELLE MÉTHODE: Jouer son break
        private void PlayBreakSound()
        {
            try
            {
                var soundPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Sounds",
                    "break.mp3");

                if (File.Exists(soundPath))
                {
                    // Pour MP3, utiliser MediaPlayer
                    var player = new System.Windows.Media.MediaPlayer();
                    player.Open(new Uri(soundPath));
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lecture son break: {ex.Message}");
            }
        }

        // NOUVELLE MÉTHODE: Logger les actions
        private async Task LogActionAsync(string action, string? details = null)
        {
            try
            {
                await _logService.AddLogAsync(new TournamentLog
                {
                    TournamentId = TournamentId,
                    Action = action,
                    Details = details,
                    Timestamp = DateTime.Now,
                    Level = CurrentLevel,
                    PlayersRemaining = PlayersRemaining,
                    Username = Environment.UserName
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur log: {ex.Message}");
            }
        }

        private async Task InternalRefreshStatsAsync()
        {
            if (_tournament == null) return;

            // Recharger le tournoi pour avoir les données à jour
            _tournament = await _tournamentService.GetTournamentAsync(TournamentId);
            if (_tournament == null) return;

            PlayersRemaining = await _tournamentService.GetRemainingPlayersCountAsync(_tournament.Id);
            TotalEntries = _tournament.Players.Count;
            TotalRebuys = _tournament.TotalRebuys;

            // Calculer les jetons totaux
            TotalChips = (long)TotalEntries * _tournament.StartingStack +
                        (long)TotalRebuys * (_tournament.RebuyStack ?? _tournament.StartingStack);

            // Tapis moyen
            if (PlayersRemaining > 0)
            {
                AverageStack = (int)(TotalChips / PlayersRemaining);
            }
            else
            {
                AverageStack = 0;
            }

            PrizePool = await _tournamentService.CalculatePrizePoolAsync(_tournament.Id);

            // Vérifier si le tournoi est terminé (1 seul joueur restant)
            await CheckTournamentEndAsync();
        }

        private async Task CheckTournamentEndAsync()
        {
            if (PlayersRemaining == 1 && _tournament != null)
            {
                IsTournamentFinished = true;
                _timer.Stop();
                IsRunning = false;
                IsPaused = true;

                _tournament.Status = TournamentStatus.Finished;
                _tournament.EndTime = DateTime.Now;

                // Trouver le gagnant
                var winner = _tournament.Players.FirstOrDefault(p => !p.IsEliminated);
                if (winner != null)
                {
                    WinnerName = winner.Player?.Name ?? "Inconnu";
                    winner.FinishPosition = 1;
                    winner.Winnings = PrizePool;
                    await _tournamentService.UpdateTournamentPlayerAsync(winner);
                }

                await _tournamentService.UpdateTournamentAsync(_tournament);

                // NOUVEAU: Log fin tournoi
                await LogActionAsync("Tournoi terminé", $"Gagnant: {WinnerName}");
                

                ClearSavedState();
            }
        }

        public async Task RefreshStatsAsync()
        {
            await InternalRefreshStatsAsync();
        }

        public void StopTimer()
        {
            _timer.Stop();
            IsRunning = false;
            SaveStateAsync();
        }

        #region Modification des Blinds en cours

        [RelayCommand]
        private void EditCurrentBlinds()
        {
            // Cette commande sera liée à un dialogue dans la vue
        }

        public void UpdateBlindsAndTime(int smallBlind, int bigBlind, int ante, int timeAdjustmentSeconds)
        {
            SmallBlind = smallBlind;
            BigBlind = bigBlind;
            Ante = ante;

            var currentBlindLevel = BlindLevels.FirstOrDefault(l => l.LevelNumber == CurrentLevel);
            if (currentBlindLevel != null)
            {
                currentBlindLevel.SmallBlind = smallBlind;
                currentBlindLevel.BigBlind = bigBlind;
                currentBlindLevel.Ante = ante;
            }

            if (timeAdjustmentSeconds != 0)
            {
                int newTime = TotalSecondsRemaining + timeAdjustmentSeconds;
                if (newTime > 0)
                {
                    _elapsedBeforePause = _elapsedBeforePause - TimeSpan.FromSeconds(timeAdjustmentSeconds);
                    if (_elapsedBeforePause < TimeSpan.Zero)
                    {
                        _elapsedBeforePause = TimeSpan.Zero;
                        _levelDurationSeconds = newTime + (int)(DateTime.Now - _levelStartTime).TotalSeconds;
                    }

                    TotalSecondsRemaining = newTime;
                    ProgressBarMaximum = Math.Max(ProgressBarMaximum, TotalSecondsRemaining);

                    int minutes = TotalSecondsRemaining / 60;
                    int seconds = TotalSecondsRemaining % 60;
                    TimeRemaining = $"{minutes:D2}:{seconds:D2}";
                }
            }

            SaveStateAsync();
        }

        public void UpdateBlinds(int levelNumber, int smallBlind, int bigBlind, int ante, int durationMinutes)
        {
            var level = BlindLevels.FirstOrDefault(l => l.LevelNumber == levelNumber);
            if (level == null) return;

            level.SmallBlind = smallBlind;
            level.BigBlind = bigBlind;
            level.Ante = ante;
            level.DurationMinutes = durationMinutes;

            if (levelNumber == CurrentLevel)
            {
                SmallBlind = smallBlind;
                BigBlind = bigBlind;
                Ante = ante;
                _levelDurationSeconds = durationMinutes * 60;
                ProgressBarMaximum = _levelDurationSeconds;
            }

            SaveStateAsync();
        }

        [RelayCommand]
        private void ShowBlindStructure()
        {
            var viewModel = new BlindStructureViewModel
            {
                TournamentName = TournamentName,
                CurrentLevel = CurrentLevel,
                TotalLevels = TotalLevels,
                BlindLevels = new ObservableCollection<BlindLevelDisplay>(
                    BlindLevels.Select(bl => new BlindLevelDisplay
                    {
                        LevelNumber = bl.LevelNumber,
                        SmallBlind = bl.SmallBlind,
                        BigBlind = bl.BigBlind,
                        Ante = bl.Ante,
                        DurationMinutes = bl.DurationMinutes,
                        IsBreak = bl.IsBreak,
                        BreakName = bl.BreakName,
                        IsCurrentLevel = bl.LevelNumber == CurrentLevel
                    })
                )
            };

            var window = new BlindStructureView
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            window.ShowDialog();
        }
        #endregion

        #region Sauvegarde et Restauration d'État

        private async void SaveStateAsync()
        {
            try
            {
                var state = new TournamentState
                {
                    TournamentId = TournamentId,
                    CurrentLevel = CurrentLevel,
                    TotalSecondsRemaining = TotalSecondsRemaining,
                    ElapsedBeforePauseSeconds = (int)_elapsedBeforePause.TotalSeconds,
                    IsRunning = IsRunning,
                    IsPaused = IsPaused,
                    SavedAt = DateTime.Now
                };

                var directory = Path.GetDirectoryName(StateFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(state);
                await File.WriteAllTextAsync(StateFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde état: {ex.Message}");
            }
        }

        private async Task<bool> TryRestoreStateAsync()
        {
            try
            {
                if (!File.Exists(StateFilePath)) return false;

                var json = await File.ReadAllTextAsync(StateFilePath);
                var state = JsonSerializer.Deserialize<TournamentState>(json);

                if (state == null || state.TournamentId != TournamentId) return false;

                // Vérifier si la sauvegarde n'est pas trop vieille (24h max)
                if ((DateTime.Now - state.SavedAt).TotalHours > 24)
                {
                    ClearSavedState();
                    return false;
                }

                // Restaurer l'état
                CurrentLevel = state.CurrentLevel;
                if (_tournament != null)
                {
                    _tournament.CurrentLevel = state.CurrentLevel;
                }

                await UpdateCurrentLevelAsync();

                // Restaurer le temps restant
                TotalSecondsRemaining = state.TotalSecondsRemaining;
                _elapsedBeforePause = TimeSpan.FromSeconds(_levelDurationSeconds - state.TotalSecondsRemaining);

                int minutes = TotalSecondsRemaining / 60;
                int seconds = TotalSecondsRemaining % 60;
                TimeRemaining = $"{minutes:D2}:{seconds:D2}";

                IsRunning = state.IsRunning;
                IsPaused = true; // Toujours restaurer en pause

                await InternalRefreshStatsAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur restauration état: {ex.Message}");
                return false;
            }
        }

        private void ClearSavedState()
        {
            try
            {
                if (File.Exists(StateFilePath))
                {
                    File.Delete(StateFilePath);
                }
            }
            catch { }
        }

        #endregion

        #region Ajout de joueurs retardataires

        public async Task<bool> AddLatePlayerAsync(int playerId)
        {
            if (_tournament == null || !CanAddLatePlayers) return false;

            try
            {
                await _tournamentService.RegisterPlayerAsync(TournamentId, playerId);
                await InternalRefreshStatsAsync();

                // NOUVEAU: Log ajout joueur
                await LogActionAsync("Joueur retardataire ajouté", $"ID: {playerId}, Niveau {CurrentLevel}");

                SaveStateAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    // Classe pour la sauvegarde d'état
    public class TournamentState
    {
        public int TournamentId { get; set; }
        public int CurrentLevel { get; set; }
        public int TotalSecondsRemaining { get; set; }
        public int ElapsedBeforePauseSeconds { get; set; }
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
