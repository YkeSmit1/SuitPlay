using System.Collections.ObjectModel;
using Calculator;
using Calculator.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class Distributions2ViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] public partial ObservableCollection<DistributionItem> DistributionItems { get; set; }
    [ObservableProperty] public partial ObservableCollection<LineItem> LineItems { get; set; }
    [ObservableProperty] public partial ObservableCollection<int> PossibleNrOfTricks { get; set; }
    [ObservableProperty] public partial int RedItems { get; set; }
    [ObservableProperty] public partial int GreenItems { get; set; }
    [ObservableProperty] public partial int MinusOneItems { get; set; }
    [ObservableProperty] public partial string Combination { get; set; }


    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var result = (Result2)query["Result"];
        DistributionItems = new ObservableCollection<DistributionItem>(result.DistributionItems);
        LineItems = new ObservableCollection<LineItem>(result.LineItems.Where(x => !(bool)query["OnlyLinesInSuitPlay"] || x.LineInSuitPlay));
        PossibleNrOfTricks = new ObservableCollection<int>(result.PossibleNrOfTricks);
        GreenItems = LineItems.SelectMany(x => x.Items2).Count(x => x.Tricks.Length > 1);
        RedItems = LineItems.SelectMany(x => x.Items2).Count(x => x.IsDifferent);
        MinusOneItems = LineItems.SelectMany(x => x.Items2).Count(x => x.Tricks.First() == -1);
        Combination = $"{Utils.CardsToString(result.North)} - {Utils.CardsToString(result.South)}";
    }
}