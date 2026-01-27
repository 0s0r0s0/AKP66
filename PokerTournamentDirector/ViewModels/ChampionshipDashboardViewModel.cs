using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.ViewModels
{
    public partial class ChampionshipDashboardViewModel : ObservableObject
    {
        private readonly ChampionshipService _championshipService;
        private  int _championshipId;

        [ObservableProperty]
        private Championship? _championship;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        // === ONGLET CLASSEMENT ===
        [ObservableProperty]
        private ObservableCollection<ChampionshipStanding> _standings = new();

        [ObservableProperty]
        private ObservableCollection<ChampionshipStanding> _monthlyStandings = new();

        [ObservableProperty]
        private ObservableCollection<ChampionshipStanding> _quarterlyStandings = new();

        [ObservableProperty]
        private string _selectedStandingPeriod = "Général";

        public ObservableCollection<string> StandingPeriods { get; } = new()
        {
            "Général",
            "Mensuel",
            "Trimestriel"
        };

        // === ONGLET STATISTIQUES ===
        [ObservableProperty]
        private string _statsTimePeriod = "Toujours";

        public ObservableCollection<string> TimePeriods { get; } = new()
        {
            "Toujours",
            "Cette année",
            "Ce trimestre",
            "Ce mois",
            "10 dernières manches",
            "Personnalisé"
        };

        [ObservableProperty]
        private DateTime? _statsStartDate;

        [ObservableProperty]
        private DateTime? _statsEndDate;

        [ObservableProperty]
        private ChampionshipStanding? _selectedPlayerStats;

        [ObservableProperty]
        private ObservableCollection<PlayerStatistics> _playerStatistics = new();

        // === ONGLET MANCHES ===
        [ObservableProperty]
        private ObservableCollection<ChampionshipMatch> _matches = new();

        [ObservableProperty]
        private ChampionshipMatch? _selectedMatch;

        // === ONGLET JOUEURS ===
        [ObservableProperty]
        private ObservableCollection<ChampionshipStanding> _players = new();

        [ObservableProperty]
        private ChampionshipStanding? _selectedPlayer;

        [ObservableProperty]
        private string _playerSearchText = string.Empty;

        // === ONGLET LOGS ===
        [ObservableProperty]
        private ObservableCollection<ChampionshipLog> _logs = new();

        [ObservableProperty]
        private string _logFilterAction = "Toutes";

        public ObservableCollection<string> LogActions { get; } = new()
        {
            "Toutes",
            "Création/Modification",
            "Manches",
            "Joueurs",
            "Points",
            "Classements"
        };

        public ChampionshipDashboardViewModel(ChampionshipService championshipService, int championshipId)
        {
            _championshipService = championshipService;
            _championshipId = championshipId;
        }

        public async Task InitializeAsync()
        {
            await LoadChampionshipAsync();
            await LoadStandingsAsync();
            await LoadMatchesAsync();
            await LoadLogsAsync();
        }

        private async Task LoadChampionshipAsync()
        {
            Championship = await _championshipService.GetChampionshipAsync(_championshipId);
        }

        // === CLASSEMENT ===

        [RelayCommand]
        private Task LoadStandingsAsync()
        {
            if (Championship == null)
                return Task.CompletedTask;
            var standings = Championship.Standings
                .Where(s => s.IsActive)
                .OrderBy(s => s.CurrentPosition)
                .ToList();

            Standings.Clear();
            foreach (var s in standings)
            {
                Standings.Add(s);
            }

            return Task.CompletedTask;

            // TODO: Charger classements mensuels/trimestriels si activés
        }

        [RelayCommand]
        private async Task RefreshStandingsAsync()
        {
            await _championshipService.RecalculateStandingsAsync(_championshipId);
            await LoadStandingsAsync();
            Views.CustomMessageBox.ShowSuccess("Classements recalculés !", "Succès");
        }

        partial void OnSelectedStandingPeriodChanged(string value)
        {
            // Changer l'affichage selon la période sélectionnée
            // TODO: Filtrer sur mensuel/trimestriel
        }

        // === STATISTIQUES ===

        [RelayCommand]
        private async Task LoadStatisticsAsync()
        {
            if (Championship == null) return;

            PlayerStatistics.Clear();

            foreach (var standing in Standings)
            {
                var stats = await CalculatePlayerStatisticsAsync(standing);
                PlayerStatistics.Add(stats);
            }
        }

        private async Task<PlayerStatistics> CalculatePlayerStatisticsAsync(ChampionshipStanding standing)
        {
            // Calculer statistiques détaillées pour un joueur
            var stats = new PlayerStatistics
            {
                PlayerId = standing.PlayerId,
                PlayerName = standing.Player?.Name ?? "Inconnu",
                
                // Performances
                MatchesPlayed = standing.MatchesPlayed,
                Victories = standing.Victories,
                VictoryPercentage = standing.MatchesPlayed > 0 
                    ? (double)standing.Victories / standing.MatchesPlayed * 100 
                    : 0,
                Top3Count = standing.Top3Finishes,
                Top3Percentage = standing.MatchesPlayed > 0 
                    ? (double)standing.Top3Finishes / standing.MatchesPlayed * 100 
                    : 0,
                
                AveragePosition = (double)standing.AveragePosition,
                BestPosition = standing.BestPosition ?? 0,
                WorstPosition = standing.WorstPosition ?? 0,
                Regularity = standing.PositionStdDev,

                // Points et gains
                TotalPoints = standing.TotalPoints,
                AveragePoints = standing.MatchesPlayed > 0 
                    ? (double)standing.TotalPoints / standing.MatchesPlayed 
                    : 0,
                TotalWinnings = (double)standing.TotalWinnings,
                AverageWinnings = standing.MatchesPlayed > 0 
                    ? (double)standing.TotalWinnings / standing.MatchesPlayed 
                    : 0,
                ROI = (double)standing.ROI,

                // Éliminations
                TotalBounties = standing.TotalBounties,
                AverageBounties = standing.MatchesPlayed > 0 
                    ? (double)standing.TotalBounties / standing.MatchesPlayed 
                    : 0,

                // Temps de jeu
                TotalMinutesPlayed = standing.TotalMinutesPlayed,
                AverageMinutesPerMatch = standing.AverageMinutesPerMatch
            };

            // TODO: Ajouter stats "éliminé le plus souvent par" et "élimine le plus souvent"
            // En parsant les JSON EliminatedMostByPlayerId et EliminatedMostPlayerId

            return stats;
        }

        partial void OnStatsTimePeriodChanged(string value)
        {
            // Ajuster dates selon période
            var now = DateTime.Now;

            switch (value)
            {
                case "Cette année":
                    StatsStartDate = new DateTime(now.Year, 1, 1);
                    StatsEndDate = new DateTime(now.Year, 12, 31);
                    break;

                case "Ce trimestre":
                    var quarter = (now.Month - 1) / 3;
                    StatsStartDate = new DateTime(now.Year, quarter * 3 + 1, 1);
                    StatsEndDate = StatsStartDate.Value.AddMonths(3).AddDays(-1);
                    break;

                case "Ce mois":
                    StatsStartDate = new DateTime(now.Year, now.Month, 1);
                    StatsEndDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
                    break;

                case "Toujours":
                    StatsStartDate = null;
                    StatsEndDate = null;
                    break;

                case "Personnalisé":
                    // L'utilisateur choisit manuellement
                    break;
            }

            if (value != "Personnalisé")
            {
                _ = LoadStatisticsAsync();
            }
        }

        // === MANCHES ===

        [RelayCommand]
        private async Task LoadMatchesAsync()
        {
            if (Championship == null) return;

            var matches = Championship.Matches
                .OrderByDescending(m => m.MatchDate)
                .ToList();

            Matches.Clear();
            foreach (var m in matches)
            {
                Matches.Add(m);
            }
        }

        [RelayCommand]
        private void SwitchTab(string tabIndex)
        {
            if (int.TryParse(tabIndex, out int index))
            {
                SelectedTabIndex = index;
            }
        }

        [RelayCommand]
        private void ViewMatchDetails()
        {
            if (SelectedMatch == null)
            {
                CustomMessageBox.ShowWarning("Veuillez sélectionner une manche.", "Sélection requise");
                return;
            }

            // TODO: Ouvrir fenêtre détails de la manche
            // Afficher résultats complets du tournoi
        }

        // === JOUEURS ===

        [RelayCommand]
        private async Task SearchPlayersAsync()
        {
            if (Championship == null) return;

            var query = Standings.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(PlayerSearchText))
            {
                query = query.Where(s => 
                    s.Player.Name.Contains(PlayerSearchText, StringComparison.OrdinalIgnoreCase));
            }

            Players.Clear();
            foreach (var p in query.OrderBy(s => s.CurrentPosition))
            {
                Players.Add(p);
            }
        }

        [RelayCommand]
        private void ViewPlayerDetails()
        {
            if (SelectedPlayer == null)
            {
                CustomMessageBox.ShowWarning("Veuillez sélectionner un joueur.", "Sélection requise");
                return;
            }

            // Afficher fiche détaillée dans stats
            SelectedPlayerStats = SelectedPlayer;
            SelectedTabIndex = 1; // Onglet Statistiques
        }

        // === LOGS ===

        [RelayCommand]
        private async Task LoadLogsAsync()
        {
            var logs = await _championshipService.GetLogsAsync(_championshipId);

            Logs.Clear();
            foreach (var log in logs)
            {
                Logs.Add(log);
            }
        }

        partial void OnLogFilterActionChanged(string value)
        {
            _ = FilterLogsAsync();
        }

        private async Task FilterLogsAsync()
        {
            var allLogs = await _championshipService.GetLogsAsync(_championshipId);

            IEnumerable<ChampionshipLog> filtered = allLogs;

            switch (LogFilterAction)
            {
                case "Création/Modification":
                    filtered = allLogs.Where(l => 
                        l.Action == ChampionshipLogAction.ChampionshipCreated ||
                        l.Action == ChampionshipLogAction.ChampionshipModified ||
                        l.Action == ChampionshipLogAction.ChampionshipArchived ||
                        l.Action == ChampionshipLogAction.ChampionshipDeleted);
                    break;

                case "Manches":
                    filtered = allLogs.Where(l => 
                        l.Action == ChampionshipLogAction.MatchAdded ||
                        l.Action == ChampionshipLogAction.MatchRemoved ||
                        l.Action == ChampionshipLogAction.MatchCoefficientModified);
                    break;

                case "Joueurs":
                    filtered = allLogs.Where(l => 
                        l.Action == ChampionshipLogAction.PlayerAdded ||
                        l.Action == ChampionshipLogAction.PlayerRemoved ||
                        l.Action == ChampionshipLogAction.PlayerStatusChanged);
                    break;

                case "Points":
                    filtered = allLogs.Where(l => 
                        l.Action == ChampionshipLogAction.PointsAdjustedManually ||
                        l.Action == ChampionshipLogAction.PenaltyApplied ||
                        l.Action == ChampionshipLogAction.BonusGranted);
                    break;

                case "Classements":
                    filtered = allLogs.Where(l => 
                        l.Action == ChampionshipLogAction.StandingsRecalculated ||
                        l.Action == ChampionshipLogAction.IntermediateStandingGenerated ||
                        l.Action == ChampionshipLogAction.StandingExported ||
                        l.Action == ChampionshipLogAction.FinalStandingGenerated);
                    break;
            }

            Logs.Clear();
            foreach (var log in filtered)
            {
                Logs.Add(log);
            }
        }

        // === EXPORT ===

        [RelayCommand]
        private async Task ExportStandingsPdfAsync()
        {
            // TODO: Générer PDF du classement
            CustomMessageBox.ShowInformation("Export PDF à implémenter", "Info");
        }

        [RelayCommand]
        private async Task ExportStatisticsCsvAsync()
        {
            // TODO: Générer CSV des statistiques
            CustomMessageBox.ShowInformation("Export CSV à implémenter", "Info");
        }
    }

    // Classe helper pour statistiques détaillées
    public class PlayerStatistics
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;

        // Performances
        public int MatchesPlayed { get; set; }
        public int Victories { get; set; }
        public double VictoryPercentage { get; set; }
        public int Top3Count { get; set; }
        public double Top3Percentage { get; set; }
        public double AveragePosition { get; set; }
        public int BestPosition { get; set; }
        public int WorstPosition { get; set; }
        public double Regularity { get; set; } // Écart-type

        // Points
        public int TotalPoints { get; set; }
        public double AveragePoints { get; set; }

        // Gains
        public double TotalWinnings { get; set; }
        public double AverageWinnings { get; set; }
        public double ROI { get; set; }

        // Éliminations
        public int TotalBounties { get; set; }
        public double AverageBounties { get; set; }
        public string? EliminatedMostBy { get; set; }
        public string? EliminatesMost { get; set; }

        // Temps
        public int TotalMinutesPlayed { get; set; }
        public int AverageMinutesPerMatch { get; set; }
        public int TotalHoursPlayed => TotalMinutesPlayed / 60;
    }
}
