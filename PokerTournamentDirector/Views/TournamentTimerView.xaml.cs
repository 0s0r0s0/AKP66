using Microsoft.Extensions.DependencyInjection;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Helpers;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.ViewModels;
using PokerTournamentDirector.Views.Dialogs;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PokerTournamentDirector.Views
{
    public partial class TournamentTimerView : Window
    {
        private readonly TournamentTimerViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly TableManagementService _tableManagementService;
        private readonly int _tournamentId;
        private readonly VictoryCelebrationManager _celebrationManager;

        public TournamentTimerView(TournamentTimerViewModel viewModel, IServiceProvider? serviceProvider = null)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _serviceProvider = serviceProvider ?? App.Services;
            _tournamentId = viewModel.TournamentId;
            _tableManagementService = _serviceProvider.GetRequiredService<TableManagementService>();

            DataContext = _viewModel;

            // Gestionnaire de célébration
            _celebrationManager = new VictoryCelebrationManager(ConfettiCanvas, ActualWidth, ActualHeight);
            _viewModel.OnVictoryCelebrationNeeded += (s, e) => _ = _celebrationManager.StartCelebrationAsync();

            Unloaded += (s, e) => _celebrationManager.Dispose();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.R: ShortcutsPopup.IsOpen = !ShortcutsPopup.IsOpen; break;
                case Key.Escape: Close(); break;
                case Key.S: TogglePlayPause(); break;
                case Key.Right: _viewModel.NextLevelCommand.Execute(null); break;
                case Key.Left: _viewModel.PreviousLevelCommand.Execute(null); break;
                case Key.E: OpenEliminations(); break;
                case Key.B: _viewModel.ShowBlindStructureCommand.Execute(null); break;
                case Key.M: EditBlinds_Click(this, new RoutedEventArgs()); break;
                case Key.P: if (_viewModel.CanAddLatePlayers) Players_Click(this, new RoutedEventArgs()); break;
                case Key.T: OpenTableView(); break;
                case Key.F11: ToggleFullscreen(); break;
            }
        }

        private void TogglePlayPause()
        {
            if (_viewModel.IsPaused) _viewModel.ResumeTournamentCommand.Execute(null);
            else if (_viewModel.IsRunning) _viewModel.PauseTournamentCommand.Execute(null);
            else _viewModel.StartTournamentCommand.Execute(null);
        }

        private void ToggleFullscreen()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_viewModel.IsTournamentFinished) return;

            if (_viewModel.IsRunning || _viewModel.IsPaused)
            {
                if (CustomMessageBox.ShowConfirmation(
                    "Le tournoi est en cours !\n\nL'état sera sauvegardé.\nVoulez-vous vraiment quitter ?",
                    "Confirmation") == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _viewModel.StopTimer();
        }

        private void Eliminations_Click(object sender, RoutedEventArgs e) => OpenEliminations();

        private void OpenEliminations()
        {
            var eliminationViewModel = new EliminationViewModel(
                _serviceProvider.GetRequiredService<TournamentService>(),
                _serviceProvider.GetRequiredService<PlayerService>(),
                _tableManagementService,
                _serviceProvider.GetRequiredService<TournamentLogService>(),
                _serviceProvider.GetRequiredService<ChampionshipService>(),
                _serviceProvider.GetRequiredService<PokerDbContext>(),
                _tournamentId);

            eliminationViewModel.TournamentFinished += (s, w) => _ = _viewModel.RefreshStatsAsync();

            var window = new EliminationView(eliminationViewModel);
            window.ShowDialog();

            CheckTableBalance();
            _ = _viewModel.RefreshStatsAsync();
        }

        private async void CheckTableBalance()
        {
            var result = await _tableManagementService.AutoBalanceAfterChangeAsync(_tournamentId);

            if (result.Success && result.Movements.Any())
            {
                var message = result.TableBroken
                    ? $"🔔 Table {result.BrokenTableNumber} cassée !\n\n"
                    : "🔔 Équilibrage des tables !\n\n";

                message += "Mouvements :\n" + string.Join("\n",
                    result.Movements.Select(m => $"• {m.PlayerName}: Table {m.FromTable} → Table {m.ToTable} (siège {m.ToSeat})"));

                CustomMessageBox.ShowInformation(message, "Équilibrage");
            }
        }

        private void EditBlinds_Click(object sender, RoutedEventArgs e)
        {
            new EditBlindsAndTimeDialog(_viewModel) { Owner = this }.ShowDialog();
        }

        private async void Players_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.CanAddLatePlayers)
            {
                CustomMessageBox.ShowInformation("La période d'inscription tardive est terminée.", "Info");
                return;
            }

            var dialog = new AddLatePlayerDialog(_serviceProvider, _viewModel.TournamentId) { Owner = this };

            if (dialog.ShowDialog() == true && dialog.SelectedPlayerId.HasValue)
            {
                await AddLatePlayer(dialog.SelectedPlayerId.Value);
            }
        }

        private async Task AddLatePlayer(int playerId)
        {
            if (!await _viewModel.AddLatePlayerAsync(playerId))
            {
                CustomMessageBox.ShowError("Impossible d'ajouter le joueur.", "Erreur");
                return;
            }

            var tournamentService = _serviceProvider.GetRequiredService<TournamentService>();
            var tournament = await tournamentService.GetTournamentAsync(_tournamentId);
            var newPlayer = tournament?.Players.OrderByDescending(p => p.Id).FirstOrDefault();

            if (newPlayer != null)
            {
                var assignment = await _tableManagementService.AssignLatePlayerAsync(newPlayer.Id);
                if (assignment != null)
                {
                    CustomMessageBox.ShowSuccess(
                        $"Joueur ajouté !\n\nTable {assignment.TableNumber}, Siège {assignment.SeatNumber}",
                        "Succès");
                }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e) => OpenTableView();
        private void OpenTableView()
        {
            new TableLayoutDialog(_tableManagementService, _tournamentId) { Owner = this }.ShowDialog();
        }

        private void ShortcutsButton_Click(object sender, RoutedEventArgs e)
        {
            ShortcutsPopup.IsOpen = !ShortcutsPopup.IsOpen;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.StopTimer();
            base.OnClosed(e);
        }
    }
}