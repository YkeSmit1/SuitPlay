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
    private Result2 result;

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
        DistributionsButton.IsEnabled = enable;
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
            var calculateSettings = new Calculate.CalculateSettings
                { EtalonsDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "Calculator.etalons_suitplay"), 
                    MaxLines = Preferences.Get(Constants.MaxLinesInCalculate, 10000) };
            result = Calculate.GetResult2(bestPlay, GetHand(North), GetHand(South), calculateSettings);
            var constructLinesElapsed = stopWatch.Elapsed;
            BestPlay.Text = $@"{GetBestPlayText(result.LineItems, northSouth)} (Calculate:{calculateElapsed:s\:ff} seconds. Construct lines:{constructLinesElapsed:s\:ff} seconds)";
            EnableButtons(true);
        }
        catch (MaxLinesException)
        {
            await DisplayAlert("Too many lines", "Too many lines were generated.\nSimplify the combination or increase the value of \"Max lines in calculation\" in the settings.", "OK");
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

    private static string GetBestPlayText(List<LineItem> playList, Face[] northSouth)
    {
        var segmentsNS = northSouth.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        var bestPlay = playList.MaxBy(x => x.Average);
        var textTrickOne = GetTrickText(bestPlay.LongestLineIncludingGenerated.Take(3).ToList());
        var textTrickTwo = GetTrickText(bestPlay.LongestLineIncludingGenerated.Skip(4).Take(3).ToList());
        var bestPlayText = $"{textTrickOne.text} {textTrickOne.card}\n" +
                           $"Then, {textTrickTwo.text} {textTrickTwo.card}\n" +
                           $"Sequence:{bestPlay.LongestLineIncludingGenerated}\n" +
                           $"Average tricks:{bestPlay.Average:0.##}";
        return bestPlayText;
        
        (string text, Face card) GetTrickText(List<Face> trick)
        {
            if (trick.Count < 1)
                return ("Trick to short", Face.Dummy);
            if (segmentsNS.First().Contains(trick[0]))
                return ("Play the", trick[0]);
            if (trick.Count < 3)
                return ("Trick to short", Face.Dummy);
            if (segmentsNS.First().Contains(trick[2]))
                return ("Play the", trick[2]);
            return trick[0] < trick[2] ? ("Play to the", trick[2]) : ("Run the", trick[0]);
        }
    }

    private static Face[] GetHand(HandView handView)
    {
        return ((HandViewModel)handView.BindingContext).Cards.Select(y => y.Face).ToArray();
    }

    private async void DistributionsButton_OnClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(DistributionsPage2), new Dictionary<string, object> { ["Result"] = result});
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
    }
    
    private async void SettingsButton_OnClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
    }
    
}