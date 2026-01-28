using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.ViewModels
{
    public partial class TournamentEndValidationViewModel : ObservableObject
    {
        private readonly TournamentService _tournamentService;
        private readonly ChampionshipService? _championshipService;
        private readonly int _tournamentId;
        private readonly int? _championshipId;

        [ObservableProperty]
        private Tournament _tournament = null!;

        [ObservableProperty]
        private ObservableCollection<TournamentPlayerResult> _results = new();

        [ObservableProperty]
        private TournamentPlayerResult? _selectedResult;

        [ObservableProperty]
        private bool _isValidating = false;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        public bool IsChampionshipMatch => _championshipId.HasValue;

        public TournamentEndValidationViewModel(
            TournamentService tournamentService,
            ChampionshipService? championshipService,
            int tournamentId,
            int? championshipId = null)
        {
            _tournamentService = tournamentService;
            _championshipService = championshipService;
            _tournamentId = tournamentId;
            _championshipId = championshipId;
        }

        public async Task InitializeAsync()
        {
            Tournament = await _tournamentService.GetTournamentAsync(_tournamentId);

            // Charger les résultats
            var players = Tournament.Players
                .Where(p => p.FinishPosition.HasValue)
                .OrderBy(p => p.FinishPosition)
                .ToList();

            Results.Clear();
            foreach (var player in players)
            {
                Results.Add(new TournamentPlayerResult
                {
                    TournamentPlayerId = player.Id,
                    PlayerId = player.PlayerId,
                    PlayerName = player.Player.Name,
                    OriginalPosition = player.FinishPosition!.Value,
                    CurrentPosition = player.FinishPosition!.Value,
                    Bounties = player.BountyKills,
                    Winnings = player.Winnings ?? 0,
                    CanEdit = true
                });
            }
        }

        [RelayCommand]
        private void EditPosition(TournamentPlayerResult result)
        {
            SelectedResult = result;
            // Ouvrir dialogue modification
        }

        [RelayCommand]
        private async Task ValidateAndProcessAsync()
        {
            // Vérifier doublons positions
            var duplicates = Results.GroupBy(r => r.CurrentPosition)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                CustomMessageBox.ShowWarning(
                    $"Positions en doublon détectées : {string.Join(", ", duplicates)}\n\nVeuillez corriger.",
                    "Erreur de validation");
                return;
            }

            // Vérifier que toutes les positions sont présentes
            var expectedPositions = Enumerable.Range(1, Results.Count).ToList();
            var actualPositions = Results.Select(r => r.CurrentPosition).OrderBy(p => p).ToList();

            if (!expectedPositions.SequenceEqual(actualPositions))
            {
                CustomMessageBox.ShowWarning(
                    "Les positions ne sont pas continues (1, 2, 3...)\n\nVeuillez corriger.",
                    "Erreur de validation");
                return;
            }

            // Confirmation
            var hasChanges = Results.Any(r => r.CurrentPosition != r.OriginalPosition);

            if (hasChanges)
            {
                var changesText = string.Join("\n",
                    Results.Where(r => r.CurrentPosition != r.OriginalPosition)
                    .Select(r => $"  • {r.PlayerName} : {r.OriginalPosition} → {r.CurrentPosition}"));

                var confirmResult = CustomMessageBox.ShowConfirmation(
                    $"⚠️ Modifications détectées :\n\n{changesText}\n\nConfirmer ces changements ?",
                    "Confirmer les modifications");

                if (confirmResult != System.Windows.MessageBoxResult.Yes)
                    return;
            }
            else
            {
                var confirmResult = CustomMessageBox.ShowConfirmation(
                    "Valider le classement et calculer les points du championnat ?",
                    "Validation finale");

                if (confirmResult != System.Windows.MessageBoxResult.Yes)
                    return;
            }

            // Traitement avec loader
            IsValidating = true;
            ValidationMessage = "Sauvegarde des modifications...";

            try
            {
                // 1. Appliquer les modifications si nécessaire
                if (hasChanges)
                {
                    await ApplyPositionChangesAsync();
                }

                // 2. Si championnat, calculer les points
                if (IsChampionshipMatch && _championshipService != null && _championshipId.HasValue)
                {
                    ValidationMessage = "Calcul des points du championnat...";
                    await Task.Delay(500); // Laisser l'UI se rafraîchir

                    await _championshipService.RecalculateStandingsAsync(_championshipId.Value);

                    ValidationMessage = "Sauvegarde du classement...";
                    await Task.Delay(300);
                }

                // 3. Archiver le tournoi (historique)
                ValidationMessage = "Archivage de l'historique...";
                await ArchiveTournamentAsync();

                // 4. Export (optionnel)
                ValidationMessage = "Génération du rapport...";
                await ExportTournamentResultsAsync();

                ValidationMessage = "Terminé !";
                await Task.Delay(500);

                // Succès
                CustomMessageBox.ShowSuccess(
                    IsChampionshipMatch
                        ? "Tournoi validé et points calculés !\n\nLe classement du championnat a été mis à jour."
                        : "Tournoi validé et archivé avec succès !",
                    "Validation réussie");

                // Proposer d'ouvrir le dashboard si championnat
                if (IsChampionshipMatch && _championshipId.HasValue)
                {
                    var openDashboard = CustomMessageBox.ShowConfirmation(
                        "Voulez-vous voir le classement actualisé du championnat ?",
                        "Voir le classement");

                    if (openDashboard == System.Windows.MessageBoxResult.Yes)
                    {
                        var context = App.Services.GetRequiredService<PokerDbContext>();

                        var dashboard = new Views.ChampionshipDashboardView(
                            _championshipId.Value,
                            _championshipService, context);
                        dashboard.Show();
                    }
                }

                // Fermer cette fenêtre
                CloseWindow?.Invoke();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError(
                    $"Erreur lors du traitement :\n\n{ex.Message}",
                    "Erreur");
            }
            finally
            {
                IsValidating = false;
                ValidationMessage = string.Empty;
            }
        }

        private async Task ApplyPositionChangesAsync()
        {
            // Recharger le tournoi complet avec les joueurs
            var fullTournament = await _tournamentService.GetTournamentAsync(_tournamentId);

            foreach (var result in Results.Where(r => r.CurrentPosition != r.OriginalPosition))
            {
                var tournamentPlayer = fullTournament.Players
                    .FirstOrDefault(p => p.Id == result.TournamentPlayerId);

                if (tournamentPlayer != null)
                {
                    var oldPosition = tournamentPlayer.FinishPosition;
                    tournamentPlayer.FinishPosition = result.CurrentPosition;

                    // Sauvegarder via le contexte ou service
                    await _tournamentService.UpdateTournamentAsync(fullTournament);

                    // Log modification (si championnat)
                    if (_championshipId.HasValue && _championshipService != null)
                    {
                        var log = new ChampionshipLog
                        {
                            ChampionshipId = _championshipId.Value,
                            Action = ChampionshipLogAction.PointsAdjustedManually,
                            Description = $"Position de {result.PlayerName} modifiée : {oldPosition} → {result.CurrentPosition} (Tournoi: {Tournament.Name})",
                            Timestamp = DateTime.Now,
                            Username = Environment.UserName
                        };

                        // Appeler la méthode publique du service pour sauvegarder le log
                        await _championshipService.SaveLogAsync(log);
                    }
                }
            }
        }

        private async Task ArchiveTournamentAsync()
        {
            // TODO: Créer un snapshot JSON du tournoi complet
            // Sauvegarder dans un dossier "Archives/{Year}/{TournamentName}_{Date}.json"

            Tournament.Status = TournamentStatus.Finished;
            Tournament.EndTime = DateTime.Now;
            await _tournamentService.UpdateTournamentAsync(Tournament);
        }

        private async Task ExportTournamentResultsAsync()
        {
            // TODO: Générer PDF du classement
            // Sauvegarder dans "Reports/{Year}/{TournamentName}_{Date}.pdf"
            await Task.CompletedTask;
        }

        [RelayCommand]
        private void Cancel()
        {
            var result = CustomMessageBox.ShowConfirmation(
                "Annuler la validation ?\n\nLe tournoi ne sera pas finalisé.",
                "Annuler");

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                CloseWindow?.Invoke();
            }
        }

        // Événement pour fermer la fenêtre
        public Action? CloseWindow { get; set; }
    }

    // Classe helper pour afficher les résultats
    public partial class TournamentPlayerResult : ObservableObject
    {
        public int TournamentPlayerId { get; set; }
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;

        [ObservableProperty]
        private int _originalPosition;

        [ObservableProperty]
        private int _currentPosition;

        [ObservableProperty]
        private int _bounties;

        [ObservableProperty]
        private decimal _winnings;

        [ObservableProperty]
        private bool _canEdit;

        public bool IsModified => CurrentPosition != OriginalPosition;
    }
}
