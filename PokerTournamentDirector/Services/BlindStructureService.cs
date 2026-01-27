using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// v2.0 - Gestion des structures de blinds 
namespace PokerTournamentDirector.Services
{
    public class BlindStructureService
    {
        private readonly PokerDbContext _context;

        public BlindStructureService(PokerDbContext context)
        {
            _context = context;
        }

        #region Opérations CRUD

        /// <summary>
        /// Récupère toutes les structures de blinds triées par nom.
        /// </summary>
        public async Task<List<BlindStructure>> GetAllStructuresAsync()
        {
            return await _context.BlindStructures
                .Include(bs => bs.Levels.OrderBy(l => l.LevelNumber))
                .OrderBy(bs => bs.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère une structure de blinds par son identifiant, avec ses niveaux.
        /// </summary>
        public async Task<BlindStructure?> GetStructureAsync(int id)
        {
            return await _context.BlindStructures
                .Include(bs => bs.Levels.OrderBy(l => l.LevelNumber))
                .FirstOrDefaultAsync(bs => bs.Id == id);
        }

        /// <summary>
        /// Crée une nouvelle structure de blinds dans la base de données.
        /// </summary>
        public async Task<BlindStructure> CreateStructureAsync(BlindStructure structure)
        {
            _context.BlindStructures.Add(structure);
            await _context.SaveChangesAsync();
            return structure;
        }

        /// <summary>
        /// Met à jour une structure de blinds existante.
        /// </summary>
        public async Task UpdateStructureAsync(BlindStructure structure)
        {
            _context.BlindStructures.Update(structure);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Supprime une structure de blinds par son identifiant.
        /// </summary>
        public async Task DeleteStructureAsync(int id)
        {
            var structure = await _context.BlindStructures.FindAsync(id);
            if (structure != null)
            {
                _context.BlindStructures.Remove(structure);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Duplique une structure de blinds existante avec un nouveau nom.
        /// </summary>
        public async Task<BlindStructure> DuplicateStructureAsync(int sourceId, string newName)
        {
            var source = await GetStructureAsync(sourceId);
            if (source == null)
                throw new InvalidOperationException("Structure source introuvable");

            var newStructure = new BlindStructure
            {
                Name = newName,
                Description = source.Description
            };

            _context.BlindStructures.Add(newStructure);
            await _context.SaveChangesAsync();

            foreach (var level in source.Levels)
            {
                var newLevel = new BlindLevel
                {
                    BlindStructureId = newStructure.Id,
                    LevelNumber = level.LevelNumber,
                    SmallBlind = level.SmallBlind,
                    BigBlind = level.BigBlind,
                    Ante = level.Ante,
                    DurationMinutes = level.DurationMinutes,
                    IsBreak = level.IsBreak,
                    BreakName = level.BreakName
                };
                _context.BlindLevels.Add(newLevel);
            }

            await _context.SaveChangesAsync();
            return newStructure;
        }

        #endregion

        #region Génération automatique de structure

        /// <summary>
        /// Génère une structure de blinds automatique adaptée au type de tournoi, 
        /// avec progression fluide, pauses intelligentes et cible finale réaliste.
        /// </summary>
        public BlindStructure GenerateStructure(
            string name,
            int targetDurationMinutes,
            int startingSmallBlind,
            int levelDurationMinutes,
            bool withAnte,
            int numberOfBreaks,
            int breakDurationMinutes,
            int autoStartingStack,
            int autoAveragePlayers)
        {
            var structure = new BlindStructure
            {
                Name = name,
                Description = $"Auto – {targetDurationMinutes} min – ~{autoAveragePlayers} joueurs – Stack {autoStartingStack} ({autoStartingStack / (startingSmallBlind * 2)} BB départ)"
            };

            int totalBreakTime = numberOfBreaks * breakDurationMinutes;
            int playTime = targetDurationMinutes - totalBreakTime;
            int numberOfLevels = playTime / levelDurationMinutes;
            if (numberOfLevels < 6) numberOfLevels = 6;

            var breakPositions = CalculateBreakPositions(numberOfLevels, numberOfBreaks);

            long totalChips = (long)autoStartingStack * autoAveragePlayers;
            long targetFinalBB = withAnte ? totalChips / 25 : totalChips / 20;
            long targetFinalSB = targetFinalBB / 2;

            TournamentSpeed speed = DetermineTournamentSpeed(playTime, numberOfLevels);

            var levels = GenerateLevels(
                startingSmallBlind,
                targetFinalSB,
                numberOfLevels,
                levelDurationMinutes,
                breakDurationMinutes,
                breakPositions,
                withAnte,
                speed);

            structure.Levels = levels;
            return structure;
        }

        #endregion

        #region Vitesse du tournoi et position des pauses

        private enum TournamentSpeed
        {
            Turbo,   // < 2h ou niveaux très courts
            Fast,    // 2-3h
            Normal,  // 3-4h
            Deep     // > 4h
        }

        private TournamentSpeed DetermineTournamentSpeed(int playTimeMinutes, int numberOfLevels)
        {
            int avgLevelMinutes = playTimeMinutes / numberOfLevels;

            if (playTimeMinutes < 120 || avgLevelMinutes < 12) return TournamentSpeed.Turbo;
            if (playTimeMinutes < 180) return TournamentSpeed.Fast;
            if (playTimeMinutes < 240) return TournamentSpeed.Normal;
            return TournamentSpeed.Deep;
        }

        private HashSet<int> CalculateBreakPositions(int numberOfLevels, int numberOfBreaks)
        {
            var positions = new HashSet<int>();

            if (numberOfBreaks == 0 || numberOfLevels <= numberOfBreaks)
                return positions;

            int usableRange = numberOfLevels - 2;
            int interval = usableRange / (numberOfBreaks + 1);

            for (int i = 1; i <= numberOfBreaks; i++)
            {
                int pos = 1 + (i * interval);
                if (pos > 0 && pos < numberOfLevels)
                    positions.Add(pos);
            }

            return positions;
        }

        #endregion

        #region Génération des niveaux

        private List<BlindLevel> GenerateLevels(
            int startingSB,
            long targetFinalSB,
            int numberOfLevels,
            int levelDuration,
            int breakDuration,
            HashSet<int> breakPositions,
            bool withAnte,
            TournamentSpeed speed)
        {
            var levels = new List<BlindLevel>();
            int levelNumber = 1;
            int currentSB = startingSB;

            var progression = GetProgressionFactors(speed, numberOfLevels);

            for (int playLevel = 1; playLevel <= numberOfLevels; playLevel++)
            {
                int currentBB = currentSB * 2;
                int currentAnte = 0;

                if (withAnte && currentSB >= 100)
                {
                    currentAnte = currentBB;
                }

                levels.Add(new BlindLevel
                {
                    LevelNumber = levelNumber++,
                    SmallBlind = currentSB,
                    BigBlind = currentBB,
                    Ante = currentAnte,
                    DurationMinutes = levelDuration,
                    IsBreak = false
                });

                if (playLevel < numberOfLevels)
                {
                    double factor = progression[playLevel - 1];
                    int nextSB = CalculateNextBlind(currentSB, factor, playLevel, numberOfLevels, targetFinalSB, speed);
                    currentSB = nextSB;
                }

                if (breakPositions.Contains(playLevel))
                {
                    levels.Add(new BlindLevel
                    {
                        LevelNumber = levelNumber++,
                        SmallBlind = 0,
                        BigBlind = 0,
                        Ante = 0,
                        DurationMinutes = breakDuration,
                        IsBreak = true,
                        BreakName = $"Pause {breakDuration} min"
                    });
                }
            }

            return levels;
        }

        private List<double> GetProgressionFactors(TournamentSpeed speed, int numberOfLevels)
        {
            var factors = new List<double>();

            for (int i = 0; i < numberOfLevels - 1; i++)
            {
                double progress = (double)i / (numberOfLevels - 1);
                double factor = speed switch
                {
                    TournamentSpeed.Turbo => progress < 0.3 ? 1.6 : progress < 0.7 ? 1.7 : 1.8,
                    TournamentSpeed.Fast => progress < 0.4 ? 1.45 : progress < 0.7 ? 1.55 : 1.65,
                    TournamentSpeed.Normal => progress < 0.5 ? 1.35 : progress < 0.8 ? 1.45 : 1.55,
                    TournamentSpeed.Deep => progress < 0.6 ? 1.3 : progress < 0.85 ? 1.4 : 1.5,
                    _ => 1.5
                };

                factors.Add(factor);
            }

            return factors;
        }

        private int CalculateNextBlind(
            int currentSB,
            double factor,
            int currentLevel,
            int totalLevels,
            long targetFinalSB,
            TournamentSpeed speed)
        {
            bool skipIntermediate = speed is TournamentSpeed.Turbo or TournamentSpeed.Fast;

            int? forcedNext = currentSB switch
            {
                25 => 50,
                50 => skipIntermediate ? 100 : 75,
                75 => 100,
                100 => skipIntermediate ? 200 : 150,
                150 => 200,
                200 => skipIntermediate ? 400 : 300,
                300 => 500,
                400 => 600,
                500 => 800,
                600 => 1000,
                800 => 1200,
                1000 => 1500,
                _ => null
            };

            if (forcedNext.HasValue)
                return forcedNext.Value;

            double remainingLevels = totalLevels - currentLevel;
            if (remainingLevels <= 3 && currentSB < targetFinalSB)
            {
                double neededFactor = Math.Pow((double)targetFinalSB / currentSB, 1.0 / remainingLevels);
                factor = Math.Max(factor, neededFactor);
            }

            long nextSBLong = (long)(currentSB * factor);
            int nextSB = RoundToNice((int)nextSBLong, currentSB);

            if (nextSB <= currentSB)
            {
                int minJump = currentSB switch
                {
                    <= 100 => 25,
                    <= 500 => 50,
                    <= 1000 => 100,
                    <= 5000 => 500,
                    _ => 1000
                };
                nextSB = RoundToNice(currentSB + minJump, currentSB);
            }

            return nextSB;
        }

        #endregion

        #region Arrondi intelligent

        private int RoundToNice(int value, int currentSB)
        {
            int currentBB = currentSB * 2;

            if (currentBB >= 100000) return (int)(Math.Round(value / 5000.0) * 5000);
            if (currentBB >= 10000) return (int)(Math.Round(value / 1000.0) * 1000);
            if (currentBB >= 1000) return (int)(Math.Round(value / 100.0) * 100);

            if (value <= 100)
                return (int)(Math.Round(value / 25.0) * 25);

            if (value <= 500)
            {
                int rounded = (int)(Math.Round(value / 50.0) * 50);
                return rounded == 450 ? 500 : rounded;
            }

            if (value <= 1000)
                return (int)(Math.Round(value / 100.0) * 100);

            if (value <= 5000)
                return (int)(Math.Round(value / 500.0) * 500);

            if (value <= 10000)
                return (int)(Math.Round(value / 1000.0) * 1000);

            if (value <= 50000)
                return (int)(Math.Round(value / 5000.0) * 5000);

            return (int)(Math.Round(value / 50000.0) * 50000);
        }

        #endregion

        #region Utilitaires

        /// <summary>
        /// Calcule la durée totale de la structure (niveaux + pauses).
        /// </summary>
        public int CalculateTotalDuration(BlindStructure structure)
        {
            return structure.Levels.Sum(l => l.DurationMinutes);
        }

        #endregion
    }
}