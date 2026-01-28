using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    /// <summary>
    /// Service de chargement optimisé des données championnat
    /// Réduit les requêtes DB en préchargeant en batch
    /// </summary>
    public class ChampionshipDataLoaderService
    {
        private readonly PokerDbContext _context;

        public ChampionshipDataLoaderService(PokerDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Charge en batch TOUTES les performances des joueurs pour un championnat donné.
        /// - Une seule requête pour tous les TournamentPlayers
        /// - Une seule requête pour les ChampionshipMatches
        /// - Pré-calcule le nombre de joueurs par tournoi
        /// - Prépare les FullPerformance (points = 0, à calculer après)
        /// </summary>
        public async Task<Dictionary<int, List<FullPerformance>>> LoadAllPerformancesBatchAsync(
    int championshipId,
    Championship championship,
    ChampionshipCalculationService calculationService)
        {
            var matches = await _context.ChampionshipMatches
                .Where(m => m.ChampionshipId == championshipId)
                .Include(m => m.Tournament)
                .ToListAsync();

            var allTournamentIds = matches.Select(m => m.TournamentId).ToList();

            // UNE SEULE requête pour tous les joueurs de tous les tournois
            var allTournamentPlayers = await _context.TournamentPlayers
                .Where(tp => allTournamentIds.Contains(tp.TournamentId) && tp.FinishPosition.HasValue)
                .ToListAsync();

            // Compter joueurs par tournoi
            var playerCounts = allTournamentPlayers
                .GroupBy(tp => tp.TournamentId)
                .ToDictionary(g => g.Key, g => g.Count());

            // Dictionnaire TournamentId -> Match pour accès rapide
            var matchesByTournamentId = matches.ToDictionary(m => m.TournamentId, m => m);

            var result = new Dictionary<int, List<FullPerformance>>();

            foreach (var tp in allTournamentPlayers)
            {
                if (!result.ContainsKey(tp.PlayerId))
                    result[tp.PlayerId] = new List<FullPerformance>();

                // IMPORTANT : Calculer les points complets ici
                var match = matchesByTournamentId[tp.TournamentId];
                int fullPoints = calculationService.CalculateFullMatchPoints(championship, match, tp);

                result[tp.PlayerId].Add(new FullPerformance
                {
                    Position = tp.FinishPosition.Value,
                    Points = fullPoints, // Points COMPLETS calculés
                    Bounties = tp.BountyKills,
                    Rebuys = tp.RebuyCount,
                    TotalPlayers = playerCounts.GetValueOrDefault(tp.TournamentId, 0)
                });
            }

            return result;
        }

        public async Task<List<ChampionshipMatch>> LoadMatchesWithPlayersAsync(int championshipId)
        {
            return await _context.ChampionshipMatches
                .Where(m => m.ChampionshipId == championshipId)
                .Include(m => m.Tournament)
                    .ThenInclude(t => t.Players)
                        .ThenInclude(tp => tp.Player)
                .OrderByDescending(m => m.MatchDate)
                .ToListAsync();
        }
    }
}