using PokerTournamentDirector.Data;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.ViewModels;
using System.Windows;

namespace PokerTournamentDirector.Views
{
    public partial class ChampionshipDashboardView : Window
    {
        private readonly ChampionshipDashboardViewModel _viewModel;

        public ChampionshipDashboardView(
            int championshipId,
            ChampionshipService championshipService)
        {
            InitializeComponent();
            _viewModel = new ChampionshipDashboardViewModel(
                championshipService, championshipId );
            DataContext = _viewModel;
            Loaded += async (s, e) => await _viewModel.InitializeAsync();
        }
    }
}