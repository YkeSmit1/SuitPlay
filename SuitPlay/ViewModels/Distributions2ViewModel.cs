using System.Collections.ObjectModel;
using System.Text;
using Calculator;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels;

public partial class Distributions2ViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] private ObservableCollection<Calculate.TreeItem> treeItems;
    [ObservableProperty] private ObservableCollection<Calculate.LineItem> lineItems;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var results = (Calculate.Result2)query["Result"];
        results.TreeItems.Insert(0, new Calculate.TreeItem {EastHand = [], WestHand = []});
        TreeItems = new ObservableCollection<Calculate.TreeItem>(results.TreeItems);
        LineItems = new ObservableCollection<Calculate.LineItem>(results.LineItems);
    }
    
    public async Task ExportViewModelToCsv()
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("WestHand,EastHand,Plays");

        // Rows
        foreach (var item in TreeItems)
        {
            var itemItems = string.Join(",", item.Items.Select(x => $"{Utils.CardsToString(x.Play)}:{x.Tricks}"));
            sb.AppendLine($"{Utils.CardsToString(item.WestHand)},{Utils.CardsToString(item.EastHand)},{itemItems}");
        }

        var filePath = Path.Combine(FileSystem.AppDataDirectory, "ViewModelData.csv");
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }    
}