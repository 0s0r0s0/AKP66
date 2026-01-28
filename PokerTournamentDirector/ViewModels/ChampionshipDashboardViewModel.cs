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
        private int _championshipId;

        [ObservableProperty]
        private Championship? _championship;

        [ObservableProperty]
        private int _selectedTabIndex = 0;

        // === CLASSEMENT ===
        [ObservableProperty]
        private ObservableCollection<ChampionshipStanding> _standings = new();

        [ObservableProperty]
        private string _selectedStandingPeriod = "Général";

        public ObservableCollection<string> StandingPeriods { get; } = new()
        {
            "Général",
            "Mensuel",
            "Trimestriel"
        };

        [ObservableProperty]
        private ObservableCollection<string> _availablePeriods = new();

        [ObservableProperty]
        private string? _selectedPeriod;

        // === STATISTIQUES COMPLÈTES ===
        [ObservableProperty]
        private ObservableCollection<PlayerCompleteStat> _completeStats = new();

        // === MANCHES ===
        [ObservableProperty]
        private ObservableCollection<ChampionshipMatch> _matches = new();

        [ObservableProperty]
        private ChampionshipMatch? _selectedMatch;

        [ObservableProperty]
        private ObservableCollection<MatchPlayerResult> _matchResults = new();

        [ObservableProperty]
        private string _matchDetailsTitle = "";

        // === JOUEURS ===
        [ObservableProperty]
        private ObservableCollection<ChampionshipStanding> _players = new();

        [ObservableProperty]
        private ChampionshipStanding? _selectedPlayer;

        [ObservableProperty]
        private string _playerSearchText = string.Empty;

        // === LOGS ===
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
            await LoadCompleteStatsAsync();
        }

        private async Task LoadChampionshipAsync()
        {
            Championship = await _championshipService.GetChampionshipAsync(_championshipId);

            if (Championship != null && Championship.Matches.Any())
            {
                OnSelectedStandingPeriodChanged("Général");
            }
        }

        // === CLASSEMENT ===
        [RelayCommand]
        private async Task LoadStandingsAsync()
        {
            if (Championship == null) return;

            await FilterStandingsByPeriodAsync();
        }

        [RelayCommand]
        private async Task RefreshStandingsAsync()
        {
            await _championshipService.RecalculateStandingsAsync(_championshipId);
            Championship = await _championshipService.GetChampionshipAsync(_championshipId);
            await LoadStandingsAsync();
            CustomMessageBox.ShowSuccess("Classements recalculés !", "Succès");
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            Championship = await _championshipService.GetChampionshipAsync(_championshipId);
            await LoadStandingsAsync();
            await LoadMatchesAsync();
            await LoadLogsAsync();
            await LoadCompleteStatsAsync();
            OnSelectedStandingPeriodChanged(SelectedStandingPeriod);
        }

        partial void OnSelectedStandingPeriodChanged(string value)
        {
            if (Championship == null) return;

            AvailablePeriods.Clear();

            if (value == "Mensuel")
            {
                if (Championship.Matches == null || !Championship.Matches.Any()) return;

                var months = Championship.Matches
                    .Select(m => m.MatchDate)
                    .GroupBy(d => new { d.Year, d.Month })
                    .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
                    .ToList();

                foreach (var month in months)
                {
                    AvailablePeriods.Add($"{GetMonthName(month.Key.Month)} {month.Key.Year}");
                }

                if (AvailablePeriods.Any())
                    SelectedPeriod = AvailablePeriods.First();
            }
            else if (value == "Trimestriel")
            {
                if (Championship.Matches == null || !Championship.Matches.Any()) return;

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
        }

        partial void OnSelectedPeriodChanged(string? value)
        {
            _ = FilterStandingsByPeriodAsync();
        }

        private async Task FilterStandingsByPeriodAsync()
        {
            if (Championship == null) return;

            Standings.Clear();

            if (SelectedStandingPeriod == "Général")
            {
                // Affiche les standings déjà calculés par ChampionshipService
                var allStandings = Championship.Standings
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.CurrentPosition)
                    .ToList();

                foreach (var s in allStandings)
                    Standings.Add(s);
                return;
            }

            if (string.IsNullOrEmpty(SelectedPeriod)) return;

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
                return;
            }

            // Filtrer les matches de la période
            var periodMatches = Championship.Matches
                .Where(m => m.MatchDate >= startDate && m.MatchDate <= endDate)
                .ToList();

            // Recalculer pour chaque joueur
            var playerStats = new Dictionary<int, PeriodStanding>();

            foreach (var match in periodMatches)
            {
                var tournamentPlayers = await _context.TournamentPlayers
                    .Where(tp => tp.TournamentId == match.TournamentId && tp.FinishPosition.HasValue)
                    .Include(tp => tp.Player)
                    .ToListAsync();

                foreach (var tp in tournamentPlayers)
                {
                    if (!playerStats.ContainsKey(tp.PlayerId))
                    {
                        playerStats[tp.PlayerId] = new PeriodStanding
                        {
                            PlayerId = tp.PlayerId,
                            PlayerName = tp.Player?.Name ?? "Inconnu"
                        };
                    }

                    var stat = playerStats[tp.PlayerId];

                    // Calculer points avec TOUTES les règles
                    int basePoints = CalculateMatchPointsForPlayer(Championship, tp.FinishPosition.Value);
                    basePoints = (int)(basePoints * match.Coefficient);

                    // Bonus bounties
                    if (Championship.CountBounties)
                    {
                        basePoints += tp.BountyKills * Championship.PointsPerBounty;
                    }

                    // Bonus victoire
                    if (tp.FinishPosition == 1 && Championship.VictoryBonus > 0)
                    {
                        basePoints += Championship.VictoryBonus;
                    }

                    // Bonus top 3
                    if (tp.FinishPosition <= 3 && Championship.Top3Bonus > 0)
                    {
                        basePoints += Championship.Top3Bonus;
                    }

                    // Pénalité recaves
                    if (tp.RebuyCount > 0)
                    {
                        basePoints -= tp.RebuyCount * Championship.RebuyPointsPenalty;
                        basePoints = (int)(basePoints * Championship.RebuyPointsMultiplier);
                    }

                    stat.Performances.Add(new PeriodPerformance
                    {
                        Points = basePoints,
                        Position = tp.FinishPosition.Value,
                        Bounties = tp.BountyKills,
                        Winnings = tp.Winnings ?? 0
                    });

                    stat.MatchesPlayed++;
                    if (tp.FinishPosition == 1) stat.Victories++;
                    if (tp.FinishPosition <= 3) stat.Top3++;
                    stat.TotalBounties += tp.BountyKills;
                    stat.TotalWinnings += tp.Winnings ?? 0;
                }
            }

            // Appliquer BestXPerMonth / BestXPerQuarter
            foreach (var stat in playerStats.Values)
            {
                int? bestX = null;

                if (SelectedStandingPeriod == "Mensuel" && Championship.BestXPerMonth.HasValue)
                {
                    bestX = Championship.BestXPerMonth.Value;
                }
                else if (SelectedStandingPeriod == "Trimestriel" && Championship.BestXPerQuarter.HasValue)
                {
                    bestX = Championship.BestXPerQuarter.Value;
                }

                if (bestX.HasValue && stat.Performances.Count > bestX.Value)
                {
                    var bestPerfs = stat.Performances
                        .OrderByDescending(p => p.Points)
                        .Take(bestX.Value)
                        .ToList();

                    stat.TotalPoints = bestPerfs.Sum(p => p.Points);
                    stat.AveragePosition = bestPerfs.Average(p => (double)p.Position);
                }
                else
                {
                    stat.TotalPoints = stat.Performances.Sum(p => p.Points);
                    stat.AveragePosition = stat.Performances.Any() ? stat.Performances.Average(p => (double)p.Position) : 0;
                }
            }

            var orderedStats = playerStats.Values.OrderByDescending(s => s.TotalPoints).ToList();
            for (int i = 0; i < orderedStats.Count; i++)
            {
                var stat = orderedStats[i];
                Standings.Add(new ChampionshipStanding
                {
                    CurrentPosition = i + 1,
                    PlayerId = stat.PlayerId,
                    Player = new Player { Name = stat.PlayerName },
                    TotalPoints = stat.TotalPoints,
                    MatchesPlayed = stat.MatchesPlayed,
                    Victories = stat.Victories,
                    Top3Finishes = stat.Top3,
                    TotalBounties = stat.TotalBounties,
                    TotalWinnings = stat.TotalWinnings,
                    AveragePosition = (decimal)stat.AveragePosition,
                    IsActive = true
                });
            }
        }

        private class PeriodStanding
        {
            public int PlayerId { get; set; }
            public string PlayerName { get; set; } = "";
            public int MatchesPlayed { get; set; }
            public int TotalPoints { get; set; }
            public int Victories { get; set; }
            public int Top3 { get; set; }
            public int TotalBounties { get; set; }
            public decimal TotalWinnings { get; set; }
            public double AveragePosition { get; set; }
            public List<PeriodPerformance> Performances { get; set; } = new();
        }

        private class PeriodPerformance
        {
            public int Points { get; set; }
            public int Position { get; set; }
            public int Bounties { get; set; }
            public decimal Winnings { get; set; }
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

        // === STATISTIQUES COMPLÈTES ===
        [RelayCommand]
        private async Task LoadCompleteStatsAsync()
        {
            if (Championship == null) return;

            CompleteStats.Clear();

            var allPlayers = Championship.Standings
                .Where(s => s.IsActive)
                .OrderBy(s => s.CurrentPosition)
                .ToList();

            foreach (var standing in allPlayers)
            {
                var performances = await GetAllPlayerPerformancesAsync(standing.PlayerId);

                // HU = fois où le joueur a fini dans le top 2 (heads-up à la fin)
                var huPerformances = performances.Where(p => p.Position <= 2 && p.Position > 0).ToList(); // Position 1 ou 2, et non éliminé tôt
                int huCount = huPerformances.Count;
                int huWins = huPerformances.Count(p => p.Position == 1);

                var stat = new PlayerCompleteStat
                {
                    Position = standing.CurrentPosition,
                    PlayerName = standing.Player?.Name ?? "Inconnu",
                    MatchesPlayed = performances.Count,
                    Victories = performances.Count(p => p.Position == 1),
                    VictoryRate = performances.Any() ? (double)performances.Count(p => p.Position == 1) / performances.Count * 100 : 0,
                    Top3 = performances.Count(p => p.Position <= 3),
                    Top3Rate = performances.Any() ? (double)performances.Count(p => p.Position <= 3) / performances.Count * 100 : 0,
                    HeadToHead = huCount,
                    HeadToHeadRate = performances.Any() ? (double)huCount / performances.Count * 100 : 0,
                    HeadToHeadWins = huWins,
                    HeadToHeadWinRate = huCount > 0 ? (double)huWins / huCount * 100 : 0,
                    AveragePosition = performances.Any() ? performances.Average(p => (double)p.Position) : 0,
                    TotalPoints = performances.Sum(p => p.Points), // CORRECTION : Utilise les points calculés
                    AveragePoints = performances.Any() ? performances.Average(p => (double)p.Points) : 0,
                    TotalBounties = performances.Sum(p => p.Bounties),
                    AverageBounties = performances.Any() ? (double)performances.Sum(p => p.Bounties) / performances.Count : 0,
                    TotalRebuys = performances.Sum(p => p.Rebuys)
                };

                CompleteStats.Add(stat);
            }
        }

        private async Task<List<FullPerformance>> GetAllPlayerPerformancesAsync(int playerId)
        {
            var matches = await _context.ChampionshipMatches
                .Where(m => m.ChampionshipId == _championshipId)
                .Include(m => m.Tournament)
                    .ThenInclude(t => t.Players)
                .ToListAsync();

            var performances = new List<FullPerformance>();

            foreach (var match in matches)
            {
                var tp = await _context.TournamentPlayers
                    .FirstOrDefaultAsync(t => t.TournamentId == match.TournamentId && t.PlayerId == playerId);

                if (tp != null && tp.FinishPosition.HasValue)
                {
                    var totalPlayers = await _context.TournamentPlayers
                        .CountAsync(t => t.TournamentId == match.TournamentId && t.FinishPosition.HasValue);

                    // Calculer les points EXACTS avec toutes les règles
                    int basePoints = CalculateMatchPointsForPlayer(Championship, tp.FinishPosition.Value);
                    basePoints = (int)(basePoints * match.Coefficient);

                    // Ajouter bonus bounties
                    if (Championship.CountBounties)
                    {
                        basePoints += tp.BountyKills * Championship.PointsPerBounty;
                    }

                    // Ajouter bonus victoire
                    if (tp.FinishPosition == 1 && Championship.VictoryBonus > 0)
                    {
                        basePoints += Championship.VictoryBonus;
                    }

                    // Ajouter bonus top 3
                    if (tp.FinishPosition <= 3 && Championship.Top3Bonus > 0)
                    {
                        basePoints += Championship.Top3Bonus;
                    }

                    // Pénalité recaves
                    if (tp.RebuyCount > 0)
                    {
                        basePoints -= tp.RebuyCount * Championship.RebuyPointsPenalty;
                        basePoints = (int)(basePoints * Championship.RebuyPointsMultiplier);
                    }

                    performances.Add(new FullPerformance
                    {
                        Position = tp.FinishPosition.Value,
                        Points = basePoints,
                        Bounties = tp.BountyKills,
                        Rebuys = tp.RebuyCount,
                        TotalPlayers = totalPlayers
                    });
                }
            }

            return performances;
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

                MatchResults.Clear();

                foreach (var tp in tournament.Players.Where(p => p.FinishPosition.HasValue).OrderBy(p => p.FinishPosition))
                {
                    // CORRECTION : Utilise CalculateMatchPointsForPlayer au lieu de CalculateMatchPoints
                    int points = CalculateMatchPointsForPlayer(championship, tp.FinishPosition.Value);
                    int displayPoints = (int)(points * match.Coefficient);

                    MatchResults.Add(new MatchPlayerResult
                    {
                        Position = tp.FinishPosition.Value,
                        PlayerName = tp.Player?.Name ?? "Inconnu",
                        BasePoints = points,
                        Coefficient = match.Coefficient,
                        TotalPoints = displayPoints,
                        Bounties = tp.BountyKills,
                        Rebuys = tp.RebuyCount,
                        Winnings = tp.Winnings ?? 0
                    });
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
                CustomMessageBox.ShowError($"Erreur : {ex.Message}");
            }
        }

        private int CalculateMatchPointsForPlayer(Championship championship, int position)
        {
            int basePoints = 0;

            switch (championship.PointsMode)
            {
                case ChampionshipPointsMode.Linear:
                    basePoints = Math.Max(1, championship.LinearFirstPlacePoints - (position - 1));
                    break;

                case ChampionshipPointsMode.FixedByPosition:
                    basePoints = GetFixedPointsForPosition(championship, position);
                    break;

                case ChampionshipPointsMode.ProportionalPrizePool:
                    // Pour l'instant, utilise linéaire comme fallback
                    basePoints = Math.Max(1, championship.LinearFirstPlacePoints - (position - 1));
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

        [RelayCommand]
        private void SwitchTab(string tabIndex)
        {
            if (int.TryParse(tabIndex, out int index))
            {
                SelectedTabIndex = index;

                // Charger données à la demande
                if (index == 1) _ = LoadCompleteStatsAsync();
                if (index == 3) _ = SearchPlayersAsync();
            }
        }

        // === JOUEURS ===
        [RelayCommand]
        private async Task SearchPlayersAsync()
        {
            if (Championship == null) return;

            var query = Championship.Standings.Where(s => s.IsActive).AsEnumerable();

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

            SelectedTabIndex = 1;
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
                        l.Action == ChampionshipLogAction.MatchRemoved);
                    break;

                case "Points":
                case "Classements":
                    filtered = allLogs.Where(l =>
                        l.Action == ChampionshipLogAction.StandingsRecalculated);
                    break;
            }

            Logs.Clear();
            foreach (var log in filtered)
            {
                Logs.Add(log);
            }
        }

        [RelayCommand]
        private void ExportStandingsPdf()
        {
            CustomMessageBox.ShowInformation("Export PDF en cours de développement...");
        }
    }

    // === CLASSES HELPERS ===
    public class PlayerCompleteStat
    {
        public int Position { get; set; }
        public string PlayerName { get; set; } = "";
        public int MatchesPlayed { get; set; }
        public int Victories { get; set; }
        public double VictoryRate { get; set; }
        public int Top3 { get; set; }
        public double Top3Rate { get; set; }
        public int HeadToHead { get; set; }
        public double HeadToHeadRate { get; set; }
        public int HeadToHeadWins { get; set; }
        public double HeadToHeadWinRate { get; set; }
        public double AveragePosition { get; set; }
        public int TotalPoints { get; set; }
        public double AveragePoints { get; set; } // AJOUT
        public int TotalBounties { get; set; }
        public double AverageBounties { get; set; }
        public int TotalRebuys { get; set; }
    }

    public class FullPerformance
    {
        public int Position { get; set; }
        public int Points { get; set; } // AJOUT
        public int Bounties { get; set; }
        public int Rebuys { get; set; }
        public int TotalPlayers { get; set; }
    }

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
    }
}