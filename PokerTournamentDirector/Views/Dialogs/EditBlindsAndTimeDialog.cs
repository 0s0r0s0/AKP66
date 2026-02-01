using PokerTournamentDirector.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PokerTournamentDirector.Views.Dialogs
{
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
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"));
            ResizeMode = ResizeMode.NoResize;

            var mainPanel = new StackPanel { Margin = new Thickness(25) };

            // Titre
            mainPanel.Children.Add(new TextBlock
            {
                Text = $"Niveau {_viewModel.CurrentLevel}",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 25)
            });

            // Section Blinds
            mainPanel.Children.Add(new TextBlock
            {
                Text = "💰 BLINDS",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")),
                Margin = new Thickness(0, 0, 0, 10)
            });

            var blindsGrid = CreateBlindsGrid();
            mainPanel.Children.Add(blindsGrid);

            // Section Temps
            mainPanel.Children.Add(new TextBlock
            {
                Text = "⏱️ TEMPS",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffd700")),
                Margin = new Thickness(0, 20, 0, 10)
            });

            _txtCurrentTime = new TextBlock
            {
                Text = _viewModel.TimeRemaining,
                FontSize = 36,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };
            mainPanel.Children.Add(_txtCurrentTime);

            var timeButtons = CreateTimeButtons();
            mainPanel.Children.Add(timeButtons);

            var adjustmentLabel = new TextBlock
            {
                Text = "Ajustement: 0s",
                Name = "adjustmentLabel",
                FontSize = 14,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 20)
            };
            mainPanel.Children.Add(adjustmentLabel);

            var buttons = CreateActionButtons();
            mainPanel.Children.Add(buttons);

            Content = mainPanel;
        }

        private Grid CreateBlindsGrid()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var sbPanel = new StackPanel { Margin = new Thickness(5) };
            sbPanel.Children.Add(CreateLabel("Small Blind"));
            _txtSmallBlind = CreateTextBox(_viewModel.SmallBlind.ToString());
            sbPanel.Children.Add(_txtSmallBlind);
            Grid.SetColumn(sbPanel, 0);
            grid.Children.Add(sbPanel);

            var bbPanel = new StackPanel { Margin = new Thickness(5) };
            bbPanel.Children.Add(CreateLabel("Big Blind"));
            _txtBigBlind = CreateTextBox(_viewModel.BigBlind.ToString());
            bbPanel.Children.Add(_txtBigBlind);
            Grid.SetColumn(bbPanel, 1);
            grid.Children.Add(bbPanel);

            var antePanel = new StackPanel { Margin = new Thickness(5) };
            antePanel.Children.Add(CreateLabel("Ante"));
            _txtAnte = CreateTextBox(_viewModel.Ante.ToString());
            antePanel.Children.Add(_txtAnte);
            Grid.SetColumn(antePanel, 2);
            grid.Children.Add(antePanel);

            return grid;
        }

        private StackPanel CreateTimeButtons()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };

            panel.Children.Add(CreateTimeButton("-5 min", -300));
            panel.Children.Add(CreateTimeButton("-1 min", -60));
            panel.Children.Add(CreateTimeButton("-30s", -30));
            panel.Children.Add(CreateTimeButton("+30s", 30));
            panel.Children.Add(CreateTimeButton("+1 min", 60));
            panel.Children.Add(CreateTimeButton("+5 min", 300));

            return panel;
        }

        private StackPanel CreateActionButtons()
        {
            var panel = new StackPanel
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
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e")),
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(5, 0, 5, 0)
            };
            btnSave.Click += BtnSave_Click;
            panel.Children.Add(btnSave);

            var btnCancel = new Button
            {
                Content = "Annuler",
                Width = 130,
                Height = 45,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e94560")),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(5, 0, 5, 0)
            };
            btnCancel.Click += (s, e) => Close();
            panel.Children.Add(btnCancel);

            return panel;
        }

        private Button CreateTimeButton(string text, int seconds)
        {
            var btn = new Button
            {
                Content = text,
                Width = 60,
                Height = 35,
                Margin = new Thickness(3),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460")),
                Foreground = Brushes.White,
                FontSize = 12
            };
            btn.Click += (s, e) => AdjustTime(seconds);
            return btn;
        }

        private void AdjustTime(int seconds)
        {
            _timeAdjustment += seconds;
            int currentSeconds = _viewModel.TotalSecondsRemaining + _timeAdjustment;

            if (currentSeconds < 0)
            {
                _timeAdjustment -= seconds;
                return;
            }

            _txtCurrentTime.Text = $"{currentSeconds / 60:D2}:{currentSeconds % 60:D2}";

            foreach (var child in ((StackPanel)Content).Children)
            {
                if (child is TextBlock tb && tb.Text.StartsWith("Ajustement"))
                {
                    tb.Text = $"Ajustement: {(_timeAdjustment >= 0 ? "+" : "")}{_timeAdjustment}s";
                    tb.Foreground = _timeAdjustment == 0 ? Brushes.Gray :
                        (_timeAdjustment > 0
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88"))
                            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e94560")));
                    break;
                }
            }
        }

        private TextBlock CreateLabel(string text) => new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 5)
        };

        private TextBox CreateTextBox(string text) => new TextBox
        {
            Text = text,
            Height = 40,
            FontSize = 18,
            Padding = new Thickness(8),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(_txtSmallBlind.Text, out int sb) ||
                !int.TryParse(_txtBigBlind.Text, out int bb) ||
                !int.TryParse(_txtAnte.Text, out int ante))
            {
                CustomMessageBox.ShowError("Valeurs invalides.", "Erreur");
                return;
            }

            if (sb <= 0 || bb <= 0 || bb < sb)
            {
                CustomMessageBox.ShowError("Blinds invalides.", "Erreur");
                return;
            }

            _viewModel.UpdateBlindsAndTime(sb, bb, ante, _timeAdjustment);
            CustomMessageBox.ShowSuccess("Modifications appliquées !", "Succès");
            Close();
        }
    }
}