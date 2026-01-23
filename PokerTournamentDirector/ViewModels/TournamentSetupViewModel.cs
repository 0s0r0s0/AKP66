using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PokerTournamentDirector.ViewModels
{
    public partial class TournamentSetupViewModel : ObservableObject
    {
        private readonly TournamentService _tournamentService;
        private readonly TournamentTemplateService _templateService;
        private readonly PlayerService _playerService;
        private readonly BlindStructureService _blindService;
        private readonly TableManagementService _tableManagementService;

        // Tournoi en cours de configuration
        private Tournament? _tournament;
        public int? TournamentId => _tournament?.Id;

        // Étape actuelle - 4 étapes
        [ObservableProperty]
        private int _currentStep = 1; // 1=Config, 2=Joueurs, 3=Tables, 4=Prêt

        [ObservableProperty]
        private string _stepTitle = "Configuration du tournoi";

        // === ÉTAPE 1 : Configuration ===
        [ObservableProperty]
        private ObservableCollection<TournamentTemplate> _availableTemplates = new();

        [ObservableProperty]
        private TournamentTemplate? _selectedTemplate;

        [ObservableProperty]
        private ObservableCollection<BlindStructure> _availableBlindStructures = new();

        [ObservableProperty]
        private BlindStructure? _selectedBlindStructure;

        [ObservableProperty]
        private string _tournamentName = $"Tournoi du {DateTime.Now:dd/MM/yyyy}";

        [ObservableProperty]
        private DateTime _tournamentDate = DateTime.Now;

        [ObservableProperty]
        private decimal _buyIn = 20;

        [ObservableProperty]
        private int _startingStack = 10000;

        [ObservableProperty]
        private int _maxPlayers = 50;

        [ObservableProperty]
        private int _seatsPerTable = 9;

        [ObservableProperty]
        private int _lateRegLevels = 4;

        [ObservableProperty]
        private bool _allowRebuys = false;

        [ObservableProperty]
        private decimal _rebuyAmount = 20;

        [ObservableProperty]
        private int _maxRebuysPerPlayer = 3;

        // === ÉTAPE 2 : Inscription joueurs ===
        [ObservableProperty]
        private ObservableCollection<Player> _availablePlayers = new();

        [ObservableProperty]
        private ObservableCollection<Player> _registeredPlayers = new();

        [ObservableProperty]
        private Player? _selectedAvailablePlayer;

        [ObservableProperty]
        private Player? _selectedRegisteredPlayer;

        [ObservableProperty]
        private string _playerSearchText = string.Empty;

        // === ÉTAPE 3 : Gestion des tables ===
        [ObservableProperty]
        private ObservableCollection<TableLayout> _tableLayouts = new();

        [ObservableProperty]
        private bool _tablesCreated = false;

        [ObservableProperty]
        private int _tableCount = 0;

        [ObservableProperty]
        private bool _allPlayersAssigned = false;

        [ObservableProperty]
        private string _balanceStatus = "";

        // === ÉTAPE 4 : Résumé ===
        [ObservableProperty]
        private int _totalPlayers = 0;

        [ObservableProperty]
        private decimal _totalPrizePool = 0;

        [ObservableProperty]
        private bool _canStartTournament = false;

        // Événement pour signaler que le tournoi est prêt à démarrer
        public event EventHandler<int>? TournamentReadyToStart;

        public TournamentSetupViewModel(
            TournamentService tournamentService,
            TournamentTemplateService templateService,
            PlayerService playerService,
            BlindStructureService blindService,
            TableManagementService tableManagementService)
        {
            _tournamentService = tournamentService;
            _templateService = templateService;
            _playerService = playerService;
            _blindService = blindService;
            _tableManagementService = tableManagementService;
        }

        public async Task InitializeAsync()
        {
            // Charger les templates
            var templates = await _templateService.GetAllTemplatesAsync();
            AvailableTemplates = new ObservableCollection<TournamentTemplate>(templates);

            // Charger les structures de blinds
            var blindStructures = await _blindService.GetAllStructuresAsync();
            AvailableBlindStructures = new ObservableCollection<BlindStructure>(blindStructures);

            if (AvailableBlindStructures.Any())
            {
                SelectedBlindStructure = AvailableBlindStructures.First();
            }

            // Charger tous les joueurs
            await LoadPlayersAsync();
        }

        private async Task LoadPlayersAsync()
        {
            var players = await _playerService.GetAllPlayersAsync(activeOnly: true);

            AvailablePlayers.Clear();
            foreach (var player in players.OrderBy(p => p.Name))
            {
                // Ne pas inclure les joueurs déjà inscrits
                if (!RegisteredPlayers.Any(rp => rp.Id == player.Id))
                {
                    AvailablePlayers.Add(player);
                }
            }
        }

        partial void OnSelectedTemplateChanged(TournamentTemplate? value)
        {
            if (value != null)
            {
                // Appliquer les valeurs du template
                TournamentName = $"{value.Name} - {DateTime.Now:dd/MM/yyyy}";
                BuyIn = value.BuyIn;
                StartingStack = value.StartingStack;
                MaxPlayers = value.MaxPlayers;
                SeatsPerTable = value.SeatsPerTable;
                LateRegLevels = value.LateRegLevels;
                AllowRebuys = value.AllowRebuys;
                RebuyAmount = value.RebuyAmount ?? value.BuyIn;
                MaxRebuysPerPlayer = value.MaxRebuysPerPlayer;

                // Sélectionner la structure de blinds du template
                SelectedBlindStructure = AvailableBlindStructures
                    .FirstOrDefault(bs => bs.Id == value.BlindStructureId);
            }
        }

        partial void OnPlayerSearchTextChanged(string value)
        {
            _ = FilterPlayersAsync(value);
        }

        private async Task FilterPlayersAsync(string searchText)
        {
            var players = string.IsNullOrWhiteSpace(searchText)
                ? await _playerService.GetAllPlayersAsync(activeOnly: true)
                : await _playerService.SearchPlayersAsync(searchText);

            AvailablePlayers.Clear();
            foreach (var player in players.OrderBy(p => p.Name))
            {
                if (!RegisteredPlayers.Any(rp => rp.Id == player.Id))
                {
                    AvailablePlayers.Add(player);
                }
            }
        }

        [RelayCommand]
        private async Task NextStepAsync()
        {
            if (CurrentStep == 1)
            {
                // Valider la configuration et créer le tournoi
                if (!await ValidateAndCreateTournamentAsync())
                    return;

                CurrentStep = 2;
                StepTitle = "Inscription des joueurs";
            }
            else if (CurrentStep == 2)
            {
                if (RegisteredPlayers.Count < 2)
                {
                    CustomMessageBox.ShowWarning(
                        "Il faut au moins 2 joueurs pour démarrer un tournoi.",
                        "Attention");
                    return;
                }

                // Passer à l'étape de gestion des tables
                CurrentStep = 3;
                StepTitle = "Gestion des tables";
                await CreateTablesAsync();
            }
            else if (CurrentStep == 3)
            {
                // Valider que tous les joueurs sont placés
                if (!await ValidateTableAssignmentsAsync())
                {
                    CustomMessageBox.ShowWarning(
                    "Tous les joueurs doivent être placés à une table avant de continuer.",
                    "Attention");
                    return;
                }

                CurrentStep = 4;
                StepTitle = "Prêt à démarrer";
                UpdateSummary();
            }
        }

        [RelayCommand]
        private void PreviousStep()
        {
            if (CurrentStep > 1)
            {
                CurrentStep--;
                StepTitle = CurrentStep switch
                {
                    1 => "Configuration du tournoi",
                    2 => "Inscription des joueurs",
                    3 => "Gestion des tables",
                    _ => "Prêt à démarrer"
                };
            }
        }

        private async Task<bool> ValidateAndCreateTournamentAsync()
        {
            // Validation
            if (string.IsNullOrWhiteSpace(TournamentName))
            {
                CustomMessageBox.ShowWarning("Veuillez entrer un nom pour le tournoi.", "Erreur");
                return false;
            }

            if (SelectedBlindStructure == null)
            {
                CustomMessageBox.ShowWarning("Veuillez sélectionner une structure de blinds.", "Erreur");
                return false;
            }

            // Créer le tournoi
            _tournament = new Tournament
            {
                Name = TournamentName,
                Date = TournamentDate,
                TemplateId = SelectedTemplate?.Id,
                BuyIn = BuyIn,
                StartingStack = StartingStack,
                MaxPlayers = MaxPlayers,
                SeatsPerTable = SeatsPerTable,
                LateRegistrationLevels = LateRegLevels,
                BlindStructureId = SelectedBlindStructure.Id,
                AllowRebuys = AllowRebuys,
                RebuyAmount = AllowRebuys ? RebuyAmount : null,
                MaxRebuysPerPlayer = MaxRebuysPerPlayer,
                Status = TournamentStatus.Registration
            };

            // Définir le type selon les options
            if (AllowRebuys)
            {
                _tournament.Type = MaxRebuysPerPlayer == 0
                    ? TournamentType.RebuyUnlimited
                    : TournamentType.RebuyLimited;
            }
            else
            {
                _tournament.Type = TournamentType.Freezeout;
            }

            _tournament = await _tournamentService.CreateTournamentAsync(_tournament);
            return true;
        }

        [RelayCommand]
        private async Task RegisterPlayerAsync()
        {
            if (SelectedAvailablePlayer == null || _tournament == null) return;

            try
            {
                await _tournamentService.RegisterPlayerAsync(_tournament.Id, SelectedAvailablePlayer.Id);

                RegisteredPlayers.Add(SelectedAvailablePlayer);
                AvailablePlayers.Remove(SelectedAvailablePlayer);
                SelectedAvailablePlayer = null;

                TotalPlayers = RegisteredPlayers.Count;
                UpdatePrizePool();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private async Task UnregisterPlayerAsync()
        {
            if (SelectedRegisteredPlayer == null || _tournament == null) return;

            try
            {
                await _tournamentService.UnregisterPlayerAsync(_tournament.Id, SelectedRegisteredPlayer.Id);

                AvailablePlayers.Add(SelectedRegisteredPlayer);
                RegisteredPlayers.Remove(SelectedRegisteredPlayer);

                // Re-trier la liste des disponibles
                var sorted = AvailablePlayers.OrderBy(p => p.Name).ToList();
                AvailablePlayers.Clear();
                foreach (var p in sorted) AvailablePlayers.Add(p);

                SelectedRegisteredPlayer = null;

                TotalPlayers = RegisteredPlayers.Count;
                UpdatePrizePool();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private async Task RegisterAllPlayersAsync()
        {
            if (_tournament == null) return;

            var playersToRegister = AvailablePlayers.ToList();
            foreach (var player in playersToRegister)
            {
                try
                {
                    await _tournamentService.RegisterPlayerAsync(_tournament.Id, player.Id);
                    RegisteredPlayers.Add(player);
                }
                catch { /* Ignorer les erreurs individuelles */ }
            }

            AvailablePlayers.Clear();
            TotalPlayers = RegisteredPlayers.Count;
            UpdatePrizePool();
        }

        // === MÉTHODES POUR L'ÉTAPE 3 : GESTION DES TABLES ===

        private async Task CreateTablesAsync()
        {
            if (_tournament == null) return;

            try
            {
                // Créer les tables
                var tables = await _tableManagementService.CreateTablesAsync(_tournament.Id);
                TableCount = tables.Count;

                // Charger les tables créées
                await LoadTableLayoutsAsync();

                TablesCreated = true;
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError(
    $"Erreur lors de la création des tables: {ex.Message}",
    "Erreur");
            }
        }

        private async Task LoadTableLayoutsAsync()
        {
            if (_tournament == null) return;

            var layouts = await _tableManagementService.GetTableLayoutAsync(_tournament.Id);
            TableLayouts = new ObservableCollection<TableLayout>(layouts);

            // Vérifier l'équilibre
            if (layouts.Any())
            {
                var playerCounts = layouts.Select(t => t.PlayerCount).ToList();
                int minPlayers = playerCounts.Min();
                int maxPlayers = playerCounts.Max();
                int diff = maxPlayers - minPlayers;

                if (diff == 0)
                {
                    BalanceStatus = "✅ Parfaitement équilibré";
                }
                else if (diff == 1)
                {
                    BalanceStatus = "✅ Bien équilibré";
                }
                else
                {
                    BalanceStatus = $"⚠️ Déséquilibré (écart de {diff} joueurs)";
                }
            }
            else
            {
                BalanceStatus = "Aucune table";
            }

            // Vérifier si tous les joueurs sont placés
            await CheckAllPlayersAssignedAsync();
        }

        [RelayCommand]
        private async Task AutoAssignPlayersAsync()
        {
            if (_tournament == null) return;

            try
            {
                await _tableManagementService.AutoAssignPlayersAsync(_tournament.Id);
                await LoadTableLayoutsAsync();

                CustomMessageBox.ShowSuccess("Joueurs placés automatiquement !", "Succès");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private async Task BalanceTablesAsync()
        {
            if (_tournament == null) return;

            try
            {
                var result = await _tableManagementService.AutoBalanceAfterChangeAsync(_tournament.Id);
                await LoadTableLayoutsAsync();

                if (result.Movements.Any())
                {
                    var movementDetails = string.Join("\n", result.Movements
                        .Select(m => $"• {m.PlayerName}: Table {m.FromTable} → Table {m.ToTable}"));

                    CustomMessageBox.ShowInformation(
                     $"{result.Message}\n\n{movementDetails}",
                     "Équilibrage effectué");
                }
                else
                {
                    CustomMessageBox.ShowInformation(result.Message, "Information");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
            }
        }


        [RelayCommand]
        private async Task ToggleLockPlayerAsync(int tournamentPlayerId)
        {
            try
            {
                var isLocked = await _tableManagementService.ToggleLockPlayerAsync(tournamentPlayerId);
                await LoadTableLayoutsAsync();

                CustomMessageBox.ShowInformation(
                    isLocked ? "🔒 Joueur verrouillé" : "🔓 Joueur déverrouillé",
                    "Verrouillage");
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private async Task MovePlayerAsync((int playerId, int targetTableId, int targetSeat) param)
        {
            if (_tournament == null) return;

            try
            {
                var success = await _tableManagementService.MovePlayerAsync(
                    param.playerId,
                    param.targetTableId,
                    param.targetSeat);

                if (success)
                {
                    await LoadTableLayoutsAsync();
                    CustomMessageBox.ShowSuccess("Joueur déplacé avec succès !");
                }
                else
                {
                    CustomMessageBox.ShowWarning("Impossible de déplacer le joueur (siège occupé).", "Déplacement impossible");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur: {ex.Message}", "Erreur");
            }
        }

        [RelayCommand]
        private async Task RefreshTablesAsync()
        {
            await LoadTableLayoutsAsync();
        }

        private async Task<bool> ValidateTableAssignmentsAsync()
        {
            if (_tournament == null) return false;

            // Recharger le tournoi avec tous les joueurs
            var tournament = await _tournamentService.GetTournamentAsync(_tournament.Id);
            if (tournament == null) return false;

            var unassignedPlayers = tournament.Players
                .Where(p => !p.IsEliminated && (!p.TableId.HasValue || !p.SeatNumber.HasValue))
                .ToList();

            return unassignedPlayers.Count == 0;
        }

        private async Task CheckAllPlayersAssignedAsync()
        {
            AllPlayersAssigned = await ValidateTableAssignmentsAsync();
        }

        // === FIN MÉTHODES ÉTAPE 3 ===

        private void UpdatePrizePool()
        {
            TotalPrizePool = TotalPlayers * BuyIn;
        }

        private void UpdateSummary()
        {
            TotalPlayers = RegisteredPlayers.Count;
            UpdatePrizePool();
            CanStartTournament = TotalPlayers >= 2 && AllPlayersAssigned;
        }

        [RelayCommand]
        private async Task StartTournamentAsync()
        {
            if (_tournament == null || TotalPlayers < 2) return;

            // Les tables sont déjà créées à l'étape 3
            // Mettre à jour le statut
            _tournament.Status = TournamentStatus.Running;
            _tournament.StartTime = DateTime.Now;
            await _tournamentService.UpdateTournamentAsync(_tournament);

            // Signaler que le tournoi est prêt
            TournamentReadyToStart?.Invoke(this, _tournament.Id);
        }

        [RelayCommand]
        private async Task CancelTournamentAsync()
        {
            if (_tournament != null)
            {
                var result = CustomMessageBox.ShowQuestion(
                    "Voulez-vous vraiment annuler ce tournoi ?\nToutes les inscriptions seront perdues.",
                    "Confirmation");

                if (result == MessageBoxResult.Yes)
                {
                    await _tournamentService.DeleteTournamentAsync(_tournament.Id);
                    _tournament = null;
                }
            }
        }
    }
}