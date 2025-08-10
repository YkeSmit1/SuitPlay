using System.Collections.ObjectModel;
using System.Text;
using Calculator;
using Calculator.Models;
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
    private List<Face> North { get; set; }
    private List<Face> South { get; set; }
    

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var result = (Result2)query["Result"];
        DistributionItems = new ObservableCollection<DistributionItem>(result.DistributionItems);
        LineItems = new ObservableCollection<LineItem>(result.LineItems.Where(x => !(bool)query["OnlyLinesInSuitPlay"] || x.LineInSuitPlay));
        PossibleNrOfTricks = new ObservableCollection<int>(result.PossibleNrOfTricks);
        PurpleItems = result.LineItems.SelectMany(x => x.Items2).Count(x => x.IsDifferent && x.IsSubstitute);
        GreenItems = result.LineItems.SelectMany(x => x.Items2).Count(x => x.IsDifferent && !x.IsSubstitute);
        MinusOneItems = result.LineItems.SelectMany(x => x.Items2).Count(x => x.Tricks == -1);
        Combination = $"{Utils.CardsToString(result.North)} - {Utils.CardsToString(result.South)}";
        North = result.North;
        South = result.South;
    }
    
    public async Task ExportViewModelToCsv()
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("WestHand,EastHand,Plays");

        // Rows
        var cardsNS = North.Concat(South).OrderDescending().ToList();
        foreach (var item in DistributionItems)
        {
            var itemItems = string.Join(",", LineItems.Select(x =>
                    $"{Utils.CardsToString(x.Line)}:{x.Items2.Single(y => y.Combination.ConvertToSmallCards(cardsNS).SequenceEqual(item.East)).Tricks}"));
            sb.AppendLine($"{Utils.CardsToString(item.West)},{Utils.CardsToString(item.East)},{itemItems}");
        }

        var filePath = Path.Combine(FileSystem.AppDataDirectory, "ViewModelData.csv");
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }    
}