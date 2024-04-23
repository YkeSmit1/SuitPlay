﻿using System.Collections.ObjectModel;
using System.Diagnostics;
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
    }

    private void InitCards()
    {
        ((HandViewModel)Cards.BindingContext).ShowHand(",AKQJT98765432,,", "default", dictionary);
        ((HandViewModel)North.BindingContext).Cards.Clear();
        ((HandViewModel)South.BindingContext).Cards.Clear();
        SelectedHandView = North;
    }

    private void TapGestureRecognizer_OnTapped(object sender, TappedEventArgs e)
    {
        var card = (Card)((Image)sender).BindingContext;
        ((HandViewModel)((HandView)e.Parameter)?.BindingContext)?.RemoveCard(card);
        ((HandViewModel)((HandView)e.Parameter == Cards ? SelectedHandView : Cards).BindingContext).AddCard(card);
    }
    
    private void TapGestureRecognizerSelect_OnTapped(object sender, TappedEventArgs e)
    {
        SelectedHandView = (HandView)e.Parameter;
    }
    
    private void ResetButton_OnClicked(object sender, EventArgs e)
    {
        InitCards();
    }

    private async void CalculateButton_OnClicked(object sender, EventArgs e)
    {
        CalculateButton.IsEnabled = false;
        try
        {
            BestPlay.Text = "Calculating...";
            var stopWatch = Stopwatch.StartNew();
            var southHand = GetHand(((HandViewModel)South.BindingContext).Cards);
            var northHand = GetHand(((HandViewModel)North.BindingContext).Cards);
            var tricks = await GetAverageTrickCount(northHand, southHand);
            var twoTricks = tricks.Where(x => x.Key.Count == 8).ToList();
            if (twoTricks.Count == 0)
            {
                BestPlay.Text = "Unable to calculate best play";
                return;
            }
            var bestTrick = twoTricks.OrderByDescending(x => x.Average()).First();
            var firstTrick = string.Join(",",bestTrick.Key.Take(4));
            var secondTrick = string.Join(",", bestTrick.Key.Skip(4).Take(4));
            BestPlay.Text = $"First trick: {firstTrick}\nSecond trick:{secondTrick}\nAverage tricks:{bestTrick.Average():0.##} ({stopWatch.Elapsed:s\\:ff} seconds)";
        }
        finally
        {
            CalculateButton.IsEnabled = true;
        }
        
        return;

        string GetHand(ObservableCollection<Card> observableCollection)
        {
            return observableCollection.Aggregate("",
                (current, card) => current + dictionary.Single(x => x.Value == card.Source).Key.card);
        }
    }

    private static Task<IEnumerable<IGrouping<IList<Calculator.Card>, int>>> GetAverageTrickCount(string northHand, string southHand)
    {
        return Task.Run(() => Calculate.GetAverageTrickCount(northHand, southHand));
    }
}