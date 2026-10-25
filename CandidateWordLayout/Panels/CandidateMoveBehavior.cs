using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;

namespace CandidateWordLayout.Panels;

public class CandidateMoveBehavior : Behavior<Panel>
{
    private readonly Dictionary<Control, MoveState> _states = new();

    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<CandidateMoveBehavior, TimeSpan>(
            nameof(Duration), TimeSpan.FromMilliseconds(300),
            validate: duration => duration >= TimeSpan.Zero);

    public static readonly StyledProperty<Easing> EasingProperty =
        AvaloniaProperty.Register<CandidateMoveBehavior, Easing>(nameof(Easing), new CubicEaseOut());

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

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is { } panel)
            panel.LayoutUpdated += OnLayoutUpdated;
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is { } panel)
            panel.LayoutUpdated -= OnLayoutUpdated;

        foreach (var (child, state) in _states)
            child.RenderTransform = state.OriginalTransform;
        _states.Clear();
        base.OnDetaching();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (AssociatedObject is not { } panel)
            return;

        foreach (var child in new List<Control>(_states.Keys))
        {
            if (!panel.Children.Contains(child))
            {
                child.RenderTransform = _states[child].OriginalTransform;
                _states.Remove(child);
            }
        }

        foreach (var child in panel.Children)
        {
            var position = child.Bounds.Position;
            if (!_states.TryGetValue(child, out var state))
            {
                state = new MoveState(position, child.RenderTransform);
                var group = new TransformGroup();
                if (state.OriginalTransform is { } original)
                    group.Children.Add(original as Transform ?? new MatrixTransform(original.Value));
                group.Children.Add(state.Offset);
                child.RenderTransform = group;
                _states.Add(child, state);
                continue;
            }

            if (position == state.Position)
                continue;

            // Bounds excludes RenderTransform, so animation frames cannot feed back into layout deltas.
            var offset = new Vector(state.Offset.X, state.Offset.Y) + state.Position - position;
            state.Offset.Transitions = null;
            state.Offset.X = offset.X;
            state.Offset.Y = offset.Y;
            state.Offset.Transitions = new Transitions
            {
                new DoubleTransition { Property = TranslateTransform.XProperty, Duration = Duration, Easing = Easing },
                new DoubleTransition { Property = TranslateTransform.YProperty, Duration = Duration, Easing = Easing }
            };
            state.Offset.X = 0;
            state.Offset.Y = 0;
            state.Position = position;
        }
    }

    private sealed class MoveState(Point position, ITransform? originalTransform)
    {
        public Point Position { get; set; } = position;
        public ITransform? OriginalTransform { get; } = originalTransform;
        public TranslateTransform Offset { get; } = new();
    }
}
