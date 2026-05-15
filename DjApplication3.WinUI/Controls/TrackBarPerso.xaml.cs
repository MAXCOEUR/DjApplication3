using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace DjApplication3.WinUI.Controls
{
    public sealed partial class TrackBarPerso : UserControl
    {
        private double _targetValue;
        private bool _isDragging;

        private readonly DispatcherTimer _animationTimer;

        public static readonly DependencyProperty BarHeightProperty =
            DependencyProperty.Register(
                nameof(BarHeight),
                typeof(double),
                typeof(TrackBarPerso),
                new PropertyMetadata(36d, OnBarHeightChanged));

        public event EventHandler<double>? ValueChanged;

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(TrackBarPerso),
                new PropertyMetadata(0d, OnValueChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(TrackBarPerso),
                new PropertyMetadata(0d, OnRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(TrackBarPerso),
                new PropertyMetadata(100d, OnRangeChanged));

        public static readonly DependencyProperty DefaultProperty =
            DependencyProperty.Register(
                nameof(Default),
                typeof(double),
                typeof(TrackBarPerso),
                new PropertyMetadata(50d));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Clamp(value, Minimum, Maximum));
        }

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Default
        {
            get => (double)GetValue(DefaultProperty);
            set => SetValue(DefaultProperty, value);
        }

        public double AnimationStep { get; set; } = 1d;

        public int AnimationIntervalMs
        {
            get => (int)_animationTimer.Interval.TotalMilliseconds;
            set => _animationTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, value));
        }

        public TrackBarPerso()
        {
            InitializeComponent();

            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };

            _animationTimer.Tick += AnimationTimer_Tick;

            Loaded += TrackBarPerso_Loaded;
            Unloaded += TrackBarPerso_Unloaded;
        }

        public double BarHeight
        {
            get => (double)GetValue(BarHeightProperty);
            set => SetValue(BarHeightProperty, value);
        }

        private void TrackBarPerso_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyBarHeight();
            UpdateThumbPosition();
        }

        private void TrackBarPerso_Unloaded(object sender, RoutedEventArgs e)
        {
            _animationTimer.Stop();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TrackBarPerso)d;

            var newValue = (double)e.NewValue;
            var clampedValue = Clamp(newValue, control.Minimum, control.Maximum);

            if (Math.Abs(newValue - clampedValue) > 0.001)
            {
                control.Value = clampedValue;
                return;
            }

            control.UpdateThumbPosition();
            control.ValueChanged?.Invoke(control, clampedValue);
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TrackBarPerso)d;

            if (control.Maximum < control.Minimum)
            {
                control.Maximum = control.Minimum;
                return;
            }

            control.Value = Clamp(control.Value, control.Minimum, control.Maximum);
            control.UpdateThumbPosition();
        }

        private static void OnBarHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TrackBarPerso)d;
            control.ApplyBarHeight();
        }

        private void ApplyBarHeight()
        {
            try
            {
                var h = Math.Max(8.0, BarHeight);
                this.MinHeight = h;

                if (BackgroundBar != null)
                {
                    BackgroundBar.Height = Math.Max(4.0, h / 2.0);
                }

                if (Thumb != null)
                {
                    Thumb.Height = Math.Max(10.0, h - 2.0);
                    // keep width as-is
                }

                UpdateThumbPosition();
            }
            catch
            {
                // swallow errors to avoid UI crashes
            }
        }

        private void Root_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(Root);

            if (point.Properties.IsRightButtonPressed)
            {
                AnimateTo(Default);
                e.Handled = true;
                return;
            }

            if (point.Properties.IsLeftButtonPressed)
            {
                var target = GetValueFromPointerPosition(point.Position.X);
                AnimateTo(target);
                e.Handled = true;
            }
        }

        private void Thumb_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(Root);

            if (!point.Properties.IsLeftButtonPressed)
            {
                return;
            }

            _animationTimer.Stop();
            _isDragging = true;

            Thumb.CapturePointer(e.Pointer);

            e.Handled = true;
        }

        private void Thumb_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            StopDragging();
            e.Handled = true;
        }

        private void Root_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging)
            {
                return;
            }

            var point = e.GetCurrentPoint(Root);

            if (!point.Properties.IsLeftButtonPressed)
            {
                StopDragging();
                return;
            }

            Value = GetValueFromPointerPosition(point.Position.X);
            e.Handled = true;
        }

        private void Root_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                StopDragging();
                e.Handled = true;
            }
        }

        private void Root_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(Root);

            if (!point.Properties.IsLeftButtonPressed)
            {
                StopDragging();
            }
        }

        private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateThumbPosition();
        }

        private void AnimateTo(double value)
        {
            _targetValue = Clamp(value, Minimum, Maximum);

            if (Math.Abs(Value - _targetValue) < 0.001)
            {
                return;
            }

            _animationTimer.Stop();
            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(object? sender, object e)
        {
            if (Math.Abs(Value - _targetValue) < 0.001)
            {
                Value = _targetValue;
                _animationTimer.Stop();
                return;
            }

            var step = Math.Max(0.1, AnimationStep);

            if (Value < _targetValue)
            {
                Value = Math.Min(Value + step, _targetValue);
            }
            else
            {
                Value = Math.Max(Value - step, _targetValue);
            }

            if (Math.Abs(Value - _targetValue) < 0.001)
            {
                Value = _targetValue;
                _animationTimer.Stop();
            }
        }

        private double GetValueFromPointerPosition(double pointerX)
        {
            var width = Root.ActualWidth;
            var thumbWidth = Thumb.ActualWidth;

            if (width <= thumbWidth || Maximum <= Minimum)
            {
                return Minimum;
            }

            var usableWidth = width - thumbWidth;
            var centeredX = pointerX - thumbWidth / 2.0;
            var ratio = centeredX / usableWidth;

            ratio = Math.Clamp(ratio, 0, 1);

            return Minimum + ratio * (Maximum - Minimum);
        }

        private void UpdateThumbPosition()
        {
            if (Root == null || Thumb == null || ThumbTransform == null)
            {
                return;
            }

            var width = Root.ActualWidth;
            var thumbWidth = Thumb.ActualWidth;

            if (width <= 0 || thumbWidth <= 0 || Maximum <= Minimum)
            {
                return;
            }

            var usableWidth = Math.Max(0, width - thumbWidth);
            var ratio = (Value - Minimum) / (Maximum - Minimum);

            ratio = Math.Clamp(ratio, 0, 1);

            ThumbTransform.X = ratio * usableWidth;
        }

        private void StopDragging()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            Thumb.ReleasePointerCaptures();
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}