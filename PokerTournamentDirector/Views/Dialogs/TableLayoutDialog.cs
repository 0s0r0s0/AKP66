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
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"));

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
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString("#0f3460"), 0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#16213e"), 1)
                }
                },
                Padding = new Thickness(30, 25, 30, 25)
            };
            header.Child = new TextBlock
            {
                Text = "🎲 PLAN DES TABLES",
                FontSize = 36,
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
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"))
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
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460")),
                Padding = new Thickness(25),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")),
                BorderThickness = new Thickness(0, 2, 0, 0)
            };

            var footerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var btnBalance = CreateFooterButton("⚖️ Équilibrer", "#00ff88");
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

            var btnRefresh = CreateFooterButton("🔄 Rafraîchir", "#ffd700");
            btnRefresh.Click += (s, e) => LoadTables();
            footerStack.Children.Add(btnRefresh);

            var btnClose = CreateFooterButton("✕ Fermer", "#e94560");
            btnClose.Click += (s, e) => Close();
            footerStack.Children.Add(btnClose);

            footer.Child = footerStack;
            Grid.SetRow(footer, 2);
            mainGrid.Children.Add(footer);

            Content = mainGrid;
        }

        private Button CreateFooterButton(string content, string color)
        {
            var isGreenButton = color == "#00ff88";

            var btn = new Button
            {
                Content = content,
                Width = 170,
                Height = 55,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = isGreenButton
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"))
                    : Brushes.White,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(10, 0, 10, 0),
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
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            factory.SetValue(Border.PaddingProperty, new Thickness(20, 0, 20, 0));

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
                var emptyBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")),
                    CornerRadius = new CornerRadius(15),
                    Padding = new Thickness(40, 50, 40, 50),
                    Margin = new Thickness(50),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var emptyStack = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                emptyStack.Children.Add(new TextBlock
                {
                    Text = "🎲",
                    FontSize = 72,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 15)
                });
                emptyStack.Children.Add(new TextBlock
                {
                    Text = "Aucune table créée pour le moment",
                    FontSize = 24,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0a0")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                emptyBorder.Child = emptyStack;
                _tablesPanel.Children.Add(emptyBorder);
            }
        }

        private Border CreateTableCard(TableLayout table)
        {
            var card = new Border
            {
                Width = 340,
                Margin = new Thickness(15),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16213e")),
                CornerRadius = new CornerRadius(20),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")),
                BorderThickness = new Thickness(2),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 8,
                    BlurRadius = 20,
                    Opacity = 0.5
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
                    new GradientStop((Color)ColorConverter.ConvertFromString("#0f3460"), 0),
                    new GradientStop((Color)ColorConverter.ConvertFromString("#16213e"), 1)
                }
                },
                CornerRadius = new CornerRadius(18, 18, 0, 0),
                Padding = new Thickness(20, 18, 20, 18)
            };

            var headerStack = new StackPanel();
            headerStack.Children.Add(new TextBlock
            {
                Text = $"TABLE {table.TableNumber}",
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var occupancyText = new TextBlock
            {
                Text = $"{table.PlayerCount} / {table.MaxSeats} joueurs",
                FontSize = 17,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0a0")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            };
            headerStack.Children.Add(occupancyText);

            headerBorder.Child = headerStack;
            mainStack.Children.Add(headerBorder);

            // Zone des sièges avec espacement optimisé
            var seatsStack = new StackPanel
            {
                Margin = new Thickness(18, 20, 18, 20)
            };

            foreach (var seat in table.Seats)
            {
                var seatBorder = new Border
                {
                    Background = seat.IsOccupied
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e")),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 10),
                    BorderBrush = seat.IsOccupied
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460")),
                    BorderThickness = new Thickness(2)
                };

                var seatGrid = new Grid();
                seatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                seatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Numéro de siège avec cercle
                var seatNumberBorder = new Border
                {
                    Width = 38,
                    Height = 38,
                    Background = seat.IsOccupied
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff88"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f3460")),
                    CornerRadius = new CornerRadius(19),
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                seatNumberBorder.Child = new TextBlock
                {
                    Text = $"{seat.SeatNumber}",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = seat.IsOccupied
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a2e"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0a0")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(seatNumberBorder, 0);
                seatGrid.Children.Add(seatNumberBorder);

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
                        FontSize = 17,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    if (seat.IsLocked)
                    {
                        playerStack.Children.Add(new TextBlock
                        {
                            Text = " 🔒",
                            FontSize = 15,
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
                        FontSize = 17,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0a0")),
                        FontStyle = FontStyles.Italic,
                        VerticalAlignment = VerticalAlignment.Center
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
}
