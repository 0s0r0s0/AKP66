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
            ChampionshipService championshipService,
            PokerDbContext context) 
        {
            InitializeComponent();
            _viewModel = new ChampionshipDashboardViewModel(
                championshipService,
                context,
                championshipId);
            DataContext = _viewModel;
            Loaded += async (s, e) => await _viewModel.InitializeAsync();
            Closed += (s, e) => _viewModel.Dispose(); 
        }
    }
}