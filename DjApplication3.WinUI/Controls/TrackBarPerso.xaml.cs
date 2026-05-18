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

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Microsoft.UI.Xaml.Controls.Orientation),
                typeof(TrackBarPerso),
                new PropertyMetadata(Microsoft.UI.Xaml.Controls.Orientation.Horizontal, OnOrientationChanged));

        public static readonly DependencyProperty IsDirectionReversedProperty =
            DependencyProperty.Register(
                nameof(IsDirectionReversed),
                typeof(bool),
                typeof(TrackBarPerso),
                new PropertyMetadata(false, OnDirectionChanged));

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

        public Microsoft.UI.Xaml.Controls.Orientation Orientation
        {
            get => (Microsoft.UI.Xaml.Controls.Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public bool IsDirectionReversed
        {
            get => (bool)GetValue(IsDirectionReversedProperty);
            set => SetValue(IsDirectionReversedProperty, value);
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

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TrackBarPerso)d;
            control.ApplyBarHeight();
        }

        private static void OnDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TrackBarPerso)d;
            control.UpdateThumbPosition();
        }

        private void ApplyBarHeight()
        {
            try
            {
                var h = Math.Max(8.0, BarHeight);
                var isVertical = Orientation == Microsoft.UI.Xaml.Controls.Orientation.Vertical;
                var barThickness = Math.Max(4.0, h / 2.0);
                var thumbCrossSize = Math.Max(10.0, h - 2.0);

                MinHeight = isVertical ? 0 : h;
                MinWidth = isVertical ? h : 0;

                if (BackgroundBar != null)
                {
                    BackgroundBar.Width = isVertical ? barThickness : double.NaN;
                    BackgroundBar.Height = isVertical ? double.NaN : barThickness;
                    BackgroundBar.HorizontalAlignment = isVertical ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
                    BackgroundBar.VerticalAlignment = isVertical ? VerticalAlignment.Stretch : VerticalAlignment.Center;
                    BackgroundBar.CornerRadius = new CornerRadius(barThickness / 2.0);
                }

                if (Thumb != null)
                {
                    Thumb.Width = isVertical ? thumbCrossSize : 24.0;
                    Thumb.Height = isVertical ? 24.0 : thumbCrossSize;
                    Thumb.HorizontalAlignment = isVertical ? HorizontalAlignment.Center : HorizontalAlignment.Left;
                    Thumb.VerticalAlignment = isVertical ? VerticalAlignment.Top : VerticalAlignment.Center;
                }

                CenterLineVertical.Visibility = isVertical ? Visibility.Collapsed : Visibility.Visible;
                CenterLineHorizontal.Visibility = isVertical ? Visibility.Visible : Visibility.Collapsed;
                ThumbGripVertical.Visibility = isVertical ? Visibility.Collapsed : Visibility.Visible;
                ThumbGripHorizontal.Visibility = isVertical ? Visibility.Visible : Visibility.Collapsed;

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
                var target = GetValueFromPointerPosition(point.Position.X, point.Position.Y);
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

            Value = GetValueFromPointerPosition(point.Position.X, point.Position.Y);
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

        private double GetValueFromPointerPosition(double pointerX, double pointerY)
        {
            if (Orientation == Microsoft.UI.Xaml.Controls.Orientation.Vertical)
            {
                var height = Root.ActualHeight;
                var thumbHeight = Thumb.ActualHeight;

                if (height <= thumbHeight || Maximum <= Minimum)
                {
                    return Minimum;
                }

                var usableHeight = height - thumbHeight;
                var centeredY = pointerY - thumbHeight / 2.0;
                var verticalRatio = 1.0 - centeredY / usableHeight;

                verticalRatio = Math.Clamp(verticalRatio, 0, 1);

                if (IsDirectionReversed)
                {
                    verticalRatio = 1.0 - verticalRatio;
                }

                return Minimum + verticalRatio * (Maximum - Minimum);
            }

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

            if (IsDirectionReversed)
            {
                ratio = 1.0 - ratio;
            }

            return Minimum + ratio * (Maximum - Minimum);
        }

        private void UpdateThumbPosition()
        {
            if (Root == null || Thumb == null || ThumbTransform == null)
            {
                return;
            }

            var width = Root.ActualWidth;
            var height = Root.ActualHeight;
            var thumbWidth = Thumb.ActualWidth;
            var thumbHeight = Thumb.ActualHeight;

            if (width <= 0 || height <= 0 || thumbWidth <= 0 || thumbHeight <= 0 || Maximum <= Minimum)
            {
                return;
            }

            var ratio = (Value - Minimum) / (Maximum - Minimum);

            ratio = Math.Clamp(ratio, 0, 1);

            if (IsDirectionReversed)
            {
                ratio = 1.0 - ratio;
            }

            if (Orientation == Microsoft.UI.Xaml.Controls.Orientation.Vertical)
            {
                var usableHeight = Math.Max(0, height - thumbHeight);
                ThumbTransform.X = 0;
                ThumbTransform.Y = (1.0 - ratio) * usableHeight;
                return;
            }

            var usableWidth = Math.Max(0, width - thumbWidth);
            ThumbTransform.X = ratio * usableWidth;
            ThumbTransform.Y = 0;
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
