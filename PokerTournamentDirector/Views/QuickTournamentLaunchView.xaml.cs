using PokerTournamentDirector.ViewModels;
using System;
using System.Windows;

namespace PokerTournamentDirector.Views
{
    public partial class QuickTournamentLaunchView : Window
    {
        private readonly QuickTournamentLaunchViewModel _viewModel;
        public int CreatedTournamentId { get; private set; }

        public QuickTournamentLaunchView(QuickTournamentLaunchViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += async (s, e) =>
            {
                try
                {
                    await _viewModel.InitializeAsync();
                }
                catch (Exception ex)
                {
                    CustomMessageBox.ShowError(ex.Message, "Erreur");
                    DialogResult = false;
                    Close();
                }
            };
        }

        private async void Launch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // APPELLE DIRECTEMENT LA MÉTHODE PUBLIQUE
                CreatedTournamentId = await _viewModel.LaunchTournamentAsync();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError(ex.Message, "Erreur");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}