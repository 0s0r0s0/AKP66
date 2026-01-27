using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    public class ChampionshipService
    {
        private readonly PokerDbContext _context;

        public ChampionshipService(PokerDbContext context)
        {
            _context = context;
        }

        // === CRUD CHAMPIONNATS ===

        public async Task<Championship> CreateChampionshipAsync(Championship championship)
        {
            _context.Championships.Add(championship);
            await _context.SaveChangesAsync();

            // Log création
            await LogActionAsync(championship.Id, ChampionshipLogAction.ChampionshipCreated,
                $"Championnat '{championship.Name}' créé");

            return championship;
        }

        public async Task<Championship?> GetChampionshipAsync(int id)
        {
            return await _context.Championships
                .Include(c => c.Matches)
                    .ThenInclude(m => m.Tournament)
                .Include(c => c.Standings)
                    .ThenInclude(s => s.Player)
                .Include(c => c.Logs)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Championship>> GetAllChampionshipsAsync(bool includeArchived = false)
        {
            var query = _context.Championships.AsQueryable();

            if (!includeArchived)
            {
                query = query.Where(c => c.Status != ChampionshipStatus.Archived);
            }

            return await query
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();
        }

        public async Task<Championship> UpdateChampionshipAsync(Championship championship)
        {
            var existing = await _context.Championships.FindAsync(championship.Id);
            if (existing == null)
                throw new Exception("Championnat introuvable");

            // Capturer changements pour log
            var changes = new Dictionary<string, object>();
            // TODO: Détecter changements spécifiques

            championship.UpdatedAt = DateTime.Now;
            _context.Entry(existing).CurrentValues.SetValues(championship);
            await _context.SaveChangesAsync();

            await LogActionAsync(championship.Id, ChampionshipLogAction.ChampionshipModified,
                "Championnat modifié", beforeData: JsonSerializer.Serialize(changes));

            return championship;
        }

        public async Task ArchiveChampionshipAsync(int championshipId)
        {
            var championship = await _context.Championships.FindAsync(championshipId);
            if (championship == null)
                throw new Exception("Championnat introuvable");

            championship.Status = ChampionshipStatus.Archived;
            championship.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await LogActionAsync(championshipId, ChampionshipLogAction.ChampionshipArchived,
                $"Championnat '{championship.Name}' archivé");
        }

        public async Task DeleteChampionshipAsync(int championshipId)
        {
            var championship = await _context.Championships
                .Include(c => c.Matches)
                .Include(c => c.Standings)
                .Include(c => c.Logs)
                .FirstOrDefaultAsync(c => c.Id == championshipId);

            if (championship == null)
                throw new Exception("Championnat introuvable");

            _context.Championships.Remove(championship);
            await _context.SaveChangesAsync();
        }

        // === GESTION MANCHES ===

        public async Task<ChampionshipMatch> AddMatchAsync(int championshipId, int tournamentId, 
            bool isFinal = false, bool isMainEvent = false)
        {
            var championship = await GetChampionshipAsync(championshipId);
            if (championship == null)
                throw new Exception("Championnat introuvable");

            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null)
                throw new Exception("Tournoi introuvable");

            // Calculer numéro de manche
            var matchNumber = await _context.ChampionshipMatches
                .Where(m => m.ChampionshipId == championshipId)
                .CountAsync() + 1;

            // Déterminer coefficient
            decimal coefficient = championship.DefaultMatchCoefficient;
            if (isFinal) coefficient = championship.FinalMatchCoefficient;
            else if (isMainEvent) coefficient = championship.MainEventCoefficient;

            var match = new ChampionshipMatch
            {
                ChampionshipId = championshipId,
                TournamentId = tournamentId,
                MatchNumber = matchNumber,
                MatchDate = tournament.Date,
                Coefficient = coefficient,
                IsFinal = isFinal,
                IsMainEvent = isMainEvent
            };

            _context.ChampionshipMatches.Add(match);
            await _context.SaveChangesAsync();

            await LogActionAsync(championshipId, ChampionshipLogAction.MatchAdded,
                $"Manche #{matchNumber} ajoutée (Tournoi: {tournament.Name})",
                matchId: match.Id);

            return match;
        }

        public async Task RemoveMatchAsync(int matchId)
        {
            var match = await _context.ChampionshipMatches.FindAsync(matchId);
            if (match == null)
                throw new Exception("Manche introuvable");

            var championshipId = match.ChampionshipId;

            _context.ChampionshipMatches.Remove(match);
            await _context.SaveChangesAsync();

            await LogActionAsync(championshipId, ChampionshipLogAction.MatchRemoved,
                $"Manche #{match.MatchNumber} retirée");

            // Recalculer classements
            await RecalculateStandingsAsync(championshipId);
        }

        // === CALCUL DES POINTS ===

        public async Task RecalculateStandingsAsync(int championshipId)
        {
            var championship = await GetChampionshipAsync(championshipId);
            if (championship == null)
                throw new Exception("Championnat introuvable");

            // Récupérer toutes les manches
            var matches = championship.Matches.OrderBy(m => m.MatchNumber).ToList();

            // Récupérer tous les joueurs ayant participé
            var playerIds = new HashSet<int>();
            foreach (var match in matches)
            {
                var tournamentPlayers = await _context.TournamentPlayers
                    .Where(tp => tp.TournamentId == match.TournamentId)
                    .Select(tp => tp.PlayerId)
                    .ToListAsync();
                
                foreach (var pid in tournamentPlayers)
                    playerIds.Add(pid);
            }

            // Calculer points pour chaque joueur
            foreach (var playerId in playerIds)
            {
                await CalculatePlayerStandingAsync(championshipId, playerId);
            }

            // Assigner positions finales
            await AssignPositionsAsync(championshipId);

            await LogActionAsync(championshipId, ChampionshipLogAction.StandingsRecalculated,
                "Classements recalculés");
        }

        private async Task<ChampionshipStanding> CalculatePlayerStandingAsync(int championshipId, int playerId)
        {
            var championship = await GetChampionshipAsync(championshipId);
            if (championship == null)
                throw new Exception("Championnat introuvable");

            var standing = await _context.ChampionshipStandings
                .FirstOrDefaultAsync(s => s.ChampionshipId == championshipId && s.PlayerId == playerId);

            if (standing == null)
            {
                standing = new ChampionshipStanding
                {
                    ChampionshipId = championshipId,
                    PlayerId = playerId
                };
                _context.ChampionshipStandings.Add(standing);
            }

            // Récupérer toutes les performances du joueur
            var performances = await GetPlayerPerformancesAsync(championshipId, playerId);

            // Appliquer le mode de comptage et récupérer les performances conservées
            var countingResult = ApplyCountingMode(championship, performances);

            int totalPoints = countingResult.TotalPoints;
            var retainedPerformances = countingResult.RetainedPerformances;

            // Stats basées UNIQUEMENT sur les performances conservées
            int matchesPlayed = retainedPerformances.Count;
            int victories = retainedPerformances.Count(p => p.Position == 1);
            int top3 = retainedPerformances.Count(p => p.Position <= 3);
            int totalBounties = retainedPerformances.Sum(p => p.Bounties);
            decimal totalWinnings = retainedPerformances.Sum(p => p.Winnings);
            var positions = retainedPerformances.Select(p => p.Position).ToList();

            // Appliquer bonus bounties (uniquement sur tournois conservés)
            if (championship.CountBounties)
            {
                totalPoints += totalBounties * championship.PointsPerBounty;
            }

            totalPoints += victories * championship.VictoryBonus;
            totalPoints += top3 * championship.Top3Bonus;

            // Statistiques
            standing.TotalPoints = totalPoints;
            standing.MatchesPlayed = matchesPlayed;
            standing.Victories = victories;
            standing.Top3Finishes = top3;
            standing.TotalBounties = totalBounties;
            standing.TotalWinnings = totalWinnings;

            if (positions.Any())
            {
                standing.AveragePosition = (decimal)positions.Average();
                standing.BestPosition = positions.Min();
                standing.WorstPosition = positions.Max();
                standing.PositionStdDev = CalculateStdDev(positions);
            }

            standing.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            return standing;
        }

        // NOUVELLE CLASSE HELPER pour retourner à la fois les points et les performances conservées
        private class CountingResult
        {
            public int TotalPoints { get; set; }
            public List<PlayerPerformance> RetainedPerformances { get; set; } = new();
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
                    basePoints = CalculateProportionalPoints(championship, perf);
                    break;
            }

            // Appliquer coefficient de manche
            basePoints = (int)(basePoints * perf.Coefficient);

            // Points de participation
            if (championship.EnableParticipationPoints)
            {
                basePoints += championship.ParticipationPoints;
            }

            return basePoints;
        }

        private int GetFixedPointsForPosition(Championship championship, int position)
        {
            if (string.IsNullOrEmpty(championship.FixedPointsTable))
                return 0;

            try
            {
                var table = JsonSerializer.Deserialize<Dictionary<string, int>>(championship.FixedPointsTable);
                if (table == null) return 0;

                // Chercher position exacte ou plage
                if (table.ContainsKey(position.ToString()))
                    return table[position.ToString()];

                // Chercher dans les plages (ex: "5-10")
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

        private int CalculateProportionalPoints(Championship championship, PlayerPerformance perf)
        {
            if (perf.Winnings == 0 || perf.TotalPrizePool == 0)
                return 0;

            double percentage = (double)perf.Winnings / (double)perf.TotalPrizePool;
            return (int)(percentage * championship.ProportionalTotalPoints);
        }

        private CountingResult ApplyCountingMode(Championship championship, List<PlayerPerformance> performances)
        {
            var result = new CountingResult();
            List<PlayerPerformance> retained = performances; // Par défaut, tous conservés

            switch (championship.CountingMode)
            {
                case ChampionshipCountingMode.BestXOfSeason:
                    if (championship.BestXOfSeason.HasValue && performances.Count > championship.BestXOfSeason.Value)
                    {
                        retained = performances
                            .OrderByDescending(p => CalculateMatchPoints(championship, p))
                            .Take(championship.BestXOfSeason.Value)
                            .ToList();
                    }
                    break;

                case ChampionshipCountingMode.BestXPerPeriod:
                    retained = ApplyBestXPerPeriod(championship, performances);
                    break;
            }

            // Exclure les pires résultats (après sélection des meilleurs)
            if (championship.ExcludeWorstX.HasValue && championship.ExcludeWorstX.Value > 0)
            {
                if (retained.Count > championship.ExcludeWorstX.Value)
                {
                    retained = retained
                        .OrderByDescending(p => CalculateMatchPoints(championship, p))
                        .Skip(championship.ExcludeWorstX.Value)
                        .ToList();
                }
            }

            result.RetainedPerformances = retained;
            result.TotalPoints = retained.Sum(p => CalculateMatchPoints(championship, p));

            return result;
        }

        private List<PlayerPerformance> ApplyBestXPerPeriod(Championship championship, List<PlayerPerformance> performances)
        {
            int? bestXCount = null;
            Func<DateTime, string> getPeriodKey = null;

            if (championship.BestXPerMonth.HasValue)
            {
                bestXCount = championship.BestXPerMonth.Value;
                getPeriodKey = date => $"{date.Year}-{date.Month:D2}";
            }
            else if (championship.BestXPerQuarter.HasValue)
            {
                bestXCount = championship.BestXPerQuarter.Value;
                getPeriodKey = date => $"{date.Year}-Q{(date.Month - 1) / 3 + 1}";
            }
            else
            {
                return performances; // Pas de config => tous conservés
            }

            var matchDates = _context.ChampionshipMatches
                .Where(m => m.ChampionshipId == championship.Id && performances.Select(p => p.MatchId).Contains(m.Id))
                .Select(m => new { m.Id, m.MatchDate })
                .ToList()
                .ToDictionary(m => m.Id, m => m.MatchDate);

            var performancesByPeriod = performances
                .Where(p => matchDates.ContainsKey(p.MatchId))
                .GroupBy(p => getPeriodKey(matchDates[p.MatchId]))
                .ToList();

            var retainedPerformances = new List<PlayerPerformance>();

            foreach (var periodGroup in performancesByPeriod)
            {
                var bestOfPeriod = periodGroup
                    .OrderByDescending(p => CalculateMatchPoints(championship, p))
                    .Take(bestXCount.Value)
                    .ToList();

                retainedPerformances.AddRange(bestOfPeriod);
            }

            return retainedPerformances;
        }

        private async Task AssignPositionsAsync(int championshipId)
        {
            var standings = await _context.ChampionshipStandings
                .Where(s => s.ChampionshipId == championshipId && s.IsActive)
                .OrderByDescending(s => s.TotalPoints)
                .ToListAsync();

            var championship = await GetChampionshipAsync(championshipId);
            if (championship == null) return;

            for (int i = 0; i < standings.Count; i++)
            {
                var standing = standings[i];
                standing.PreviousPosition = standing.CurrentPosition > 0 ? standing.CurrentPosition : null;
                standing.CurrentPosition = i + 1;

                // Gérer égalités avec tiebreakers
                if (i > 0 && standings[i - 1].TotalPoints == standing.TotalPoints)
                {
                    int tiebreakResult = ApplyTiebreakers(championship, standings[i - 1], standing);
                    if (tiebreakResult == 0)
                    {
                        // Égalité parfaite
                        standing.CurrentPosition = standings[i - 1].CurrentPosition;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private int ApplyTiebreakers(Championship championship, ChampionshipStanding s1, ChampionshipStanding s2)
        {
            var tiebreakers = new[] { championship.Tiebreaker1, championship.Tiebreaker2, championship.Tiebreaker3 }
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .ToList();

            foreach (var tiebreaker in tiebreakers)
            {
                int result = 0;

                switch (tiebreaker)
                {
                    case ChampionshipTiebreaker.NumberOfWins:
                        result = s2.Victories.CompareTo(s1.Victories);
                        break;

                    case ChampionshipTiebreaker.BestIndividualResult:
                        result = (s1.BestPosition ?? int.MaxValue).CompareTo(s2.BestPosition ?? int.MaxValue);
                        break;

                    case ChampionshipTiebreaker.SumOfPositions:
                        // Moins de points = meilleur
                        var sum1 = s1.AveragePosition * s1.MatchesPlayed;
                        var sum2 = s2.AveragePosition * s2.MatchesPlayed;
                        result = sum1.CompareTo(sum2);
                        break;

                    case ChampionshipTiebreaker.MoreMatchesPlayed:
                        result = s2.MatchesPlayed.CompareTo(s1.MatchesPlayed);
                        break;
                }

                if (result != 0)
                    return result;
            }

            return 0; // Égalité parfaite
        }

        // === HELPERS ===

        private decimal CalculatePointsForPosition(int position, int totalPlayers, Championship championship)
        {
            return championship.LinearFirstPlacePoints - (position - 1) * 10;
        }

        private async Task<List<PlayerPerformance>> GetPlayerPerformancesAsync(int championshipId, int playerId)
        {
            var matches = await _context.ChampionshipMatches
                .Where(m => m.ChampionshipId == championshipId)
                .Include(m => m.Tournament)
                .ToListAsync();

            var performances = new List<PlayerPerformance>();

            foreach (var match in matches)
            {
                var tournamentPlayer = await _context.TournamentPlayers
                    .FirstOrDefaultAsync(tp => tp.TournamentId == match.TournamentId && tp.PlayerId == playerId);

                if (tournamentPlayer != null && tournamentPlayer.FinishPosition.HasValue)
                {
                    var prizePool = await _context.Tournaments
                        .Where(t => t.Id == match.TournamentId)
                        .Select(t => t.BuyIn * t.Players.Count)
                        .FirstOrDefaultAsync();

                    performances.Add(new PlayerPerformance
                    {
                        MatchId = match.Id,
                        Position = tournamentPlayer.FinishPosition.Value,
                        Bounties = tournamentPlayer.BountyKills,
                        Winnings = tournamentPlayer.Winnings ?? 0,
                        TotalPrizePool = prizePool,
                        Coefficient = match.Coefficient,
                        MatchDate = match.MatchDate
                    });
                }
            }

            return performances.OrderBy(p => p.MatchDate).ToList();
        }

        private double CalculateStdDev(List<int> values)
        {
            if (values.Count < 2) return 0;

            double avg = values.Average();
            double sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
            return Math.Sqrt(sumOfSquares / values.Count);
        }

        // === LOGS ===

        private async Task LogActionAsync(int championshipId, ChampionshipLogAction action, 
            string? description = null, string? beforeData = null, string? afterData = null, 
            int? playerId = null, int? matchId = null)
        {
            var log = new ChampionshipLog
            {
                ChampionshipId = championshipId,
                Action = action,
                Description = description,
                BeforeData = beforeData,
                AfterData = afterData,
                PlayerId = playerId,
                MatchId = matchId,
                Username = "Admin" // TODO: Récupérer utilisateur actuel
            };

            _context.ChampionshipLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChampionshipLog>> GetLogsAsync(int championshipId, DateTime? since = null)
        {
            var query = _context.ChampionshipLogs
                .Where(l => l.ChampionshipId == championshipId);

            if (since.HasValue)
            {
                query = query.Where(l => l.Timestamp >= since.Value);
            }

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task SaveLogAsync(ChampionshipLog log)
        {
            _context.ChampionshipLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task RecalculateMonthlyStandingsAsync(int championshipId)
        {
            var championship = await GetChampionshipAsync(championshipId);
            if (championship == null || !championship.EnableMonthlyStandings) return;

            var matches = await _context.ChampionshipMatches
                .Include(m => m.Tournament).ThenInclude(t => t!.Players).ThenInclude(p => p.Player)
                .Where(m => m.ChampionshipId == championshipId)
                .ToListAsync();

            // Grouper par joueur
            var playerStandings = await _context.ChampionshipStandings
                .Where(s => s.ChampionshipId == championshipId)
                .ToDictionaryAsync(s => s.PlayerId);

            foreach (var (playerId, standing) in playerStandings)
            {
                var monthlyData = new Dictionary<string, decimal>();

                // Calculer points par mois
                var playerMatches = matches
                    .SelectMany(m => m.Tournament!.Players.Where(p => p.PlayerId == playerId))
                    .GroupBy(p => $"{p.Tournament!.Date.Year}-{p.Tournament.Date.Month:D2}");

                foreach (var month in playerMatches)
                {
                    decimal monthPoints = 0;
                    foreach (var player in month)
                    {
                        if (player.FinishPosition.HasValue)
                        {
                            var points = CalculatePointsForPosition(
                                player.FinishPosition.Value,
                                month.Count(),
                                championship);
                            monthPoints += points;
                        }
                    }
                    monthlyData[month.Key] = monthPoints;
                }

                standing.MonthlyPoints = System.Text.Json.JsonSerializer.Serialize(monthlyData);
                standing.LastUpdated = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RecalculateQuarterlyStandingsAsync(int championshipId)
        {
            var championship = await GetChampionshipAsync(championshipId);
            if (championship == null || !championship.EnableQuarterlyStandings) return;

            var matches = await _context.ChampionshipMatches
                .Include(m => m.Tournament).ThenInclude(t => t!.Players)
                .Where(m => m.ChampionshipId == championshipId)
                .ToListAsync();

            var playerStandings = await _context.ChampionshipStandings
                .Where(s => s.ChampionshipId == championshipId)
                .ToDictionaryAsync(s => s.PlayerId);

            foreach (var (playerId, standing) in playerStandings)
            {
                var quarterlyData = new Dictionary<string, decimal>();

                var playerMatches = matches
                    .SelectMany(m => m.Tournament!.Players.Where(p => p.PlayerId == playerId))
                    .GroupBy(p => $"{p.Tournament!.Date.Year}-Q{(p.Tournament.Date.Month - 1) / 3 + 1}");

                foreach (var quarter in playerMatches)
                {
                    decimal quarterPoints = 0;
                    foreach (var player in quarter)
                    {
                        if (player.FinishPosition.HasValue)
                        {
                            var points = CalculatePointsForPosition(
                                player.FinishPosition.Value,
                                quarter.Count(),
                                championship);
                            quarterPoints += points;
                        }
                    }
                    quarterlyData[quarter.Key] = quarterPoints;
                }

                standing.QuarterlyPoints = System.Text.Json.JsonSerializer.Serialize(quarterlyData);
                standing.LastUpdated = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        // Classes helper
        private class MonthlyStandingData
        {
            public int PlayerId { get; set; }
            public string PlayerName { get; set; } = string.Empty;
            public int Year { get; set; }
            public int Month { get; set; }
            public decimal TotalPoints { get; set; }
            public int MatchesPlayed { get; set; }
            public int Wins { get; set; }
            public int Top3 { get; set; }
            public int BestFinish { get; set; } = int.MaxValue;
        }

        private class QuarterlyStandingData
        {
            public int PlayerId { get; set; }
            public string PlayerName { get; set; } = string.Empty;
            public int Year { get; set; }
            public int Quarter { get; set; }
            public decimal TotalPoints { get; set; }
            public int MatchesPlayed { get; set; }
            public int Wins { get; set; }
            public int Top3 { get; set; }
            public int BestFinish { get; set; } = int.MaxValue;
        }

        private async Task SaveOrUpdateMonthlyStandingAsync(int championshipId, MonthlyStandingData data)
        {
            // TODO: Implémenter avec table MonthlyStanding
            // Pour l'instant, on peut stocker en JSON dans ChampionshipStanding
            var standing = await _context.ChampionshipStandings
                .FirstOrDefaultAsync(s => s.ChampionshipId == championshipId &&
                                          s.PlayerId == data.PlayerId);

            if (standing != null)
            {
                // Mettre à jour métadonnées avec info mensuelle
                // standing.MonthlyData = JsonSerializer.Serialize(data);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SaveOrUpdateQuarterlyStandingAsync(int championshipId, QuarterlyStandingData data)
        {
            // TODO: Implémenter avec table QuarterlyStanding
            var standing = await _context.ChampionshipStandings
                .FirstOrDefaultAsync(s => s.ChampionshipId == championshipId &&
                                          s.PlayerId == data.PlayerId);

            if (standing != null)
            {
                // Mettre à jour métadonnées avec info trimestrielle
                // standing.QuarterlyData = JsonSerializer.Serialize(data);
                await _context.SaveChangesAsync();
            }
        }
    }

    // Classe helper pour calculs
    internal class PlayerPerformance
    {
        public int MatchId { get; set; }
        public int Position { get; set; }
        public int Bounties { get; set; }
        public decimal Winnings { get; set; }
        public decimal TotalPrizePool { get; set; }
        public decimal Coefficient { get; set; }
        public DateTime MatchDate { get; set; }
    }
}
