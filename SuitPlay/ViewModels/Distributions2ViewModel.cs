using System.Collections.ObjectModel;
using Calculator;
using Calculator.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class Distributions2ViewModel : ObservableObject, IQueryAttributable
{
    private readonly ArrayEqualityComparer<Face> arrayEqualityComparer = new();
    [ObservableProperty] public partial bool DeveloperMode { get; set; } = Preferences.Get("DeveloperMode", true);
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
        var onlyLinesInSuitPlay = DeveloperMode && Preferences.Get("OnlyLinesInSuitPlay", true);
        var onlyCombinationsInSuitPlay = DeveloperMode && Preferences.Get("OnlyCombinationsInSuitPlay", true);
        var maxLinesInDistributions = Preferences.Get("MaxLinesInDistributions", 5);
        DistributionItems = new ObservableCollection<DistributionItem>(result.DistributionItems.Where(x =>
            !onlyCombinationsInSuitPlay || result.CombinationsInSuitPlay.Contains(x.East, arrayEqualityComparer)));
        var lineItems = DeveloperMode
            ? result.LineItems.Where(x => !onlyLinesInSuitPlay || x.LineInSuitPlay)
            : result.LineItems.GroupBy(x => x.Line.First()).Select(x => x.MaxBy(y => y.Average)).ToList();
        var lineItemsOrdered = lineItems
            .OrderByDescending(x => x.Average).Take(maxLinesInDistributions).ToList();
        var cardsNS = result.North.Concat(result.South).OrderDescending().ToList();
        if (onlyCombinationsInSuitPlay)
        {
            foreach (var lineItem in lineItemsOrdered)
            {
                lineItem.Items2 = lineItem.Items2.Where(x => result.CombinationsInSuitPlay.Contains(x.Combination.ConvertToSmallCards(cardsNS),
                        arrayEqualityComparer)).ToList();
            }
        }

        LineItems = new ObservableCollection<LineItem>(lineItemsOrdered);
        PossibleNrOfTricks = new ObservableCollection<int>(result.PossibleNrOfTricks);
        GreenItems = LineItems.SelectMany(x => x.Items2).Count(x => x.Tricks.Length > 1);
        RedItems = LineItems.SelectMany(x => x.Items2).Count(x => x.IsDifferent);
        MinusOneItems = LineItems.SelectMany(x => x.Items2).Count(x => x.Tricks.First() == -1);
        Combination = $"{Utils.CardsToString(result.North)} - {Utils.CardsToString(result.South)}";
    }
}