using System.Diagnostics;
using Calculator;
using Calculator.Models;
using MoreLinq;
using SuitPlay.ViewModels;
using SuitPlay.Views;

namespace SuitPlay.Pages;

public partial class MainPage
{
    private HandView SelectedHandView
    {
        get;
        set
        {
            field = value;
            North.BackgroundColor = Colors.Black;
            South.BackgroundColor = Colors.Black;
            field.BackgroundColor = Colors.Gray;
        }
    }

    private readonly Dictionary<(string suit, string card), string> dictionary;
    private IDictionary<Face[], List<Item>> bestPlay;
    private Result result;

    public MainPage()
    {
        InitializeComponent();
        dictionary = SplitImages.Split(CardImageSettings.GetCardImageSettings("default"));
        Cards.OnImageTapped += TapGestureRecognizer_OnImageTapped;
        North.OnImageTapped += TapGestureRecognizer_OnImageTapped;
        North.OnHandTapped += TapGestureRecognizer_OnHandTapped;
        South.OnImageTapped += TapGestureRecognizer_OnImageTapped;
        South.OnHandTapped += TapGestureRecognizer_OnHandTapped;
        InitCards();
        LoadSettings();
        SelectedHandView = North;
    }

    private void LoadSettings()
    {
        var north = Preferences.Get("North", "");
        var south = Preferences.Get("South", "");
        OnlyLinesInSuitPlayCheckBox.IsChecked = Preferences.Get("OnlyLinesInSuitPlay", false);
        var remainingCards = Utils.CardsToString(Utils.GetAllCards()).Except(north).Except(south);
        ((HandViewModel)North.BindingContext).ShowHand($"{north}", "default", dictionary);
        ((HandViewModel)South.BindingContext).ShowHand($"{south}", "default", dictionary);
        ((HandViewModel)Cards.BindingContext).ShowHand(new string(remainingCards.ToArray()), "default", dictionary);
    }
    
    private void SaveSettings()
    {
        Preferences.Set("North", Utils.CardsToString(GetHand(North)));
        Preferences.Set("South", Utils.CardsToString(GetHand(South)));
    }

    private void InitCards()
    {
        ((HandViewModel)Cards.BindingContext).ShowHand(Utils.CardsToString(Utils.GetAllCards()), "default", dictionary);
        ((HandViewModel)North.BindingContext).Cards.Clear();
        ((HandViewModel)South.BindingContext).Cards.Clear();
    }

    private void TapGestureRecognizer_OnImageTapped(object sender, HandView handView)
    {
        var card = (UiCard)((Image)sender).BindingContext;
        ((HandViewModel)handView?.BindingContext)?.RemoveCard(card);
        ((HandViewModel)(handView == Cards ? SelectedHandView : Cards).BindingContext).AddCard(card);
        SaveSettings();
        EnableButtons(false);
    }

    private void EnableButtons(bool enable)
    {
        OverviewButton.IsEnabled = enable;
        DistributionsButton.IsEnabled = enable;
        TreeItemsButton.IsEnabled = enable;
    }

    private void TapGestureRecognizer_OnHandTapped(object sender, HandView handView)
    {
        SelectedHandView = handView;
    }
    
    private void ResetButton_OnClicked(object sender, EventArgs e)
    {
        InitCards();
        SaveSettings();
        EnableButtons(false);
        SelectedHandView = North;
    }

    private async void CalculateButton_OnClicked(object sender, EventArgs e)
    {
        try
        {
            CalculateButton.IsEnabled = false;
            BestPlay.Text = "Calculating...\nAverage";
            var stopWatch = Stopwatch.StartNew();
            var northHand = GetHand(North);
            var southHand = GetHand(South);
            var northSouth = northHand.Concat(southHand).OrderDescending().ToArray();
            bestPlay = await Task.Run(() => Calculate.CalculateBestPlay(northHand, southHand));
            var calculateElapsed = stopWatch.Elapsed;
            stopWatch.Restart();
            result = await GetResult(northHand, southHand);
            var backTrackingElapsed = stopWatch.Elapsed;
            BestPlay.Text = $@"{GetBestPlayText(result.PlayList, northSouth)} (Calculate:{calculateElapsed:s\:ff} seconds. BackTracking:{backTrackingElapsed:s\:ff} seconds)";
            EnableButtons(true);
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
        finally
        {
            CalculateButton.IsEnabled = true;
        }
    }

    private static string GetBestPlayText(List<PlayItem> playList, Face[] northSouth)
    {
        var segmentsNS = northSouth.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        var bestPlay = playList.Where(x => x.Play.Count() == 7 && x.Play[1] == Face.SmallCard).MaxBy(x => x.Average);
        var textTrickOne = GetTrickText(bestPlay.Play.Take(3).ToList());
        var textTrickTwo = GetTrickText(bestPlay.Play.Skip(4).Take(3).ToList());
        var bestPlayText = $"{textTrickOne.text} {textTrickOne.card}\n" +
                           $"Then, {textTrickTwo.text} {textTrickTwo.card}\n" +
                           $"Sequence:{bestPlay.Play}\n" +
                           $"Average tricks:{bestPlay.Average:0.##}";
        return bestPlayText;
        
        (string text, Face card) GetTrickText(List<Face> trick)
        {
            if (segmentsNS.First().Contains(trick[0]))
                return ("Play the", trick[0]);
            if (segmentsNS.First().Contains(trick[2]))
                return ("Play the", trick[2]);
            return trick[0] < trick[2] ? ("Play to the", trick[2]) : ("Run the", trick[0]);
        }
    }

    private async void ButtonOverview_OnClicked(object sender, EventArgs e)
    {
        try
        {
            var overviewList = result.PlayList.Where(y => y.Play.Count() > 2 && y.Play.All(z => z != Face.Dummy)).Select(x => new OverviewItem
                { FirstTrick = x.Play.ToString(), Average = x.Average, Count = x.NrOfTricks.Count }).ToList();
            await Shell.Current.GoToAsync(nameof(OverviewPage), new Dictionary<string, object> { ["OverviewList"] = overviewList });
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
    }

    private async void ButtonDistributions_OnClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(DistributionsPage), new Dictionary<string, object> { ["Result"] = result });
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
    }

    private static Face[] GetHand(HandView handView)
    {
        return ((HandViewModel)handView.BindingContext).Cards.Select(y => y.Face).ToArray();
    }

    private Task<Result> GetResult(Face[] north, Face[] south)
    {
        return Task.Run(() =>
        {
            var resultLocal = Calculate.GetResult(bestPlay, north, south);
            var filename = Path.Combine(FileSystem.AppDataDirectory, $"{Utils.CardsToString(north)}-{Utils.CardsToString(south)}.json");
            Utils.SaveTrees(resultLocal, filename);
            return resultLocal;
        });
    }
    
    private async void TreeItemsButton_OnClicked(object sender, EventArgs e)
    {
        try
        {
            var lResult = Calculate.GetResult2(bestPlay, GetHand(North), GetHand(South));
            await Shell.Current.GoToAsync(nameof(DistributionsPage2), new Dictionary<string, object>
                    { ["Result"] = lResult, ["OnlyLinesInSuitPlay"] = OnlyLinesInSuitPlayCheckBox.IsChecked });
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
    }

    private void OnlyLinesInSuitPlayCheckBox_OnCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Preferences.Set("OnlyLinesInSuitPlay", e.Value);
    }
}