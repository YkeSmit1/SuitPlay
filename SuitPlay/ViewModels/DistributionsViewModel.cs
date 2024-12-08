using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class DistributionsViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private ObservableCollection<DistributionItem> distributionItems;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        DistributionItems = new ObservableCollection<DistributionItem>((IEnumerable<DistributionItem>)query["DistributionList"]);
    }
}