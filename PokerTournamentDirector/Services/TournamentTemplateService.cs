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
    public class TournamentTemplateService
    {
        private readonly PokerDbContext _context;

        public TournamentTemplateService(PokerDbContext context)
        {
            _context = context;
        }

        // ==================== CRUD TEMPLATES ====================

        public async Task<List<TournamentTemplate>> GetAllTemplatesAsync()
        {
            return await _context.TournamentTemplates
                .Include(t => t.BlindStructure)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<TournamentTemplate?> GetTemplateAsync(int id)
        {
            return await _context.TournamentTemplates
                .Include(t => t.BlindStructure)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TournamentTemplate> CreateTemplateAsync(TournamentTemplate template)
        {
            template.CreatedDate = DateTime.Now;
            _context.TournamentTemplates.Add(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task UpdateTemplateAsync(TournamentTemplate template)
        {
            template.LastModified = DateTime.Now;
            _context.TournamentTemplates.Update(template);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTemplateAsync(int id)
        {
            var template = await _context.TournamentTemplates.FindAsync(id);
            if (template != null)
            {
                _context.TournamentTemplates.Remove(template);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<TournamentTemplate> DuplicateTemplateAsync(int sourceId, string newName)
        {
            var source = await GetTemplateAsync(sourceId);
            if (source == null)
                throw new InvalidOperationException("Template introuvable");

            var duplicate = new TournamentTemplate
            {
                Name = newName,
                Description = source.Description,
                Type = source.Type,
                Currency = source.Currency,
                BuyIn = source.BuyIn,
                Rake = source.Rake,
                RakeType = source.RakeType,
                BlindStructureId = source.BlindStructureId,
                StartingStack = source.StartingStack,
                MaxPlayers = source.MaxPlayers,
                SeatsPerTable = source.SeatsPerTable,
                LateRegLevels = source.LateRegLevels,
                AllowRebuys = source.AllowRebuys,
                RebuyAmount = source.RebuyAmount,
                RebuyLimit = source.RebuyLimit,
                RebuyLimitType = source.RebuyLimitType,
                RebuyMaxLevel = source.RebuyMaxLevel,
                RebuyUntilPlayersLeft = source.RebuyUntilPlayersLeft,
                RebuyStack = source.RebuyStack,
                AllowAddOn = source.AllowAddOn,
                AddOnAmount = source.AddOnAmount,
                AddOnStack = source.AddOnStack,
                AddOnAtLevel = source.AddOnAtLevel,
                AllowBounty = source.AllowBounty,
                BountyAmount = source.BountyAmount,
                BountyType = source.BountyType,
                PayoutStructureJson = source.PayoutStructureJson
            };

            return await CreateTemplateAsync(duplicate);
        }

        // ==================== CRÉATION TOURNOI DEPUIS TEMPLATE ====================

        public async Task<Tournament> CreateTournamentFromTemplateAsync(int templateId, string tournamentName, DateTime date)
        {
            var template = await GetTemplateAsync(templateId);
            if (template == null)
                throw new InvalidOperationException("Template introuvable");

            var tournament = new Tournament
            {
                Name = tournamentName,
                Date = date,
                TemplateId = templateId,
                Currency = template.Currency,
                BuyIn = template.BuyIn,
                Rake = template.Rake,
                RakeType = template.RakeType,
                BlindStructureId = template.BlindStructureId,
                StartingStack = template.StartingStack,
                MaxPlayers = template.MaxPlayers,
                SeatsPerTable = template.SeatsPerTable,
                LateRegistrationLevels = template.LateRegLevels,
                AllowRebuys = template.AllowRebuys,
                RebuyAmount = template.RebuyAmount,
                RebuyLimit = template.RebuyLimit,
                RebuyLimitType = template.RebuyLimitType,
                RebuyMaxLevel = template.RebuyMaxLevel,
                RebuyUntilPlayersLeft = template.RebuyUntilPlayersLeft,
                RebuyStack = template.RebuyStack,
                AllowAddOn = template.AllowAddOn,
                AddOnAmount = template.AddOnAmount,
                AddOnStack = template.AddOnStack,
                AddOnAtLevel = template.AddOnAtLevel,
                AllowBounty = template.AllowBounty,
                BountyAmount = template.BountyAmount,
                BountyType = template.BountyType,
                PayoutStructureJson = template.PayoutStructureJson,
                Status = TournamentStatus.Pending
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            return tournament;
        }

        // ==================== PAYOUT HELPERS ====================

        public PayoutStructure? DeserializePayoutStructure(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<PayoutStructure>(json);
            }
            catch
            {
                return null;
            }
        }

        public string SerializePayoutStructure(PayoutStructure structure)
        {
            return JsonSerializer.Serialize(structure);
        }

        public PayoutStructure CreateDefaultPayoutStructure(int places)
        {
            var structure = new PayoutStructure
            {
                Type = "percentage",
                MinPlayers = 10
            };

            // Structures par défaut selon le nombre de places
            if (places == 1)
            {
                structure.Payouts.Add(new PayoutPosition { Position = 1, Percentage = 100 });
            }
            else if (places == 3)
            {
                structure.Payouts.Add(new PayoutPosition { Position = 1, Percentage = 50 });
                structure.Payouts.Add(new PayoutPosition { Position = 2, Percentage = 30 });
                structure.Payouts.Add(new PayoutPosition { Position = 3, Percentage = 20 });
            }
            else if (places == 5)
            {
                structure.Payouts.Add(new PayoutPosition { Position = 1, Percentage = 40 });
                structure.Payouts.Add(new PayoutPosition { Position = 2, Percentage = 25 });
                structure.Payouts.Add(new PayoutPosition { Position = 3, Percentage = 17 });
                structure.Payouts.Add(new PayoutPosition { Position = 4, Percentage = 10 });
                structure.Payouts.Add(new PayoutPosition { Position = 5, Percentage = 8 });
            }

            return structure;
        }
    }
}