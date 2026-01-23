using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PokerTournamentDirector.ViewModels
{
    public partial class EliminationViewModel : ObservableObject
    {
        private readonly TournamentService _tournamentService;
        private readonly PlayerService _playerService;
        private readonly TableManagementService _tableManagementService; // ← AJOUTÉ
        private readonly int _tournamentId;
        private Tournament? _tournament;

        [ObservableProperty]
        private ObservableCollection<TournamentPlayer> _activePlayers = new();

        [ObservableProperty]
        private ObservableCollection<TournamentPlayer> _availableKillers = new();

        [ObservableProperty]
        private TournamentPlayer? _selectedEliminatedPlayer;

        [ObservableProperty]
        private TournamentPlayer? _selectedKillerPlayer;

        [ObservableProperty]
        private ObservableCollection<EliminationHistoryItem> _eliminationHistory = new();

        // Gestion recave
        [ObservableProperty]
        private bool _canRebuy = false;

        [ObservableProperty]
        private string _rebuyMessage = string.Empty;

        [ObservableProperty]
        private int _rebuyCount = 0;

        [ObservableProperty]
        private decimal _rebuyAmount = 0;

        [ObservableProperty]
        private bool _showRebuySection = false;

        // Stats
        [ObservableProperty]
        private int _playersRemaining = 0;

        [ObservableProperty]
        private int _totalEliminations = 0;

        [ObservableProperty]
        private decimal _currentPrizePool = 0;

        // Indicateur de fin de tournoi
        [ObservableProperty]
        private bool _isTournamentFinished = false;

        [ObservableProperty]
        private string _winnerName = string.Empty;

        // Événement pour notifier le parent que le tournoi est fini
        public event EventHandler<string>? TournamentFinished;

        public EliminationViewModel(
            TournamentService tournamentService,
            PlayerService playerService,
            TableManagementService tableManagementService, // ← AJOUTÉ
            int tournamentId)
        {
            _tournamentService = tournamentService;
            _playerService = playerService;
            _tableManagementService = tableManagementService; // ← AJOUTÉ
            _tournamentId = tournamentId;
        }

        public async Task InitializeAsync()
        {
            _tournament = await _tournamentService.GetTournamentAsync(_tournamentId);
            if (_tournament == null) return;

            RebuyAmount = _tournament.RebuyAmount ?? 0;
            ShowRebuySection = _tournament.Type == TournamentType.RebuyUnlimited ||
                              _tournament.Type == TournamentType.RebuyLimited ||
                              _tournament.Type == TournamentType.DoubleChance;

            await RefreshPlayersAsync();
            await RefreshStatsAsync();
        }

        private async Task RefreshPlayersAsync()
        {
            var players = await _tournamentService.GetActivePlayers(_tournamentId);

            ActivePlayers.Clear();
            foreach (var player in players.OrderBy(p => p.Player!.Name))
            {
                ActivePlayers.Add(player);
            }

            // Mettre à jour la liste des killers disponibles
            UpdateAvailableKillers();

            PlayersRemaining = ActivePlayers.Count;
        }

        private void UpdateAvailableKillers()
        {
            AvailableKillers.Clear();

            foreach (var player in ActivePlayers)
            {
                // Ne pas inclure le joueur sélectionné pour élimination
                if (SelectedEliminatedPlayer == null || player.Id != SelectedEliminatedPlayer.Id)
                {
                    AvailableKillers.Add(player);
                }
            }
        }

        private async Task RefreshStatsAsync()
        {
            if (_tournament == null) return;

            // Recharger le tournoi
            _tournament = await _tournamentService.GetTournamentAsync(_tournamentId);
            if (_tournament == null) return;

            CurrentPrizePool = await _tournamentService.CalculatePrizePoolAsync(_tournamentId);

            var allPlayers = _tournament.Players.ToList();
            TotalEliminations = allPlayers.Count(p => p.IsEliminated);
        }

        partial void OnSelectedEliminatedPlayerChanged(TournamentPlayer? value)
        {
            // Mettre à jour la liste des killers disponibles pour exclure le joueur sélectionné
            UpdateAvailableKillers();

            // Réinitialiser le killer sélectionné si c'est le même que le joueur éliminé
            if (SelectedKillerPlayer != null && value != null && SelectedKillerPlayer.Id == value.Id)
            {
                SelectedKillerPlayer = null;
            }

            if (value != null)
            {
                _ = CheckRebuyAvailabilityAsync();
            }
        }

        private async Task CheckRebuyAvailabilityAsync()
        {
            if (SelectedEliminatedPlayer == null || _tournament == null) return;

            var playerId = SelectedEliminatedPlayer.PlayerId;

            // Vérifier si le joueur peut recaver
            CanRebuy = await _playerService.CanPlayerRebuyAsync(playerId, _tournamentId);
            RebuyCount = await _playerService.GetPlayerRebuyCountAsync(playerId, _tournamentId);

            if (CanRebuy)
            {
                if (_tournament.MaxRebuysPerPlayer > 0)
                {
                    int remaining = _tournament.MaxRebuysPerPlayer - RebuyCount;
                    RebuyMessage = $"✅ Recave disponible ({remaining} restante(s))";
                }
                else
                {
                    RebuyMessage = $"✅ Recave disponible (illimitées - {RebuyCount} effectuée(s))";
                }
            }
            else
            {
                var nextDate = await _playerService.GetNextRebuyAvailableDateAsync(playerId, _tournamentId);
                if (nextDate.HasValue)
                {
                    RebuyMessage = $"❌ Limite atteinte ({RebuyCount}/{_tournament.MaxRebuysPerPlayer})\n" +
                                  $"Prochaine recave le {nextDate.Value:dd/MM/yyyy à HH:mm}";
                }
                else
                {
                    RebuyMessage = $"❌ Recaves non autorisées pour ce tournoi";
                }
            }
        }

        [RelayCommand]
        private async Task EliminatePlayerAsync()
        {
            if (SelectedEliminatedPlayer == null)
            {
                CustomMessageBox.ShowWarning("Veuillez sélectionner un joueur à éliminer.", "Erreur");
                return;
            }

            // SÉCURITÉ: Vérifier qu'un joueur ne s'élimine pas lui-même
            if (SelectedKillerPlayer != null && SelectedKillerPlayer.Id == SelectedEliminatedPlayer.Id)
            {
                CustomMessageBox.ShowWarning("Un joueur ne peut pas s'éliminer lui-même !", "Erreur");
                return;
            }

            var playerName = SelectedEliminatedPlayer.Player?.Name ?? "Joueur";
            var killerName = SelectedKillerPlayer?.Player?.Name ?? "inconnu";

            // Calculer la position AVANT l'élimination
            var activePlayers = await _tournamentService.GetActivePlayers(_tournamentId);
            var position = activePlayers.Count;

            var result = CustomMessageBox.ShowConfirmation(
                $"Confirmer l'élimination de {playerName}" +
                (SelectedKillerPlayer != null ? $" par {killerName}" : "") +
                $"\n\nPosition finale : #{position}",
                "Confirmation");

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Sauvegarder les infos du joueur AVANT l'élimination
                    var eliminatedPlayerId = SelectedEliminatedPlayer.PlayerId;
                    var eliminatedPlayerTournamentId = SelectedEliminatedPlayer.Id;

                    // Éliminer le joueur
                    await _tournamentService.EliminatePlayerAsync(
                        SelectedEliminatedPlayer.Id,
                        SelectedKillerPlayer?.Id);

                    // ===== AUTO-ÉQUILIBRAGE APRÈS ÉLIMINATION - NOUVEAU =====
                    var balanceResult = await _tableManagementService.AutoBalanceAfterChangeAsync(_tournamentId);

                    string eliminationMessage = $"{playerName} éliminé(e) en position #{position}.";

                    // Si des mouvements ont eu lieu, notifier l'utilisateur
                    if (balanceResult.Movements.Any())
                    {
                        var movementDetails = string.Join("\n", balanceResult.Movements
                            .Take(5)
                            .Select(m => $"• {m.PlayerName}: Table {m.FromTable} → Table {m.ToTable}"));

                        if (balanceResult.Movements.Count > 5)
                        {
                            movementDetails += $"\n... et {balanceResult.Movements.Count - 5} autre(s) mouvement(s)";
                        }

                        eliminationMessage = $"{playerName} éliminé(e) en position #{position}.\n\n" +
                                           $"{balanceResult.Message}\n\n{movementDetails}";
                    }
                    // ===== FIN AUTO-ÉQUILIBRAGE =====

                    // Ajouter à l'historique
                    EliminationHistory.Insert(0, new EliminationHistoryItem
                    {
                        PlayerName = playerName,
                        PlayerId = SelectedEliminatedPlayer.Id,
                        KillerName = SelectedKillerPlayer != null ? killerName : "-",
                        KillerId = SelectedKillerPlayer?.Id,
                        Position = position,
                        Time = DateTime.Now
                    });

                    // Réinitialiser les sélections AVANT le refresh
                    SelectedEliminatedPlayer = null;
                    SelectedKillerPlayer = null;

                    await RefreshPlayersAsync();
                    await RefreshStatsAsync();

                    // Vérifier si le tournoi est terminé
                    if (PlayersRemaining == 1)
                    {
                        await HandleTournamentEndAsync();
                        return;
                    }

                    // NOUVEAU: Proposer la recave si autorisée
                    if (ShowRebuySection && await _playerService.CanPlayerRebuyAsync(eliminatedPlayerId, _tournamentId))
                    {
                        var rebuyResult = CustomMessageBox.ShowConfirmation(
                $"{playerName} souhaite-t-il recaver ?\n\n" +
                            $"Montant : {RebuyAmount:C}\n" +
                            $"Stack : {_tournament!.StartingStack:N0}",
                "Proposition de recave");

                        if (rebuyResult == MessageBoxResult.Yes)
                        {
                            // Recharger le tournoi pour avoir les données à jour
                            _tournament = await _tournamentService.GetTournamentAsync(_tournamentId);
                            var playerToRebuy = _tournament?.Players.FirstOrDefault(p => p.Id == eliminatedPlayerTournamentId);
                            if (playerToRebuy != null)
                            {
                                await ProcessRebuyAsync(playerToRebuy, playerName);
                            }
                        }
                        else
                        {
                            CustomMessageBox.ShowInformation(eliminationMessage, "Élimination confirmée");
                        }
                    }
                    else
                    {
                        CustomMessageBox.ShowInformation(eliminationMessage, "Élimination confirmée");
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.ShowError($"Erreur lors de l'élimination : {ex.Message}", "Erreur");
                }
            }
        }

        /// <summary>
        /// Traite la recave d'un joueur (utilisé après élimination ou manuellement)
        /// </summary>
        private async Task ProcessRebuyAsync(TournamentPlayer player, string playerName)
        {
            if (_tournament == null) return;

            try
            {
                // Enregistrer la recave
                await _playerService.RecordRebuyAsync(
                    player.PlayerId,
                    _tournamentId,
                    RebuyAmount);

                // Réactiver le joueur avec le starting stack
                player.IsEliminated = false;
                player.CurrentStack = _tournament.StartingStack;
                player.RebuyCount++;
                player.FinishPosition = null;
                player.EliminationTime = null;
                player.TableId = null;        // ← IMPORTANT : Réinitialiser la table
                player.SeatNumber = null;

                await _tournamentService.UpdateTournamentPlayerAsync(player);

                // Mettre à jour le total des rebuys du tournoi
                _tournament.TotalRebuys++;
                await _tournamentService.UpdateTournamentAsync(_tournament);

                // ===== REPLACER LE JOUEUR - NOUVEAU =====
                // Le joueur est de retour, il faut le replacer
                var assignment = await _tableManagementService.AssignLatePlayerAsync(player.Id);
                if (assignment == null)
                {
                    CustomMessageBox.ShowError(
                        "Impossible de replacer le joueur aux tables.",
                        "Erreur de placement");
                    return;
                }

                // ===== AUTO-ÉQUILIBRAGE APRÈS REBUY - NOUVEAU =====
                var balanceResult = await _tableManagementService.AutoBalanceAfterChangeAsync(_tournamentId);
                string rebuyMessage = $"✅ {playerName} a recavé !\n\nNouveau stack : {_tournament.StartingStack:N0}\nPrize Pool : {CurrentPrizePool:C}";

                if (balanceResult.Movements.Any())
                {
                    rebuyMessage += $"\n\n{balanceResult.Message}";
                }
                // ===== FIN AUTO-ÉQUILIBRAGE =====

                // Retirer de l'historique d'élimination
                var historyItem = EliminationHistory.FirstOrDefault(h => h.PlayerId == player.Id);
                if (historyItem != null)
                {
                    EliminationHistory.Remove(historyItem);
                }

                await RefreshPlayersAsync();
                await RefreshStatsAsync();

                System.Media.SystemSounds.Asterisk.Play();
                CustomMessageBox.ShowInformation(rebuyMessage, "Recave effectuée");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur lors de la recave : {ex.Message}", "Erreur");
            }
        }

        private async Task HandleTournamentEndAsync()
        {
            if (_tournament == null) return;

            IsTournamentFinished = true;

            // Trouver le gagnant
            var winner = ActivePlayers.FirstOrDefault();
            if (winner != null)
            {
                WinnerName = winner.Player?.Name ?? "Inconnu";

                // Mettre à jour le gagnant
                winner.FinishPosition = 1;
                winner.Winnings = CurrentPrizePool;
                await _tournamentService.UpdateTournamentPlayerAsync(winner);

                // Mettre à jour le tournoi
                _tournament.Status = TournamentStatus.Finished;
                _tournament.EndTime = DateTime.Now;
                await _tournamentService.UpdateTournamentAsync(_tournament);

                // Notifier
                TournamentFinished?.Invoke(this, WinnerName);

                CustomMessageBox.ShowInformation($"🏆 TOURNOI TERMINÉ ! 🏆\n\n" +
                    $"Vainqueur : {WinnerName}\n" +
                    $"Prize Pool : {CurrentPrizePool:C}", "Fin du Tournoi");
            }
        }

        [RelayCommand]
        private async Task RebuyPlayerAsync()
        {
            if (SelectedEliminatedPlayer == null || _tournament == null) return;

            if (!CanRebuy)
            {
                CustomMessageBox.ShowWarning(RebuyMessage, "Recave impossible");
                return;
            }

            var playerName = SelectedEliminatedPlayer.Player?.Name ?? "Joueur";

            var result = CustomMessageBox.ShowConfirmation(
                $"Confirmer la recave de {playerName} ?\n\n" +
                $"Montant : {RebuyAmount:C}\n" +
                $"Recave n°{RebuyCount + 1}",
                "Confirmation Recave");

            if (result == MessageBoxResult.Yes)
            {
                await ProcessRebuyAsync(SelectedEliminatedPlayer, playerName);
                SelectedEliminatedPlayer = null;
                SelectedKillerPlayer = null;
            }
        }

        [RelayCommand]
        private void ClearSelection()
        {
            SelectedEliminatedPlayer = null;
            SelectedKillerPlayer = null;
            CanRebuy = false;
            RebuyMessage = string.Empty;
        }

        [RelayCommand]
        private async Task UndoLastEliminationAsync()
        {
            if (!EliminationHistory.Any())
            {
                CustomMessageBox.ShowInformation("Aucune élimination à annuler.", "Info");
                return;
            }

            var lastElimination = EliminationHistory.First();

            var result = CustomMessageBox.ShowConfirmation(
                $"Annuler l'élimination de {lastElimination.PlayerName} ?" +
                (lastElimination.KillerId.HasValue ? $"\n\nLe kill de {lastElimination.KillerName} sera également retiré." : ""),
                "Confirmation");

            if (result == MessageBoxResult.Yes)
            {
                // Trouver le joueur éliminé
                var player = _tournament?.Players.FirstOrDefault(p =>
                    p.Id == lastElimination.PlayerId &&
                    p.IsEliminated);

                if (player != null)
                {
                    // Réactiver le joueur
                    player.IsEliminated = false;
                    player.FinishPosition = null;
                    player.EliminationTime = null;
                    player.EliminatedByPlayerId = null;

                    await _tournamentService.UpdateTournamentPlayerAsync(player);

                    // IMPORTANT: Retirer le kill au killer
                    if (lastElimination.KillerId.HasValue)
                    {
                        var killer = _tournament?.Players.FirstOrDefault(p => p.Id == lastElimination.KillerId.Value);
                        if (killer != null && killer.BountyKills > 0)
                        {
                            killer.BountyKills--;
                            await _tournamentService.UpdateTournamentPlayerAsync(killer);
                        }
                    }

                    EliminationHistory.Remove(lastElimination);

                    // ===== AUTO-ÉQUILIBRAGE APRÈS ANNULATION - NOUVEAU =====
                    await _tableManagementService.AutoBalanceAfterChangeAsync(_tournamentId);
                    // ===== FIN AUTO-ÉQUILIBRAGE =====

                    await RefreshPlayersAsync();
                    await RefreshStatsAsync();

                    CustomMessageBox.ShowSuccess("Élimination annulée.", "Succès");
                }
                else
                {
                    CustomMessageBox.ShowError("Impossible de trouver le joueur éliminé.", "Erreur");
                }
            }
        }
    }

    // Classe helper pour l'historique
    public class EliminationHistoryItem
    {
        public string PlayerName { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public string KillerName { get; set; } = string.Empty;
        public int? KillerId { get; set; }
        public int Position { get; set; }
        public DateTime Time { get; set; }
        public string TimeFormatted => Time.ToString("HH:mm:ss");
    }
}