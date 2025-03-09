﻿using System.Diagnostics;
using Calculator;
using MoreLinq;
using SuitPlay.ViewModels;
using SuitPlay.Views;

namespace SuitPlay.Pages;

public partial class MainPage
{
    private HandView selectedHandView;
    private HandView SelectedHandView
    {
        get => selectedHandView;
        set
        {
            selectedHandView = value;
            North.BackgroundColor = Colors.Black;
            South.BackgroundColor = Colors.Black;
            selectedHandView.BackgroundColor = Colors.Gray;
        }
    }

    private readonly Dictionary<(string suit, string card), string> dictionary;
    private IDictionary<List<Face>, List<Calculate.Item>> bestPlay;
    private Calculate.Result result;

    public MainPage()
    {
        InitializeComponent();
        dictionary = SplitImages.Split(CardImageSettings.GetCardImageSettings("default"));
        Cards.OnImageTapped += TapGestureRecognizer_OnTapped;
        North.OnImageTapped += TapGestureRecognizer_OnTapped;
        North.OnHandTapped += TapGestureRecognizerSelect_OnTapped;
        South.OnImageTapped += TapGestureRecognizer_OnTapped;
        South.OnHandTapped += TapGestureRecognizerSelect_OnTapped;
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

    private void TapGestureRecognizer_OnTapped(object sender, TappedEventArgs e)
    {
        var card = (UiCard)((Image)sender).BindingContext;
        ((HandViewModel)((HandView)e.Parameter)?.BindingContext)?.RemoveCard(card);
        ((HandViewModel)((HandView)e.Parameter == Cards ? SelectedHandView : Cards).BindingContext).AddCard(card);
        SaveSettings();
        OverviewButton.IsEnabled = false;
        DistributionsButton.IsEnabled = false;
    }
    
    private void TapGestureRecognizerSelect_OnTapped(object sender, TappedEventArgs e)
    {
        SelectedHandView = (HandView)e.Parameter;
    }
    
    private void ResetButton_OnClicked(object sender, EventArgs e)
    {
        InitCards();
        SaveSettings();
        OverviewButton.IsEnabled = false;
        DistributionsButton.IsEnabled = false;
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
            var northSouth = northHand.Concat(southHand).OrderDescending().ToList();
            bestPlay = await Task.Run(() => Calculate.CalculateBestPlay(northHand, southHand));
            var calculateElapsed = stopWatch.Elapsed;
            stopWatch.Restart();
            result = await GetResult(northHand, southHand);
            var backTrackingElapsed = stopWatch.Elapsed;
            BestPlay.Text = $@"{GetBestPlayText(result.PlayList, northSouth)} (Calculate:{calculateElapsed:s\:ff} seconds. BackTracking:{backTrackingElapsed:s\:ff} seconds)";
            OverviewButton.IsEnabled = true;
            DistributionsButton.IsEnabled = true;
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

    private static string GetBestPlayText(List<PlayItem> playList, List<Face> northSouth)
    {
        var segmentsNS = northSouth.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        var threeCards = playList.Where(x => x.Play[1] == Face.SmallCard && x.Play.Count == 3).MaxBy(x => x.Average);
        var textTrickOne = GetTrickText(threeCards.Play);
        var fourCards =  playList.Where(x => x.Play.StartsWith(threeCards.Play) && x.Play.Count == 4).MinBy(x => x.Average);
        var sevenCards = playList.Where(x => x.Play.StartsWith(fourCards.Play) && x.Play.Count == 7).ToList();
        if (sevenCards.Count == 0)
            return "Unable to calculate best play";
        var sevenCardsSmall = sevenCards.Where(x => x.Play[5] == Face.SmallCard).ToList();
        var bestPlaySecondTrick = sevenCardsSmall.Count != 0 ? sevenCardsSmall.MaxBy(x => x.Average) : sevenCards.MaxBy(x => x.Average);
        var textTrickTwo = GetTrickText(bestPlaySecondTrick.Play.Skip(4).Take(3).ToList());
        var bestPlayText = $"{textTrickOne.text} {textTrickOne.card}\n" +
                           $"Then, {textTrickTwo.text} {textTrickTwo.card}\n" +
                           $"Sequence:{Utils.CardsToString(bestPlaySecondTrick.Play)}\n)" +
                           $"Average tricks:{threeCards.Average:0.##}";
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
            var overviewList = result.PlayList.Where(y => y.Play.Count > 2 && y.Play.All(z => z != Face.Dummy)).Select(x => new OverviewItem
                { FirstTrick = Utils.CardsToString(x.Play), Average = x.Average, Count = x.NrOfTricks.Count }).ToList();
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

    private static List<Face> GetHand(HandView handView)
    {
        return ((HandViewModel)handView.BindingContext).Cards.Select(y => y.Face).ToList();
    }

    private Task<Calculate.Result> GetResult(List<Face> north, List<Face> south)
    {
        return Task.Run(() =>
        {
            var cardsNS = north.Concat(south).OrderDescending().ToList();
            var resultLocal = Calculate.GetResult(bestPlay, cardsNS);
            var filename = Path.Combine(FileSystem.AppDataDirectory, $"{Utils.CardsToString(north)}-{Utils.CardsToString(south)}.json");
            Utils.SaveTrees(resultLocal, filename);
            return resultLocal;
        });
    }
}