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
using System.Diagnostics;

namespace PokerTournamentDirector.ViewModels
{
    // Classe wrapper pour ajouter IsSelected aux joueurs
    public partial class SelectablePlayer : ObservableObject
    {
        public Player Player { get; set; }

        [ObservableProperty]
        private bool isSelected;

        public int Id => Player.Id;
        public string Name => Player.Name;
        public string Nickname => Player.Nickname;

        public SelectablePlayer(Player player)
        {
            Player = player;
        }
    }

    public partial class TournamentSetupViewModel : ObservableObject
    {
        private readonly TournamentService _tournamentService;
        private readonly ChampionshipService _championshipService;
        private readonly TournamentTemplateService _templateService;
        private readonly PlayerService _playerService;
        private readonly BlindStructureService _blindService;
        private readonly TableManagementService _tableManagementService;

        // Tournoi en cours de configuration
        private Tournament? _tournament;
        public int? TournamentId => _tournament?.Id;

        // Étape actuelle - 4 étapes
        [ObservableProperty]
        private int _currentStep = 1;

        [ObservableProperty]
        private string _stepTitle = "Configuration tournoi";

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

        [ObservableProperty]
        private int _rebuyUntilPlayersLeft = 1;

        [ObservableProperty]
        private int _rebuyMaxLevel = 0;

        // === ÉTAPE 2 : Inscription joueurs avec sélection multiple ===
        [ObservableProperty]
        private ObservableCollection<SelectablePlayer> _availablePlayers = new();

        [ObservableProperty]
        private ObservableCollection<SelectablePlayer> _registeredPlayers = new();

        [ObservableProperty]
        private string _playerSearchText = string.Empty;

        // Sélection multiple
        [ObservableProperty]
        private bool _selectAllAvailable;

        [ObservableProperty]
        private bool _selectAllRegistered;

        [ObservableProperty]
        private int _selectedAvailableCount;

        [ObservableProperty]
        private int _selectedRegisteredCount;

        public bool HasAvailableSelection => SelectedAvailableCount > 0;
        public bool HasRegisteredSelection => SelectedRegisteredCount > 0;

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

        // === Manche de championnat ===
        [ObservableProperty]
        private bool _isChampionshipMatch = false;

        [ObservableProperty]
        private ObservableCollection<Championship> _availableChampionships = new();

        [ObservableProperty]
        private Championship? _selectedChampionship;

        [ObservableProperty]
        private bool _isFinalMatch = false;

        [ObservableProperty]
        private bool _isMainEvent = false;

        // Événement pour signaler que le tournoi est prêt à démarrer
        public event EventHandler<int>? TournamentReadyToStart;

        public TournamentSetupViewModel(
            TournamentService tournamentService,
            TournamentTemplateService templateService,
            PlayerService playerService,
            BlindStructureService blindService,
            TableManagementService tableManagementService,
            ChampionshipService championshipService)
        {
            _tournamentService = tournamentService;
            _templateService = templateService;
            _playerService = playerService;
            _blindService = blindService;
            _tableManagementService = tableManagementService;
            _championshipService = championshipService;
        }

        public async Task InitializeAsync()
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            AvailableTemplates = new ObservableCollection<TournamentTemplate>(templates);

            var blindStructures = await _blindService.GetAllStructuresAsync();
            AvailableBlindStructures = new ObservableCollection<BlindStructure>(blindStructures);

            if (AvailableBlindStructures.Any())
            {
                SelectedBlindStructure = AvailableBlindStructures.First();
            }

            await LoadPlayersAsync();
            await LoadChampionshipsAsync();
        }

        private async Task LoadPlayersAsync()
        {
            var players = await _playerService.GetAllPlayersAsync(activeOnly: true);

            AvailablePlayers.Clear();
            foreach (var player in players.OrderBy(p => p.Name))
            {
                if (!RegisteredPlayers.Any(rp => rp.Id == player.Id))
                {
                    var selectablePlayer = new SelectablePlayer(player);
                    selectablePlayer.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectablePlayer.IsSelected))
                        {
                            UpdateSelectionCounts();
                        }
                    };
                    AvailablePlayers.Add(selectablePlayer);
                }
            }
            UpdateSelectionCounts();
        }

        partial void OnSelectedTemplateChanged(TournamentTemplate? value)
        {
            if (value != null)
            {
                TournamentName = $"{value.Name} - {DateTime.Now:dd/MM/yyyy}";
                BuyIn = value.BuyIn;
                StartingStack = value.StartingStack;
                MaxPlayers = value.MaxPlayers;
                SeatsPerTable = value.SeatsPerTable;
                LateRegLevels = value.LateRegLevels;
                AllowRebuys = value.AllowRebuys;
                RebuyAmount = value.RebuyAmount ?? value.BuyIn;
                MaxRebuysPerPlayer = value.MaxRebuysPerPlayer;
                RebuyMaxLevel = value.RebuyMaxLevel ?? 0;


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
                    var selectablePlayer = new SelectablePlayer(player);
                    selectablePlayer.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectablePlayer.IsSelected))
                        {
                            UpdateSelectionCounts();
                        }
                    };
                    AvailablePlayers.Add(selectablePlayer);
                }
            }
            UpdateSelectionCounts();
        }

        // === MÉTHODES SÉLECTION MULTIPLE ===

        partial void OnSelectAllAvailableChanged(bool value)
        {
            foreach (var player in AvailablePlayers)
            {
                player.IsSelected = value;
            }
            UpdateSelectionCounts();
        }

        partial void OnSelectAllRegisteredChanged(bool value)
        {
            foreach (var player in RegisteredPlayers)
            {
                player.IsSelected = value;
            }
            UpdateSelectionCounts();
        }

        private void UpdateSelectionCounts()
        {
            SelectedAvailableCount = AvailablePlayers.Count(p => p.IsSelected);
            SelectedRegisteredCount = RegisteredPlayers.Count(p => p.IsSelected);

            OnPropertyChanged(nameof(HasAvailableSelection));
            OnPropertyChanged(nameof(HasRegisteredSelection));
        }

        [RelayCommand]
        private void ClearAvailableSelection()
        {
            SelectAllAvailable = false;
            foreach (var player in AvailablePlayers)
            {
                player.IsSelected = false;
            }
            UpdateSelectionCounts();
        }

        [RelayCommand]
        private void ClearRegisteredSelection()
        {
            SelectAllRegistered = false;
            foreach (var player in RegisteredPlayers)
            {
                player.IsSelected = false;
            }
            UpdateSelectionCounts();
        }

        [RelayCommand]
        private async Task RegisterSelectedPlayersAsync()
        {
            if (_tournament == null) return;

            var playersToRegister = AvailablePlayers.Where(p => p.IsSelected).ToList();
            if (!playersToRegister.Any()) return;

            int successCount = 0;
            int errorCount = 0;

            foreach (var selectablePlayer in playersToRegister)
            {
                try
                {
                    await _tournamentService.RegisterPlayerAsync(_tournament.Id, selectablePlayer.Id);

                    // Réabonner au PropertyChanged pour la nouvelle liste
                    selectablePlayer.IsSelected = false;
                    selectablePlayer.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectablePlayer.IsSelected))
                        {
                            UpdateSelectionCounts();
                        }
                    };

                    RegisteredPlayers.Add(selectablePlayer);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Debug.WriteLine($"Erreur inscription {selectablePlayer.Name}: {ex.Message}");
                }
            }

            // Retirer les joueurs inscrits avec succès
            foreach (var player in playersToRegister)
            {
                AvailablePlayers.Remove(player);
            }

            UpdateSelectionCounts();
            TotalPlayers = RegisteredPlayers.Count;
            UpdatePrizePool();

            // Message de confirmation
            if (errorCount > 0)
            {
                CustomMessageBox.ShowWarning(
                    $"{successCount} joueur(s) inscrit(s)\n{errorCount} erreur(s)",
                    "Inscription partielle");
            }
            else if (successCount > 0)
            {
                CustomMessageBox.ShowSuccess(
                    $"✅ {successCount} joueur(s) inscrit(s) avec succès!",
                    "Succès");
            }
        }

        [RelayCommand]
        private async Task UnregisterSelectedPlayersAsync()
        {
            if (_tournament == null) return;

            var playersToUnregister = RegisteredPlayers.Where(p => p.IsSelected).ToList();
            if (!playersToUnregister.Any()) return;

            int successCount = 0;
            int errorCount = 0;

            foreach (var selectablePlayer in playersToUnregister)
            {
                try
                {
                    await _tournamentService.UnregisterPlayerAsync(_tournament.Id, selectablePlayer.Id);

                    // Réabonner au PropertyChanged pour la nouvelle liste
                    selectablePlayer.IsSelected = false;
                    selectablePlayer.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectablePlayer.IsSelected))
                        {
                            UpdateSelectionCounts();
                        }
                    };

                    AvailablePlayers.Add(selectablePlayer);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Debug.WriteLine($"Erreur désinscription {selectablePlayer.Name}: {ex.Message}");
                }
            }

            // Retirer les joueurs désinscrits avec succès
            foreach (var player in playersToUnregister)
            {
                RegisteredPlayers.Remove(player);
            }

            // Re-trier la liste des disponibles
            var sorted = AvailablePlayers.OrderBy(p => p.Name).ToList();
            AvailablePlayers.Clear();
            foreach (var p in sorted)
            {
                p.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SelectablePlayer.IsSelected))
                    {
                        UpdateSelectionCounts();
                    }
                };
                AvailablePlayers.Add(p);
            }

            UpdateSelectionCounts();
            TotalPlayers = RegisteredPlayers.Count;
            UpdatePrizePool();

            if (errorCount > 0)
            {
                CustomMessageBox.ShowWarning(
                    $"{successCount} joueur(s) désinscrit(s)\n{errorCount} erreur(s)",
                    "Désinscription partielle");
            }
            else if (successCount > 0)
            {
                CustomMessageBox.ShowSuccess(
                    $"✅ {successCount} joueur(s) désinscrit(s) avec succès!",
                    "Succès");
            }
        }

        [RelayCommand]
        private async Task RegisterAllPlayersAsync()
        {
            if (_tournament == null) return;

            var playersToRegister = AvailablePlayers.ToList();
            int successCount = 0;

            foreach (var selectablePlayer in playersToRegister)
            {
                try
                {
                    await _tournamentService.RegisterPlayerAsync(_tournament.Id, selectablePlayer.Id);

                    selectablePlayer.IsSelected = false;
                    selectablePlayer.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectablePlayer.IsSelected))
                        {
                            UpdateSelectionCounts();
                        }
                    };

                    RegisteredPlayers.Add(selectablePlayer);
                    successCount++;
                }
                catch { /* Ignorer les erreurs individuelles */ }
            }

            AvailablePlayers.Clear();
            TotalPlayers = RegisteredPlayers.Count;
            UpdatePrizePool();

            CustomMessageBox.ShowSuccess(
                $"✅ {successCount} joueur(s) inscrit(s)!",
                "Inscription complète");
        }

        // === NAVIGATION ÉTAPES ===

        [RelayCommand]
        private async Task NextStepAsync()
        {
            if (CurrentStep == 1)
            {
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

                CurrentStep = 3;
                StepTitle = "Gestion des tables";
                await CreateTablesAsync();
            }
            else if (CurrentStep == 3)
            {
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
                    1 => "Tournoi",
                    2 => "Joueurs",
                    3 => "Tables",
                    _ => "Lancement"
                };
            }
        }

        private async Task<bool> ValidateAndCreateTournamentAsync()
        {
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

            if (IsChampionshipMatch && SelectedChampionship == null)
            {
                CustomMessageBox.ShowWarning(
                    "Veuillez sélectionner un championnat ou décocher l'option.",
                    "Championnat requis");
                return false;
            }

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
                Status = TournamentStatus.Registration,
                RebuyMaxLevel = RebuyMaxLevel
            };

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

        // === MÉTHODES ÉTAPE 3 : GESTION DES TABLES ===

        private async Task CreateTablesAsync()
        {
            if (_tournament == null) return;

            try
            {
                var tables = await _tableManagementService.CreateTablesAsync(_tournament.Id);
                TableCount = tables.Count;
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

        public async Task LoadChampionshipsAsync()
        {
            var championships = await _championshipService.GetAllChampionshipsAsync(includeArchived: false);

            var active = championships.Where(c =>
                c.Status == ChampionshipStatus.Active ||
                c.Status == ChampionshipStatus.Upcoming)
                .ToList();

            AvailableChampionships.Clear();
            foreach (var c in active)
            {
                AvailableChampionships.Add(c);
            }
        }

        [RelayCommand]
        private async Task StartTournamentAsync()
        {
            if (_tournament == null || TotalPlayers < 2) return;

            try
            {
                if (IsChampionshipMatch && SelectedChampionship == null)
                {
                    CustomMessageBox.ShowWarning(
                        "Veuillez sélectionner un championnat.",
                        "Championnat requis");
                    return;
                }

                _tournament.Status = TournamentStatus.Running;
                _tournament.StartTime = DateTime.Now;
                await _tournamentService.UpdateTournamentAsync(_tournament);

                if (IsChampionshipMatch && SelectedChampionship != null)
                {
                    await _championshipService.AddMatchAsync(
                        championshipId: SelectedChampionship.Id,
                        tournamentId: _tournament.Id,
                        isFinal: IsFinalMatch,
                        isMainEvent: IsMainEvent);

                    CustomMessageBox.ShowSuccess(
                        $"Tournoi ajouté au championnat '{SelectedChampionship.Name}' !",
                        "Championnat");
                }

                TournamentReadyToStart?.Invoke(this, _tournament.Id);
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError(
                    $"Erreur lors du démarrage : {ex.Message}",
                    "Erreur");
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