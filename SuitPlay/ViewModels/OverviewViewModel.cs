using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class OverviewViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] public partial ObservableCollection<OverviewItem> OverviewList {get; set;} = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        OverviewList = new ObservableCollection<OverviewItem>((IEnumerable<OverviewItem>)query["OverviewList"]);
    }
}