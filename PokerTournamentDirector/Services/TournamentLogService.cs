// Services/TournamentLogService.cs

using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    public class TournamentLogService
    {
        private readonly PokerDbContext _context;

        public TournamentLogService(PokerDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Ajouter un log
        /// </summary>
        public async Task AddLogAsync(TournamentLog log)
        {
            _context.TournamentLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Obtenir tous les logs d'un tournoi
        /// </summary>
        public async Task<List<TournamentLog>> GetTournamentLogsAsync(int tournamentId)
        {
            return await _context.TournamentLogs
                .Where(l => l.TournamentId == tournamentId)
                .OrderBy(l => l.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Obtenir les logs récents (par défaut 50 derniers)
        /// </summary>
        public async Task<List<TournamentLog>> GetRecentLogsAsync(int tournamentId, int count = 50)
        {
            return await _context.TournamentLogs
                .Where(l => l.TournamentId == tournamentId)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .OrderBy(l => l.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Exporter les logs en texte
        /// </summary>
        public async Task<string> ExportLogsAsTextAsync(int tournamentId)
        {
            var logs = await GetTournamentLogsAsync(tournamentId);
            var tournament = await _context.Tournaments.FindAsync(tournamentId);

            var text = new System.Text.StringBuilder();
            text.AppendLine($"=== LOGS TOURNOI: {tournament?.Name} ===");
            text.AppendLine($"Date: {tournament?.Date:dd/MM/yyyy HH:mm}");
            text.AppendLine($"Généré le: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            text.AppendLine();
            text.AppendLine("Timestamp\t\tNiveau\tJoueurs\tAction\t\tDétails");
            text.AppendLine(new string('-', 100));

            foreach (var log in logs)
            {
                text.AppendLine($"{log.Timestamp:HH:mm:ss}\t\t{log.Level}\t{log.PlayersRemaining}\t{log.Action}\t\t{log.Details}");
            }

            return text.ToString();
        }

        /// <summary>
        /// Supprimer les logs d'un tournoi
        /// </summary>
        public async Task DeleteTournamentLogsAsync(int tournamentId)
        {
            var logs = await _context.TournamentLogs
                .Where(l => l.TournamentId == tournamentId)
                .ToListAsync();

            _context.TournamentLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();
        }
    }
}
