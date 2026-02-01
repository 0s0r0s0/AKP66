using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PokerTournamentDirector.Views.Dialogs
{
    public class AddLatePlayerDialog : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly int _tournamentId;
        private ListBox _playerList = null!;
        private TextBox _searchBox = null!;
        private List<PlayerDisplayItem> _allAvailablePlayers;

        public int? SelectedPlayerId { get; private set; }

        // Classe interne pour afficher les joueurs
        private class PlayerDisplayItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty; 
            public string Nickname { get; set; } = string.Empty; 
            public string DisplayText => string.IsNullOrEmpty(Nickname) ? Name : $"{Name} (@{Nickname})";
        }

        public AddLatePlayerDialog(IServiceProvider serviceProvider, int tournamentId)
        {
            _serviceProvider = serviceProvider;
            _tournamentId = tournamentId;
            _allAvailablePlayers = new List<PlayerDisplayItem>();
            InitializeDialog();
            LoadPlayers();
        }

        private void InitializeDialog()
        {
            Title = "Ajouter un joueur retardataire";
            Width = 550;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"));
            ResizeMode = ResizeMode.NoResize;

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // List
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // ===== HEADER =====
            var headerBorder = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#0f3460"), 0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#16213e"), 1)
                }
                },
                Padding = new Thickness(25, 20, 25, 20),
                CornerRadius = new CornerRadius(0)
            };

            var headerStack = new StackPanel();
            headerStack.Children.Add(new TextBlock
            {
                Text = "👥 AJOUTER UN JOUEUR",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = "Sélectionnez un joueur à inscrire tardivement",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0a0")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            });

            headerBorder.Child = headerStack;
            Grid.SetRow(headerBorder, 0);
            mainGrid.Children.Add(headerBorder);

            // ===== BARRE DE RECHERCHE =====
            var searchContainer = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")),
                Padding = new Thickness(25, 20, 25, 20)
            };

            var searchStack = new StackPanel();

            var searchLabel = new TextBlock
            {
                Text = "🔍 Rechercher",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0a0")),
                Margin = new Thickness(0, 0, 0, 8)
            };
            searchStack.Children.Add(searchLabel);

            var searchBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Height = 50
            };

            _searchBox = new TextBox
            {
                FontSize = 16,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(15, 0, 15, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                CaretBrush = Brushes.White
            };
            _searchBox.TextChanged += SearchBox_TextChanged;

            // Placeholder effect
            var searchGrid = new Grid();
            var placeholder = new TextBlock
            {
                Text = "Tapez un nom ou un pseudo...",
                FontSize = 16,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0a0")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 15, 0),
                IsHitTestVisible = false,
                Name = "SearchPlaceholder"
            };

            _searchBox.TextChanged += (s, e) =>
            {
                placeholder.Visibility = string.IsNullOrEmpty(_searchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            };

            searchGrid.Children.Add(placeholder);
            searchGrid.Children.Add(_searchBox);
            searchBorder.Child = searchGrid;
            searchStack.Children.Add(searchBorder);

            searchContainer.Child = searchStack;
            Grid.SetRow(searchContainer, 1);
            mainGrid.Children.Add(searchContainer);

            // ===== LISTE DES JOUEURS =====
            var listContainer = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")),
                Padding = new Thickness(25, 10, 25, 20)
            };

            var listStack = new StackPanel();

            var playerCountLabel = new TextBlock
            {
                Name = "PlayerCountLabel",
                Text = "Chargement...",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0a0")),
                Margin = new Thickness(0, 0, 0, 12)
            };
            listStack.Children.Add(playerCountLabel);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 500 // AJOUT : Hauteur maximale pour activer le scroll
            };

            _playerList = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 16
            };
            _playerList.MouseDoubleClick += (s, e) =>
            {
                if (_playerList.SelectedItem != null)
                    BtnAdd_Click(s, new RoutedEventArgs());
            };

            scrollViewer.Content = _playerList;
            listStack.Children.Add(scrollViewer);
            listContainer.Child = listStack;

            Grid.SetRow(listContainer, 2);
            mainGrid.Children.Add(listContainer);

            // ===== BOUTONS =====
            var buttonPanel = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e")),
                Padding = new Thickness(25, 20, 25, 25),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460")),
                BorderThickness = new Thickness(0, 2, 0, 0)
            };

            var buttonStack = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var btnAdd = CreateStyledButton("✅ Ajouter", "#00ff88", 150);
            btnAdd.Click += BtnAdd_Click;
            buttonStack.Children.Add(btnAdd);

            var btnCancel = CreateStyledButton("✕ Annuler", "#e94560", 150);
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
            buttonStack.Children.Add(btnCancel);

            buttonPanel.Child = buttonStack;
            Grid.SetRow(buttonPanel, 3);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;

            // Focus sur la barre de recherche au chargement
            Loaded += (s, e) => _searchBox.Focus();
        }

        private System.Windows.Controls.Button CreateStyledButton(string content, string color, double width)
        {
            var btn = new Button
            {
                Content = content,
                Width = width,
                Height = 50,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = color == "#00ff88"
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"))
                    : Brushes.White,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(8, 0, 8, 0),
                Cursor = Cursors.Hand
            };

            // Style avec template pour coins arrondis
            var template = new ControlTemplate(typeof(Button));
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.Name = "border";
            factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            factory.SetValue(Border.PaddingProperty, new Thickness(20, 0, 20, 0));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            factory.AppendChild(contentFactory);

            template.VisualTree = factory;
            btn.Template = template;

            return btn;
        }

        private async void LoadPlayers()
        {
            var playerService = _serviceProvider.GetRequiredService<PlayerService>();
            var tournamentService = _serviceProvider.GetRequiredService<TournamentService>();

            var allPlayers = await playerService.GetAllPlayersAsync();
            var tournament = await tournamentService.GetTournamentAsync(_tournamentId);
            var registeredPlayerIds = tournament?.Players.Select(p => p.PlayerId).ToList() ?? new List<int>();

            _allAvailablePlayers = allPlayers
                .Where(p => !registeredPlayerIds.Contains(p.Id))
                .Select(p => new PlayerDisplayItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Nickname = p.Nickname
                })
                .OrderBy(p => p.Name)
                .ToList();

            UpdatePlayerList(_allAvailablePlayers);
            UpdatePlayerCount(_allAvailablePlayers.Count);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = _searchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                UpdatePlayerList(_allAvailablePlayers);
                UpdatePlayerCount(_allAvailablePlayers.Count);
            }
            else
            {
                var filtered = _allAvailablePlayers
                    .Where(p => p.Name.ToLower().Contains(searchText) ||
                               (!string.IsNullOrEmpty(p.Nickname) && p.Nickname.ToLower().Contains(searchText)))
                    .ToList();

                UpdatePlayerList(filtered);
                UpdatePlayerCount(filtered.Count);
            }
        }

        private void UpdatePlayerList(List<PlayerDisplayItem> players)
        {
            _playerList.Items.Clear();

            if (!players.Any())
            {
                var emptyItem = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460")),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20, 30, 20, 30),
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var emptyStack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                emptyStack.Children.Add(new TextBlock
                {
                    Text = "😕",
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                });
                emptyStack.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(_searchBox.Text)
                        ? "Aucun joueur disponible"
                        : "Aucun résultat",
                    FontSize = 16,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0a0")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                emptyItem.Child = emptyStack;
                _playerList.Items.Add(emptyItem);
                return;
            }

            foreach (var player in players)
            {
                var item = CreatePlayerListItem(player);
                _playerList.Items.Add(item);
            }
        }

        private ListBoxItem CreatePlayerListItem(PlayerDisplayItem player)
        {
            var listItem = new ListBoxItem
            {
                Tag = player.Id,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 8),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch // Pour que le contenu prenne toute la largeur
            };

            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 14, 18, 14),
                MinHeight = 70 // Hauteur minimale uniforme
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icône
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Contenu

            // Icône
            var icon = new Border
            {
                Width = 45,
                Height = 45,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")),
                CornerRadius = new CornerRadius(22.5),
                Margin = new Thickness(0, 0, 15, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.Child = new TextBlock
            {
                Text = player.Name.Substring(0, 1).ToUpper(),
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            // Info joueur
            var infoStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            var nameBlock = new TextBlock
            {
                Text = player.Name,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            infoStack.Children.Add(nameBlock);

            if (!string.IsNullOrEmpty(player.Nickname))
            {
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"@{player.Nickname}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")),
                    Margin = new Thickness(0, 2, 0, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }

            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            border.Child = grid;
            listItem.Content = border;

            // Effet hover
            listItem.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffd700"));
            };
            listItem.MouseLeave += (s, e) =>
            {
                if (!listItem.IsSelected)
                {
                    border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460"));
                    border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88"));
                }
            };
            listItem.Selected += (s, e) =>
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffd700"));
            };
            listItem.Unselected += (s, e) =>
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88"));
            };

            return listItem;
        }

        private void UpdatePlayerCount(int count)
        {
            // Trouver le label dans l'arbre visuel
            var mainGrid = Content as Grid;
            if (mainGrid != null)
            {
                var listContainer = mainGrid.Children[2] as Border;
                var listStack = listContainer?.Child as StackPanel;
                var countLabel = listStack?.Children[0] as TextBlock;

                if (countLabel != null)
                {
                    countLabel.Text = count == 0
                        ? "Aucun joueur disponible"
                        : $"{count} joueur{(count > 1 ? "s" : "")} disponible{(count > 1 ? "s" : "")}";
                }
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
}


