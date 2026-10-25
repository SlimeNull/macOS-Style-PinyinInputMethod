using System;
using Avalonia;
using Avalonia.Controls;

namespace CandidateWordLayout.Panels;

public class CandidateWorldPanel : Panel
{
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

    private Size LayoutCore(Size finalSize, bool measure, bool arrange)
    {
        var columnWidth = finalSize.Width / ColumnCount;

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

        if (ShrinkToOneRow)
        {
            return new Size(maxWidth, firstRowHeight);
        }
        
        return new Size(maxWidth, currentTop + currentLineHeight);
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
}