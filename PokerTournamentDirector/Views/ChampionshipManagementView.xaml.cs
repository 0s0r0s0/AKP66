using PokerTournamentDirector.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace PokerTournamentDirector.Views
{
    public partial class ChampionshipManagementView : Window
    {
        private readonly ChampionshipManagementViewModel _viewModel;

        public ChampionshipManagementView(ChampionshipManagementViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }

        private void ChampionshipsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-clic = Ouvrir le tableau de bord
            if (_viewModel.SelectedChampionship != null)
            {
                _viewModel.OpenDashboardCommand.Execute(null);
            }
        }
    }
}
