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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PokerTournamentDirector.ViewModels
{
    public partial class EliminationViewModel : ObservableObject
    {
        private readonly TournamentService _tournamentService;
        private readonly PlayerService _playerService;
        private readonly TableManagementService _tableManagementService;
        private readonly TournamentLogService _logService;
        private readonly ChampionshipService _championshipService;
        private readonly PokerDbContext _context;
        private readonly int _tournamentId;
        private Tournament? _tournament;
        private Championship? _championship;
        private int? _championshipId;
        private MediaPlayer? _victoryPlayer;


        [ObservableProperty]
        private ObservableCollection<TournamentPlayer> _activePlayers = new();

        [ObservableProperty]
        private ObservableCollection<TournamentPlayer> _availableKillers = new();

        [ObservableProperty]
        private TournamentPlayer? _selectedEliminatedPlayer;

        [ObservableProperty]
        private TournamentPlayer? _selectedKillerPlayer;

        [ObservableProperty]
        private ObservableCollection<HistoryItem> _eliminationHistory = new();

        [ObservableProperty]
        private bool _canRebuy;

        [ObservableProperty]
        private bool _showRebuyOption;

        [ObservableProperty]
        private string _rebuyMessage = string.Empty;

        [ObservableProperty]
        private decimal _rebuyAmount;

        [ObservableProperty]
        private bool _willRebuy;

        [ObservableProperty]
        private int _playersRemaining;

        [ObservableProperty]
        private int _totalEliminations;

        [ObservableProperty]
        private int _nextPosition;

        public event EventHandler? RefreshRequested;
        public event EventHandler<string>? TournamentFinished;

        public EliminationViewModel(
            TournamentService tournamentService,
            PlayerService playerService,
            TableManagementService tableManagementService,
            TournamentLogService logService,
            ChampionshipService championshipService,
            PokerDbContext context,
            int tournamentId)
        {
            _tournamentService = tournamentService;
            _playerService = playerService;
            _tableManagementService = tableManagementService;
            _logService = logService;
            _championshipService = championshipService;
            _context = context;
            _tournamentId = tournamentId;
        }

        public async Task InitializeAsync()
        {
            _tournament = await _tournamentService.GetTournamentAsync(_tournamentId);
            if (_tournament == null) return;

            var championshipMatch = await _context.ChampionshipMatches
                .FirstOrDefaultAsync(m => m.TournamentId == _tournamentId);

            if (championshipMatch != null)
            {
                _championshipId = championshipMatch.ChampionshipId;
                _championship = await _championshipService.GetChampionshipAsync(_championshipId.Value);
            }

            RebuyAmount = _tournament.RebuyAmount ?? _tournament.BuyIn;

            await RefreshDataAsync();
        }

        private async Task RefreshDataAsync()
        {
            await LoadPlayersAsync();
            await LoadHistoryAsync();
        }

        private async Task LoadPlayersAsync()
        {
            var players = await _tournamentService.GetActivePlayers(_tournamentId);
            ActivePlayers.Clear();
            foreach (var p in players.OrderBy(x => x.Player!.Name))
                ActivePlayers.Add(p);

            PlayersRemaining = ActivePlayers.Count;

            // FIX: Calculer la position suivante basée sur le nombre total de joueurs inscrits
            // (actifs + éliminés), pas seulement les actifs
            var allPlayers = await _context.TournamentPlayers
                .Where(tp => tp.TournamentId == _tournamentId)
                .ToListAsync();

            var totalEliminatedWithPosition = allPlayers.Count(p => p.IsEliminated && p.FinishPosition.HasValue);
            NextPosition = allPlayers.Count - totalEliminatedWithPosition;
        }

        private async Task LoadHistoryAsync()
        {
            if (_tournament == null) return;

            _tournament = await _tournamentService.GetTournamentAsync(_tournamentId);

            var history = new List<HistoryItem>();

            // Récupérer tous les logs du tournoi
            var logs = await _context.TournamentLogs
                .Where(l => l.TournamentId == _tournamentId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            foreach (var log in logs)
            {
                switch (log.Action)
                {
                    case "Élimination":
                        // Parser: "PlayerName éliminé #Position par KillerName"
                        var eliminationParts = log.Details.Split(new[] { " éliminé #", " par " }, StringSplitOptions.None);
                        if (eliminationParts.Length >= 2)
                        {
                            var playerName = eliminationParts[0];
                            var positionStr = eliminationParts[1].Split(' ')[0];
                            var killerName = eliminationParts.Length > 2 ? eliminationParts[2] : "Aucun";

                            if (int.TryParse(positionStr, out int position))
                            {
                                history.Add(new HistoryItem
                                {
                                    Position = position,
                                    PlayerName = playerName,
                                    KillerName = killerName,
                                    ActionType = "💀 Élimination",
                                    IsRebuy = false,
                                    IsUndo = false,
                                    Timestamp = log.Timestamp
                                });
                            }
                        }
                        break;

                    case "Recave":
                        // Parser: "PlayerName - €X"
                        var rebuyParts = log.Details.Split(new[] { " - " }, StringSplitOptions.None);
                        if (rebuyParts.Length >= 1)
                        {
                            var playerName = rebuyParts[0];
                            var amount = rebuyParts.Length > 1 ? rebuyParts[1] : "";

                            history.Add(new HistoryItem
                            {
                                Position = 0, // Pas de position pour une recave
                                PlayerName = playerName,
                                KillerName = "",
                                ActionType = $"💰 Recave {amount}",
                                IsRebuy = true,
                                IsUndo = false,
                                Timestamp = log.Timestamp
                            });
                        }
                        break;

                    case "Annulation élimination":
                        // Parser: "PlayerName (#Position) - Killer: KillerName"
                        var undoParts = log.Details.Split(new[] { " (#", ") - Killer: " }, StringSplitOptions.None);
                        if (undoParts.Length >= 2)
                        {
                            var playerName = undoParts[0];
                            var positionStr = undoParts[1];
                            var killerName = undoParts.Length > 2 ? undoParts[2] : "Aucun";

                            if (int.TryParse(positionStr, out int position))
                            {
                                history.Add(new HistoryItem
                                {
                                    Position = position,
                                    PlayerName = playerName,
                                    KillerName = killerName,
                                    ActionType = "↩️ Annulation",
                                    IsRebuy = false,
                                    IsUndo = true,
                                    Timestamp = log.Timestamp
                                });
                            }
                        }
                        break;
                }
            }

            EliminationHistory.Clear();
            foreach (var item in history.OrderByDescending(h => h.Timestamp))
                EliminationHistory.Add(item);

            TotalEliminations = _tournament.Players.Count(p => p.IsEliminated && p.FinishPosition.HasValue);
        }

        [RelayCommand]
        private async Task SelectPlayerAsync(TournamentPlayer player)
        {
            SelectedEliminatedPlayer = player;

            AvailableKillers.Clear();
            AvailableKillers.Add(new TournamentPlayer
            {
                Id = -1,
                Player = new Player { Name = "Aucun killer" }
            });

            foreach (var p in ActivePlayers.Where(p => p.Id != player.Id))
                AvailableKillers.Add(p);

            SelectedKillerPlayer = AvailableKillers[0];

            await CheckRebuyAsync();
        }

        [RelayCommand]
        private void CancelSelection()
        {
            SelectedEliminatedPlayer = null;
            SelectedKillerPlayer = null;
            WillRebuy = false;
            ShowRebuyOption = false;
        }

        private async Task CheckRebuyAsync()
        {
            if (SelectedEliminatedPlayer == null || _tournament == null)
            {
                ShowRebuyOption = false;
                return;
            }

            if (!_tournament.AllowRebuys)
            {
                ShowRebuyOption = false;
                return;
            }

            ShowRebuyOption = true;
            var playerId = SelectedEliminatedPlayer.PlayerId;

            if (_championship != null)
            {
                var canRebuy = await CheckChampionshipRebuyAsync(playerId);
                if (!canRebuy) return;
            }

            // FIX: Récupérer le nombre de recaves depuis TournamentPlayer.RebuyCount
            var rebuyCount = SelectedEliminatedPlayer.RebuyCount;

            // Vérifier si le joueur peut encore recaver
            if (_tournament.MaxRebuysPerPlayer > 0 && rebuyCount >= _tournament.MaxRebuysPerPlayer)
            {
                CanRebuy = false;
                RebuyMessage = $"❌ Limite atteinte ({rebuyCount}/{_tournament.MaxRebuysPerPlayer})";
            }
            else
            {
                CanRebuy = true;
                var remaining = _tournament.MaxRebuysPerPlayer > 0
                    ? _tournament.MaxRebuysPerPlayer - rebuyCount
                    : 999;
                RebuyMessage = $"✅ Recave disponible ({remaining} restantes) - {RebuyAmount:C0}";
            }
        }

        private async Task<bool> CheckChampionshipRebuyAsync(int playerId)
        {
            if (_championship == null) return true;

            switch (_championship.RebuyMode)
            {
                case ChampionshipRebuyMode.NoRebuy:
                    RebuyMessage = "❌ Recaves interdites (championnat)";
                    CanRebuy = false;
                    return false;

                case ChampionshipRebuyMode.LimitedPerMonth:
                    return await CheckMonthlyLimitAsync(playerId);

                case ChampionshipRebuyMode.LimitedPerQuarter:
                    return await CheckQuarterlyLimitAsync(playerId);

                case ChampionshipRebuyMode.LimitedPerSeason:
                    return await CheckSeasonLimitAsync(playerId);

                default:
                    return true;
            }
        }

        private async Task<bool> CheckMonthlyLimitAsync(int playerId)
        {
            if (!_championship!.RebuyLimit.HasValue) return true;

            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = start.AddMonths(1);

            var count = await _context.TournamentPlayers
                .Where(tp => tp.PlayerId == playerId &&
                             tp.Tournament!.ChampionshipMatches.Any(cm => cm.ChampionshipId == _championshipId) &&
                             tp.Tournament.Date >= start && tp.Tournament.Date < end)
                .SumAsync(tp => tp.RebuyCount);

            if (count >= _championship.RebuyLimit.Value)
            {
                RebuyMessage = $"❌ Limite mensuelle atteinte ({count}/{_championship.RebuyLimit})";
                CanRebuy = false;
                return false;
            }

            return true;
        }

        private async Task<bool> CheckQuarterlyLimitAsync(int playerId)
        {
            if (!_championship!.RebuyLimit.HasValue) return true;

            var quarter = (DateTime.Now.Month - 1) / 3 + 1;
            var start = new DateTime(DateTime.Now.Year, (quarter - 1) * 3 + 1, 1);
            var end = start.AddMonths(3);

            var count = await _context.TournamentPlayers
                .Where(tp => tp.PlayerId == playerId &&
                             tp.Tournament!.ChampionshipMatches.Any(cm => cm.ChampionshipId == _championshipId) &&
                             tp.Tournament.Date >= start && tp.Tournament.Date < end)
                .SumAsync(tp => tp.RebuyCount);

            if (count >= _championship.RebuyLimit.Value)
            {
                RebuyMessage = $"❌ Limite trimestrielle Q{quarter} atteinte ({count}/{_championship.RebuyLimit})";
                CanRebuy = false;
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task UndoLastEliminationAsync()
        {
            if (!EliminationHistory.Any())
            {
                CustomMessageBox.ShowInformation("Aucune élimination à annuler.", "Info");
                return;
            }

            // Trouver la dernière élimination (pas une recave)
            var lastElimination = EliminationHistory.FirstOrDefault(h => !h.IsRebuy);
            if (lastElimination == null)
            {
                CustomMessageBox.ShowInformation("Aucune élimination à annuler.", "Info");
                return;
            }

            // Préparer le message de confirmation
            var msg = $"Annuler l'élimination de {lastElimination.PlayerName} (#{lastElimination.Position}) ?";

            if (!string.IsNullOrEmpty(lastElimination.KillerName) && lastElimination.KillerName != "Aucun")
            {
                msg += $"\n\nLe kill de {lastElimination.KillerName} sera également retiré.";
            }

            var result = CustomMessageBox.ShowConfirmation(msg, "Annuler l'élimination");
            if (result != MessageBoxResult.Yes) return;
            PlayEliminatedSound("undo.mp3");
            try
            {
                // Recharger le tournoi pour avoir les données à jour
                _tournament = await _tournamentService.GetTournamentAsync(_tournamentId);
                if (_tournament == null)
                {
                    CustomMessageBox.ShowError("Tournoi introuvable.", "Erreur");
                    return;
                }

                // Trouver le joueur éliminé dans la liste
                var eliminatedPlayer = _tournament.Players
                    .FirstOrDefault(p => p.Player?.Name == lastElimination.PlayerName &&
                                        p.IsEliminated &&
                                        p.FinishPosition == lastElimination.Position);

                if (eliminatedPlayer == null)
                {
                    CustomMessageBox.ShowError("Impossible de trouver le joueur éliminé.", "Erreur");
                    return;
                }

                // Sauvegarder les infos pour le log
                int? killerId = eliminatedPlayer.EliminatedByPlayerId;
                string killerName = lastElimination.KillerName;

                // 1. RÉACTIVER LE JOUEUR
                eliminatedPlayer.IsEliminated = false;
                eliminatedPlayer.FinishPosition = null;
                eliminatedPlayer.EliminationTime = null;
                eliminatedPlayer.EliminatedByPlayerId = null;

                await _tournamentService.UpdateTournamentPlayerAsync(eliminatedPlayer);

                // 2. RETIRER LE KILL AU KILLER
                if (killerId.HasValue)
                {
                    var killer = _tournament.Players.FirstOrDefault(p => p.Id == killerId.Value);
                    if (killer != null && killer.BountyKills > 0)
                    {
                        killer.BountyKills--;
                        await _tournamentService.UpdateTournamentPlayerAsync(killer);
                    }
                }

                // 3. SI LE TOURNOI ÉTAIT TERMINÉ, LE REMETTRE EN COURS
                if (_tournament.Status == TournamentStatus.Finished)
                {
                    _tournament.Status = TournamentStatus.Running;
                    _tournament.EndTime = null;
                    await _tournamentService.UpdateTournamentAsync(_tournament);
                }

                // 4. LOGGER L'ANNULATION
                await _logService.AddLogAsync(new TournamentLog
                {
                    TournamentId = _tournamentId,
                    Action = "Annulation élimination",
                    Details = $"{lastElimination.PlayerName} (#{lastElimination.Position}) - Killer: {killerName}",
                    Timestamp = DateTime.Now,
                    Username = Environment.UserName
                });

                // 5. AUTO-ÉQUILIBRAGE DES TABLES
                await _tableManagementService.AutoBalanceAfterChangeAsync(_tournamentId);

                // 6. RAFRAÎCHIR L'INTERFACE
                await RefreshDataAsync();
                RefreshRequested?.Invoke(this, EventArgs.Empty);

                CustomMessageBox.ShowSuccess($"Élimination de {lastElimination.PlayerName} annulée avec succès.", "Succès");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur lors de l'annulation :\n\n{ex.Message}", "Erreur");
            }
        }

        private async Task<bool> CheckSeasonLimitAsync(int playerId)
        {
            if (!_championship!.RebuyLimit.HasValue) return true;

            var count = await _context.TournamentPlayers
                .Where(tp => tp.PlayerId == playerId &&
                             tp.Tournament!.ChampionshipMatches.Any(cm => cm.ChampionshipId == _championshipId))
                .SumAsync(tp => tp.RebuyCount);

            if (count >= _championship.RebuyLimit.Value)
            {
                RebuyMessage = $"❌ Limite saison atteinte ({count}/{_championship.RebuyLimit})";
                CanRebuy = false;
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task EliminatePlayerAsync()
        {
            if (SelectedEliminatedPlayer == null) return;

            var playerName = SelectedEliminatedPlayer.Player?.Name ?? "Joueur";
            var killerName = SelectedKillerPlayer?.Id > 0 ? SelectedKillerPlayer.Player?.Name : "Aucun";

            var msg = $"Élimination de {playerName} par {killerName}\nPosition : #{NextPosition}";
            if (WillRebuy && CanRebuy)
                msg += $"\n\n💰 Recave de {RebuyAmount:C0} sera effectuée";

            PlayEliminatedSound("kill.mp3");

            var result = CustomMessageBox.ShowQuestion(msg, "Confirmer ?");
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var killerId = SelectedKillerPlayer?.Id > 0 ? SelectedKillerPlayer.Id : (int?)null;
                await _tournamentService.EliminatePlayerAsync(SelectedEliminatedPlayer.Id, killerId);

                await _logService.AddLogAsync(new TournamentLog
                {
                    TournamentId = _tournamentId,
                    Action = "Élimination",
                    Details = $"{playerName} éliminé #{NextPosition} par {killerName}",
                    Timestamp = DateTime.Now,
                    Username = Environment.UserName
                });

                if (WillRebuy && CanRebuy)
                {
                    var rebuyStack = _tournament?.RebuyStack ?? _tournament?.StartingStack ?? 0;
                    await _playerService.ProcessRebuyAsync(SelectedEliminatedPlayer.PlayerId, _tournamentId, rebuyStack);

                    await _logService.AddLogAsync(new TournamentLog
                    {
                        TournamentId = _tournamentId,
                        Action = "Recave",
                        Details = $"{playerName} - {RebuyAmount:C0}",
                        Timestamp = DateTime.Now,
                        Username = Environment.UserName
                    });
                    PlayEliminatedSound("rebuy.mp3");
                }

                await _tableManagementService.AutoBalanceAfterChangeAsync(_tournamentId);

                CancelSelection();
                await RefreshDataAsync();
                RefreshRequested?.Invoke(this, EventArgs.Empty);

                if (PlayersRemaining == 1)
                {
                    await HandleTournamentEndAsync();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur : {ex.Message}", "Erreur");
            }
        }

        private async Task HandleTournamentEndAsync()
        {
            var winner = ActivePlayers.FirstOrDefault();
            if (winner == null) return;

            var winnerName = winner.Player?.Name ?? "Inconnu";

            // AJOUTER : Passer le tournoi en Finished
            if (_tournament != null)
            {
                _tournament.Status = TournamentStatus.Finished;
                _tournament.EndTime = DateTime.Now;
                await _tournamentService.UpdateTournamentAsync(_tournament);
            }

            await _logService.AddLogAsync(new TournamentLog
            {
                TournamentId = _tournamentId,
                Action = "Fin tournoi",
                Details = $"Gagnant: {winnerName}",
                Timestamp = DateTime.Now,
                Username = Environment.UserName
            });

            if (_championshipId.HasValue)
            {
                var validationView = new TournamentEndValidationView(
                    _tournamentService,
                    _championshipService,
                    _tournamentId,
                    _championshipId);
                validationView.ShowDialog();

                // Après validation championnat, on peut archiver
                CustomMessageBox.ShowSuccess($"🏆 Gagnant : {winnerName} !", "Tournoi terminé");
            }
            else
            {
                CustomMessageBox.ShowSuccess($"🏆 Gagnant : {winnerName} !", "Tournoi terminé");

            }

            TournamentFinished?.Invoke(this, winnerName);
        }
        //1 -> kill
        //2 -> rebuy
        //3 -> undo
        private void PlayEliminatedSound(string nameMp3)
        {
            try
            {
                var soundPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Sounds",
                    nameMp3);

                if (!File.Exists(soundPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Son de victoire introuvable : {soundPath}");
                    return;
                }

                // Créer un nouveau MediaPlayer à chaque fois pour éviter les conflits
                _victoryPlayer?.Close();
                _victoryPlayer = new MediaPlayer
                {
                    Volume = 0.7
                };

                _victoryPlayer.Open(new Uri(soundPath, UriKind.Absolute));
                _victoryPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lecture son victoire : {ex.Message}");
            }
        }
    }

    public class HistoryItem
    {
        public int Position { get; set; }
        public string PlayerName { get; set; } = "";
        public string KillerName { get; set; } = "";
        public string ActionType { get; set; } = "";
        public bool IsRebuy { get; set; }
        public bool IsUndo { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimeDisplay => Timestamp.ToString("HH:mm:ss");
    }
}
