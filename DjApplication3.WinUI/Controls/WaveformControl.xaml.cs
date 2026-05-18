using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DjApplication3.WinUI.Controls
{
    public sealed partial class WaveformControl : UserControl
    {
        public event EventHandler<double>? SeekRequested;

        private CancellationTokenSource? _renderCts;
        private int _renderVersion;
        private bool _isLoaded;
        private bool _isRendering;

        public WaveformControl()
        {
            InitializeComponent();

            ApplyNormalBackground();

            _endWarningBlinkTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(450)
            };

            _endWarningBlinkTimer.Tick += (_, _) =>
            {
                _blinkState = !_blinkState;
                Root.Background = _blinkState ? _warningBackgroundBrush : WaveformBackground;
            };

            Loaded += (_, _) =>
            {
                _isLoaded = true;
                RequestRender();
                UpdateMarker();
            };

            Unloaded += (_, _) =>
            {
                _isLoaded = false;

                _renderCts?.Cancel();
                _renderCts?.Dispose();
                _renderCts = null;

                _endWarningBlinkTimer.Stop();
                ApplyNormalBackground();
            };
        }

        public static readonly DependencyProperty WaveformProperty = DependencyProperty.Register(
            nameof(Waveform),
            typeof(sbyte[]),
            typeof(WaveformControl),
            new PropertyMetadata(Array.Empty<sbyte>(), OnWaveformChanged));

        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            nameof(Position),
            typeof(float),
            typeof(WaveformControl),
            new PropertyMetadata(0f, OnPositionChanged));

        public static readonly DependencyProperty EndWarningActiveProperty = DependencyProperty.Register(
            nameof(EndWarningActive),
            typeof(bool),
            typeof(WaveformControl),
            new PropertyMetadata(false, OnEndWarningActiveChanged));

        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
            nameof(IsLoading),
            typeof(bool),
            typeof(WaveformControl),
            new PropertyMetadata(false, OnIsLoadingChanged));

        public static readonly DependencyProperty WaveformBackgroundProperty = DependencyProperty.Register(
            nameof(WaveformBackground),
            typeof(Brush),
            typeof(WaveformControl),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 32, 36, 39)), OnWaveformBackgroundChanged));

        public bool EndWarningActive
        {
            get => (bool)GetValue(EndWarningActiveProperty);
            set => SetValue(EndWarningActiveProperty, value);
        }

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public Brush WaveformBackground
        {
            get => (Brush)GetValue(WaveformBackgroundProperty);
            set => SetValue(WaveformBackgroundProperty, value);
        }

        private static void OnEndWarningActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaveformControl)d).UpdateEndWarningState();
        }

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaveformControl)d).UpdateLoadingOverlay();
        }

        private static void OnWaveformBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaveformControl)d).ApplyNormalBackground();
        }

        private readonly DispatcherTimer _endWarningBlinkTimer;
        private bool _blinkState;

        private readonly SolidColorBrush _warningBackgroundBrush =
            new(Color.FromArgb(255, 90, 0, 0));
        public sbyte[] Waveform
        {
            get => (sbyte[])GetValue(WaveformProperty);
            set => SetValue(WaveformProperty, value);
        }

        public float Position
        {
            get => (float)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        private static void OnWaveformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaveformControl)d).RequestRender();
        }

        private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaveformControl)d).UpdateMarker();
        }

        private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RequestRender();
            UpdateMarker();
        }

        private void Root_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var width = Root.ActualWidth;

            if (width <= 0)
            {
                return;
            }

            var point = e.GetCurrentPoint(Root).Position;
            SeekRequested?.Invoke(this, Math.Clamp(point.X / width, 0, 1));
        }

        private void RequestRender()
        {
            if (!_isLoaded)
            {
                return;
            }

            var width = (int)Math.Ceiling(Root.ActualWidth);
            var height = (int)Math.Ceiling(Root.ActualHeight);

            if (Waveform == null || Waveform.Length == 0 || width < 2 || height < 2)
            {
                _renderCts?.Cancel();
                _isRendering = false;
                WaveImage.Source = null;
                UpdateLoadingOverlay();
                return;
            }

            _renderCts?.Cancel();
            _renderCts?.Dispose();

            var cts = new CancellationTokenSource();
            _renderCts = cts;

            var version = Interlocked.Increment(ref _renderVersion);
            var waveformSnapshot = Waveform.ToArray();

            _isRendering = true;
            UpdateLoadingOverlay();

            _ = RenderWaveformAsync(waveformSnapshot, width, height, version, cts.Token);
        }

        private async Task RenderWaveformAsync(
            sbyte[] waveform,
            int width,
            int height,
            int version,
            CancellationToken token)
        {
            try
            {
                // Petit délai pour éviter de recalculer 20 fois pendant un resize.
                await Task.Delay(80, token);

                // Là, le calcul lourd est hors thread UI.
                var pixels = await Task.Run(
                    () => BuildWaveformPixels(waveform, width, height, token),
                    token);

                if (token.IsCancellationRequested || version != _renderVersion)
                {
                    return;
                }

                // À partir d’ici, on revient sur l’UI uniquement pour poser l’image.
                var bitmap = new WriteableBitmap(width, height);

                using (Stream stream = bitmap.PixelBuffer.AsStream())
                {
                    stream.Write(pixels, 0, pixels.Length);
                }

                bitmap.Invalidate();

                if (token.IsCancellationRequested || version != _renderVersion)
                {
                    return;
                }

                WaveImage.Source = bitmap;
                _isRendering = false;
                UpdateLoadingOverlay();
                UpdateMarker();
            }
            catch (OperationCanceledException)
            {
                // Normal si une autre musique arrive ou si la taille change.
            }
            catch
            {
                if (version == _renderVersion)
                {
                    _isRendering = false;
                    UpdateLoadingOverlay();
                }
            }
        }

        private void UpdateLoadingOverlay()
        {
            LoadingOverlay.Visibility = IsLoading || _isRendering
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private static byte[] BuildWaveformPixels(
            sbyte[] waveform,
            int width,
            int height,
            CancellationToken token)
        {
            var pixels = new byte[width * height * 4];

            if (waveform.Length == 0)
            {
                return pixels;
            }

            var center = (height - 1) / 2.0;
            var halfHeight = Math.Max(1, center);
            var stride = width * 4;
            var centerY = Math.Clamp((int)Math.Round(center), 0, height - 1);

            for (var x = 0; x < width; x++)
            {
                if ((x & 63) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                var startIndex = (int)Math.Floor((double)x / width * waveform.Length);
                var endIndex = (int)Math.Floor((double)(x + 1) / width * waveform.Length);

                startIndex = Math.Clamp(startIndex, 0, waveform.Length - 1);
                endIndex = Math.Clamp(endIndex, startIndex + 1, waveform.Length);

                double amplitudeSum = 0;
                var sampleCount = 0;

                for (var i = startIndex; i < endIndex; i++)
                {
                    amplitudeSum += Math.Abs(waveform[i]);
                    sampleCount++;
                }

                var amplitude = sampleCount == 0
                    ? 0
                    : Math.Clamp(amplitudeSum / sampleCount / 100.0, 0.0, 1.0);

                if (amplitude < 0.015)
                {
                    var silentIndex = centerY * stride + x * 4;
                    pixels[silentIndex + 0] = 120;
                    pixels[silentIndex + 1] = 132;
                    pixels[silentIndex + 2] = 142;
                    pixels[silentIndex + 3] = 95;
                    continue;
                }

                var visualAmplitude = Math.Pow(amplitude, 0.65);
                var barHalfHeight = Math.Max(1, visualAmplitude * halfHeight);
                var yTop = (int)Math.Round(center - barHalfHeight);
                var yBottom = (int)Math.Round(center + barHalfHeight);

                yTop = Math.Clamp(yTop, 0, height - 1);
                yBottom = Math.Clamp(yBottom, 0, height - 1);

                if (yBottom < yTop)
                {
                    (yTop, yBottom) = (yBottom, yTop);
                }

                for (var y = yTop; y <= yBottom; y++)
                {
                    var index = y * stride + x * 4;

                    // BGRA
                    pixels[index + 0] = 255; // Blue
                    pixels[index + 1] = 255; // Green
                    pixels[index + 2] = 255; // Red
                    pixels[index + 3] = 225; // Alpha
                }
            }

            return pixels;
        }

        private void UpdateMarker()
        {
            var width = Root.ActualWidth;
            var height = Root.ActualHeight;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            PositionMarker.Height = height;

            var markerX = Math.Clamp(Position, 0f, 1f) * Math.Max(0, width - PositionMarker.Width);
            PositionMarkerTransform.X = markerX;
        }

        private void UpdateEndWarningState()
        {
            if (EndWarningActive)
            {
                if (!_endWarningBlinkTimer.IsEnabled)
                {
                    _blinkState = false;
                    _endWarningBlinkTimer.Start();
                }
            }
            else
            {
                _endWarningBlinkTimer.Stop();
                _blinkState = false;
                ApplyNormalBackground();
            }
        }

        private void ApplyNormalBackground()
        {
            Root.Background = WaveformBackground;
        }
    }
}
