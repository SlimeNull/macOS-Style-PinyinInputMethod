using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CandidateWordLayout.Panels;
using CandidateWordLayout.ViewModels;

namespace CandidateWordLayout.Views;

public partial class MainWindow : Window
{
    private int? _preferredColumn;
    private bool _isVerticalNavigation;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnNavigationKeyDown, RoutingStrategies.Tunnel);
        ListBox.SelectionChanged += (_, _) =>
        {
            if (!_isVerticalNavigation)
                _preferredColumn = null;
        };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        ListBox.SelectedIndex = 0;
    }

    private void OnNavigationKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Up or Key.Down or Key.Left or Key.Right) ||
            e.KeyModifiers != KeyModifiers.None ||
            DataContext is not MainViewModel viewModel ||
            ListBox.ItemsPanelRoot is not CandidateWorldPanel candidateWorldPanel ||
            ListBox.Items.Count == 0)
            return;

        e.Handled = true;
        candidateWorldPanel.UpdateLayout();

        if (ListBox.SelectedIndex < 0)
        {
            ListBox.SelectedIndex = 0;
            return;
        }

        if (e.Key is Key.Left or Key.Right)
        {
            _preferredColumn = null;
            var offset = e.Key == Key.Left ? -1 : 1;
            ListBox.SelectedIndex = Math.Clamp(ListBox.SelectedIndex + offset, 0, ListBox.Items.Count - 1);
            if (ListBox.SelectedIndex >= candidateWorldPanel.SecondRowChildStartIndex)
            {
                viewModel.IsExpanded = true;
            }
            return;
        }

        if (!viewModel.IsExpanded)
        {
            if (e.Key == Key.Down)
                viewModel.IsExpanded = true;
            _preferredColumn = null;
            return;
        }

        if (e.Key == Key.Up && candidateWorldPanel.IsInFirstRow(ListBox.SelectedIndex))
        {
            viewModel.IsExpanded = false;
            _preferredColumn = null;
            return;
        }

        _isVerticalNavigation = true;
        try
        {
            ListBox.SelectedIndex = candidateWorldPanel.GetVerticalNeighbor(
                ListBox.SelectedIndex, e.Key == Key.Up ? -1 : 1, ref _preferredColumn);
        }
        finally
        {
            _isVerticalNavigation = false;
        }
    }
}