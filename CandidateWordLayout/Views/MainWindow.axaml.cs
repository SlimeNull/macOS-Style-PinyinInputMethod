using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CandidateWordLayout.Panels;
using CandidateWordLayout.ViewModels;

namespace CandidateWordLayout.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        ListBox.SelectedIndex = 0;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext!;
        var candidateWorldPanel = (CandidateWorldPanel)ListBox.ItemsPanelRoot!;

        if (e.Key == Key.Down)
        {
            if (!viewModel.IsExpanded)
            {
                viewModel.IsExpanded = true;
            }
        }

        if (e.Key == Key.Up)
        {
            if (viewModel.IsExpanded && ListBox.SelectedIndex < candidateWorldPanel.SecondRowChildStartIndex)
            {
                viewModel.IsExpanded = false;
            }
        }

        if (e.Key == Key.Left)
        {
            ListBox.SelectedIndex = Math.Max(ListBox.SelectedIndex - 1, 0);
        }

        if (e.Key == Key.Right)
        {
            ListBox.SelectedIndex = Math.Min(ListBox.SelectedIndex + 1, ListBox.Items.Count - 1);
        }
        
        base.OnKeyDown(e);
    }
}