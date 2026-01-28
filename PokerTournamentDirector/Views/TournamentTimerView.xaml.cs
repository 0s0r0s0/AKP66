using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PokerTournamentDirector;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Models;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.ViewModels;
using PokerTournamentDirector.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

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

        private MediaPlayer? _victoryPlayer;
        private readonly Random _random = new Random();
        private readonly DispatcherTimer _confettiCleanupTimer;
        private const int CONFETTI_PER_WAVE = 150; // Réduit pour meilleures perfs
        private const int MAX_CONFETTI = 600; // Limite totale

        public TournamentTimerView(TournamentTimerViewModel viewModel, IServiceProvider? serviceProvider = null)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _serviceProvider = serviceProvider ?? App.Services;
            _tournamentId = viewModel.TournamentId;
            _tableManagementService = _serviceProvider.GetRequiredService<TableManagementService>();

            DataContext = _viewModel;

            // Timer pour nettoyer les confettis régulièrement
            _confettiCleanupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _confettiCleanupTimer.Tick += (s, e) => CleanupOldConfetti();

            // Event handler pour la célébration
            _viewModel.OnVictoryCelebrationNeeded += OnVictoryCelebration;
            Unloaded += OnWindowUnloaded;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;

                case Key.S:
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



        // ===== CELEBRATION VICTOIRE =====
        #region Animation victoire
        private async void OnVictoryCelebration(object? sender, EventArgs e)
        {
            await StartVictoryCelebrationAsync();
        }

        private async Task StartVictoryCelebrationAsync()
        {
            // Son de victoire
            PlayVictorySound();

            // Démarrer le timer de nettoyage
            _confettiCleanupTimer.Start();

            // 3 vagues de confettis avec délais
            await LaunchConfettiWaveAsync();
            await Task.Delay(800);
            await LaunchConfettiWaveAsync();
            await Task.Delay(1000);
            await LaunchConfettiWaveAsync();

            // Arrêter le timer après 15 secondes
            await Task.Delay(15000);
            _confettiCleanupTimer.Stop();
            CleanupAllConfetti();
        }

        private Task LaunchConfettiWaveAsync()
        {
            return Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    // Vérifier qu'on ne dépasse pas la limite
                    if (ConfettiCanvas.Children.Count >= MAX_CONFETTI)
                        return;

                    CreateConfettiWave();
                });
            });
        }

        private void CreateConfettiWave()
        {
            var colors = GetVibrantColors();
            var canvasWidth = ActualWidth;
            var canvasHeight = ActualHeight;

            for (int i = 0; i < CONFETTI_PER_WAVE; i++)
            {
                var confetti = CreateConfettiShape(colors);

                // Position aléatoire sur TOUTE la largeur (pas centrée)
                var startX = _random.NextDouble() * canvasWidth;
                var startY = -_random.Next(50, 200);

                Canvas.SetLeft(confetti, startX);
                Canvas.SetTop(confetti, startY);

                // IMPORTANT: ZIndex élevé pour passer devant tout
                Canvas.SetZIndex(confetti, 999);

                ConfettiCanvas.Children.Add(confetti);

                // Animer le confetti
                AnimateConfetti(confetti, canvasHeight);
            }
        }

        private Shape CreateConfettiShape(Color[] colors)
        {
            Shape confetti;
            var shapeType = _random.Next(0, 3);

            switch (shapeType)
            {
                case 0: // Rectangle
                    confetti = new Rectangle
                    {
                        Width = _random.Next(6, 14),
                        Height = _random.Next(12, 24),
                        RadiusX = 3,
                        RadiusY = 3
                    };
                    break;
                case 1: // Ellipse
                    confetti = new Ellipse
                    {
                        Width = _random.Next(8, 16),
                        Height = _random.Next(8, 16)
                    };
                    break;
                default: // Triangle (Polygon)
                    var triangle = new Polygon();
                    var size = _random.Next(8, 16);
                    triangle.Points = new PointCollection
                {
                    new Point(0, size),
                    new Point(size / 2, 0),
                    new Point(size, size)
                };
                    confetti = triangle;
                    break;
            }

            confetti.Fill = new SolidColorBrush(colors[_random.Next(colors.Length)]);
            confetti.Opacity = 0.7 + _random.NextDouble() * 0.3;
            confetti.RenderTransformOrigin = new Point(0.5, 0.5);
            confetti.RenderTransform = new TransformGroup
            {
                Children =
            {
                new RotateTransform(),
                new TranslateTransform(),
                new ScaleTransform(1, 1)
            }
            };

            return confetti;
        }

        private void AnimateConfetti(Shape confetti, double canvasHeight)
        {
            var duration = TimeSpan.FromSeconds(_random.Next(3, 7));
            var storyboard = new Storyboard();

            // Chute avec effet de gravité
            var fallAnimation = new DoubleAnimation
            {
                To = canvasHeight + 100,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(fallAnimation, confetti);
            Storyboard.SetTargetProperty(fallAnimation, new PropertyPath("(Canvas.Top)"));
            storyboard.Children.Add(fallAnimation);

            // Rotation continue
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = _random.Next(720, 1440) * (_random.NextDouble() > 0.5 ? 1 : -1),
                Duration = duration
            };
            Storyboard.SetTarget(rotateAnimation, confetti);
            Storyboard.SetTargetProperty(rotateAnimation,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)"));
            storyboard.Children.Add(rotateAnimation);

            // Mouvement horizontal (zigzag)
            var zigzagAnimation = new DoubleAnimation
            {
                From = -60,
                To = 60,
                Duration = TimeSpan.FromSeconds(1.2),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(zigzagAnimation, confetti);
            Storyboard.SetTargetProperty(zigzagAnimation,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)"));
            storyboard.Children.Add(zigzagAnimation);

            // Fade out vers la fin
            var opacityAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(1),
                BeginTime = TimeSpan.FromSeconds(duration.TotalSeconds - 1)
            };
            Storyboard.SetTarget(opacityAnimation, confetti);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(opacityAnimation);

            // Supprimer le confetti à la fin
            storyboard.Completed += (s, e) =>
            {
                ConfettiCanvas.Children.Remove(confetti);
            };

            storyboard.Begin();
        }

        private Color[] GetVibrantColors()
        {
            return new[]
            {
            Color.FromRgb(255, 215, 0),   // Gold
            Color.FromRgb(0, 255, 136),   // Green neon
            Color.FromRgb(255, 0, 255),   // Magenta
            Color.FromRgb(0, 255, 255),   // Cyan
            Color.FromRgb(255, 69, 96),   // Red
            Color.FromRgb(255, 165, 0),   // Orange
            Color.FromRgb(138, 43, 226),  // Purple
            Color.FromRgb(255, 255, 0),   // Yellow
            Color.FromRgb(0, 255, 127),   // Spring green
            Color.FromRgb(255, 20, 147)   // Deep pink
        };
        }

        private void CleanupOldConfetti()
        {
            // Supprimer les confettis qui sont hors de l'écran
            var toRemove = ConfettiCanvas.Children
                .OfType<Shape>()
                .Where(c => Canvas.GetTop(c) > ActualHeight + 200)
                .ToList();

            foreach (var confetti in toRemove)
            {
                ConfettiCanvas.Children.Remove(confetti);
            }
        }

        private void CleanupAllConfetti()
        {
            ConfettiCanvas.Children.Clear();
        }

        // ===== SON DE VICTOIRE =====
        private void PlayVictorySound()
        {
            try
            {
                var soundPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Sounds",
                    "bravo.mp3");

                if (!File.Exists(soundPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Son de victoire introuvable : {soundPath}");
                    return;
                }

                // Créer un nouveau MediaPlayer à chaque fois pour éviter les conflits
                _victoryPlayer?.Close();
                _victoryPlayer = new MediaPlayer
                {
                    Volume = 0.7
                };

                _victoryPlayer.Open(new Uri(soundPath, UriKind.Absolute));
                _victoryPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lecture son victoire : {ex.Message}");
            }
        }

        // ===== NETTOYAGE =====
        private void OnWindowUnloaded(object sender, RoutedEventArgs e)
        {
            // Arrêter le timer
            _confettiCleanupTimer?.Stop();

            // Nettoyer le son
            if (_victoryPlayer != null)
            {
                _victoryPlayer.Stop();
                _victoryPlayer.Close();
                _victoryPlayer = null;
            }

            // Nettoyer les confettis
            CleanupAllConfetti();

            // Désabonner l'event
            if (_viewModel != null)
            {
                _viewModel.OnVictoryCelebrationNeeded -= OnVictoryCelebration;
            }
        }

        #endregion

        private void Eliminations_Click(object sender, RoutedEventArgs e)
        {
            OpenEliminations();
        }

        private void OpenEliminations()
        {
            var tournamentService = _serviceProvider.GetRequiredService<TournamentService>();
            var playerService = _serviceProvider.GetRequiredService<PlayerService>();

            var tournamentId = _viewModel.TournamentId;



            var logService = _serviceProvider.GetRequiredService<TournamentLogService>();
            var championshipService = _serviceProvider.GetRequiredService<ChampionshipService>();
            var context = _serviceProvider.GetRequiredService<PokerDbContext>();

            var eliminationViewModel = new EliminationViewModel(
                tournamentService,
                playerService,
                _tableManagementService,
                logService,
                championshipService,
                context,
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

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
    #region Modifier blinds en cours de partie
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
                PokerTournamentDirector.Views.CustomMessageBox.ShowError("Veuillez entrer des valeurs numériques valides.", "Erreur");
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
    #endregion

    /// <summary>
    /// Dialogue pour ajouter un joueur retardataire
    /// </summary>
    #region Entrée tardive joueur
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
    #endregion

    /// <summary>
    /// Dialogue pour afficher et gérer le plan des tables
    /// </summary>
    #region Plan de tables
    public partial class TableLayoutDialog : Window
    {
        private readonly TableManagementService _tableService;
        private readonly int _tournamentId;
        private WrapPanel _tablesPanel;

        public TableLayoutDialog(TableManagementService tableService, int tournamentId)
        {
            _tableService = tableService;
            _tournamentId = tournamentId;
            InitializeDialog();
            LoadTables();
        }

        private void InitializeDialog()
        {
            Title = "🎲 Plan des Tables";
            Width = 1400;
            Height = 950;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0a0e27"));

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header avec gradient
            var header = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0),
                    GradientStops = new GradientStopCollection
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#1e3a8a"), 0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#3b82f6"), 1)
                }
                },
                Padding = new Thickness(30, 20, 30, 20)
            };
            header.Child = new TextBlock
            {
                Text = "🎲 PLAN DES TABLES",
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // Zone de contenu avec ScrollViewer
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(30, 20, 30, 20),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0a0e27"))
            };

            _tablesPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            scrollViewer.Content = _tablesPanel;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Footer
            var footer = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e")),
                Padding = new Thickness(20),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2a2a3e")),
                BorderThickness = new Thickness(0, 2, 0, 0)
            };

            var footerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var btnBalance = CreateFooterButton("⚖️ Équilibrer", "#10b981");
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
            footerStack.Children.Add(btnBalance);

            var btnRefresh = CreateFooterButton("🔄 Rafraîchir", "#3b82f6");
            btnRefresh.Click += (s, e) => LoadTables();
            footerStack.Children.Add(btnRefresh);

            var btnClose = CreateFooterButton("✕ Fermer", "#ef4444");
            btnClose.Click += (s, e) => Close();
            footerStack.Children.Add(btnClose);

            footer.Child = footerStack;
            Grid.SetRow(footer, 2);
            mainGrid.Children.Add(footer);

            Content = mainGrid;
        }

        private Button CreateFooterButton(string content, string color)
        {
            var btn = new Button
            {
                Content = content,
                Width = 160,
                Height = 50,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(8, 0, 8, 0),
                Cursor = Cursors.Hand,
                Style = CreateButtonStyle()
            };
            return btn;
        }

        private Style CreateButtonStyle()
        {
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Button.TemplateProperty, CreateButtonTemplate()));
            return style;
        }

        private ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.Name = "border";
            factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            factory.SetValue(Border.PaddingProperty, new Thickness(20, 12, 20, 12));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(contentFactory);

            template.VisualTree = factory;
            return template;
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
                var emptyText = new TextBlock
                {
                    Text = "Aucune table créée pour le moment",
                    FontSize = 26,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(50)
                };
                _tablesPanel.Children.Add(emptyText);
            }
        }

        private Border CreateTableCard(TableLayout table)
        {
            var card = new Border
            {
                Width = 320,
                Margin = new Thickness(15),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                BorderThickness = new Thickness(2),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 10,
                    BlurRadius = 25,
                    Opacity = 0.6
                }
            };

            var mainStack = new StackPanel();

            // En-tête de table avec design moderne
            var headerBorder = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#3b82f6"), 0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#2563eb"), 1)
                }
                },
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Padding = new Thickness(20, 15, 20, 15)
            };

            var headerStack = new StackPanel();
            headerStack.Children.Add(new TextBlock
            {
                Text = $"TABLE {table.TableNumber}",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var occupancyText = new TextBlock
            {
                Text = $"{table.PlayerCount} / {table.MaxSeats} joueurs",
                FontSize = 16,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bfdbfe")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };
            headerStack.Children.Add(occupancyText);

            headerBorder.Child = headerStack;
            mainStack.Children.Add(headerBorder);

            // Zone des sièges avec espacement optimisé
            var seatsStack = new StackPanel
            {
                Margin = new Thickness(15, 20, 15, 20)
            };

            foreach (var seat in table.Seats)
            {
                var seatBorder = new Border
                {
                    Background = seat.IsOccupied
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f172a"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 8),
                    BorderBrush = seat.IsOccupied
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3b82f6"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                    BorderThickness = new Thickness(2)
                };

                var seatGrid = new Grid();
                seatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                seatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Numéro de siège
                var seatNumber = new TextBlock
                {
                    Text = $"{seat.SeatNumber}",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = seat.IsOccupied
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60a5fa"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                    Width = 40,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(seatNumber, 0);
                seatGrid.Children.Add(seatNumber);

                // Nom du joueur
                var playerStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (seat.IsOccupied)
                {
                    playerStack.Children.Add(new TextBlock
                    {
                        Text = seat.PlayerName,
                        FontSize = 18,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });

                    if (seat.IsLocked)
                    {
                        playerStack.Children.Add(new TextBlock
                        {
                            Text = " 🔒",
                            FontSize = 16,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(8, 0, 0, 0)
                        });
                    }
                }
                else
                {
                    playerStack.Children.Add(new TextBlock
                    {
                        Text = "Libre",
                        FontSize = 18,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                        FontStyle = FontStyles.Italic
                    });
                }

                Grid.SetColumn(playerStack, 1);
                seatGrid.Children.Add(playerStack);

                seatBorder.Child = seatGrid;
                seatsStack.Children.Add(seatBorder);
            }

            mainStack.Children.Add(seatsStack);
            card.Child = mainStack;

            return card;
        }
    }
    #endregion
}

