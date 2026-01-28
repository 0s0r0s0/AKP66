using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
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
        private readonly PokerDbContext _context;
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

        [ObservableProperty]
        private ObservableCollection<string> availablePeriods = new();

        [ObservableProperty]
        private string? selectedPeriod;

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

        [ObservableProperty]
        private string selectedStatsPlayer = "Tous";

        [ObservableProperty]
        private ObservableCollection<string> statsPlayerList = new();

        [ObservableProperty]
        private PlayerDetailedStats? detailedStats;

        // === ONGLET MANCHES ===
        [ObservableProperty]
        private ObservableCollection<ChampionshipMatch> _matches = new();
        [ObservableProperty]
        private ObservableCollection<MatchPlayerResult> _matchResults = new();

        [ObservableProperty]
        private string _matchDetailsTitle = "";

        [ObservableProperty]
        private bool _isMatchDetailsVisible = false;
        [ObservableProperty]
        private bool showCoefficientColumn = false;



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
            _context = new PokerDbContext();
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

            // Initialiser les périodes APRÈS chargement
            if (Championship != null && Championship.Matches.Any())
            {
                OnSelectedStandingPeriodChanged("Général"); // Force init
            }
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
            if (Championship == null) return;

            AvailablePeriods.Clear();

            // Debug : vérifier nombre de matches
            System.Diagnostics.Debug.WriteLine($"Matches count: {Championship.Matches?.Count ?? 0}");

            if (value == "Mensuel")
            {
                if (Championship.Matches == null || !Championship.Matches.Any())
                {
                    System.Diagnostics.Debug.WriteLine("Aucun match trouvé !");
                    return;
                }

                var months = Championship.Matches
                    .Select(m => m.MatchDate)
                    .GroupBy(d => new { d.Year, d.Month })
                    .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Mois trouvés: {months.Count}");

                foreach (var month in months)
                {
                    var monthName = $"{GetMonthName(month.Key.Month)} {month.Key.Year}";
                    AvailablePeriods.Add(monthName);
                    System.Diagnostics.Debug.WriteLine($"Ajout période: {monthName}");
                }

                if (AvailablePeriods.Any())
                    SelectedPeriod = AvailablePeriods.First();
            }
            else if (value == "Trimestriel")
            {
                if (Championship.Matches == null || !Championship.Matches.Any())
                    return;

                var quarters = Championship.Matches
                    .Select(m => m.MatchDate)
                    .GroupBy(d => new { d.Year, Quarter = (d.Month - 1) / 3 + 1 })
                    .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Quarter)
                    .ToList();

                foreach (var q in quarters)
                {
                    AvailablePeriods.Add($"Q{q.Key.Quarter} {q.Key.Year}");
                }

                if (AvailablePeriods.Any())
                    SelectedPeriod = AvailablePeriods.First();
            }

            if (value != "Général")
                _ = FilterStandingsByPeriodAsync();

            System.Diagnostics.Debug.WriteLine($"AvailablePeriods.Count = {AvailablePeriods.Count}");
            foreach (var p in AvailablePeriods)
            {
                System.Diagnostics.Debug.WriteLine($"  - {p}");
            }
        }

        partial void OnSelectedPeriodChanged(string? value)
        {
            _ = FilterStandingsByPeriodAsync();
        }

        // Dans ChampionshipDashboardViewModel.cs - REMPLACE FilterStandingsByPeriodAsync

        private async Task FilterStandingsByPeriodAsync()
        {
            if (Championship == null || string.IsNullOrEmpty(SelectedStandingPeriod) || string.IsNullOrEmpty(SelectedPeriod))
                return;

            // Parse la période sélectionnée
            DateTime startDate, endDate;

            if (SelectedStandingPeriod == "Mensuel")
            {
                var parts = SelectedPeriod.Split(' ');
                int month = GetMonthNumber(parts[0]);
                int year = int.Parse(parts[1]);
                startDate = new DateTime(year, month, 1);
                endDate = startDate.AddMonths(1).AddDays(-1);
            }
            else if (SelectedStandingPeriod == "Trimestriel")
            {
                var parts = SelectedPeriod.Split(' ');
                int quarter = int.Parse(parts[0].Replace("Q", ""));
                int year = int.Parse(parts[1]);
                int startMonth = (quarter - 1) * 3 + 1;
                startDate = new DateTime(year, startMonth, 1);
                endDate = startDate.AddMonths(3).AddDays(-1);
            }
            else
            {
                await LoadStandingsAsync();
                return;
            }

            // Filtrer standings (affiche juste tous pour l'instant)
            // TODO: Calcul réel par période dans ChampionshipService
            Standings.Clear();

            var allStandings = Championship.Standings
                .OrderBy(s => s.CurrentPosition)
                .ToList();

            foreach (var s in allStandings)
                Standings.Add(s);
        }
        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            Championship = await _championshipService.GetChampionshipAsync(_championshipId);
            await LoadStandingsAsync();
            await LoadMatchesAsync();
            OnSelectedStandingPeriodChanged(SelectedStandingPeriod);
            await LoadStatisticsAsync();
        }
        private string GetMonthName(int month) => month switch
        {
            1 => "Janvier",
            2 => "Février",
            3 => "Mars",
            4 => "Avril",
            5 => "Mai",
            6 => "Juin",
            7 => "Juillet",
            8 => "Août",
            9 => "Septembre",
            10 => "Octobre",
            11 => "Novembre",
            12 => "Décembre",
            _ => ""
        };

        private int GetMonthNumber(string month) => month switch
        {
            "Janvier" => 1,
            "Février" => 2,
            "Mars" => 3,
            "Avril" => 4,
            "Mai" => 5,
            "Juin" => 6,
            "Juillet" => 7,
            "Août" => 8,
            "Septembre" => 9,
            "Octobre" => 10,
            "Novembre" => 11,
            "Décembre" => 12,
            _ => 1
        };

        // === STATISTIQUES ===
        [RelayCommand]
        private async Task LoadStatisticsAsync()
        {
            if (Championship == null) return;

            // Remplir liste des joueurs
            StatsPlayerList.Clear();
            StatsPlayerList.Add("Tous");

            // Recharger Standings si vide
            if (!Standings.Any())
            {
                await LoadStandingsAsync();
            }

            foreach (var s in Standings.OrderBy(s => s.CurrentPosition))
            {
                var playerName = s.Player?.Name ?? "Inconnu";
                System.Diagnostics.Debug.WriteLine($"Ajout joueur stats: {playerName}");
                StatsPlayerList.Add(playerName);
            }

            System.Diagnostics.Debug.WriteLine($"Total joueurs: {StatsPlayerList.Count}");

            if (string.IsNullOrEmpty(SelectedStatsPlayer) || SelectedStatsPlayer == "Tous")
            {
                DetailedStats = null;
                PlayerStatistics.Clear();

                foreach (var standing in Standings.OrderBy(s => s.CurrentPosition))
                {
                    var stats = await CalculatePlayerDetailedStatsAsync(standing);
                    PlayerStatistics.Add(new PlayerStatistics
                    {
                        PlayerName = stats.PlayerName,
                        MatchesPlayed = stats.MatchesPlayed,
                        Victories = stats.Victories,
                        VictoryPercentage = stats.WinRate,
                        Top3Count = stats.Top3Count,
                        Top3Percentage = stats.Top3Rate,
                        AveragePosition = stats.AveragePosition,
                        TotalPoints = stats.TotalPoints,
                        AveragePoints = stats.AveragePoints,
                        TotalBounties = stats.TotalBounties,
                        TotalWinnings = (double)stats.TotalWinnings,
                        ROI = stats.ROI
                    });
                }
            }
            else
            {
                var standing = Standings.FirstOrDefault(s => s.Player?.Name == SelectedStatsPlayer);
                if (standing != null)
                {
                    DetailedStats = await CalculatePlayerDetailedStatsAsync(standing);
                }
            }
            System.Diagnostics.Debug.WriteLine($"StatsPlayerList.Count = {StatsPlayerList.Count}");
            foreach (var p in StatsPlayerList)
            {
                System.Diagnostics.Debug.WriteLine($"  - {p}");
            }
        }

        private async Task<PlayerDetailedStats> CalculatePlayerDetailedStatsAsync(ChampionshipStanding standing)
        {
            var performances = await GetPlayerPerformancesWithDetailsAsync(Championship.Id, standing.PlayerId);

            var stats = new PlayerDetailedStats
            {
                PlayerName = standing.Player?.Name ?? "Inconnu",
                MatchesPlayed = standing.MatchesPlayed,
                CurrentRank = standing.CurrentPosition,
                TotalPoints = standing.TotalPoints,

                // Performance
                Victories = standing.Victories,
                WinRate = standing.MatchesPlayed > 0 ? (double)standing.Victories / standing.MatchesPlayed * 100 : 0,
                Top3Count = standing.Top3Finishes,
                Top3Rate = standing.MatchesPlayed > 0 ? (double)standing.Top3Finishes / standing.MatchesPlayed * 100 : 0,
                AveragePosition = (double)standing.AveragePosition,
                BestPosition = standing.BestPosition ?? 0,
                WorstPosition = standing.WorstPosition ?? 0,

                // Points
                AveragePoints = standing.MatchesPlayed > 0 ? (double)standing.TotalPoints / standing.MatchesPlayed : 0,
                BestPointsMatch = performances.Any() ? performances.Max(p => p.Points) : 0,
                WorstPointsMatch = performances.Any() ? performances.Min(p => p.Points) : 0,

                // Bounties
                TotalBounties = standing.TotalBounties,
                AverageBounties = standing.MatchesPlayed > 0 ? (double)standing.TotalBounties / standing.MatchesPlayed : 0,
                BestBountiesMatch = performances.Any() ? performances.Max(p => p.Bounties) : 0,

                // Financier
                TotalWinnings = standing.TotalWinnings,
                AverageWinnings = standing.MatchesPlayed > 0 ? standing.TotalWinnings / standing.MatchesPlayed : 0,
                BestWinningsMatch = performances.Any() ? performances.Max(p => p.Winnings) : 0,
                ROI = (double)standing.ROI,

                // Progression (5 derniers)
                FormRecent = string.Join(" ", performances.OrderByDescending(p => p.Date).Take(5).Select(p =>
                    p.Position == 1 ? "🥇" : p.Position == 2 ? "🥈" : p.Position == 3 ? "🥉" : p.Position <= 5 ? "⭐" : "•")),
                PositionChange = standing.CurrentPosition - (standing.PreviousPosition ?? standing.CurrentPosition),
                Trend = GetTrend(standing.CurrentPosition, standing.PreviousPosition),

                // Meilleurs matchs
                BestMatches = performances.OrderByDescending(p => p.Points).Take(3).Select(p => new MatchHighlight
                {
                    MatchName = p.MatchName,
                    Date = p.Date,
                    Position = p.Position,
                    Points = p.Points,
                    Bounties = p.Bounties,
                    Winnings = p.Winnings
                }).ToList(),

                // Pires matchs
                WorstMatches = performances.OrderBy(p => p.Points).Take(3).Select(p => new MatchHighlight
                {
                    MatchName = p.MatchName,
                    Date = p.Date,
                    Position = p.Position,
                    Points = p.Points,
                    Bounties = p.Bounties,
                    Winnings = p.Winnings
                }).ToList()
            };

            return stats;
        }

        private async Task<List<PerformanceDetail>> GetPlayerPerformancesWithDetailsAsync(int championshipId, int playerId)
        {
            var matches = await _context.ChampionshipMatches
                .Where(m => m.ChampionshipId == championshipId)
                .Include(m => m.Tournament)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();

            var performances = new List<PerformanceDetail>();

            foreach (var match in matches)
            {
                var tp = await _context.TournamentPlayers
                    .FirstOrDefaultAsync(t => t.TournamentId == match.TournamentId && t.PlayerId == playerId);

                if (tp != null && tp.FinishPosition.HasValue)
                {
                    performances.Add(new PerformanceDetail
                    {
                        MatchName = match.Tournament?.Name ?? "",
                        Date = match.MatchDate,
                        Position = tp.FinishPosition.Value,
                        Points = tp.ChampionshipPoints,
                        Bounties = tp.BountyKills,
                        Winnings = tp.Winnings ?? 0
                    });
                }
            }

            return performances;
        }

        private string GetTrend(int current, int? previous)
        {
            if (!previous.HasValue || previous == current) return "➡️";
            return current < previous ? "⬆️" : "⬇️";
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
        private async Task ViewMatchDetailsAsync()
        {
            if (SelectedMatch == null)
            {
                CustomMessageBox.ShowWarning("Veuillez sélectionner une manche.", "Sélection requise");
                return;
            }

            try
            {
                var match = await _context.ChampionshipMatches
                    .Include(m => m.Tournament)
                        .ThenInclude(t => t.Players)
                            .ThenInclude(tp => tp.Player)
                    .Include(m => m.Championship)
                    .FirstOrDefaultAsync(m => m.Id == SelectedMatch.Id);

                if (match?.Tournament == null)
                {
                    CustomMessageBox.ShowError("Impossible de charger les détails de la manche.");
                    return;
                }

                var championship = match.Championship;
                var tournament = match.Tournament;

                var rebuys = await _context.PlayerRebuys
                    .Where(r => r.TournamentId == tournament.Id)
                    .GroupBy(r => r.PlayerId)
                    .Select(g => new { PlayerId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var rebuyDict = rebuys.ToDictionary(r => r.PlayerId, r => r.Count);

                MatchResults.Clear();

                foreach (var tp in tournament.Players.Where(p => p.FinishPosition.HasValue).OrderBy(p => p.FinishPosition))
                {
                    var perf = new PlayerPerformance
                    {
                        MatchId = match.Id,
                        Position = tp.FinishPosition.Value,
                        Bounties = tp.BountyKills,
                        Winnings = tp.Winnings ?? 0,
                        Coefficient = match.Coefficient
                    };

                    int points = CalculateMatchPoints(championship, perf);
                    int displayPoints = (int)(points * match.Coefficient);

                    MatchResults.Add(new MatchPlayerResult
                    {
                        Position = tp.FinishPosition.Value,
                        PlayerName = tp.Player?.Name ?? "Inconnu",
                        BasePoints = points,
                        Coefficient = match.Coefficient,
                        TotalPoints = displayPoints,
                        Bounties = tp.BountyKills,
                        Rebuys = tp.RebuyCount, // 
                        Winnings = tp.Winnings ?? 0,
                        ShowCoefficient = match.Coefficient != 1.0m
                    });

                    ShowCoefficientColumn = MatchResults.Any(r => r.Coefficient != 1.0m);
                }

                

                MatchDetailsTitle = $"Manche #{match.MatchNumber} - {tournament.Name} ({match.MatchDate:dd/MM/yyyy})";

                var detailsWindow = new MatchDetailsWindow
                {
                    DataContext = this,
                    Owner = System.Windows.Application.Current.MainWindow
                };
                detailsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur lors du chargement : {ex.Message}");
            }
        }

        private int CalculateMatchPoints(Championship championship, PlayerPerformance perf)
        {
            int basePoints = 0;

            switch (championship.PointsMode)
            {
                case ChampionshipPointsMode.Linear:
                    basePoints = Math.Max(1, championship.LinearFirstPlacePoints - (perf.Position - 1));
                    break;

                case ChampionshipPointsMode.FixedByPosition:
                    basePoints = GetFixedPointsForPosition(championship, perf.Position);
                    break;

                case ChampionshipPointsMode.ProportionalPrizePool:
                    if (perf.Winnings > 0)
                        basePoints = (int)((perf.Winnings / Math.Max(1, perf.TotalPrizePool)) * championship.ProportionalTotalPoints);
                    break;
            }

            if (championship.EnableParticipationPoints)
                basePoints += championship.ParticipationPoints;

            return basePoints;
        }

        private int GetFixedPointsForPosition(Championship championship, int position)
        {
            if (string.IsNullOrEmpty(championship.FixedPointsTable)) return 0;

            try
            {
                var table = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(championship.FixedPointsTable);
                if (table == null) return 0;

                if (table.ContainsKey(position.ToString()))
                    return table[position.ToString()];

                foreach (var kvp in table)
                {
                    if (kvp.Key.Contains("-"))
                    {
                        var parts = kvp.Key.Split('-');
                        if (int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                        {
                            if (position >= min && position <= max)
                                return kvp.Value;
                        }
                    }
                }
            }
            catch { }

            return 0;
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

    // Classe helper pour les résultats
    public class MatchPlayerResult
    {
        public int Position { get; set; }
        public string PlayerName { get; set; } = "";
        public int BasePoints { get; set; }
        public decimal Coefficient { get; set; }
        public int TotalPoints { get; set; }
        public int Bounties { get; set; }
        public int Rebuys { get; set; }
        public decimal Winnings { get; set; }
        public bool ShowCoefficient { get; set; } 
    }

    public class PlayerDetailedStats
    {
        // Général
        public string PlayerName { get; set; } = "";
        public int MatchesPlayed { get; set; }
        public int CurrentRank { get; set; }
        public int TotalPoints { get; set; }

        // Performance
        public int Victories { get; set; }
        public double WinRate { get; set; }
        public int Top3Count { get; set; }
        public double Top3Rate { get; set; }
        public double AveragePosition { get; set; }
        public int BestPosition { get; set; }
        public int WorstPosition { get; set; }

        // Points
        public double AveragePoints { get; set; }
        public int BestPointsMatch { get; set; }
        public int WorstPointsMatch { get; set; }

        // Bounties
        public int TotalBounties { get; set; }
        public double AverageBounties { get; set; }
        public int BestBountiesMatch { get; set; }

        // Financier
        public decimal TotalWinnings { get; set; }
        public decimal AverageWinnings { get; set; }
        public decimal BestWinningsMatch { get; set; }
        public double ROI { get; set; }

        // Progression
        public string FormRecent { get; set; } = ""; // 5 derniers résultats
        public int PositionChange { get; set; }
        public string Trend { get; set; } = ""; // "⬆️", "⬇️", "➡️"

        // Matchs mémorables
        public List<MatchHighlight> BestMatches { get; set; } = new();
        public List<MatchHighlight> WorstMatches { get; set; } = new();
    }

    public class MatchHighlight
    {
        public string MatchName { get; set; } = "";
        public DateTime Date { get; set; }
        public int Position { get; set; }
        public int Points { get; set; }
        public int Bounties { get; set; }
        public decimal Winnings { get; set; }
    }
    public class PerformanceDetail
    {
        public string MatchName { get; set; } = "";
        public DateTime Date { get; set; }
        public int Position { get; set; }
        public int Points { get; set; }
        public int Bounties { get; set; }
        public decimal Winnings { get; set; }
    }
}
