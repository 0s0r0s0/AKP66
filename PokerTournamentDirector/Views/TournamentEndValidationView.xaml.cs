using PokerTournamentDirector.Services;
using PokerTournamentDirector.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PokerTournamentDirector.Views
{
    public partial class TournamentEndValidationView : Window
    {
        private readonly TournamentEndValidationViewModel _viewModel;

        public TournamentEndValidationView(
            TournamentService tournamentService,
            ChampionshipService? championshipService,
            int tournamentId,
            int? championshipId = null)
        {
            InitializeComponent();

            _viewModel = new TournamentEndValidationViewModel(
                tournamentService,
                championshipService,
                tournamentId,
                championshipId);

            _viewModel.CloseWindow = () => DialogResult = true;

            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedResult == null) return;

            var result = _viewModel.SelectedResult;

            var dialog = new PositionEditDialog(result.PlayerName, result.CurrentPosition);
            if (dialog.ShowDialog() == true && dialog.NewPosition.HasValue)
            {
                result.CurrentPosition = dialog.NewPosition.Value;
            }
        }

    }

    // Dialogue simple pour éditer une position
    public class PositionEditDialog : Window
    {
        public int? NewPosition { get; private set; }

        public PositionEditDialog(string playerName, int currentPosition)
        {
            Title = "Modifier la position";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(26, 26, 46));

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var titleBlock = new System.Windows.Controls.TextBlock
            {
                Text = $"Modifier la position de {playerName}",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var label = new System.Windows.Controls.TextBlock
            {
                Text = "Nouvelle position :",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var textBox = new System.Windows.Controls.TextBox
            {
                Text = currentPosition.ToString(),
                Height = 40,
                FontSize = 14,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15)
            };
            textBox.SelectAll();

            var buttonsPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Annuler",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0)
            };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 100,
                Height = 35,
                IsDefault = true
            };
            okButton.Click += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out int pos) && pos > 0)
                {
                    NewPosition = pos;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    CustomMessageBox.ShowWarning("Position invalide", "Erreur");
                }
            };

            buttonsPanel.Children.Add(cancelButton);
            buttonsPanel.Children.Add(okButton);

            System.Windows.Controls.Grid.SetRow(titleBlock, 0);
            System.Windows.Controls.Grid.SetRow(label, 1);
            System.Windows.Controls.Grid.SetRow(textBox, 2);
            System.Windows.Controls.Grid.SetRow(buttonsPanel, 4);

            grid.Children.Add(titleBlock);
            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonsPanel);

            Content = grid;
        }
    }
}
