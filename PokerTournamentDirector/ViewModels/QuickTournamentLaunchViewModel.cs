using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PokerTournamentDirector.ViewModels
{
    public partial class QuickTournamentLaunchViewModel : ObservableObject
    {
        private readonly TournamentService _tournamentService;
        private readonly TournamentTemplateService _templateService;
        private readonly BlindStructureService _blindService;
        private readonly ChampionshipService _championshipService;
        private readonly PlayerService _playerService;
        private readonly PokerDbContext _context;

        [ObservableProperty] private ObservableCollection<SelectablePlayer> _availablePlayers = new();
        [ObservableProperty] private ObservableCollection<SelectablePlayer> _registeredPlayers = new();
        [ObservableProperty] private Championship? _selectedChampionship;
        [ObservableProperty] private string _tournamentName = "";

        private TournamentTemplate? _favoriteTemplate;
        private BlindStructure? _favoriteBlindStructure;

        public QuickTournamentLaunchViewModel(
            TournamentService tournamentService,
            TournamentTemplateService templateService,
            BlindStructureService blindService,
            ChampionshipService championshipService,
            PlayerService playerService,
            PokerDbContext context)
        {
            _tournamentService = tournamentService;
            _templateService = templateService;
            _blindService = blindService;
            _championshipService = championshipService;
            _playerService = playerService;
            _context = context;
        }

        public async Task InitializeAsync()
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            _favoriteTemplate = templates.FirstOrDefault(t => t.IsFavorite);

            var blindStructures = await _blindService.GetAllStructuresAsync();
            _favoriteBlindStructure = blindStructures.FirstOrDefault(b => b.IsFavorite);

            if (_favoriteTemplate == null || _favoriteBlindStructure == null)
            {
                throw new InvalidOperationException(
                    "Aucun template ou structure de blinds favoris défini.\n\n" +
                    "Veuillez définir un favori dans :\n" +
                    "• Modèles de tournois (⭐)\n" +
                    "• Structures de blinds (⭐)");
            }

            var now = DateTime.Now;
            SelectedChampionship = await _context.Championships
                .Where(c => c.Status == ChampionshipStatus.Active &&
                           c.StartDate <= now &&
                           c.EndDate >= now)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync();

            if (SelectedChampionship == null)
            {
                throw new InvalidOperationException("Aucun championnat actif trouvé.");
            }

            TournamentName = $"Manche {DateTime.Now:dd/MM/yyyy HH:mm}";
            await LoadPlayersAsync();
        }

        private async Task LoadPlayersAsync()
        {
            var allPlayers = await _playerService.GetAllPlayersAsync();

            AvailablePlayers.Clear();
            foreach (var player in allPlayers.OrderBy(p => p.Name))
            {
                AvailablePlayers.Add(new SelectablePlayer(player));
            }
        }

        [RelayCommand]
        private void RegisterSelectedPlayers()
        {
            var playersToRegister = AvailablePlayers.Where(p => p.IsSelected).ToList();
            if (!playersToRegister.Any()) return;

            foreach (var player in playersToRegister)
            {
                player.IsSelected = false;
                RegisteredPlayers.Add(player);
            }

            foreach (var player in playersToRegister)
            {
                AvailablePlayers.Remove(player);
            }
        }

        [RelayCommand]
        private void UnregisterSelectedPlayers()
        {
            var playersToUnregister = RegisteredPlayers.Where(p => p.IsSelected).ToList();
            if (!playersToUnregister.Any()) return;

            foreach (var player in playersToUnregister)
            {
                player.IsSelected = false;
                AvailablePlayers.Add(player);
            }

            foreach (var player in playersToUnregister)
            {
                RegisteredPlayers.Remove(player);
            }

            var sorted = AvailablePlayers.OrderBy(p => p.Name).ToList();
            AvailablePlayers.Clear();
            foreach (var p in sorted)
                AvailablePlayers.Add(p);
        }

        public async Task<int> LaunchTournamentAsync()
        {
            if (RegisteredPlayers.Count < 2)
            {
                throw new InvalidOperationException("Il faut au moins 2 joueurs.");
            }

            // Convertir TournamentTemplateType vers TournamentType
            TournamentType tournamentType;
            switch (_favoriteTemplate!.Type)
            {
                case TournamentTemplateType.Cash:
                    tournamentType = TournamentType.Freezeout;
                    break;
                case TournamentTemplateType.Freeroll:
                    tournamentType = TournamentType.Freezeout;
                    break;
                case TournamentTemplateType.Points:
                    tournamentType = TournamentType.Freezeout;
                    break;
                default:
                    tournamentType = TournamentType.Freezeout;
                    break;
            }

            var tournament = new Tournament
            {
                Name = TournamentName,
                Type = tournamentType,
                BuyIn = _favoriteTemplate.BuyIn,
                Currency = _favoriteTemplate.Currency,
                Rake = _favoriteTemplate.Rake,
                RakeType = _favoriteTemplate.RakeType,
                RebuyAmount = _favoriteTemplate.RebuyAmount,
                AddOnAmount = _favoriteTemplate.AddOnAmount,
                AllowRebuys = _favoriteTemplate.AllowRebuys,
                RebuyLimit = _favoriteTemplate.RebuyLimit,
                RebuyLimitType = _favoriteTemplate.RebuyLimitType,
                RebuyMaxLevel = _favoriteTemplate.RebuyMaxLevel,
                RebuyUntilPlayersLeft = _favoriteTemplate.RebuyUntilPlayersLeft,
                RebuyStack = _favoriteTemplate.RebuyStack,
                MaxRebuysPerPlayer = _favoriteTemplate.MaxRebuysPerPlayer,
                RebuyPeriodMonths = _favoriteTemplate.RebuyPeriodMonths,
                AllowAddOn = _favoriteTemplate.AllowAddOn,
                AddOnStack = _favoriteTemplate.AddOnStack,
                AddOnAtLevel = _favoriteTemplate.AddOnAtLevel,
                AllowBounty = _favoriteTemplate.AllowBounty,
                BountyAmount = _favoriteTemplate.BountyAmount,
                BountyType = _favoriteTemplate.BountyType,
                PayoutStructureJson = _favoriteTemplate.PayoutStructureJson,
                StartingStack = _favoriteTemplate.StartingStack,
                MaxPlayers = _favoriteTemplate.MaxPlayers,
                SeatsPerTable = _favoriteTemplate.SeatsPerTable,
                LateRegistrationLevels = 4,
                BlindStructureId = _favoriteBlindStructure!.Id,
                Status = TournamentStatus.Registration,
                TotalPrizePool = 0
            };

            var createdTournament = await _tournamentService.CreateTournamentAsync(tournament);

            // Inscrire joueurs
            foreach (var player in RegisteredPlayers)
            {
                await _tournamentService.RegisterPlayerAsync(createdTournament.Id, player.Id);
            }

            // Ajouter au championnat
            await _championshipService.AddMatchAsync(
                SelectedChampionship!.Id,
                createdTournament.Id);

            // Créer tables et placer joueurs automatiquement
            var tableService = new TableManagementService(_context);
            await tableService.CreateTablesAsync(createdTournament.Id);

            // Récupérer les joueurs inscrits et les distribuer
            var tournamentPlayers = await _context.TournamentPlayers
                .Where(tp => tp.TournamentId == createdTournament.Id && !tp.IsEliminated)
                .ToListAsync();

            // Distribuer aux tables
            var tables = await _context.PokerTables
                .Where(t => t.TournamentId == createdTournament.Id && t.IsActive)
                .ToListAsync();

            int playerIndex = 0;
            foreach (var table in tables)
            {
                for (int seat = 1; seat <= table.MaxSeats && playerIndex < tournamentPlayers.Count; seat++)
                {
                    tournamentPlayers[playerIndex].TableId = table.Id;
                    tournamentPlayers[playerIndex].SeatNumber = seat;
                    playerIndex++;
                }
            }
            await _context.SaveChangesAsync();

            // Démarrer tournoi
            createdTournament.Status = TournamentStatus.Running;
            createdTournament.StartTime = DateTime.Now;
            await _tournamentService.UpdateTournamentAsync(createdTournament);

            return createdTournament.Id;
        }
    }
}