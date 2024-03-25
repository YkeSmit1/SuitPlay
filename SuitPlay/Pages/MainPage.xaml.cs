using System.Collections.ObjectModel;
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
            North.BackgroundColor = Colors.DarkGreen;
            South.BackgroundColor = Colors.DarkGreen;
            selectedHandView.BackgroundColor = Colors.Blue;
        }
    }

    private readonly Dictionary<(string suit, string card), string> dictionary;

    public MainPage()
    {
        InitializeComponent();
        dictionary = SplitImages.Split(CardImageSettings.GetCardImageSettings("default"));
        InitCards();
    }

    private void InitCards()
    {
        ((HandViewModel)Cards.BindingContext).ShowHand(",,,AKQJT98765432", "default", dictionary);
        Cards.OnImageTapped += TapGestureRecognizer_OnTapped;
        North.OnImageTapped += TapGestureRecognizer_OnTapped;
        South.OnImageTapped += TapGestureRecognizer_OnTapped;
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
    
    private void ResetButton_OnClicked(object sender, EventArgs e)
    {
        InitCards();
    }

    private void CalculateButton_OnClicked(object sender, EventArgs e)
    {
        var southHand = GetHand(((HandViewModel)South.BindingContext).Cards);
        var northHand = GetHand(((HandViewModel)North.BindingContext).Cards);
        var tricks = Calculate.GetAverageTrickCount(northHand, southHand).ToList();
        var twoTricks = tricks.Where(x => x.Key.Count == 4);
        var bestTrick = twoTricks.First();
        var firstTrick = string.Join(",",bestTrick.Key.Take(2));
        var secondTrick = string.Join(",", bestTrick.Key.Skip(2).Take(2));
        BestPlay.Text = $"First trick: {firstTrick}\nSecond trick:{secondTrick}\nAverage tricks:{bestTrick.tricks:0.##}";
        return;

        string GetHand(ObservableCollection<Card> observableCollection)
        {
            return observableCollection.Aggregate("",
                (current, card) => current + dictionary.Single(x => x.Value == card.Source).Key.card);
        }
    }

    private void ButtonSelectNorth_OnClicked(object sender, EventArgs e)
    {
        SelectedHandView = North;
    }

    private void ButtonSelectSouth_OnClicked(object sender, EventArgs e)
    {
        SelectedHandView = South;
    }
}