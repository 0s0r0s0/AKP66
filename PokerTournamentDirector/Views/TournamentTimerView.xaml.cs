using PokerTournamentDirector.ViewModels;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;

namespace PokerTournamentDirector.Views
{
    public partial class TournamentTimerView : Window
    {
        private readonly TournamentTimerViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly TableManagementService _tableManagementService;
        private readonly int _tournamentId;
        private bool _forceClose = false;
        private StackPanel _tablesPanel = null!;
        private TextBlock _txtCurrentTime = null!;
        private TextBlock _txtSmallBlind = null!;
        private TextBlock _txtBigBlind = null!;
        private TextBlock _txtAnte = null!;
        private ListBox _playerList = null!;

        public TournamentTimerView(TournamentTimerViewModel viewModel, IServiceProvider? serviceProvider = null)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _serviceProvider = serviceProvider ?? App.Services;
            _tournamentId = viewModel.TournamentId; 
            _tableManagementService = _serviceProvider.GetRequiredService<TableManagementService>(); 
            DataContext = _viewModel;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;

                case Key.Space:
                    if (_viewModel.IsPaused)
                        _viewModel.ResumeTournamentCommand.Execute(null);
                    else if (_viewModel.IsRunning)
                        _viewModel.PauseTournamentCommand.Execute(null);
                    else
                        _viewModel.StartTournamentCommand.Execute(null);
                    break;

                case Key.Right:
                    _viewModel.NextLevelCommand.Execute(null);
                    break;

                case Key.Left:
                    _viewModel.PreviousLevelCommand.Execute(null);
                    break;

                case Key.E:
                    OpenEliminations();
                    break;

                case Key.B:
                    EditBlinds_Click(this, new RoutedEventArgs());
                    break;

                case Key.P:
                    if (_viewModel.CanAddLatePlayers)
                    {
                        Players_Click(this, new RoutedEventArgs());
                    }
                    break;

                case Key.T:
                    OpenTableView();
                    break;

                case Key.F11:
                    WindowState = WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                    break;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_viewModel.IsTournamentFinished || _forceClose)
            {
                _viewModel.StopTimer();
                return;
            }

            if (_viewModel.IsRunning || _viewModel.IsPaused)
            {
                var result = CustomMessageBox.ShowConfirmation("Le tournoi est en cours !\n\n" +
                    "L'état sera sauvegardé et vous pourrez reprendre plus tard.\n\n" +
                    "Voulez-vous vraiment quitter ?", "Confirmation de fermeture");

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _viewModel.StopTimer();
        }

        private void Eliminations_Click(object sender, RoutedEventArgs e)
        {
            OpenEliminations();
        }

        private void OpenEliminations()
        {
            var tournamentService = _serviceProvider.GetRequiredService<TournamentService>();
            var playerService = _serviceProvider.GetRequiredService<PlayerService>();

            var tournamentId = _viewModel.TournamentId;

            var eliminationViewModel = new EliminationViewModel(
    tournamentService,
    playerService,
    _tableManagementService,
    tournamentId
);

            eliminationViewModel.TournamentFinished += (s, winnerName) =>
            {
                _ = _viewModel.RefreshStatsAsync();
            };

            var eliminationWindow = new EliminationView(eliminationViewModel);
            eliminationWindow.ShowDialog();

            // Après fermeture, vérifier si besoin de rééquilibrer les tables
            CheckTableBalance();
            _ = _viewModel.RefreshStatsAsync();
        }

        private async void CheckTableBalance()
        {
            var tableService = _serviceProvider.GetRequiredService<TableManagementService>();
            var result = await _tableManagementService.AutoBalanceAfterChangeAsync(_tournamentId);

            if (result.Success && result.Movements.Any())
            {
                var message = result.TableBroken
                    ? $"🔔 Table {result.BrokenTableNumber} cassée !\n\n"
                    : "🔔 Équilibrage des tables !\n\n";

                message += "Mouvements :\n";
                foreach (var m in result.Movements)
                {
                    message += $"• {m.PlayerName}: Table {m.FromTable} → Table {m.ToTable} (siège {m.ToSeat})\n";
                }

                CustomMessageBox.ShowInformation(message, "Équilibrage des tables");
            }
        }

        private void EditBlinds_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditBlindsAndTimeDialog(_viewModel);
            editWindow.Owner = this;
            editWindow.ShowDialog();
        }

        private async void Players_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.CanAddLatePlayers)
            {
                CustomMessageBox.ShowInformation("La période d'inscription tardive est terminée.", "Info");
                return;
            }

            var addPlayerWindow = new AddLatePlayerDialog(_serviceProvider, _viewModel.TournamentId);
            addPlayerWindow.Owner = this;
            
            if (addPlayerWindow.ShowDialog() == true && addPlayerWindow.SelectedPlayerId.HasValue)
            {
                var success = await _viewModel.AddLatePlayerAsync(addPlayerWindow.SelectedPlayerId.Value);
                if (success)
                {
                    // Assigner le joueur à une table
                    var tableService = _serviceProvider.GetRequiredService<TableManagementService>();
                    var tournamentService = _serviceProvider.GetRequiredService<TournamentService>();
                    
                    var tournament = await tournamentService.GetTournamentAsync(_viewModel.TournamentId);
                    var newPlayer = tournament?.Players.OrderByDescending(p => p.Id).FirstOrDefault();
                    
                    if (newPlayer != null)
                    {
                        var assignment = await tableService.AssignLatePlayerAsync(newPlayer.Id);
                        if (assignment != null)
                        {
                            CustomMessageBox.Show(
                                     $"Joueur ajouté !\n\nTable {assignment.TableNumber}, Siège {assignment.SeatNumber}",
                                     "Succès",
                                     CustomMessageBox.MessageBoxType.Success,
                                     CustomMessageBox.MessageBoxButtons.OK);
                        }
                    }
                }
                else
                {
                    CustomMessageBox.ShowError("Impossible d'ajouter le joueur.", "Erreur");
                }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            OpenTableView();
        }

        private void OpenTableView()
        {
            var tableService = _serviceProvider.GetRequiredService<TableManagementService>();
            var tableWindow = new TableLayoutDialog(tableService, _viewModel.TournamentId);
            tableWindow.Owner = this;
            tableWindow.ShowDialog();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.StopTimer();
            base.OnClosed(e);
        }
    }

    /// <summary>
    /// Dialogue amélioré pour modifier les blinds ET le temps du niveau actuel
    /// </summary>
    public class EditBlindsAndTimeDialog : Window
    {
        private readonly TournamentTimerViewModel _viewModel;
        private TextBox _txtSmallBlind;
        private TextBox _txtBigBlind;
        private TextBox _txtAnte;
        private TextBlock _txtCurrentTime;
        private int _timeAdjustment = 0;

        public EditBlindsAndTimeDialog(TournamentTimerViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Title = "Modifier le niveau actuel";
            Width = 450;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e"));
            ResizeMode = ResizeMode.NoResize;

            var mainPanel = new StackPanel { Margin = new Thickness(25) };

            // Titre
            mainPanel.Children.Add(new TextBlock
            {
                Text = $"Niveau {_viewModel.CurrentLevel}",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 25)
            });

            // Section Blinds
            mainPanel.Children.Add(new TextBlock
            {
                Text = "💰 BLINDS",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00ff88")),
                Margin = new Thickness(0, 0, 0, 10)
            });

            var blindsGrid = new Grid();
            blindsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            blindsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            blindsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Small Blind
            var sbPanel = new StackPanel { Margin = new Thickness(5) };
            sbPanel.Children.Add(CreateLabel("Small Blind"));
            _txtSmallBlind = CreateTextBox(_viewModel.SmallBlind.ToString());
            sbPanel.Children.Add(_txtSmallBlind);
            Grid.SetColumn(sbPanel, 0);
            blindsGrid.Children.Add(sbPanel);

            // Big Blind
            var bbPanel = new StackPanel { Margin = new Thickness(5) };
            bbPanel.Children.Add(CreateLabel("Big Blind"));
            _txtBigBlind = CreateTextBox(_viewModel.BigBlind.ToString());
            bbPanel.Children.Add(_txtBigBlind);
            Grid.SetColumn(bbPanel, 1);
            blindsGrid.Children.Add(bbPanel);

            // Ante
            var antePanel = new StackPanel { Margin = new Thickness(5) };
            antePanel.Children.Add(CreateLabel("Ante"));
            _txtAnte = CreateTextBox(_viewModel.Ante.ToString());
            antePanel.Children.Add(_txtAnte);
            Grid.SetColumn(antePanel, 2);
            blindsGrid.Children.Add(antePanel);

            mainPanel.Children.Add(blindsGrid);

            // Section Temps
            mainPanel.Children.Add(new TextBlock
            {
                Text = "⏱️ TEMPS",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffd700")),
                Margin = new Thickness(0, 20, 0, 10)
            });

            // Affichage temps actuel
            _txtCurrentTime = new TextBlock
            {
                Text = _viewModel.TimeRemaining,
                FontSize = 36,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };
            mainPanel.Children.Add(_txtCurrentTime);

            // Boutons +/- temps
            var timeButtonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };

            timeButtonsPanel.Children.Add(CreateTimeButton("-5 min", -300));
            timeButtonsPanel.Children.Add(CreateTimeButton("-1 min", -60));
            timeButtonsPanel.Children.Add(CreateTimeButton("-30s", -30));
            timeButtonsPanel.Children.Add(CreateTimeButton("+30s", 30));
            timeButtonsPanel.Children.Add(CreateTimeButton("+1 min", 60));
            timeButtonsPanel.Children.Add(CreateTimeButton("+5 min", 300));

            mainPanel.Children.Add(timeButtonsPanel);

            // Label ajustement
            var adjustmentLabel = new TextBlock
            {
                Text = "Ajustement: 0s",
                Name = "adjustmentLabel",
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 20)
            };
            mainPanel.Children.Add(adjustmentLabel);

            // Boutons action
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var btnSave = new Button
            {
                Content = "✅ Appliquer",
                Width = 130,
                Height = 45,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00ff88")),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(5, 0, 5, 0)
            };
            btnSave.Click += BtnSave_Click;
            buttonPanel.Children.Add(btnSave);

            var btnCancel = new Button
            {
                Content = "Annuler",
                Width = 130,
                Height = 45,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#e94560")),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(5, 0, 5, 0)
            };
            btnCancel.Click += (s, e) => Close();
            buttonPanel.Children.Add(btnCancel);

            mainPanel.Children.Add(buttonPanel);
            Content = mainPanel;
        }

        private Button CreateTimeButton(string text, int seconds)
        {
            var btn = new Button
            {
                Content = text,
                Width = 60,
                Height = 35,
                Margin = new Thickness(3),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f3460")),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12
            };
            btn.Click += (s, e) => AdjustTime(seconds);
            return btn;
        }

        private void AdjustTime(int seconds)
        {
            _timeAdjustment += seconds;
            
            // Calculer le nouveau temps
            int currentSeconds = _viewModel.TotalSecondsRemaining + _timeAdjustment;
            if (currentSeconds < 0) 
            {
                _timeAdjustment -= seconds; // Annuler si ça donne un temps négatif
                return;
            }

            int mins = currentSeconds / 60;
            int secs = currentSeconds % 60;
            _txtCurrentTime.Text = $"{mins:D2}:{secs:D2}";

            // Mettre à jour le label d'ajustement
            var adjustmentText = _timeAdjustment >= 0 ? $"+{_timeAdjustment}s" : $"{_timeAdjustment}s";
            foreach (var child in ((StackPanel)Content).Children)
            {
                if (child is TextBlock tb && tb.Text.StartsWith("Ajustement"))
                {
                    tb.Text = $"Ajustement: {adjustmentText}";
                    tb.Foreground = _timeAdjustment == 0 
                        ? System.Windows.Media.Brushes.Gray 
                        : (_timeAdjustment > 0 
                            ? new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00ff88"))
                            : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#e94560")));
                    break;
                }
            }
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5)
            };
        }

        private TextBox CreateTextBox(string text)
        {
            return new TextBox
            {
                Text = text,
                Height = 40,
                FontSize = 18,
                Padding = new Thickness(8),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(_txtSmallBlind.Text, out int smallBlind) ||
                !int.TryParse(_txtBigBlind.Text, out int bigBlind) ||
                !int.TryParse(_txtAnte.Text, out int ante))
            {
                CustomMessageBox.ShowError("Veuillez entrer des valeurs numériques valides.", "Erreur");
                return;
            }

            if (smallBlind <= 0 || bigBlind <= 0)
            {
                CustomMessageBox.ShowError("Les blinds doivent être positives.", "Erreur");
                return;
            }

            if (bigBlind < smallBlind)
            {
                CustomMessageBox.ShowError("La Big Blind doit être supérieure ou égale à la Small Blind.", "Erreur");
                return;
            }

            // Appliquer les changements
            _viewModel.UpdateBlindsAndTime(smallBlind, bigBlind, ante, _timeAdjustment);
            
            CustomMessageBox.ShowSuccess("Modifications appliquées !", "Succès");
            Close();
        }
    }

    /// <summary>
    /// Dialogue pour ajouter un joueur retardataire
    /// </summary>
    public class AddLatePlayerDialog : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly int _tournamentId;
        private ListBox _playerList;
        
        public int? SelectedPlayerId { get; private set; }

        public AddLatePlayerDialog(IServiceProvider serviceProvider, int tournamentId)
        {
            _serviceProvider = serviceProvider;
            _tournamentId = tournamentId;
            InitializeDialog();
            LoadPlayers();
        }

        private void InitializeDialog()
        {
            Title = "Ajouter un joueur retardataire";
            Width = 450;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e"));
            ResizeMode = ResizeMode.NoResize;

            var mainPanel = new StackPanel { Margin = new Thickness(20) };

            mainPanel.Children.Add(new TextBlock
            {
                Text = "Sélectionnez un joueur à ajouter",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            _playerList = new ListBox
            {
                Height = 300,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f3460")),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16,
                BorderThickness = new Thickness(0)
            };
            mainPanel.Children.Add(_playerList);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var btnAdd = new Button
            {
                Content = "Ajouter",
                Width = 120,
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00ff88")),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 0, 5, 0)
            };
            btnAdd.Click += BtnAdd_Click;
            buttonPanel.Children.Add(btnAdd);

            var btnCancel = new Button
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

        private async void LoadPlayers()
        {
            var playerService = _serviceProvider.GetRequiredService<PlayerService>();
            var tournamentService = _serviceProvider.GetRequiredService<TournamentService>();

            var allPlayers = await playerService.GetAllPlayersAsync();
            var tournament = await tournamentService.GetTournamentAsync(_tournamentId);
            var registeredPlayerIds = tournament?.Players.Select(p => p.PlayerId).ToList() ?? new List<int>();

            var availablePlayers = allPlayers.Where(p => !registeredPlayerIds.Contains(p.Id)).ToList();

            _playerList.Items.Clear();
            foreach (var player in availablePlayers.OrderBy(p => p.Name))
            {
                _playerList.Items.Add(new ListBoxItem
                {
                    Content = $"{player.Name}" + (string.IsNullOrEmpty(player.Nickname) ? "" : $" (@{player.Nickname})"),
                    Tag = player.Id,
                    Padding = new Thickness(10, 8, 10, 8),
                    Foreground = System.Windows.Media.Brushes.White
                });
            }

            if (!availablePlayers.Any())
            {
                _playerList.Items.Add(new ListBoxItem
                {
                    Content = "Aucun joueur disponible",
                    IsEnabled = false,
                    Foreground = System.Windows.Media.Brushes.Gray
                });
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_playerList.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is int playerId)
            {
                SelectedPlayerId = playerId;
                DialogResult = true;
                Close();
            }
            else
            {
                CustomMessageBox.ShowInformation("Veuillez sélectionner un joueur.", "Info");
            }
        }
    }

    /// <summary>
    /// Dialogue pour afficher et gérer le plan des tables
    /// </summary>
    public class TableLayoutDialog : Window
    {
        private readonly TableManagementService _tableService;
        private readonly int _tournamentId;
        private StackPanel _tablesPanel;

        public TableLayoutDialog(TableManagementService tableService, int tournamentId)
        {
            _tableService = tableService;
            _tournamentId = tournamentId;
            InitializeDialog();
            LoadTables();
        }

        private void InitializeDialog()
        {
            Title = "🪑 Plan des Tables";
            Width = 900;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e"));

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var header = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f3460")),
                Padding = new Thickness(20)
            };
            header.Child = new TextBlock
            {
                Text = "🪑​ PLAN DES TABLES",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Tables
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(20)
            };

            _tablesPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            scrollViewer.Content = _tablesPanel;

            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Footer buttons
            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20)
            };

            var btnBalance = new Button
            {
                Content = "⚖️ Équilibrer",
                Width = 140,
                Height = 45,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ffd700")),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e")),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5)
            };
            btnBalance.Click += async (s, e) =>
            {
                var result = await _tableService.AutoBalanceAfterChangeAsync(_tournamentId);
                if (result.Movements.Any())
                {
                    CustomMessageBox.ShowInformation(result.Message, "Équilibrage");
                    LoadTables();
                }
                else
                {
                    CustomMessageBox.ShowInformation("Les tables sont déjà équilibrées.", "Info");
                }
            };
            footer.Children.Add(btnBalance);

            var btnRefresh = new Button
            {
                Content = "🔄 Rafraîchir",
                Width = 140,
                Height = 45,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f3460")),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5)
            };
            btnRefresh.Click += (s, e) => LoadTables();
            footer.Children.Add(btnRefresh);

            var btnClose = new Button
            {
                Content = "Fermer",
                Width = 140,
                Height = 45,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#e94560")),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5)
            };
            btnClose.Click += (s, e) => Close();
            footer.Children.Add(btnClose);

            Grid.SetRow(footer, 2);
            mainGrid.Children.Add(footer);

            Content = mainGrid;
        }

        private async void LoadTables()
        {
            var layouts = await _tableService.GetTableLayoutAsync(_tournamentId);
            _tablesPanel.Children.Clear();

            foreach (var table in layouts)
            {
                var tableCard = CreateTableCard(table);
                _tablesPanel.Children.Add(tableCard);
            }

            if (!layouts.Any())
            {
                _tablesPanel.Children.Add(new TextBlock
                {
                    Text = "Aucune table créée",
                    FontSize = 18,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(50)
                });
            }
        }

        private Border CreateTableCard(TableLayout table)
        {
            var card = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#16213e")),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(15),
                Margin = new Thickness(10),
                MinWidth = 200
            };

            var stack = new StackPanel();

            // Header table
            stack.Children.Add(new TextBlock
            {
                Text = $"TABLE {table.TableNumber}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00ff88")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"{table.PlayerCount}/{table.MaxSeats} joueurs",
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // Sièges
            foreach (var seat in table.Seats)
            {
                var seatBorder = new Border
                {
                    Background = seat.IsOccupied
                        ? new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f3460"))
                        : new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2a2a3e")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var seatStack = new StackPanel { Orientation = Orientation.Horizontal };
                
                seatStack.Children.Add(new TextBlock
                {
                    Text = $"{seat.SeatNumber}.",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    Width = 25
                });

                if (seat.IsOccupied)
                {
                    seatStack.Children.Add(new TextBlock
                    {
                        Text = seat.PlayerName + (seat.IsLocked ? " 🔒" : ""),
                        FontSize = 12,
                        Foreground = System.Windows.Media.Brushes.White
                    });
                }
                else
                {
                    seatStack.Children.Add(new TextBlock
                    {
                        Text = "— vide —",
                        FontSize = 12,
                        Foreground = System.Windows.Media.Brushes.Gray,
                        FontStyle = System.Windows.FontStyles.Italic
                    });
                }

                seatBorder.Child = seatStack;
                stack.Children.Add(seatBorder);
            }

            card.Child = stack;
            return card;
        }
    }
}
