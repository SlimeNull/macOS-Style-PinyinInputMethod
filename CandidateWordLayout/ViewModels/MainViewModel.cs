using System;
using System.Collections.ObjectModel;
using CandidateWordLayout.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CandidateWordLayout.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    public ObservableCollection<CandidateWord> CandidateWords { get; } = [];
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ShowFirstRowOnly))]
    public partial bool IsExpanded { get; set; } = false;
    
    public bool ShowFirstRowOnly => !IsExpanded;

    public MainViewModel()
    {
        ReadOnlySpan<string> words =
        [
            "水杉输入法", "水杉输入发", "水杉输入阀", "水杉输入乏", "水杉输入罚", "水杉",
            "谁", "水", "睡", "税", "说", "帨",
            "税", "氺", "涗", "涚", "脽", "誰",
            "裞", "祱", "㥨", "㽷", "䬽", "䭨",
            "娷", "捝", "氵", "説", "閖", "𠻜"
        ];

        for (int i = 0; i < words.Length; i++)
        {
            CandidateWords.Add(new CandidateWord()
            {
                Index = i,
                Word = words[i]
            });
        }
    }
}