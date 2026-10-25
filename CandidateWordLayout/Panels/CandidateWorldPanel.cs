using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CandidateWordLayout.Panels;

public class CandidateWorldPanel : Panel
{
    private readonly List<CandidatePosition> _positions = new();
    private readonly List<Rect> _rowBounds = new();
    private Size _layoutSize;

    public static readonly AttachedProperty<int> NumberProperty =
        AvaloniaProperty.RegisterAttached<CandidateWorldPanel, Control, int>("Number", 1);

    public static int GetNumber(Control control) => control.GetValue(NumberProperty);
    public static void SetNumber(Control control, int value) => control.SetValue(NumberProperty, value);

    public static readonly AttachedProperty<double> NumberOpacityProperty =
        AvaloniaProperty.RegisterAttached<CandidateWorldPanel, Control, double>("NumberOpacity");

    public static double GetNumberOpacity(Control control) => control.GetValue(NumberOpacityProperty);
    public static void SetNumberOpacity(Control control, double value) => control.SetValue(NumberOpacityProperty, value);

    public static readonly StyledProperty<double> SelectedRowCornerRadiusProperty =
        AvaloniaProperty.Register<CandidateWorldPanel, double>(nameof(SelectedRowCornerRadius), 12,
            validate: value => double.IsFinite(value) && value >= 0);

    public double SelectedRowCornerRadius
    {
        get => GetValue(SelectedRowCornerRadiusProperty);
        set => SetValue(SelectedRowCornerRadiusProperty, value);
    }

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<CandidateWorldPanel, int>(nameof(SelectedIndex), -1);

    public static readonly StyledProperty<IBrush?> SelectedRowBrushProperty =
        AvaloniaProperty.Register<CandidateWorldPanel, IBrush?>(nameof(SelectedRowBrush));

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public IBrush? SelectedRowBrush
    {
        get => GetValue(SelectedRowBrushProperty);
        set => SetValue(SelectedRowBrushProperty, value);
    }

    static CandidateWorldPanel()
    {
        AffectsMeasure<CandidateWorldPanel>(ColumnCountProperty);
        AffectsMeasure<CandidateWorldPanel>(ColumnAlignmentProperty);
        AffectsMeasure<CandidateWorldPanel>(ShrinkToOneRowProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty ||
            change.Property == SelectedRowBrushProperty ||
            change.Property == SelectedRowCornerRadiusProperty ||
            change.Property == ShrinkToOneRowProperty)
            UpdateRowBackground();
    }

    private void UpdateRowBackground()
    {
        var selectedRow = SelectedIndex >= 0 && SelectedIndex < _positions.Count
            ? _positions[SelectedIndex].Row
            : -1;
        for (var index = 0; index < Children.Count; index++)
        {
            SetNumberOpacity(Children[index],
                index < _positions.Count && _positions[index].Row == selectedRow ? 1 : 0);
        }

        if (ShrinkToOneRow || SelectedIndex < 0 || SelectedIndex >= _positions.Count ||
            SelectedRowBrush is null || _positions[SelectedIndex].Row >= _rowBounds.Count)
        {
            SetCurrentValue(BackgroundProperty, null);
            return;
        }

        var drawing = new DrawingGroup();
        // Include the full panel bounds so the brush does not stretch the selected row.
        drawing.Children.Add(new GeometryDrawing
        {
            Brush = Brushes.Transparent,
            Geometry = new RectangleGeometry(new Rect(_layoutSize))
        });
        drawing.Children.Add(new GeometryDrawing
        {
            Brush = SelectedRowBrush,
            Geometry = new RectangleGeometry(_rowBounds[_positions[SelectedIndex].Row])
            {
                RadiusX = SelectedRowCornerRadius,
                RadiusY = SelectedRowCornerRadius
            }
        });
        SetCurrentValue(BackgroundProperty, new DrawingBrush { Drawing = drawing });
    }
    
    public int ColumnCount
    {
        get => GetValue(ColumnCountProperty);
        set => SetValue(ColumnCountProperty, value);
    }

    public bool ColumnAlignment
    {
        get => GetValue(ColumnAlignmentProperty);
        set => SetValue(ColumnAlignmentProperty, value);
    }

    public bool ShrinkToOneRow
    {
        get => GetValue(ShrinkToOneRowProperty);
        set => SetValue(ShrinkToOneRowProperty, value);
    }

    public int SecondRowChildStartIndex { get; private set; }

    public bool IsInFirstRow(int index) =>
        index >= 0 && index < _positions.Count && _positions[index].Row == 0;

    /// <summary>
    /// Finds the nearest column in the target row. Keep preferredColumn between vertical moves
    /// to preserve the column through spanning items; reset it for a new navigation sequence.
    /// </summary>
    public int GetVerticalNeighbor(int index, int rowOffset, ref int? preferredColumn)
    {
        if (index < 0 || index >= _positions.Count)
            return index;

        var position = _positions[index];
        preferredColumn ??= position.Column;
        var targetRow = position.Row + rowOffset;
        var nearestIndex = index;
        var nearestDistance = int.MaxValue;

        for (var i = 0; i < _positions.Count; i++)
        {
            var candidate = _positions[i];
            if (candidate.Row != targetRow)
                continue;

            var distance = Math.Max(0, Math.Max(
                candidate.Column - preferredColumn.Value,
                preferredColumn.Value - (candidate.Column + candidate.ColumnSpan - 1)));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private Size LayoutCore(Size finalSize, bool measure, bool arrange)
    {
        var columnWidth = finalSize.Width / ColumnCount;
        var row = 0;
        var number = 1;
        if (arrange)
        {
            _positions.Clear();
            _rowBounds.Clear();
        }

        var currentLeft = 0.0;
        var currentTop = 0.0;
        var maxWidth = 0.0;
        var currentLineHeight = 0.0;
        
        var firstRowHeight = 0.0;

        SecondRowChildStartIndex = 0;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            if (measure)
            {
                SetNumber(child, number);
                child.Measure(finalSize);
            }

            if (currentLeft > 0 &&
                currentLeft + child.DesiredSize.Width > finalSize.Width)
            {
                maxWidth = Math.Max(maxWidth, currentLeft);

                currentLeft = 0;
                currentTop += currentLineHeight;
                row++;
                number = 1;
                firstRowHeight = currentLineHeight;
                currentLineHeight = 0.0;

                if (SecondRowChildStartIndex == 0)
                {
                    SecondRowChildStartIndex = index;
                }

                if (measure)
                {
                    SetNumber(child, number);
                    child.Measure(finalSize);
                }
            }

            var desiredSize = child.DesiredSize;

            if (arrange)
            {
                var column = 0;
                var columnSpan = 1;
                if (double.IsFinite(columnWidth) && columnWidth > 0)
                {
                    column = (int)Math.Floor(currentLeft / columnWidth + 1e-8);
                    columnSpan = Math.Max(1, (int)Math.Ceiling(desiredSize.Width / columnWidth - 1e-8));
                }
                _positions.Add(new CandidatePosition(row, column, columnSpan));
                if (_rowBounds.Count == row)
                    _rowBounds.Add(new Rect(0, currentTop, finalSize.Width, desiredSize.Height));
                else
                    _rowBounds[row] = new Rect(0, currentTop, finalSize.Width,
                        Math.Max(_rowBounds[row].Height, desiredSize.Height));
                child.Arrange(new Rect(currentLeft, currentTop, desiredSize.Width, desiredSize.Height));
            }

            number++;
            currentLeft += desiredSize.Width;
            currentLineHeight = Math.Max(currentLineHeight, desiredSize.Height);

            if (ColumnAlignment)
            {
                currentLeft = Math.Ceiling(currentLeft / columnWidth) * columnWidth;
            }
        }

        maxWidth = Math.Max(maxWidth, currentLeft);
        if (firstRowHeight == 0.0)
        {
            firstRowHeight = currentLineHeight;
        }

        if (SecondRowChildStartIndex > 0)
        {
            maxWidth = Math.Max(finalSize.Width, maxWidth);
        }
        
        return ShrinkToOneRow ? 
            new Size(maxWidth, firstRowHeight) : 
            new Size(maxWidth, currentTop + currentLineHeight);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = LayoutCore(availableSize, true, false);
        return new Size(
            Math.Min(availableSize.Width, size.Width),
            Math.Min(availableSize.Height, size.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = LayoutCore(finalSize, false, true);
        _layoutSize = finalSize;
        UpdateRowBackground();
        return finalSize;
    }

    public static readonly StyledProperty<int> ColumnCountProperty =
        AvaloniaProperty.Register<CandidateWorldPanel, int>(nameof(ColumnCount), 1);
    
    public static readonly StyledProperty<bool> ColumnAlignmentProperty =
        AvaloniaProperty.Register<CandidateWorldPanel, bool>(nameof(ColumnAlignment), false);
    
    public static readonly StyledProperty<bool> ShrinkToOneRowProperty =
        AvaloniaProperty.Register<CandidateWorldPanel, bool>(nameof(ShrinkToOneRow), false);

    private readonly record struct CandidatePosition(int Row, int Column, int ColumnSpan);
}