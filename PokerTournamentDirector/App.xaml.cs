using Microsoft.Extensions.DependencyInjection;
using PokerTournamentDirector.Data;
using PokerTournamentDirector.Services;
using PokerTournamentDirector.ViewModels;
using PokerTournamentDirector.Views;
using System.Windows;

namespace PokerTournamentDirector
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configuration DI (Dependency Injection)
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            Services = _serviceProvider;

            // Créer la base de données si elle n'existe pas
            var dbContext = _serviceProvider.GetRequiredService<PokerDbContext>();
            dbContext.EnsureDatabaseCreated();

            // Démarrer avec le menu principal
            var mainMenu = new MainMenuView(_serviceProvider);
            mainMenu.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // DbContext
            services.AddDbContext<PokerDbContext>();

            // Services
            services.AddSingleton<TournamentService>();
            services.AddSingleton<PlayerService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<BlindStructureService>();
            services.AddSingleton<TournamentTemplateService>();
            services.AddSingleton<TableManagementService>();
            services.AddSingleton<AudioService>();
            services.AddTransient<TableManagementService>();
            services.AddTransient<ChampionshipService>();
            services.AddScoped<TournamentLogService>();

            // ViewModels
            services.AddTransient<TournamentTimerViewModel>();
            services.AddTransient<TournamentSetupViewModel>();
            services.AddTransient<PlayerManagementViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<BlindStructureEditorViewModel>();
            services.AddTransient<TournamentTemplateViewModel>();

        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider != null)
            {
                _serviceProvider.Dispose();
            }
            base.OnExit(e);
        }
    }
}
