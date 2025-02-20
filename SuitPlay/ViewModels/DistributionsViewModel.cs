using System.Collections.ObjectModel;
using Calculator;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class DistributionsViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] private ObservableCollection<DistributionItem> distributionItems;
    [ObservableProperty] private ObservableCollection<List<Face>> allPlays;
    [ObservableProperty] private ObservableCollection<PlayItem> playItems;
    [ObservableProperty] private ObservableCollection<int> possibleNrOfTricks;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var result = (Calculate.Result)query["Result"];
        DistributionItems = new ObservableCollection<DistributionItem>(result.DistributionList);
        AllPlays = new ObservableCollection<List<Face>>(result.AllPlays.Where(x => x.Count == 3));
        PlayItems = new ObservableCollection<PlayItem>(result.PlayList.Where(x => x.Play.Count == 3));
        PossibleNrOfTricks = new ObservableCollection<int>(result.PossibleNrOfTricks);
    }
}