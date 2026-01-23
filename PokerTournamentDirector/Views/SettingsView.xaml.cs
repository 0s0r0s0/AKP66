using PokerTournamentDirector.ViewModels;
using System.Windows;

namespace PokerTournamentDirector.Views
{
    public partial class SettingsView : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}