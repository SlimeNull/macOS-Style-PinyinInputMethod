using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace CandidateWordLayout.Panels;

public class CandidateWorldPanel : Panel
{
    private readonly List<CandidatePosition> _positions = new();

    static CandidateWorldPanel()
    {
        AffectsMeasure<CandidateWorldPanel>(ColumnCountProperty);
        AffectsMeasure<CandidateWorldPanel>(ColumnAlignmentProperty);
        AffectsMeasure<CandidateWorldPanel>(ShrinkToOneRowProperty);
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
        if (arrange)
            _positions.Clear();

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
                child.Measure(finalSize);
            }

            if (currentLeft > 0 &&
                currentLeft + child.DesiredSize.Width > finalSize.Width)
            {
                maxWidth = Math.Max(maxWidth, currentLeft);

                currentLeft = 0;
                currentTop += currentLineHeight;
                row++;
                firstRowHeight = currentLineHeight;
                currentLineHeight = 0.0;

                if (SecondRowChildStartIndex == 0)
                {
                    SecondRowChildStartIndex = index;
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
                child.Arrange(new Rect(currentLeft, currentTop, desiredSize.Width, desiredSize.Height));
            }

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