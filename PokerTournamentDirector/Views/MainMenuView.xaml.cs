using Microsoft.Extensions.DependencyInjection;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.ViewModels;
using System.Windows;
using System.Linq;

namespace PokerTournamentDirector.Views
{
    public partial class MainMenuView : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainMenuView(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
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
            var tableManagementService = _serviceProvider.GetRequiredService<TableManagementService>(); // ← AJOUTÉ

            var viewModel = new TournamentSetupViewModel(
                tournamentService,
                templateService,
                playerService,
                blindService,
                tableManagementService); // ← AJOUTÉ

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

            // Si un seul tournoi, le lancer directement
            if (activeTournaments.Count == 1)
            {
                await LaunchTournamentTimerAsync(activeTournaments.First().Id);
                return;
            }

            // Sinon, afficher une liste de sélection
            var selectWindow = new TournamentSelectDialog(activeTournaments);
            if (selectWindow.ShowDialog() == true && selectWindow.SelectedTournamentId.HasValue)
            {
                await LaunchTournamentTimerAsync(selectWindow.SelectedTournamentId.Value);
            }
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
            var playerService = _serviceProvider.GetRequiredService<PlayerService>();
            var viewModel = new PlayerManagementViewModel(playerService);

            var playerWindow = new PlayerManagementView(viewModel);
            playerWindow.ShowDialog();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsService = _serviceProvider.GetRequiredService<SettingsService>();
            var viewModel = new SettingsViewModel(settingsService);

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
    }

    /// <summary>
    /// Dialogue simple pour sélectionner un tournoi à reprendre
    /// </summary>
    public class TournamentSelectDialog : Window
    {
        public int? SelectedTournamentId { get; private set; }
        private System.Windows.Controls.ListBox _listBox;

        public TournamentSelectDialog(System.Collections.Generic.List<Models.Tournament> tournaments)
        {
            Title = "Sélectionner un tournoi";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e"));

            var mainPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };

            mainPanel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Choisissez un tournoi à reprendre",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            });

            _listBox = new System.Windows.Controls.ListBox
            {
                Height = 250,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f3460")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };

            foreach (var tournament in tournaments)
            {
                var statusText = tournament.Status switch
                {
                    Models.TournamentStatus.Running => "▶️ En cours",
                    Models.TournamentStatus.Paused => "⏸️ En pause",
                    Models.TournamentStatus.Registration => "📝 Inscriptions",
                    _ => tournament.Status.ToString()
                };

                _listBox.Items.Add(new System.Windows.Controls.ListBoxItem
                {
                    Content = $"{tournament.Name} - {tournament.Date:dd/MM/yyyy} ({statusText})",
                    Tag = tournament.Id,
                    Padding = new Thickness(10, 8, 10, 8),
                    Foreground = System.Windows.Media.Brushes.White
                });
            }

            mainPanel.Children.Add(_listBox);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var btnSelect = new System.Windows.Controls.Button
            {
                Content = "Reprendre",
                Width = 120,
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00ff88")),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 0, 5, 0)
            };
            btnSelect.Click += (s, e) =>
            {
                if (_listBox.SelectedItem is System.Windows.Controls.ListBoxItem item && item.Tag is int id)
                {
                    SelectedTournamentId = id;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    CustomMessageBox.ShowInformation("Veuillez sélectionner un tournoi.", "Info");
                }
            };
            buttonPanel.Children.Add(btnSelect);

            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Annuler",
                Width = 120,
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#e94560")),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 0, 5, 0)
            };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(btnCancel);

            mainPanel.Children.Add(buttonPanel);
            Content = mainPanel;
        }
    }
}