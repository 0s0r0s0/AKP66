using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    /// <summary>
    /// Service amélioré pour la gestion des tables et le placement des joueurs
    /// </summary>
    public class TableManagementService
    {
        private readonly PokerDbContext _context;

        public TableManagementService(PokerDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Crée les tables nécessaires pour un tournoi
        /// </summary>
        public async Task<List<PokerTable>> CreateTablesAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Players)
                .Include(t => t.Tables)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return new List<PokerTable>();

            var activePlayers = tournament.Players.Where(p => !p.IsEliminated).ToList();
            int playerCount = activePlayers.Count;
            int seatsPerTable = tournament.SeatsPerTable;

            // Calculer le nombre de tables nécessaires
            int tableCount = (int)Math.Ceiling((double)playerCount / seatsPerTable);
            if (tableCount == 0) tableCount = 1;

            // Supprimer les anciennes tables
            if (tournament.Tables.Any())
            {
                _context.PokerTables.RemoveRange(tournament.Tables);
                await _context.SaveChangesAsync();
            }

            // Créer les nouvelles tables
            var tables = new List<PokerTable>();
            for (int i = 1; i <= tableCount; i++)
            {
                var table = new PokerTable
                {
                    TournamentId = tournamentId,
                    TableNumber = i,
                    MaxSeats = seatsPerTable,
                    IsActive = true
                };
                tables.Add(table);
                _context.PokerTables.Add(table);
            }

            await _context.SaveChangesAsync();
            return tables;
        }

        /// <summary>
        /// Place automatiquement tous les joueurs aux tables de manière équilibrée
        /// </summary>
        public async Task<List<TableSeatAssignment>> AutoAssignPlayersAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Players).ThenInclude(tp => tp.Player)
                .Include(t => t.Tables)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return new List<TableSeatAssignment>();

            var tables = tournament.Tables.Where(t => t.IsActive).OrderBy(t => t.TableNumber).ToList();
            var players = tournament.Players.Where(p => !p.IsEliminated && !p.IsLocked).OrderBy(_ => Guid.NewGuid()).ToList();
            var lockedPlayers = tournament.Players.Where(p => !p.IsEliminated && p.IsLocked).ToList();

            if (!tables.Any()) return new List<TableSeatAssignment>();

            var assignments = new List<TableSeatAssignment>();
            var seatCountPerTable = new Dictionary<int, int>();

            foreach (var table in tables)
            {
                seatCountPerTable[table.Id] = 0;
            }

            // D'abord, placer les joueurs verrouillés
            foreach (var player in lockedPlayers)
            {
                if (player.TableId.HasValue && player.SeatNumber.HasValue)
                {
                    var existingTable = tables.FirstOrDefault(t => t.Id == player.TableId);
                    if (existingTable != null)
                    {
                        seatCountPerTable[existingTable.Id]++;
                        assignments.Add(new TableSeatAssignment
                        {
                            TournamentPlayerId = player.Id,
                            PlayerName = player.Player?.Name ?? "?",
                            TableId = existingTable.Id,
                            TableNumber = existingTable.TableNumber,
                            SeatNumber = player.SeatNumber.Value,
                            IsLocked = true
                        });
                    }
                }
            }

            // Ensuite, placer les autres joueurs de manière équilibrée
            foreach (var player in players)
            {
                // Trouver la table avec le moins de joueurs
                var targetTable = tables
                    .Where(t => seatCountPerTable[t.Id] < t.MaxSeats)
                    .OrderBy(t => seatCountPerTable[t.Id])
                    .ThenBy(t => t.TableNumber)
                    .FirstOrDefault();

                if (targetTable == null) continue;

                // Trouver le prochain siège disponible
                var occupiedSeats = assignments
                    .Where(a => a.TableId == targetTable.Id)
                    .Select(a => a.SeatNumber)
                    .ToHashSet();

                int nextSeat = 1;
                while (occupiedSeats.Contains(nextSeat) && nextSeat <= targetTable.MaxSeats)
                {
                    nextSeat++;
                }

                player.TableId = targetTable.Id;
                player.SeatNumber = nextSeat;
                seatCountPerTable[targetTable.Id]++;

                assignments.Add(new TableSeatAssignment
                {
                    TournamentPlayerId = player.Id,
                    PlayerName = player.Player?.Name ?? "?",
                    TableId = targetTable.Id,
                    TableNumber = targetTable.TableNumber,
                    SeatNumber = nextSeat,
                    IsLocked = false
                });
            }

            await _context.SaveChangesAsync();
            return assignments.OrderBy(a => a.TableNumber).ThenBy(a => a.SeatNumber).ToList();
        }

        /// <summary>
        /// Déplace un joueur vers une table/siège spécifique (gestion manuelle)
        /// </summary>
        public async Task<bool> MovePlayerAsync(int tournamentPlayerId, int targetTableId, int targetSeat)
        {
            var player = await _context.TournamentPlayers.FindAsync(tournamentPlayerId);
            if (player == null) return false;

            var table = await _context.PokerTables.FindAsync(targetTableId);
            if (table == null) return false;

            // Vérifier que le siège est libre
            var seatOccupied = await _context.TournamentPlayers
                .AnyAsync(p => p.TableId == targetTableId && p.SeatNumber == targetSeat && p.Id != tournamentPlayerId && !p.IsEliminated);

            if (seatOccupied) return false;

            player.TableId = targetTableId;
            player.SeatNumber = targetSeat;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Verrouille/Déverrouille un joueur à sa position actuelle
        /// </summary>
        public async Task<bool> ToggleLockPlayerAsync(int tournamentPlayerId)
        {
            var player = await _context.TournamentPlayers.FindAsync(tournamentPlayerId);
            if (player == null) return false;

            player.IsLocked = !player.IsLocked;

            await _context.SaveChangesAsync();
            return player.IsLocked;
        }

        /// <summary>
        /// Équilibre automatiquement les tables après une élimination ou un ajout de joueur
        /// AMÉLIORÉ : Détecte correctement les déséquilibres
        /// </summary>
        public async Task<BalanceResult> AutoBalanceAfterChangeAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Players).ThenInclude(tp => tp.Player)
                .Include(t => t.Tables)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
                return new BalanceResult { Success = false, Message = "Tournoi introuvable" };

            var activePlayers = tournament.Players.Where(p => !p.IsEliminated).ToList();
            var activeTables = tournament.Tables.Where(t => t.IsActive).OrderBy(t => t.TableNumber).ToList();

            if (!activeTables.Any() || !activePlayers.Any())
                return new BalanceResult { Success = true, Message = "Rien à équilibrer" };

            int playerCount = activePlayers.Count;
            int seatsPerTable = tournament.SeatsPerTable;

            // Calculer le nombre optimal de tables
            int optimalTableCount = (int)Math.Ceiling((double)playerCount / seatsPerTable);

            // Si on peut consolider sur une seule table
            if (playerCount <= seatsPerTable && activeTables.Count > 1)
            {
                return await ConsolidateToOneTableAsync(tournament, activePlayers, activeTables);
            }

            // Si on a trop de tables, casser une table
            if (activeTables.Count > optimalTableCount)
            {
                return await BreakTableAsync(tournament, activePlayers, activeTables);
            }

            // Équilibrer les joueurs entre les tables existantes
            return await SmartEqualizeTables(tournament, activePlayers, activeTables);
        }

        /// <summary>
        /// Équilibrage intelligent - Détecte les vrais déséquilibres
        /// </summary>
        private async Task<BalanceResult> SmartEqualizeTables(
            Tournament tournament,
            List<TournamentPlayer> activePlayers,
            List<PokerTable> activeTables)
        {
            var movements = new List<PlayerMovement>();

            // Calculer le nombre idéal de joueurs par table
            int totalPlayers = activePlayers.Count;
            int tableCount = activeTables.Count;
            int basePlayersPerTable = totalPlayers / tableCount;
            int extraPlayers = totalPlayers % tableCount;

            // Compter les joueurs par table avec leur target
            var tableCounts = activeTables
                .Select(t => new
                {
                    Table = t,
                    Count = activePlayers.Count(p => p.TableId == t.Id),
                    Target = basePlayersPerTable + (t.TableNumber <= extraPlayers ? 1 : 0)
                })
                .ToList();

            // DÉTECTION AMÉLIORÉE : Vérifier s'il y a un vrai déséquilibre
            // Un déséquilibre existe si la différence entre deux tables > 1
            int minPlayers = tableCounts.Min(t => t.Count);
            int maxPlayers = tableCounts.Max(t => t.Count);

            if (maxPlayers - minPlayers <= 1)
            {
                // Tables déjà bien équilibrées
                return new BalanceResult
                {
                    Success = true,
                    TableBroken = false,
                    Movements = new List<PlayerMovement>(),
                    Message = "Tables déjà équilibrées."
                };
            }

            // Identifier les tables déséquilibrées
            var tablesWithTooMany = tableCounts.Where(t => t.Count > t.Target).OrderByDescending(t => t.Count).ToList();
            var tablesWithTooFew = tableCounts.Where(t => t.Count < t.Target).OrderBy(t => t.Count).ToList();

            // Déplacer les joueurs pour équilibrer
            foreach (var overloaded in tablesWithTooMany)
            {
                int playersToMove = overloaded.Count - overloaded.Target;

                var playersToMoveList = activePlayers
                    .Where(p => p.TableId == overloaded.Table.Id && !p.IsLocked)
                    .OrderBy(_ => Guid.NewGuid()) // Aléatoire pour être équitable
                    .Take(playersToMove)
                    .ToList();

                foreach (var player in playersToMoveList)
                {
                    var targetTableInfo = tablesWithTooFew.FirstOrDefault();
                    if (targetTableInfo == null) break;

                    var targetTable = targetTableInfo.Table;

                    // Trouver un siège libre
                    var occupiedSeats = activePlayers
                        .Where(p => p.TableId == targetTable.Id)
                        .Select(p => p.SeatNumber ?? 0)
                        .ToHashSet();

                    int seatNumber = 1;
                    while (occupiedSeats.Contains(seatNumber) && seatNumber <= targetTable.MaxSeats)
                    {
                        seatNumber++;
                    }

                    movements.Add(new PlayerMovement
                    {
                        PlayerName = player.Player?.Name ?? "?",
                        FromTable = overloaded.Table.TableNumber,
                        FromSeat = player.SeatNumber ?? 0,
                        ToTable = targetTable.TableNumber,
                        ToSeat = seatNumber
                    });

                    player.TableId = targetTable.Id;
                    player.SeatNumber = seatNumber;

                    // Mettre à jour le compteur
                    targetTableInfo = tableCounts.First(t => t.Table.Id == targetTable.Id);
                    var updated = new { targetTableInfo.Table, Count = targetTableInfo.Count + 1, targetTableInfo.Target };
                    tableCounts.Remove(targetTableInfo);
                    tableCounts.Add(updated);
                }
            }

            if (movements.Any())
            {
                await _context.SaveChangesAsync();
            }

            return new BalanceResult
            {
                Success = true,
                TableBroken = false,
                Movements = movements,
                Message = movements.Any()
                    ? $"Équilibrage effectué : {movements.Count} joueur(s) déplacé(s)."
                    : "Tables déjà équilibrées."
            };
        }

        /// <summary>
        /// Consolide tous les joueurs sur une seule table
        /// </summary>
        private async Task<BalanceResult> ConsolidateToOneTableAsync(
            Tournament tournament,
            List<TournamentPlayer> activePlayers,
            List<PokerTable> activeTables)
        {
            // Garder la table 1
            var mainTable = activeTables.OrderBy(t => t.TableNumber).First();
            var otherTables = activeTables.Where(t => t.Id != mainTable.Id).ToList();

            var movements = new List<PlayerMovement>();

            // Récupérer les sièges déjà occupés sur la table principale
            var occupiedSeats = activePlayers
                .Where(p => p.TableId == mainTable.Id)
                .Select(p => p.SeatNumber ?? 0)
                .ToHashSet();

            int seatNumber = 1;
            foreach (var player in activePlayers.Where(p => p.TableId != mainTable.Id && !p.IsLocked))
            {
                // Trouver le prochain siège libre
                while (occupiedSeats.Contains(seatNumber) && seatNumber <= mainTable.MaxSeats)
                {
                    seatNumber++;
                }

                movements.Add(new PlayerMovement
                {
                    PlayerName = player.Player?.Name ?? "?",
                    FromTable = activeTables.FirstOrDefault(t => t.Id == player.TableId)?.TableNumber ?? 0,
                    FromSeat = player.SeatNumber ?? 0,
                    ToTable = mainTable.TableNumber,
                    ToSeat = seatNumber
                });

                player.TableId = mainTable.Id;
                player.SeatNumber = seatNumber;
                occupiedSeats.Add(seatNumber);
                seatNumber++;
            }

            // Gérer les joueurs verrouillés sur les tables à fermer
            foreach (var player in activePlayers.Where(p => p.IsLocked && p.TableId != mainTable.Id))
            {
                while (occupiedSeats.Contains(seatNumber) && seatNumber <= mainTable.MaxSeats)
                {
                    seatNumber++;
                }

                movements.Add(new PlayerMovement
                {
                    PlayerName = player.Player?.Name ?? "? (verrouillé)",
                    FromTable = activeTables.FirstOrDefault(t => t.Id == player.TableId)?.TableNumber ?? 0,
                    FromSeat = player.SeatNumber ?? 0,
                    ToTable = mainTable.TableNumber,
                    ToSeat = seatNumber
                });

                player.TableId = mainTable.Id;
                player.SeatNumber = seatNumber;
                player.IsLocked = false; // Déverrouiller car la table n'existe plus
                occupiedSeats.Add(seatNumber);
                seatNumber++;
            }

            // Désactiver les autres tables
            foreach (var table in otherTables)
            {
                table.IsActive = false;
            }

            await _context.SaveChangesAsync();

            return new BalanceResult
            {
                Success = true,
                TableBroken = true,
                BrokenTableNumber = otherTables.FirstOrDefault()?.TableNumber ?? 0,
                Movements = movements,
                Message = $"Consolidation : Tous les joueurs déplacés vers la Table {mainTable.TableNumber}. {otherTables.Count} table(s) fermée(s)."
            };
        }

        /// <summary>
        /// Casse une table et redistribue les joueurs
        /// </summary>
        private async Task<BalanceResult> BreakTableAsync(
            Tournament tournament,
            List<TournamentPlayer> activePlayers,
            List<PokerTable> activeTables)
        {
            // Choisir la table à casser (celle avec le moins de joueurs et le numéro le plus élevé)
            var tableToBreak = activeTables
                .Select(t => new
                {
                    Table = t,
                    PlayerCount = activePlayers.Count(p => p.TableId == t.Id)
                })
                .OrderBy(x => x.PlayerCount)
                .ThenByDescending(x => x.Table.TableNumber)
                .First()
                .Table;

            var remainingTables = activeTables.Where(t => t.Id != tableToBreak.Id).OrderBy(t => t.TableNumber).ToList();
            var playersToMove = activePlayers.Where(p => p.TableId == tableToBreak.Id).ToList();

            var movements = new List<PlayerMovement>();

            foreach (var player in playersToMove)
            {
                // Trouver la table avec le moins de joueurs
                var targetTable = remainingTables
                    .Select(t => new
                    {
                        Table = t,
                        Count = activePlayers.Count(p => p.TableId == t.Id)
                    })
                    .OrderBy(t => t.Count)
                    .ThenBy(t => t.Table.TableNumber)
                    .First()
                    .Table;

                // Trouver un siège libre
                var occupiedSeats = activePlayers
                    .Where(p => p.TableId == targetTable.Id)
                    .Select(p => p.SeatNumber ?? 0)
                    .ToHashSet();

                int seatNumber = 1;
                while (occupiedSeats.Contains(seatNumber) && seatNumber <= targetTable.MaxSeats)
                {
                    seatNumber++;
                }

                movements.Add(new PlayerMovement
                {
                    PlayerName = player.Player?.Name ?? "?",
                    FromTable = tableToBreak.TableNumber,
                    FromSeat = player.SeatNumber ?? 0,
                    ToTable = targetTable.TableNumber,
                    ToSeat = seatNumber
                });

                player.TableId = targetTable.Id;
                player.SeatNumber = seatNumber;
                if (player.IsLocked)
                {
                    player.IsLocked = false; // Déverrouiller car la table est cassée
                }
            }

            // Désactiver la table
            tableToBreak.IsActive = false;

            await _context.SaveChangesAsync();

            return new BalanceResult
            {
                Success = true,
                TableBroken = true,
                BrokenTableNumber = tableToBreak.TableNumber,
                Movements = movements,
                Message = $"Table {tableToBreak.TableNumber} cassée ! {movements.Count} joueur(s) réparti(s)."
            };
        }

        /// <summary>
        /// Ajoute un joueur retardataire (ou late reg) à la table la plus appropriée
        /// AMÉLIORÉ : Crée une nouvelle table si nécessaire
        /// </summary>
        public async Task<TableSeatAssignment?> AssignLatePlayerAsync(int tournamentPlayerId)
        {
            var player = await _context.TournamentPlayers
                .Include(tp => tp.Player)
                .Include(tp => tp.Tournament)
                    .ThenInclude(t => t!.Tables)
                .FirstOrDefaultAsync(tp => tp.Id == tournamentPlayerId);

            if (player == null || player.Tournament == null) return null;

            var tournament = player.Tournament;
            var activeTables = tournament.Tables.Where(t => t.IsActive).OrderBy(t => t.TableNumber).ToList();
            var activePlayers = await _context.TournamentPlayers
                .Where(tp => tp.TournamentId == tournament.Id && !tp.IsEliminated)
                .ToListAsync();

            // Chercher une table avec de la place
            var tableWithSpace = activeTables
                .Select(t => new
                {
                    Table = t,
                    PlayerCount = activePlayers.Count(p => p.TableId == t.Id)
                })
                .Where(t => t.PlayerCount < t.Table.MaxSeats)
                .OrderBy(t => t.PlayerCount)
                .FirstOrDefault();

            PokerTable targetTable;

            if (tableWithSpace == null)
            {
                // Toutes les tables sont pleines, créer une nouvelle table
                int nextTableNumber = activeTables.Any() ? activeTables.Max(t => t.TableNumber) + 1 : 1;

                targetTable = new PokerTable
                {
                    TournamentId = tournament.Id,
                    TableNumber = nextTableNumber,
                    MaxSeats = tournament.SeatsPerTable,
                    IsActive = true
                };

                _context.PokerTables.Add(targetTable);
                await _context.SaveChangesAsync();
            }
            else
            {
                targetTable = tableWithSpace.Table;
            }

            // Trouver un siège libre
            var occupiedSeats = activePlayers
                .Where(p => p.TableId == targetTable.Id)
                .Select(p => p.SeatNumber ?? 0)
                .ToHashSet();

            int seatNumber = 1;
            while (occupiedSeats.Contains(seatNumber) && seatNumber <= targetTable.MaxSeats)
            {
                seatNumber++;
            }

            player.TableId = targetTable.Id;
            player.SeatNumber = seatNumber;

            await _context.SaveChangesAsync();

            // Auto-équilibrer après l'ajout
            await AutoBalanceAfterChangeAsync(tournament.Id);

            return new TableSeatAssignment
            {
                TournamentPlayerId = player.Id,
                PlayerName = player.Player?.Name ?? "?",
                TableId = targetTable.Id,
                TableNumber = targetTable.TableNumber,
                SeatNumber = seatNumber,
                IsLocked = false
            };
        }

        /// <summary>
        /// Obtient le layout de toutes les tables
        /// </summary>
        public async Task<List<TableLayout>> GetTableLayoutAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Players).ThenInclude(tp => tp.Player)
                .Include(t => t.Tables)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return new List<TableLayout>();

            var layouts = new List<TableLayout>();

            foreach (var table in tournament.Tables.Where(t => t.IsActive).OrderBy(t => t.TableNumber))
            {
                var layout = new TableLayout
                {
                    TableId = table.Id,
                    TableNumber = table.TableNumber,
                    MaxSeats = table.MaxSeats,
                    Seats = new List<SeatInfo>()
                };

                var playersAtTable = tournament.Players
                    .Where(p => p.TableId == table.Id && !p.IsEliminated)
                    .OrderBy(p => p.SeatNumber)
                    .ToList();

                for (int seat = 1; seat <= table.MaxSeats; seat++)
                {
                    var playerAtSeat = playersAtTable.FirstOrDefault(p => p.SeatNumber == seat);
                    layout.Seats.Add(new SeatInfo
                    {
                        SeatNumber = seat,
                        IsOccupied = playerAtSeat != null,
                        TournamentPlayerId = playerAtSeat?.Id,
                        PlayerName = playerAtSeat?.Player?.Name,
                        IsLocked = playerAtSeat?.IsLocked ?? false,
                        CurrentStack = playerAtSeat?.CurrentStack ?? 0
                    });
                }

                layout.PlayerCount = playersAtTable.Count;
                layouts.Add(layout);
            }

            return layouts;
        }
    }

    // Classes de données pour les résultats
    public class TableSeatAssignment
    {
        public int TournamentPlayerId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int TableId { get; set; }
        public int TableNumber { get; set; }
        public int SeatNumber { get; set; }
        public bool IsLocked { get; set; }
    }

    public class BalanceResult
    {
        public bool Success { get; set; }
        public bool TableBroken { get; set; }
        public int BrokenTableNumber { get; set; }
        public List<PlayerMovement> Movements { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class PlayerMovement
    {
        public string PlayerName { get; set; } = string.Empty;
        public int FromTable { get; set; }
        public int FromSeat { get; set; }
        public int ToTable { get; set; }
        public int ToSeat { get; set; }
    }

    public class TableLayout
    {
        public int TableId { get; set; }
        public int TableNumber { get; set; }
        public int MaxSeats { get; set; }
        public int PlayerCount { get; set; }
        public List<SeatInfo> Seats { get; set; } = new();
    }

    public class SeatInfo
    {
        public int SeatNumber { get; set; }
        public bool IsOccupied { get; set; }
        public int? TournamentPlayerId { get; set; }
        public string? PlayerName { get; set; }
        public bool IsLocked { get; set; }
        public int CurrentStack { get; set; }
    }
}