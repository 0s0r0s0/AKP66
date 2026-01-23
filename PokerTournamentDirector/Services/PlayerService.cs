using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    public class PlayerService
    {
        private readonly PokerDbContext _context;

        public PlayerService(PokerDbContext context)
        {
            _context = context;
        }

        // ==================== CRUD JOUEURS ====================

        public async Task<Player> CreatePlayerAsync(Player player)
        {
            _context.Players.Add(player);
            await _context.SaveChangesAsync();
            return player;
        }

        public async Task<Player?> GetPlayerAsync(int id)
        {
            return await _context.Players
                .Include(p => p.TournamentParticipations)
                .Include(p => p.Rebuys)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Player>> GetAllPlayersAsync(bool activeOnly = false)
        {
            var query = _context.Players.AsQueryable();

            if (activeOnly)
            {
                query = query.Where(p => p.IsActive);
            }

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<List<Player>> SearchPlayersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllPlayersAsync();

            var term = searchTerm.ToLower();
            return await _context.Players
                .Where(p => p.Name.ToLower().Contains(term) ||
                           (p.Nickname != null && p.Nickname.ToLower().Contains(term)) ||
                           (p.Email != null && p.Email.ToLower().Contains(term)))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task UpdatePlayerAsync(Player player)
        {
            _context.Players.Update(player);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePlayerAsync(int id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player != null)
            {
                // Soft delete : on désactive au lieu de supprimer
                player.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        public async Task HardDeletePlayerAsync(int id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player != null)
            {
                _context.Players.Remove(player);
                await _context.SaveChangesAsync();
            }
        }

        // ==================== STATISTIQUES ====================

        public async Task UpdatePlayerStatsAsync(int playerId)
        {
            var player = await _context.Players
                .Include(p => p.TournamentParticipations)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null) return;

            player.TotalTournamentsPlayed = player.TournamentParticipations.Count;
            player.TotalWins = player.TournamentParticipations.Count(tp => tp.FinishPosition == 1);
            player.TotalITM = player.TournamentParticipations.Count(tp => tp.Winnings > 0);
            player.TotalWinnings = player.TournamentParticipations.Sum(tp => tp.Winnings ?? 0);

            await _context.SaveChangesAsync();
        }

        // ==================== GESTION RECAVES ====================

        public async Task<bool> CanPlayerRebuyAsync(int playerId, int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null) return false;

            // Si pas de limite (0 = illimité), toujours autorisé
            if (tournament.MaxRebuysPerPlayer == 0)
                return true;

            // Calculer la date de début de la période
            var periodStartDate = DateTime.Now;
            if (tournament.RebuyPeriodMonths > 0)
            {
                periodStartDate = DateTime.Now.AddMonths(-tournament.RebuyPeriodMonths);
            }

            // Compter les recaves du joueur sur la période
            var rebuyCount = await _context.PlayerRebuys
                .Where(r => r.PlayerId == playerId &&
                           r.RebuyDate >= periodStartDate)
                .CountAsync();

            return rebuyCount < tournament.MaxRebuysPerPlayer;
        }

        public async Task<DateTime?> GetNextRebuyAvailableDateAsync(int playerId, int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null) return null;

            // Si pas de restriction (0 = illimité)
            if (tournament.MaxRebuysPerPlayer == 0 || tournament.RebuyPeriodMonths == 0)
                return null;

            // Récupérer les recaves du joueur, triées par date
            var rebuys = await _context.PlayerRebuys
                .Where(r => r.PlayerId == playerId)
                .OrderBy(r => r.RebuyDate)
                .ToListAsync();

            if (rebuys.Count < tournament.MaxRebuysPerPlayer)
                return null; // Encore des recaves disponibles

            // La prochaine date disponible = date de la plus ancienne recave + période en mois
            var oldestRebuy = rebuys.First();
            return oldestRebuy.RebuyDate.AddMonths(tournament.RebuyPeriodMonths);
        }

        public async Task<PlayerRebuy> RecordRebuyAsync(int playerId, int tournamentId, decimal amount)
        {
            // Compter le numéro de recave
            var rebuyNumber = await _context.PlayerRebuys
                .Where(r => r.PlayerId == playerId && r.TournamentId == tournamentId)
                .CountAsync() + 1;

            var rebuy = new PlayerRebuy
            {
                PlayerId = playerId,
                TournamentId = tournamentId,
                RebuyDate = DateTime.Now,
                Amount = amount,
                RebuyNumber = rebuyNumber
            };

            _context.PlayerRebuys.Add(rebuy);
            await _context.SaveChangesAsync();

            return rebuy;
        }

        public async Task<int> GetPlayerRebuyCountAsync(int playerId, int tournamentId)
        {
            return await _context.PlayerRebuys
                .Where(r => r.PlayerId == playerId && r.TournamentId == tournamentId)
                .CountAsync();
        }

        // ==================== IMPORT CSV ====================

        public async Task<int> ImportPlayersFromCsvAsync(string csvContent)
        {
            var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int importedCount = 0;

            // Format attendu : Name,Nickname,Email,Phone
            // On skip la première ligne si c'est un header
            bool isFirstLine = true;

            foreach (var line in lines)
            {
                if (isFirstLine && (line.ToLower().Contains("name") || line.ToLower().Contains("nom")))
                {
                    isFirstLine = false;
                    continue;
                }
                isFirstLine = false;

                var parts = line.Split(',');
                if (parts.Length < 1) continue;

                var name = parts[0].Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Vérifier si le joueur existe déjà
                var existingPlayer = await _context.Players
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());

                if (existingPlayer != null) continue; // Skip si existe déjà

                var player = new Player
                {
                    Name = name,
                    Nickname = parts.Length > 1 ? parts[1].Trim() : null,
                    Email = parts.Length > 2 ? parts[2].Trim() : null,
                    Phone = parts.Length > 3 ? parts[3].Trim() : null
                };

                _context.Players.Add(player);
                importedCount++;
            }

            await _context.SaveChangesAsync();
            return importedCount;
        }
    }
}