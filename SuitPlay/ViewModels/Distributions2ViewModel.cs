using System.Collections.ObjectModel;
using System.Text;
using Calculator;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class Distributions2ViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] private ObservableCollection<DistributionItem> distributionItems;
    [ObservableProperty] private ObservableCollection<Calculate.LineItem> lineItems;
    [ObservableProperty] private ObservableCollection<int> possibleNrOfTricks;
    [ObservableProperty] private int purpleItems;
    [ObservableProperty] private int greenItems;
    

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var results = (Calculate.Result2)query["Result"];
        DistributionItems = new ObservableCollection<DistributionItem>(results.DistributionItems);
        LineItems = new ObservableCollection<Calculate.LineItem>(results.LineItems);
        PossibleNrOfTricks = new ObservableCollection<int>(results.PossibleNrOfTricks);
        PurpleItems = results.LineItems.SelectMany(x => x.Items2).Count(x => x.IsDifferent && x.IsSubstitute);
        GreenItems = results.LineItems.SelectMany(x => x.Items2).Count(x => x.IsDifferent && !x.IsSubstitute);
    }
    
    public async Task ExportViewModelToCsv()
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("WestHand,EastHand,Plays");

        // Rows
        // foreach (var item in TreeItems)
        // {
        //     var itemItems = string.Join(",", item.Items.Select(x => $"{Utils.CardsToString(x.Play)}:{x.Tricks}"));
        //     sb.AppendLine($"{Utils.CardsToString(item.WestHand)},{Utils.CardsToString(item.EastHand)},{itemItems}");
        // }

        var filePath = Path.Combine(FileSystem.AppDataDirectory, "ViewModelData.csv");
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }    
}