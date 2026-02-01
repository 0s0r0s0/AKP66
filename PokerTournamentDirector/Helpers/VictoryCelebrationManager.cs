using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PokerTournamentDirector.Helpers
{
    public class VictoryCelebrationManager : IDisposable
    {
        private readonly Canvas _confettiCanvas;
        private readonly double _canvasWidth;
        private readonly double _canvasHeight;
        private readonly Random _random = new();
        private readonly DispatcherTimer _cleanupTimer;
        private MediaPlayer? _victoryPlayer;

        private const int CONFETTI_PER_WAVE = 150;
        private const int MAX_CONFETTI = 600;

        public VictoryCelebrationManager(Canvas confettiCanvas, double width, double height)
        {
            _confettiCanvas = confettiCanvas;
            _canvasWidth = width;
            _canvasHeight = height;

            _cleanupTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _cleanupTimer.Tick += (s, e) => CleanupOldConfetti();
        }

        public async Task StartCelebrationAsync()
        {
            PlayVictorySound();
            _cleanupTimer.Start();

            await LaunchConfettiWaveAsync();
            await Task.Delay(800);
            await LaunchConfettiWaveAsync();
            await Task.Delay(1000);
            await LaunchConfettiWaveAsync();

            await Task.Delay(15000);
            _cleanupTimer.Stop();
            _confettiCanvas.Children.Clear();
        }

        private Task LaunchConfettiWaveAsync()
        {
            return Task.Run(() =>
            {
                _confettiCanvas.Dispatcher.Invoke(() =>
                {
                    if (_confettiCanvas.Children.Count >= MAX_CONFETTI) return;

                    for (int i = 0; i < CONFETTI_PER_WAVE; i++)
                    {
                        var confetti = CreateConfetti();
                        Canvas.SetLeft(confetti, _random.NextDouble() * _canvasWidth);
                        Canvas.SetTop(confetti, -_random.Next(50, 200));
                        Canvas.SetZIndex(confetti, 999);
                        _confettiCanvas.Children.Add(confetti);
                        AnimateConfetti(confetti);
                    }
                });
            });
        }

        private Shape CreateConfetti()
        {
            Shape shape = _random.Next(3) switch
            {
                0 => new Rectangle { Width = _random.Next(6, 14), Height = _random.Next(12, 24), RadiusX = 3, RadiusY = 3 },
                1 => new Ellipse { Width = _random.Next(8, 16), Height = _random.Next(8, 16) },
                _ => CreateTriangle()
            };

            shape.Fill = new SolidColorBrush(GetRandomColor());
            shape.Opacity = 0.7 + _random.NextDouble() * 0.3;
            shape.RenderTransformOrigin = new Point(0.5, 0.5);
            shape.RenderTransform = new TransformGroup
            {
                Children = { new RotateTransform(), new TranslateTransform(), new ScaleTransform(1, 1) }
            };

            return shape;
        }

        private Polygon CreateTriangle()
        {
            var size = _random.Next(8, 16);
            return new Polygon
            {
                Points = new PointCollection
                {
                    new Point(0, size),
                    new Point(size / 2, 0),
                    new Point(size, size)
                }
            };
        }

        private Color GetRandomColor()
        {
            var colors = new[]
            {
                Color.FromRgb(255, 215, 0), Color.FromRgb(0, 255, 136), Color.FromRgb(255, 0, 255),
                Color.FromRgb(0, 255, 255), Color.FromRgb(255, 69, 96), Color.FromRgb(255, 165, 0),
                Color.FromRgb(138, 43, 226), Color.FromRgb(255, 255, 0), Color.FromRgb(0, 255, 127),
                Color.FromRgb(255, 20, 147)
            };
            return colors[_random.Next(colors.Length)];
        }

        private void AnimateConfetti(Shape confetti)
        {
            var duration = TimeSpan.FromSeconds(_random.Next(3, 7));
            var storyboard = new Storyboard();

            // Chute
            storyboard.Children.Add(CreateAnimation(confetti, "(Canvas.Top)", _canvasHeight + 100, duration,
                new QuadraticEase { EasingMode = EasingMode.EaseIn }));

            // Rotation
            storyboard.Children.Add(CreateAnimation(confetti,
                "(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)",
                _random.Next(720, 1440) * (_random.NextDouble() > 0.5 ? 1 : -1), duration, null));

            // Zigzag
            var zigzag = CreateAnimation(confetti,
                "(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)",
                60, TimeSpan.FromSeconds(1.2), new SineEase { EasingMode = EasingMode.EaseInOut });
            zigzag.From = -60;
            zigzag.AutoReverse = true;
            zigzag.RepeatBehavior = RepeatBehavior.Forever;
            storyboard.Children.Add(zigzag);

            // Fade out
            var fade = CreateAnimation(confetti, "Opacity", 0, TimeSpan.FromSeconds(1), null);
            fade.BeginTime = TimeSpan.FromSeconds(duration.TotalSeconds - 1);
            storyboard.Children.Add(fade);

            storyboard.Completed += (s, e) => _confettiCanvas.Children.Remove(confetti);
            storyboard.Begin();
        }

        private DoubleAnimation CreateAnimation(DependencyObject target, string property, double to,
            TimeSpan duration, IEasingFunction? easing)
        {
            var anim = new DoubleAnimation { To = to, Duration = duration, EasingFunction = easing };
            Storyboard.SetTarget(anim, target);
            Storyboard.SetTargetProperty(anim, new PropertyPath(property));
            return anim;
        }

        private void CleanupOldConfetti()
        {
            var toRemove = _confettiCanvas.Children.OfType<Shape>()
                .Where(c => Canvas.GetTop(c) > _canvasHeight + 200).ToList();
            foreach (var c in toRemove) _confettiCanvas.Children.Remove(c);
        }

        private void PlayVictorySound()
        {
            try
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "bravo.mp3");
                if (!File.Exists(path)) return;

                _victoryPlayer?.Close();
                _victoryPlayer = new MediaPlayer { Volume = 0.7 };
                _victoryPlayer.Open(new Uri(path, UriKind.Absolute));
                _victoryPlayer.Play();
            }
            catch { }
        }

        public void Dispose()
        {
            _cleanupTimer?.Stop();
            _victoryPlayer?.Close();
            _confettiCanvas?.Children.Clear();
        }
    }
}