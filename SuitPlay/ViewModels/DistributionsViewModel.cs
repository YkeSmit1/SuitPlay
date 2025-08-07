using System.Collections.ObjectModel;
using Calculator;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class DistributionsViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] public partial ObservableCollection<DistributionItem> DistributionItems { get; set; }
    [ObservableProperty] public partial ObservableCollection<List<Face>> AllPlays { get; set; }
    [ObservableProperty] public partial ObservableCollection<PlayItem> PlayItems { get; set; }
    [ObservableProperty] public partial ObservableCollection<int> PossibleNrOfTricks { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var result = (Result)query["Result"];
        DistributionItems = new ObservableCollection<DistributionItem>(result.DistributionList);
        AllPlays = new ObservableCollection<List<Face>>(result.AllPlays.Where(x => x.Count == 3));
        PlayItems = new ObservableCollection<PlayItem>(result.PlayList.Where(x => x.Play.Count == 3));
        PossibleNrOfTricks = new ObservableCollection<int>(result.PossibleNrOfTricks);
    }
}