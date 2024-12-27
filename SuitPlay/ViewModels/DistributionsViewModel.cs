using System.Collections.ObjectModel;
using Calculator;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class DistributionsViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] private ObservableCollection<DistributionItem> distributionItems;
    [ObservableProperty] private ObservableCollection<IList<Face>> allPlays;
    [ObservableProperty] private ObservableCollection<double> averageNrOfTricks;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var result = (Calculate.Result)query["Result"];
        DistributionItems = new ObservableCollection<DistributionItem>(result.DistributionList);
        AllPlays = new ObservableCollection<IList<Face>>((IEnumerable<IList<Face>>)result.AllPlays);
        AverageNrOfTricks = new ObservableCollection<double>(result.AverageNrOfTricks.Select(x => x.Item2));
    }
}