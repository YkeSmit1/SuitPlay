using System.Collections.ObjectModel;
using Calculator;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class DistributionsViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] private ObservableCollection<DistributionItem> distributionItems;
    [ObservableProperty] private ObservableCollection<IList<Face>> allPlays;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        DistributionItems = new ObservableCollection<DistributionItem>((IEnumerable<DistributionItem>)query["DistributionList"]);
        AllPlays = new ObservableCollection<IList<Face>>((IEnumerable<IList<Face>>)query["AllPlays"]);
    }
}