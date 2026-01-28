using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.ViewModels
{
    public partial class ChampionshipDashboardViewModel : ObservableObject, IDisposable
    {
        private readonly ChampionshipService _championshipService;
        private readonly ChampionshipCalculationService _calculationService;
        private readonly ChampionshipDataLoaderService _dataLoaderService;
        private readonly PokerDbContext _context;
        private readonly int _championshipId;

        [ObservableProperty] private Championship? _championship;
        [ObservableProperty] private int _selectedTabIndex = 0;

        // Classement
        [ObservableProperty] private ObservableCollection<ChampionshipStanding> _standings = new();
        [ObservableProperty] private string _selectedStandingPeriod = "Général";
        [ObservableProperty] private ObservableCollection<string> _availablePeriods = new();
        [ObservableProperty] private string? _selectedPeriod;

        public ObservableCollection<string> StandingPeriods { get; } = new() { "Général", "Mensuel", "Trimestriel" };

        // Statistiques
        [ObservableProperty] private ObservableCollection<PlayerCompleteStat> _completeStats = new();
        [ObservableProperty] private InsoliteStat? _sniperStat;
        [ObservableProperty] private InsoliteStat? _touristeStat;
        [ObservableProperty] private InsoliteStat? _taulierStat;
        [ObservableProperty] private InsoliteStat? _bossStat;
        [ObservableProperty] private InsoliteStat? _marcheStat;
        [ObservableProperty] private InsoliteStat? _pilierStat;
        [ObservableProperty] private InsoliteStat? _regStat;
        [ObservableProperty] private InsoliteStat? _roiMakerStat;

        // Manches
        [ObservableProperty] private ObservableCollection<ChampionshipMatch> _matches = new();
        [ObservableProperty] private ChampionshipMatch? _selectedMatch;
        [ObservableProperty] private ObservableCollection<MatchPlayerResult> _matchResults = new();
        [ObservableProperty] private string _matchDetailsTitle = "";

        // Joueurs
        [ObservableProperty] private ObservableCollection<ChampionshipStanding> _players = new();
        [ObservableProperty] private ChampionshipStanding? _selectedPlayer;
        [ObservableProperty] private string _playerSearchText = string.Empty;

        // Logs
        [ObservableProperty] private ObservableCollection<ChampionshipLog> _logs = new();
        [ObservableProperty] private string _logFilterAction = "Toutes";

        public ObservableCollection<string> LogActions { get; } = new()
        {
            "Toutes", "Création/Modification", "Manches", "Joueurs", "Points", "Classements"
        };

        public ChampionshipDashboardViewModel(
            ChampionshipService championshipService,
            PokerDbContext context,
            int championshipId)
        {
            _championshipService = championshipService;
            _context = context;
            _championshipId = championshipId;
            _calculationService = new ChampionshipCalculationService();
            _dataLoaderService = new ChampionshipDataLoaderService(context);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadChampionshipAsync();
                await LoadStandingsAsync();
                await LoadMatchesAsync();
                await LoadLogsAsync();
                await LoadCompleteStatsAsync();
                await LoadInsolitesAsync();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur initialisation : {ex.Message}");
            }
        }

        private async Task LoadChampionshipAsync()
        {
            Championship = await _championshipService.GetChampionshipAsync(_championshipId);
            if (Championship != null && Championship.Matches.Any())
            {
                OnSelectedStandingPeriodChanged("Général");
            }
        }

        [RelayCommand]
        private async Task LoadStandingsAsync()
        {
            try
            {
                if (Championship == null) return;
                await FilterStandingsByPeriodAsync();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur chargement classements : {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RefreshStandingsAsync()
        {
            try
            {
                await _championshipService.RecalculateStandingsAsync(_championshipId);
                Championship = await _championshipService.GetChampionshipAsync(_championshipId);
                await LoadStandingsAsync();
                await LoadCompleteStatsAsync();
                await LoadInsolitesAsync();
                CustomMessageBox.ShowSuccess("Classements recalculés !", "Succès");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur recalcul : {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            await InitializeAsync();
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
            try
            {
                if (Championship == null) return;

                Standings.Clear();

                if (SelectedStandingPeriod == "Général")
                {
                    var allStandings = Championship.Standings
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.CurrentPosition)
                        .ToList();

                    foreach (var s in allStandings)
                        Standings.Add(s);
                    return;
                }

                if (string.IsNullOrEmpty(SelectedPeriod)) return;

                var (startDate, endDate) = GetPeriodDates();

                var periodMatches = Championship.Matches
                    .Where(m => m.MatchDate >= startDate && m.MatchDate <= endDate)
                    .ToList();

                var playerStats = await BuildPeriodStatsAsync(periodMatches);
                ApplyCountingRules(playerStats);
                PopulateStandingsFromStats(playerStats);
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur filtrage : {ex.Message}");
            }
        }

        private (DateTime start, DateTime end) GetPeriodDates()
        {
            if (SelectedStandingPeriod == "Mensuel")
            {
                var parts = SelectedPeriod!.Split(' ');
                int month = GetMonthNumber(parts[0]);
                int year = int.Parse(parts[1]);
                var start = new DateTime(year, month, 1);
                return (start, start.AddMonths(1).AddDays(-1));
            }
            else if (SelectedStandingPeriod == "Trimestriel")
            {
                var parts = SelectedPeriod!.Split(' ');
                int quarter = int.Parse(parts[0].Replace("Q", ""));
                int year = int.Parse(parts[1]);
                int startMonth = (quarter - 1) * 3 + 1;
                var start = new DateTime(year, startMonth, 1);
                return (start, start.AddMonths(3).AddDays(-1));
            }

            return (DateTime.MinValue, DateTime.MaxValue);
        }

        private async Task<Dictionary<int, PeriodStanding>> BuildPeriodStatsAsync(List<ChampionshipMatch> periodMatches)
        {
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
                    int totalMatchPoints = _calculationService.CalculateFullMatchPoints(Championship!, match, tp);

                    stat.Performances.Add(new PeriodPerformance
                    {
                        Points = totalMatchPoints,
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

            return playerStats;
        }

        private void ApplyCountingRules(Dictionary<int, PeriodStanding> playerStats)
        {
            int? bestX = null;

            if (SelectedStandingPeriod == "Mensuel" && Championship!.BestXPerMonth.HasValue)
                bestX = Championship.BestXPerMonth.Value;
            else if (SelectedStandingPeriod == "Trimestriel" && Championship!.BestXPerQuarter.HasValue)
                bestX = Championship.BestXPerQuarter.Value;

            foreach (var stat in playerStats.Values)
            {
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
        }

        private void PopulateStandingsFromStats(Dictionary<int, PeriodStanding> playerStats)
        {
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

        // === STATISTIQUES COMPLÈTES ===
        [RelayCommand]
        private async Task LoadCompleteStatsAsync()
        {
            try
            {
                if (Championship == null) return;

                CompleteStats.Clear();

                // CORRECTION : Passe les dépendances nécessaires au calcul
                var allPerformances = await _dataLoaderService.LoadAllPerformancesBatchAsync(
                    _championshipId,
                    Championship,
                    _calculationService);

                var allPlayers = Championship.Standings
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.CurrentPosition)
                    .ToList();

                foreach (var standing in allPlayers)
                {
                    if (!allPerformances.ContainsKey(standing.PlayerId))
                        continue;

                    var performances = allPerformances[standing.PlayerId];

                    // Stats HU : finir dans les 2 premiers (peu importe le nombre de joueurs)
                    var huPerformances = performances.Where(p => p.Position <= 2).ToList();
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
                        TotalPoints = performances.Sum(p => p.Points), // Points déjà calculés
                        AveragePoints = performances.Any() ? performances.Average(p => (double)p.Points) : 0,
                        TotalBounties = performances.Sum(p => p.Bounties),
                        AverageBounties = performances.Any() ? (double)performances.Sum(p => p.Bounties) / performances.Count : 0,
                        TotalRebuys = performances.Sum(p => p.Rebuys)
                    };

                    CompleteStats.Add(stat);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur chargement stats : {ex.Message}");
            }
        }
        [RelayCommand]
        private async Task LoadInsolitesAsync()
        {
            try
            {
                if (Championship == null || !CompleteStats.Any()) return;

                var playerData = new Dictionary<int, PlayerInsolitData>();

                var matches = await _dataLoaderService.LoadMatchesWithPlayersAsync(_championshipId);

                foreach (var standing in Championship.Standings.Where(s => s.IsActive))
                {
                    int totalMinutes = 0;
                    int tournamentsPlayed = 0;
                    int victories = 0;
                    int secondPlaces = 0;
                    int finalTables = 0;
                    int totalBounties = 0;
                    int totalPoints = 0;

                    foreach (var match in matches)
                    {
                        var tp = match.Tournament?.Players.FirstOrDefault(p => p.PlayerId == standing.PlayerId);
                        if (tp?.FinishPosition == null) continue;

                        tournamentsPlayed++;

                        if (match.Tournament?.StartTime != null && match.Tournament.EndTime != null)
                        {
                            var duration = (match.Tournament.EndTime.Value - match.Tournament.StartTime.Value).TotalMinutes;
                            var totalPlayersCount = match.Tournament.Players.Count;
                            var survivalRate = (double)(totalPlayersCount - tp.FinishPosition.Value + 1) / totalPlayersCount;
                            totalMinutes += (int)(duration * survivalRate);
                        }

                        if (tp.FinishPosition == 1) victories++;
                        if (tp.FinishPosition == 2) secondPlaces++;

                        var playersCount = match.Tournament?.Players.Count ?? 0;
                        if (tp.FinishPosition <= Math.Min(9, playersCount)) finalTables++;

                        totalBounties += tp.BountyKills;
                    
                        totalPoints += _calculationService.CalculateFullMatchPoints(Championship, match, tp);
                    }

                    if (tournamentsPlayed > 0)
                    {
                        playerData[standing.PlayerId] = new PlayerInsolitData
                        {
                            PlayerId = standing.PlayerId,
                            PlayerName = standing.Player?.Name ?? "Inconnu",
                            TotalBounties = totalBounties,
                            AverageMinutes = totalMinutes / tournamentsPlayed,
                            TotalMinutes = totalMinutes,
                            Victories = victories,
                            SecondPlaces = secondPlaces,
                            FinalTables = finalTables,
                            TournamentsPlayed = tournamentsPlayed,
                            AveragePoints = (double)totalPoints / tournamentsPlayed
                        };
                    }
                }

                SniperStat = CalculateInsoliteStat(playerData.Values, p => p.TotalBounties, "🎯", "killer");
                TouristeStat = CalculateInsoliteStat(playerData.Values, p => p.AverageMinutes, "🏖️", "rapide à bust", ascending: true);
                TaulierStat = CalculateInsoliteStat(playerData.Values, p => p.TotalMinutes, "⏱️", "temps de jeu");
                BossStat = CalculateInsoliteStat(playerData.Values, p => p.Victories, "👑", "victoires");
                MarcheStat = CalculateInsoliteStat(playerData.Values, p => p.SecondPlaces, "🥈", "secondes places");
                PilierStat = CalculateInsoliteStat(playerData.Values, p => p.FinalTables, "🤺​​", "tables finales");
                RegStat = CalculateInsoliteStat(playerData.Values, p => p.TournamentsPlayed, "🎲", "tournois");
                RoiMakerStat = CalculateInsoliteStat(playerData.Values, p => p.AveragePoints, "💰", "pts/tournoi");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur calcul stats insolites : {ex.Message}");
            }
        }

        private InsoliteStat CalculateInsoliteStat<T>(IEnumerable<PlayerInsolitData> data, Func<PlayerInsolitData, T> selector, string emoji, string unit, bool ascending = false) where T : IComparable<T>
        {
            var ordered = ascending
                ? data.OrderBy(selector).ToList()
                : data.OrderByDescending(selector).ToList();

            if (!ordered.Any())
                return new InsoliteStat { Emoji = emoji, Value = "N/A", Unit = unit };

            var topValue = selector(ordered.First());
            var topPlayers = ordered.Where(p => selector(p).CompareTo(topValue) == 0).ToList();

            var stat = new InsoliteStat
            {
                Emoji = emoji,
                Unit = unit,
                Value = topValue is double d ? d.ToString("N1") : topValue.ToString()
            };

            if (topPlayers.Count <= 3)
            {
                stat.Players = string.Join(", ", topPlayers.Select(p => p.PlayerName));
            }
            else
            {
                stat.Players = $"{topPlayers.Count} joueurs";
            }

            return stat;
        }

        // === MANCHES ===
        [RelayCommand]
        private async Task LoadMatchesAsync()
        {
            try
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
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur chargement manches : {ex.Message}");
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
                    int basePoints = _calculationService.CalculateBasePoints(championship, tp.FinishPosition.Value);
                    int totalPoints = _calculationService.CalculateFullMatchPoints(championship, match, tp);

                    MatchResults.Add(new MatchPlayerResult
                    {
                        Position = tp.FinishPosition.Value,
                        PlayerName = tp.Player?.Name ?? "Inconnu",
                        BasePoints = basePoints,
                        Coefficient = match.Coefficient,
                        TotalPoints = totalPoints,
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

        [RelayCommand]
        private void SwitchTab(string tabIndex)
        {
            if (int.TryParse(tabIndex, out int index))
            {
                SelectedTabIndex = index;

                if (index == 1) _ = LoadCompleteStatsAsync();
                if (index == 3) _ = SearchPlayersAsync();
            }
        }

        // === JOUEURS ===
        [RelayCommand]
        private async Task SearchPlayersAsync()
        {
            try
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
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur recherche : {ex.Message}");
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
            try
            {
                var logs = await _championshipService.GetLogsAsync(_championshipId);

                Logs.Clear();
                foreach (var log in logs)
                {
                    Logs.Add(log);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur chargement logs : {ex.Message}");
            }
        }

        partial void OnLogFilterActionChanged(string value)
        {
            _ = FilterLogsAsync();
        }

        private async Task FilterLogsAsync()
        {
            try
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
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur filtrage logs : {ex.Message}");
            }
        }

        // === EXPORT PDF ===
        [RelayCommand]
        private async Task ExportStandingsPdfAsync()
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Classement_{Championship?.Name}_{SelectedStandingPeriod}_{SelectedPeriod?.Replace(" ", "_")}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await GenerateStandingsPdf(saveDialog.FileName);
                    CustomMessageBox.ShowSuccess($"PDF exporté : {saveDialog.FileName}", "Export réussi");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur export PDF : {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ExportMatchPdfAsync()
        {
            if (SelectedMatch == null || !MatchResults.Any())
            {
                CustomMessageBox.ShowWarning("Aucune manche sélectionnée ou détails non chargés.");
                return;
            }

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Manche_{SelectedMatch.MatchNumber}_{SelectedMatch.MatchDate:yyyyMMdd}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await GenerateMatchPdf(saveDialog.FileName);
                    CustomMessageBox.ShowSuccess($"PDF exporté : {saveDialog.FileName}", "Export réussi");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur export PDF : {ex.Message}");
            }
        }

        private async Task GenerateStandingsPdf(string filePath)
        {
            var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 60, 60);
            PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            var titleFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 20, iTextSharp.text.Font.BOLD, new BaseColor(0, 255, 136));
            var headerFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 12, iTextSharp.text.Font.NORMAL, BaseColor.DARK_GRAY);
            var tableHeaderFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10, iTextSharp.text.Font.BOLD, BaseColor.WHITE);
            var tableCellFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);

            var title = new Paragraph($"Classement {Championship?.Season}", titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 10f;
            doc.Add(title);

            var subtitle = new Paragraph($"{SelectedStandingPeriod} - {SelectedPeriod ?? "Saison complète"}", headerFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            subtitle.SpacingAfter = 5f;
            doc.Add(subtitle);

            var dateGenerated = new Paragraph($"Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}", headerFont);
            dateGenerated.Alignment = Element.ALIGN_CENTER;
            dateGenerated.SpacingAfter = 20f;
            doc.Add(dateGenerated);

            var table = new PdfPTable(6) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 10f, 35f, 15f, 15f, 15f, 10f });

            AddPdfCell(table, "#", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_CENTER);
            AddPdfCell(table, "Joueur", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_LEFT);
            AddPdfCell(table, "Points", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_CENTER);
            AddPdfCell(table, "Manches", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_CENTER);
            AddPdfCell(table, "Victoires", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_CENTER);
            AddPdfCell(table, "Kills", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_CENTER);

            foreach (var standing in Standings)
            {
                var rowColor = standing.CurrentPosition % 2 == 0 ? new BaseColor(240, 240, 240) : BaseColor.WHITE;

                AddPdfCell(table, standing.CurrentPosition.ToString(), tableCellFont, rowColor, Element.ALIGN_CENTER);
                AddPdfCell(table, standing.Player?.Name ?? "Inconnu", tableCellFont, rowColor, Element.ALIGN_LEFT);
                AddPdfCell(table, standing.TotalPoints.ToString(), tableCellFont, rowColor, Element.ALIGN_CENTER);
                AddPdfCell(table, standing.MatchesPlayed.ToString(), tableCellFont, rowColor, Element.ALIGN_CENTER);
                AddPdfCell(table, standing.Victories.ToString(), tableCellFont, rowColor, Element.ALIGN_CENTER);
                AddPdfCell(table, standing.TotalBounties.ToString(), tableCellFont, rowColor, Element.ALIGN_CENTER);
            }

            doc.Add(table);
            doc.Close();
        }

        private async Task GenerateMatchPdf(string filePath)
        {
            var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 40, 40, 60, 60);
            PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            var titleFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 20, iTextSharp.text.Font.BOLD, new BaseColor(0, 255, 136));
            var headerFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 12, iTextSharp.text.Font.NORMAL, BaseColor.DARK_GRAY);
            var tableHeaderFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 10, iTextSharp.text.Font.BOLD, BaseColor.WHITE);
            var tableCellFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);

            var title = new Paragraph(MatchDetailsTitle, titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 10f;
            doc.Add(title);

            var subtitle = new Paragraph($"{Championship?.Name} - Saison {Championship?.Season}", headerFont);
            subtitle.Alignment = Element.ALIGN_CENTER;
            subtitle.SpacingAfter = 20f;
            doc.Add(subtitle);

            var table = new PdfPTable(4) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 15f, 50f, 20f, 15f });

            AddPdfCell(table, "Place", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_CENTER);
            AddPdfCell(table, "Joueur", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_LEFT);
            AddPdfCell(table, "Points", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_CENTER);
            AddPdfCell(table, "Kills", tableHeaderFont, new BaseColor(15, 52, 96), Element.ALIGN_CENTER);

            foreach (var result in MatchResults)
            {
                var rowColor = result.Position % 2 == 0 ? new BaseColor(240, 240, 240) : BaseColor.WHITE;

                AddPdfCell(table, result.Position.ToString(), tableCellFont, rowColor, Element.ALIGN_CENTER);
                AddPdfCell(table, result.PlayerName, tableCellFont, rowColor, Element.ALIGN_LEFT);
                AddPdfCell(table, result.TotalPoints.ToString(), tableCellFont, rowColor, Element.ALIGN_CENTER);
                AddPdfCell(table, result.Bounties.ToString(), tableCellFont, rowColor, Element.ALIGN_CENTER);
            }

            doc.Add(table);
            doc.Close();
        }

        private void AddPdfCell(PdfPTable table, string text, iTextSharp.text.Font font, BaseColor bgColor, int alignment)
        {
            var cell = new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = bgColor,
                Padding = 8f,
                HorizontalAlignment = alignment,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };
            table.AddCell(cell);
        }

        public void Dispose()
        {
            // Le context sera disposé par DI
        }

        // Helper methods
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
    }

    // Classes helper (gardées pour simplicité)
    public class PeriodStanding
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

    public class PeriodPerformance
    {
        public int Points { get; set; }
        public int Position { get; set; }
        public int Bounties { get; set; }
        public decimal Winnings { get; set; }
    }

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
        public double AveragePoints { get; set; }
        public int TotalBounties { get; set; }
        public double AverageBounties { get; set; }
        public int TotalRebuys { get; set; }
    }

    public class FullPerformance
    {
        public int Position { get; set; }
        public int Points { get; set; }
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

    public class PlayerInsolitData
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = "";
        public int TotalBounties { get; set; }
        public int AverageMinutes { get; set; }
        public int TotalMinutes { get; set; }
        public int Victories { get; set; }
        public int SecondPlaces { get; set; }
        public int FinalTables { get; set; }
        public int TournamentsPlayed { get; set; }
        public double AveragePoints { get; set; }
    }

    public class InsoliteStat
    {
        public string Emoji { get; set; } = "";
        public string Players { get; set; } = "";
        public string Value { get; set; } = "";
        public string Unit { get; set; } = "";
    }
}