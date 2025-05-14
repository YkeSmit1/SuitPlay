using System.Collections.ObjectModel;
using System.Text;
using Calculator;
using CommunityToolkit.Mvvm.ComponentModel;
using SuitPlay.Pages;

namespace SuitPlay.ViewModels;

public partial class Distributions2ViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty] private ObservableCollection<MainPage.TreeItem> treeItems;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        TreeItems = new ObservableCollection<MainPage.TreeItem>((List<MainPage.TreeItem>)query["TreeItems"]);
    }
    
    public async Task ExportViewModelToCsv()
    {
        var items = TreeItems;
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("WestHand,EastHand,Plays");

        // Rows
        foreach (var item in items)
        {
            var itemItems = string.Join(",", item.Items.Select(x => $"{Utils.CardsToString(x.Play)}:{x.Tricks}"));
            sb.AppendLine($"{Utils.CardsToString(item.WestHand)},{Utils.CardsToString(item.EastHand)},{itemItems}");
        }

        var filePath = Path.Combine(FileSystem.AppDataDirectory, "ViewModelData.csv");
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }    
}