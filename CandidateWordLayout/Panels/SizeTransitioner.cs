using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace CandidateWordLayout.Panels;

public class SizeTransitioner : Decorator
{
    private static readonly StyledProperty<Size> AnimatedSizeProperty =
        AvaloniaProperty.Register<SizeTransitioner, Size>(nameof(AnimatedSize));

    private bool _hasMeasured;
    private Size _targetSize;
    private readonly SizeTransition _transition = new();

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<SizeTransitioner, TimeSpan>(
            nameof(Duration), TimeSpan.FromMilliseconds(200),
            validate: duration => duration >= TimeSpan.Zero);

    public static readonly StyledProperty<Easing> EasingProperty =
        AvaloniaProperty.Register<SizeTransitioner, Easing>(nameof(Easing), new CubicEaseOut());

    static SizeTransitioner()
    {
        AffectsMeasure<SizeTransitioner>(AnimatedSizeProperty);
    }

    public SizeTransitioner()
    {
        ClipToBounds = true;
    }

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public Easing Easing
    {
        get => GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    private Size AnimatedSize
    {
        get => GetValue(AnimatedSizeProperty);
        set => SetValue(AnimatedSizeProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredSize = base.MeasureOverride(availableSize);

        if (!_hasMeasured)
        {
            _targetSize = desiredSize;
            AnimatedSize = desiredSize;
            _transition.Property = AnimatedSizeProperty;
            _transition.Duration = Duration;
            _transition.Easing = Easing;
            Transitions = new Transitions { _transition };
            _hasMeasured = true;
        }
        else if (_targetSize != desiredSize)
        {
            _targetSize = desiredSize;
            _transition.Duration = Duration;
            _transition.Easing = Easing;
            AnimatedSize = desiredSize;
        }

        return AnimatedSize;
    }
}
