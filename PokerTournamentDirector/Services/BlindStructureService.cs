using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.Services
{
    public class BlindStructureService
    {
        private readonly PokerDbContext _context;

        public BlindStructureService(PokerDbContext context)
        {
            _context = context;
        }

        public async Task<List<BlindStructure>> GetAllStructuresAsync()
        {
            return await _context.BlindStructures
                .Include(bs => bs.Levels.OrderBy(l => l.LevelNumber))
                .OrderBy(bs => bs.Name)
                .ToListAsync();
        }

        public async Task<BlindStructure?> GetStructureAsync(int id)
        {
            return await _context.BlindStructures
                .Include(bs => bs.Levels.OrderBy(l => l.LevelNumber))
                .FirstOrDefaultAsync(bs => bs.Id == id);
        }

        public async Task<BlindStructure> CreateStructureAsync(BlindStructure structure)
        {
            _context.BlindStructures.Add(structure);
            await _context.SaveChangesAsync();
            return structure;
        }

        public async Task UpdateStructureAsync(BlindStructure structure)
        {
            _context.BlindStructures.Update(structure);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteStructureAsync(int id)
        {
            var structure = await _context.BlindStructures.FindAsync(id);
            if (structure != null)
            {
                _context.BlindStructures.Remove(structure);
                await _context.SaveChangesAsync();
            }
        }

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

        // Générateur automatique de structure
        public BlindStructure GenerateStructure(
            string name,
            int targetDurationMinutes,
            int startingSmallBlind,
            int levelDurationMinutes,
            bool withAnte,
            int numberOfBreaks,
            int breakDurationMinutes)
        {
            var structure = new BlindStructure
            {
                Name = name,
                Description = $"Structure auto-générée ({targetDurationMinutes} min)"
            };

            // Calculer combien de niveaux de jeu on peut avoir
            int totalBreakTime = numberOfBreaks * breakDurationMinutes;
            int playTime = targetDurationMinutes - totalBreakTime;
            int numberOfLevels = playTime / levelDurationMinutes;

            // Calculer à quels niveaux placer les pauses (réparties équitablement)
            var breakLevels = new HashSet<int>();
            if (numberOfBreaks > 0)
            {
                int intervalBetweenBreaks = numberOfLevels / (numberOfBreaks + 1);
                for (int i = 1; i <= numberOfBreaks; i++)
                {
                    breakLevels.Add(i * intervalBetweenBreaks);
                }
            }

            var levels = new List<BlindLevel>();
            int sb = startingSmallBlind;
            int levelNumber = 1;
            int playLevelCount = 0;

            while (playLevelCount < numberOfLevels)
            {
                // Niveau de jeu
                int bb = sb * 2;
                int ante = withAnte ? (sb / 2) : 0;

                levels.Add(new BlindLevel
                {
                    LevelNumber = levelNumber++,
                    SmallBlind = sb,
                    BigBlind = bb,
                    Ante = ante,
                    DurationMinutes = levelDurationMinutes,
                    IsBreak = false
                });

                playLevelCount++;

                // Insérer une pause si nécessaire
                if (breakLevels.Contains(playLevelCount))
                {
                    levels.Add(new BlindLevel
                    {
                        LevelNumber = levelNumber++,
                        SmallBlind = 0,
                        BigBlind = 0,
                        Ante = 0,
                        DurationMinutes = breakDurationMinutes,
                        IsBreak = true,
                        BreakName = $"Pause {breakDurationMinutes} min"
                    });
                }

                // Progression : doubler la SB tous les 2-3 niveaux pour ralentir la progression
                if (playLevelCount % 2 == 0)
                {
                    sb = (int)(sb * 1.5);
                }
                else if (playLevelCount % 3 == 0)
                {
                    sb *= 2;
                }
                else
                {
                    sb = (int)(sb * 1.25);
                }

                // Arrondir à des valeurs propres
                sb = RoundToNice(sb);
            }

            structure.Levels = levels;
            return structure;
        }

        private int RoundToNice(int value)
        {
            // Arrondir à des valeurs "propres" (25, 50, 75, 100, 150, 200, etc.)
            if (value <= 100)
            {
                return (int)(Math.Round(value / 25.0) * 25);
            }
            else if (value <= 500)
            {
                return (int)(Math.Round(value / 50.0) * 50);
            }
            else if (value <= 1000)
            {
                return (int)(Math.Round(value / 100.0) * 100);
            }
            else
            {
                return (int)(Math.Round(value / 500.0) * 500);
            }
        }

        public int CalculateTotalDuration(BlindStructure structure)
        {
            return structure.Levels.Sum(l => l.DurationMinutes);
        }
    }
}