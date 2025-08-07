using System.Collections.ObjectModel;
using System.Text;
using Calculator;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class Distributions2ViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] public partial ObservableCollection<DistributionItem> DistributionItems { get; set; }
    [ObservableProperty] public partial ObservableCollection<LineItem> LineItems { get; set; }
    [ObservableProperty] public partial ObservableCollection<int> PossibleNrOfTricks { get; set; }
    [ObservableProperty] public partial int PurpleItems { get; set; }
    [ObservableProperty] public partial int GreenItems { get; set; }
    [ObservableProperty] public partial int MinusOneItems { get; set; }
    [ObservableProperty] public partial string Combination { get; set; }
    

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var result = (Result2)query["Result"];
        DistributionItems = new ObservableCollection<DistributionItem>(result.DistributionItems);
        LineItems = new ObservableCollection<LineItem>(result.LineItems);
        PossibleNrOfTricks = new ObservableCollection<int>(result.PossibleNrOfTricks);
        PurpleItems = result.LineItems.SelectMany(x => x.Items2).Count(x => x.IsDifferent && x.IsSubstitute);
        GreenItems = result.LineItems.SelectMany(x => x.Items2).Count(x => x.IsDifferent && !x.IsSubstitute);
        MinusOneItems = result.LineItems.SelectMany(x => x.Items2).Count(x => x.Tricks == -1);
        Combination = result.Combination;
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