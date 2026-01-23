using PokerTournamentDirector.Services;
using PokerTournamentDirector.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PokerTournamentDirector.Views
{
    public partial class TournamentSetupView : Window
    {
        private readonly TournamentSetupViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;

        public int? CreatedTournamentId { get; private set; }
        public bool TournamentStarted { get; private set; }

        public TournamentSetupView(TournamentSetupViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            DataContext = _viewModel;

            // S'abonner à l'événement de démarrage
            _viewModel.TournamentReadyToStart += OnTournamentReadyToStart;

            // Initialiser les données
            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }

        private void OnTournamentReadyToStart(object? sender, int tournamentId)
        {
            CreatedTournamentId = tournamentId;
            TournamentStarted = true;
            DialogResult = true;
            Close();
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TournamentId.HasValue && _viewModel.CurrentStep > 1)
            {
                var result = CustomMessageBox.ShowConfirmation(
                    "Un tournoi est en cours de création.\n\nVoulez-vous l'annuler ?",
                    "Confirmation");

                if (result == MessageBoxResult.No)
                    return;

                // Annuler le tournoi
                _viewModel.CancelTournamentCommand.Execute(null);
            }

            DialogResult = false;
            Close();
        }

        private void Seat_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is SeatInfo seat && seat.IsOccupied && seat.TournamentPlayerId.HasValue)
            {
                var viewModel = (TournamentSetupViewModel)DataContext;
                var contextMenu = new ContextMenu();

                // Option : Verrouiller/Déverrouiller
                var lockItem = new MenuItem
                {
                    Header = seat.IsLocked ? "🔓 Déverrouiller" : "🔒 Verrouiller",
                    Command = viewModel.ToggleLockPlayerCommand,
                    CommandParameter = seat.TournamentPlayerId.Value
                };
                contextMenu.Items.Add(lockItem);

                // Option : Déplacer
                var moveItem = new MenuItem
                {
                    Header = "↔️ Déplacer vers...",
                    Tag = seat
                };
                moveItem.Click += MovePlayer_Click;
                contextMenu.Items.Add(moveItem);

                contextMenu.IsOpen = true;
                border.ContextMenu = contextMenu;
            }
        }

        private async void MovePlayer_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var seat = menuItem?.Tag as SeatInfo;
            if (seat == null || !seat.TournamentPlayerId.HasValue) return;

            var viewModel = (TournamentSetupViewModel)DataContext;

            // Créer fenêtre de dialogue
            var dialog = new Window
            {
                Title = $"Déplacer {seat.PlayerName}",
                Width = 350,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 46)),
                WindowStyle = WindowStyle.ToolWindow
            };

            var stack = new StackPanel { Margin = new Thickness(20) };

            // Label Table
            stack.Children.Add(new TextBlock
            {
                Text = "Choisir la table :",
                Foreground = Brushes.White,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // ComboBox Table
            var tableCombo = new ComboBox
            {
                Height = 35,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15)
            };

            foreach (var table in viewModel.TableLayouts)
            {
                tableCombo.Items.Add(new ComboBoxItem
                {
                    Content = $"Table {table.TableNumber} ({table.PlayerCount}/{table.MaxSeats})",
                    Tag = table
                });
            }
            stack.Children.Add(tableCombo);

            // Label Siège
            stack.Children.Add(new TextBlock
            {
                Text = "Choisir le siège :",
                Foreground = Brushes.White,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            });

            // ComboBox Siège
            var seatCombo = new ComboBox
            {
                Height = 35,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Mise à jour sièges selon table sélectionnée
            tableCombo.SelectionChanged += (s, args) =>
            {
                seatCombo.Items.Clear();
                if (tableCombo.SelectedItem is ComboBoxItem selected && selected.Tag is TableLayout table)
                {
                    foreach (var k in table.Seats)
                    {
                        seatCombo.Items.Add(new ComboBoxItem
                        {
                            Content = $"Siège #{k.SeatNumber}" + (k.IsOccupied ? " (occupé)" : ""),
                            Tag = k.SeatNumber,
                            IsEnabled = !k.IsOccupied
                        });
                    }
                }
            };

            stack.Children.Add(seatCombo);

            // Boutons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelButton = new Button
            {
                Content = "Annuler",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(233, 69, 96)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cancelButton.Click += (s, args) => dialog.Close();

            var okButton = new Button
            {
                Content = "Déplacer",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(0, 255, 136)),
                Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)),
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            okButton.Click += async (s, args) =>
            {
                if (tableCombo.SelectedItem is ComboBoxItem tableItem &&
                    seatCombo.SelectedItem is ComboBoxItem seatItem &&
                    tableItem.Tag is TableLayout targetTable &&
                    seatItem.Tag is int targetSeat)
                {
                    var result = CustomMessageBox.ShowConfirmation(
                        $"Déplacer {seat.PlayerName} vers Table {targetTable.TableNumber}, Siège #{targetSeat} ?",
                        "Confirmer le déplacement");

                    if (result == MessageBoxResult.Yes)
                    {
                        await viewModel.MovePlayerCommand.ExecuteAsync((
                            seat.TournamentPlayerId.Value,
                            targetTable.TableId,
                            targetSeat
                        ));
                        dialog.Close();
                    }
                }
                else
                {
                    CustomMessageBox.ShowWarning("Veuillez sélectionner une table et un siège.", "Sélection incomplète");
                }
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);
            stack.Children.Add(buttonPanel);

            dialog.Content = stack;
            dialog.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.TournamentReadyToStart -= OnTournamentReadyToStart;
            base.OnClosed(e);
        }
    }
}