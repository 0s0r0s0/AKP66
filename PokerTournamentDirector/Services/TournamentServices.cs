using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    public class TournamentService
    {
        private readonly PokerDbContext _context;

        public TournamentService(PokerDbContext context)
        {
            _context = context;
        }

        // ==================== TOURNOI ====================

        public async Task<Tournament> CreateTournamentAsync(Tournament tournament)
        {
            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();
            return tournament;
        }

        public async Task<Tournament?> GetTournamentAsync(int id)
        {
            return await _context.Tournaments
                .Include(t => t.BlindStructure)
                    .ThenInclude(bs => bs!.Levels.OrderBy(l => l.LevelNumber))
                .Include(t => t.Players)
                    .ThenInclude(tp => tp.Player)
                .Include(t => t.Tables)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Tournament>> GetAllTournamentsAsync()
        {
            return await _context.Tournaments
                .Include(t => t.BlindStructure)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task UpdateTournamentAsync(Tournament tournament)
        {
            _context.Tournaments.Update(tournament);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTournamentAsync(int id)
        {
            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament != null)
            {
                _context.Tournaments.Remove(tournament);
                await _context.SaveChangesAsync();
            }
        }

        // ==================== JOUEURS ====================

        public async Task RegisterPlayerAsync(int tournamentId, int playerId)
        {
            var tournament = await GetTournamentAsync(tournamentId);
            if (tournament == null) throw new InvalidOperationException("Tournament not found");

            var existingRegistration = await _context.TournamentPlayers
                .FirstOrDefaultAsync(tp => tp.TournamentId == tournamentId && tp.PlayerId == playerId);

            if (existingRegistration != null)
                throw new InvalidOperationException("Player already registered");

            var tournamentPlayer = new TournamentPlayer
            {
                TournamentId = tournamentId,
                PlayerId = playerId,
                CurrentStack = tournament.StartingStack
            };

            _context.TournamentPlayers.Add(tournamentPlayer);
            await _context.SaveChangesAsync();
        }

        public async Task UnregisterPlayerAsync(int tournamentId, int playerId)
        {
            var registration = await _context.TournamentPlayers
                .FirstOrDefaultAsync(tp => tp.TournamentId == tournamentId && tp.PlayerId == playerId);

            if (registration != null)
            {
                _context.TournamentPlayers.Remove(registration);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<TournamentPlayer>> GetActivePlayers(int tournamentId)
        {
            return await _context.TournamentPlayers
                .Include(tp => tp.Player)
                .Where(tp => tp.TournamentId == tournamentId && !tp.IsEliminated)
                .ToListAsync();
        }

        // ==================== ÉLIMINATIONS ====================

        public async Task EliminatePlayerAsync(int tournamentPlayerId, int? eliminatedByPlayerId = null)
        {
            var tournamentPlayer = await _context.TournamentPlayers.FindAsync(tournamentPlayerId);
            if (tournamentPlayer == null) throw new InvalidOperationException("Player not found");

            var activePlayers = await GetActivePlayers(tournamentPlayer.TournamentId);

            tournamentPlayer.IsEliminated = true;
            tournamentPlayer.EliminationTime = DateTime.Now;
            tournamentPlayer.FinishPosition = activePlayers.Count; // Position = nombre de joueurs restants
            tournamentPlayer.EliminatedByPlayerId = eliminatedByPlayerId;

            // Si c'est un bounty killer, on incrémente
            if (eliminatedByPlayerId.HasValue)
            {
                var killer = await _context.TournamentPlayers.FindAsync(eliminatedByPlayerId.Value);
                if (killer != null)
                {
                    killer.BountyKills++;
                }
            }

            await _context.SaveChangesAsync();
        }

        // ==================== TABLES & SEATING ====================

        public async Task CreateTablesAsync(int tournamentId)
        {
            var tournament = await GetTournamentAsync(tournamentId);
            if (tournament == null) throw new InvalidOperationException("Tournament not found");

            var activePlayers = await GetActivePlayers(tournamentId);
            int playerCount = activePlayers.Count;
            int tableCount = (int)Math.Ceiling((double)playerCount / tournament.SeatsPerTable);

            // Créer les tables
            for (int i = 1; i <= tableCount; i++)
            {
                var table = new PokerTable
                {
                    TournamentId = tournamentId,
                    TableNumber = i,
                    MaxSeats = tournament.SeatsPerTable,
                    IsActive = true
                };
                _context.PokerTables.Add(table);
            }

            await _context.SaveChangesAsync();

            // Placement aléatoire
            await RandomSeatPlayersAsync(tournamentId);
        }

        public async Task RandomSeatPlayersAsync(int tournamentId)
        {
            var tournament = await GetTournamentAsync(tournamentId);
            if (tournament == null) throw new InvalidOperationException("Tournament not found");

            var activePlayers = await GetActivePlayers(tournamentId);
            var tables = await _context.PokerTables
                .Where(t => t.TournamentId == tournamentId && t.IsActive)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            // Shuffle players
            var random = new Random();
            var shuffledPlayers = activePlayers.OrderBy(x => random.Next()).ToList();

            int playerIndex = 0;
            foreach (var table in tables)
            {
                for (int seat = 1; seat <= table.MaxSeats && playerIndex < shuffledPlayers.Count; seat++)
                {
                    var player = shuffledPlayers[playerIndex];
                    player.TableId = table.Id;
                    player.SeatNumber = seat;
                    playerIndex++;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task BalanceTablesAsync(int tournamentId)
        {
            var activePlayers = await GetActivePlayers(tournamentId);
            var tables = await _context.PokerTables
                .Where(t => t.TournamentId == tournamentId && t.IsActive)
                .ToListAsync();

            // Calculer la distribution équilibrée
            int totalPlayers = activePlayers.Count;
            int tableCount = tables.Count;
            int playersPerTable = totalPlayers / tableCount;
            int remainder = totalPlayers % tableCount;

            // Réassigner tous les joueurs
            var random = new Random();
            var shuffledPlayers = activePlayers.OrderBy(x => random.Next()).ToList();

            int playerIndex = 0;
            foreach (var table in tables.OrderBy(t => t.TableNumber))
            {
                int seatsForThisTable = playersPerTable + (remainder > 0 ? 1 : 0);
                remainder--;

                for (int seat = 1; seat <= seatsForThisTable && playerIndex < shuffledPlayers.Count; seat++)
                {
                    var player = shuffledPlayers[playerIndex];
                    player.TableId = table.Id;
                    player.SeatNumber = seat;
                    playerIndex++;
                }
            }

            await _context.SaveChangesAsync();
        }

        // ==================== PRIZE POOL ====================

        public async Task<decimal> CalculatePrizePoolAsync(int tournamentId)
        {
            var tournament = await GetTournamentAsync(tournamentId);
            if (tournament == null) return 0;

            var playersCount = tournament.Players.Count;

            decimal totalBuyIns = playersCount * tournament.BuyIn;
            decimal totalRebuys = tournament.TotalRebuys * (tournament.RebuyAmount ?? 0);
            decimal totalAddOns = tournament.TotalAddOns * (tournament.AddOnAmount ?? 0);
            decimal totalRake = (totalBuyIns + totalRebuys + totalAddOns) * (tournament.Rake / 100m);

            tournament.TotalPrizePool = totalBuyIns + totalRebuys + totalAddOns - totalRake;
            await _context.SaveChangesAsync();

            return tournament.TotalPrizePool;
        }

        public async Task UpdateTournamentPlayerAsync(TournamentPlayer tournamentPlayer)
        {
            _context.TournamentPlayers.Update(tournamentPlayer);
            await _context.SaveChangesAsync();
        }

        // ==================== STATISTIQUES ====================

        public async Task<int> GetAverageStackAsync(int tournamentId)
        {
            var activePlayers = await GetActivePlayers(tournamentId);
            if (!activePlayers.Any()) return 0;

            return (int)activePlayers.Average(p => p.CurrentStack);
        }

        public async Task<int> GetRemainingPlayersCountAsync(int tournamentId)
        {
            return await _context.TournamentPlayers
                .CountAsync(tp => tp.TournamentId == tournamentId && !tp.IsEliminated);
        }
    }
}