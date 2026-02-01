using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.ViewModels;
using System.Linq;
using System.Windows;

namespace PokerTournamentDirector.Views
{
    public partial class MainMenuView : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private AudioService audioService;

        public MainMenuView(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            Loaded += async (s, e) =>
            {
                await LoadCurrentSeasonInfoAsync();
                await CheckBirthdaysAsync();
            };
        }

        private async Task CheckBirthdaysAsync()
        {
            try
            {
                var context = _serviceProvider.GetRequiredService<PokerDbContext>();
                var today = DateTime.Now.Date;
                var sixDaysAgo = today.AddDays(-6);

                // Récupérer tous les joueurs avec date de naissance
                var players = await context.Players
                    .Where(p => p.BirthDate.HasValue && p.IsActive)
                    .ToListAsync();

                var birthdayPlayers = players
                    .Where(p =>
                    {
                        var birthDate = p.BirthDate!.Value;
                        var thisYearBirthday = new DateTime(today.Year, birthDate.Month, birthDate.Day);

                        // Vérifier si l'anniversaire est entre aujourd'hui et les 6 derniers jours
                        return thisYearBirthday >= sixDaysAgo && thisYearBirthday <= today;
                    })
                    .Select(p => p.Name)
                    .ToList();

                if (birthdayPlayers.Any())
                {
                    var names = string.Join("& ", birthdayPlayers);
                    txtSubtitle.Text = $"🎂 Joyeux anniversaire {names}🎈";
                }
                else
                {
                    txtSubtitle.Text = "Los Reneg'As ¡Hasta la victoria siempre!";
                }
            }
            catch
            {
                // En cas d'erreur, garder le texte par défaut
                txtSubtitle.Text = "Los Reneg'As ¡Hasta la victoria siempre!";
            }
        }

        private async void NewTournament_Click(object sender, RoutedEventArgs e)
        {
            // Vérifier qu'il y a au moins une structure de blinds
            var blindService = _serviceProvider.GetRequiredService<BlindStructureService>();
            var blindStructures = await blindService.GetAllStructuresAsync();

            if (!blindStructures.Any())
            {
                CustomMessageBox.ShowWarning("Vous devez d'abord créer une structure de blinds avant de pouvoir créer un tournoi.", "Structure de blinds requise");

                // Ouvrir directement l'éditeur de blinds
                BlindStructures_Click(sender, e);
                return;
            }

            // Vérifier qu'il y a des joueurs
            var playerService = _serviceProvider.GetRequiredService<PlayerService>();
            var players = await playerService.GetAllPlayersAsync(activeOnly: true);

            if (!players.Any())
            {
                var result = CustomMessageBox.ShowConfirmation(
                "Aucun joueur n'est enregistré.\n\nVoulez-vous ajouter des joueurs maintenant ?",
                "Joueurs requis");

                if (result == MessageBoxResult.Yes)
                {
                    ManagePlayers_Click(sender, e);
                }
                return;
            }

            // Ouvrir l'écran de configuration du tournoi
            var tournamentService = _serviceProvider.GetRequiredService<TournamentService>();
            var templateService = _serviceProvider.GetRequiredService<TournamentTemplateService>();
            var tableManagementService = _serviceProvider.GetRequiredService<TableManagementService>();
            var championshipService = _serviceProvider.GetRequiredService<ChampionshipService>();

            var viewModel = new TournamentSetupViewModel(
                tournamentService,
                templateService,
                playerService,
                blindService,
                tableManagementService,
                championshipService);

            var setupWindow = new TournamentSetupView(viewModel, _serviceProvider);

            if (setupWindow.ShowDialog() == true && setupWindow.TournamentStarted && setupWindow.CreatedTournamentId.HasValue)
            {
                // Lancer le timer du tournoi
                await LaunchTournamentTimerAsync(setupWindow.CreatedTournamentId.Value);
            }
        }


        private async void ResumeTournament_Click(object sender, RoutedEventArgs e)
        {
            var tournamentService = _serviceProvider.GetRequiredService<TournamentService>();
            var tournaments = await tournamentService.GetAllTournamentsAsync();

            // Filtrer les tournois en cours ou en pause
            var activeTournaments = tournaments
                .Where(t => t.Status == Models.TournamentStatus.Running ||
                           t.Status == Models.TournamentStatus.Paused ||
                           t.Status == Models.TournamentStatus.Registration)
                .OrderByDescending(t => t.Date)
                .ToList();

            if (!activeTournaments.Any())
            {
                CustomMessageBox.ShowInformation("Aucun tournoi en cours à reprendre.", "Information");
                return;
            }


            await LaunchTournamentTimerAsync(activeTournaments.First().Id);
            return;

        }

        private async Task LaunchTournamentTimerAsync(int tournamentId)
        {
            var viewModel = _serviceProvider.GetRequiredService<TournamentTimerViewModel>();
            await viewModel.LoadTournamentAsync(tournamentId);

            var timerWindow = new TournamentTimerView(viewModel, _serviceProvider);
            timerWindow.Show();
        }

        private void ManagePlayers_Click(object sender, RoutedEventArgs e)
        {
            var settingsService = _serviceProvider.GetRequiredService<SettingsService>();
            var playerService = _serviceProvider.GetRequiredService<PlayerService>();
            var viewModel = new PlayerManagementViewModel(playerService, settingsService);

            var playerWindow = new PlayerManagementView(viewModel);
            playerWindow.ShowDialog();
        }

        private void Championship_Click(object sender, RoutedEventArgs e)
        {
            var championshipService = _serviceProvider.GetRequiredService<ChampionshipService>();
            var viewModel = new ChampionshipManagementViewModel(championshipService);

            var window = new ChampionshipManagementView(viewModel);
            window.Show();
        }


        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsService = _serviceProvider.GetRequiredService<SettingsService>();
            var audioService = _serviceProvider.GetRequiredService<AudioService>();
            var viewModel = new SettingsViewModel(settingsService, audioService);

            var settingsWindow = new SettingsView(viewModel);
            settingsWindow.ShowDialog();
        }

        private void BlindStructures_Click(object sender, RoutedEventArgs e)
        {
            var blindService = _serviceProvider.GetRequiredService<BlindStructureService>();
            var viewModel = new BlindStructureEditorViewModel(blindService);

            var blindWindow = new BlindStructureEditorView(viewModel);
            blindWindow.ShowDialog();
        }

        private void TournamentTemplates_Click(object sender, RoutedEventArgs e)
        {
            var templateService = _serviceProvider.GetRequiredService<TournamentTemplateService>();
            var blindService = _serviceProvider.GetRequiredService<BlindStructureService>();
            var viewModel = new TournamentTemplateViewModel(templateService, blindService);

            var templateWindow = new TournamentTemplateView(viewModel);
            templateWindow.ShowDialog();
        }

        private void Quit_Click(object sender, RoutedEventArgs e)
        {
            var result = CustomMessageBox.ShowConfirmation(
                "Voulez-vous vraiment quitter l'application ?",
                "Confirmation");

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
        // RACCOURCIS
        private async Task LoadCurrentSeasonInfoAsync()
        {
            try
            {
                var context = _serviceProvider.GetRequiredService<PokerDbContext>();
                var now = DateTime.Now;

                var currentChampionship = await context.Championships
                    .Where(c => c.Status == ChampionshipStatus.Active &&
                               c.StartDate <= now &&
                               c.EndDate >= now)
                    .OrderByDescending(c => c.StartDate)
                    .FirstOrDefaultAsync();

                if (currentChampionship != null)
                {
                    txtCurrentSeasonName.Text = $"{currentChampionship.Name}\n{currentChampionship.Season}";
                }
                else
                {
                    txtCurrentSeasonName.Text = "Aucune saison active";
                }
            }
            catch
            {
                txtCurrentSeasonName.Text = "Aucune saison active";
            }
        }

        // AJOUTE cette méthode
        private async void QuickAccessChampionship_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var context = _serviceProvider.GetRequiredService<PokerDbContext>();
                var now = DateTime.Now;

                var currentChampionship = await context.Championships
                    .Where(c => c.Status == ChampionshipStatus.Active &&
                               c.StartDate <= now &&
                               c.EndDate >= now)
                    .OrderByDescending(c => c.StartDate)
                    .FirstOrDefaultAsync();

                if (currentChampionship == null)
                {
                    var result = CustomMessageBox.ShowConfirmation(
                        "Aucune saison active trouvée.\nVoulez-vous créer un nouveau championnat ?",
                        "Aucune saison"
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        Championship_Click(sender, e);
                    }
                    return;
                }

                var championshipService = _serviceProvider.GetRequiredService<ChampionshipService>();
                var dbContext = _serviceProvider.GetRequiredService<PokerDbContext>();

                var dashboardView = new ChampionshipDashboardView(
                    currentChampionship.Id,
                    championshipService,
                    dbContext);

                dashboardView.Show();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur : {ex.Message}");
            }
        }

        // AJOUTE cette méthode
        private async void QuickAccessTournament_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tournamentService = _serviceProvider.GetRequiredService<TournamentService>();
                var templateService = _serviceProvider.GetRequiredService<TournamentTemplateService>();
                var blindService = _serviceProvider.GetRequiredService<BlindStructureService>();
                var championshipService = _serviceProvider.GetRequiredService<ChampionshipService>();
                var playerService = _serviceProvider.GetRequiredService<PlayerService>();
                var context = _serviceProvider.GetRequiredService<PokerDbContext>();

                var viewModel = new QuickTournamentLaunchViewModel(
                    tournamentService,
                    templateService,
                    blindService,
                    championshipService,
                    playerService,
                    context);

                var window = new QuickTournamentLaunchView(viewModel);

                if (window.ShowDialog() == true)
                {
                    var timerViewModel = _serviceProvider.GetRequiredService<TournamentTimerViewModel>();
                    await timerViewModel.LoadTournamentAsync(window.CreatedTournamentId);

                    var timerWindow = new TournamentTimerView(timerViewModel, _serviceProvider);
                    timerWindow.Show();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"Erreur : {ex.Message}", "Erreur");
            }
        }
    }





}