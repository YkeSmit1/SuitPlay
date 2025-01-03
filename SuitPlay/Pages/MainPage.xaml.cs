﻿using System.Diagnostics;
using Calculator;
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
    private IDictionary<List<Face>, List<(IList<Face>, int)>> bestPlay;    

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
        var remainingCards = PlayToString(Utils.GetAllCards()).Except(north).Except(south);
        ((HandViewModel)North.BindingContext).ShowHand($"{north}", "default", dictionary);
        ((HandViewModel)South.BindingContext).ShowHand($"{south}", "default", dictionary);
        ((HandViewModel)Cards.BindingContext).ShowHand(new string(remainingCards.ToArray()), "default", dictionary);
    }
    
    private void SaveSettings()
    {
        Preferences.Set("North", Utils.CardListToString(GetHand(North)));
        Preferences.Set("South", Utils.CardListToString(GetHand(South)));
    }

    private void InitCards()
    {
        ((HandViewModel)Cards.BindingContext).ShowHand(PlayToString(Utils.GetAllCards()), "default", dictionary);
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
            bestPlay = await Task.Run(() => Calculate.CalculateBestPlay(northHand, southHand));
            stopWatch.Stop();
            var result = await GetResult(northHand, southHand);
            BestPlay.Text = $"{GetBestPlayText(result)} ({stopWatch.Elapsed:s\\:ff} seconds)";
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

    private static string GetBestPlayText(Calculate.Result results)
    {
        var bestPlay = FindBestPlay(results);
        var bestPlayText = $"First trick: {PlayToString(bestPlay.Play)}\nAverage tricks:{bestPlay.Average:0.##}";
        return bestPlayText;
        
        static PlayItem FindBestPlay(Calculate.Result result)
        {
            return result.PlayList.Where(x => x.Play[1] == Face.SmallCard).MaxBy(x => x.Average);
        }
    }

    private async void ButtonOverview_OnClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await GetResult(GetHand(North), GetHand(South));
            var overviewList = result.PlayList.Where(y => y.Play.Count > 2 && y.Play.All(z => z != Face.Dummy)).Select(x => new OverviewItem
                { FirstTrick = PlayToString(x.Play), Average = x.Average, Count = x.NrOfTricks.Count }).ToList();
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
            var result = GetResult(GetHand(North), GetHand(South));
            await Shell.Current.GoToAsync(nameof(DistributionsPage), new Dictionary<string, object> { ["Result"] = await result });
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
    }

    private List<Face> GetHand(HandView handView)
    {
        return ((HandViewModel)handView.BindingContext).Cards.Select(y => y.Face).ToList();
    }

    private Task<Calculate.Result> GetResult(List<Face> northHand, List<Face> southHand)
    {
        var result = Task.Run(() =>
        {
            var cardsNS = northHand.Concat(southHand).OrderByDescending(z => z).ToList();
            var filteredTrees = bestPlay.ToDictionary(x => x.Key, y => y.Value.Where(x => x.Item1.Count == 3), new ListEqualityComparer<Face>());
            return Calculate.GetResult(filteredTrees, cardsNS);
        });
        return result;
    }

    private static string PlayToString(IEnumerable<Face> tuple)
    {
        return string.Join("", tuple.Select(Utils.CardToChar));
    }
}