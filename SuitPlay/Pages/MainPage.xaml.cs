using System.Diagnostics;
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
        var card = (UiCard)((Image)sender).BindingContext;
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
            BestPlay.Text = "Calculating...\nAverage";
            var stopWatch = Stopwatch.StartNew();
            var southHand = GetHand(((HandViewModel)South.BindingContext).Cards);
            var northHand = GetHand(((HandViewModel)North.BindingContext).Cards);
            var tricks = await GetAverageTrickCount(northHand, southHand);
            BestPlay.Text = $"{GetBestPlayText(tricks.ToList())} ({stopWatch.Elapsed:s\\:ff} seconds)";
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
        finally
        {
            CalculateButton.IsEnabled = true;
        }
        
        return;

        string GetHand(IEnumerable<UiCard> observableCollection)
        {
            return observableCollection.Aggregate("",
                (current, card) => current + dictionary.Single(x => x.Value == card.Source).Key.card);
        }
    }

    private static string GetBestPlayText(IReadOnlyCollection<IGrouping<IList<Face>, int>> tricks)
    {
        var bestPlay = FindBestPlay(tricks);
        if (bestPlay.Count < 3)
        {
            return "Unable to calculate first trick";
        }
        var bestFirstTrickAsString = string.Join(",", bestPlay);
        var average = tricks.Single(x => x.Key.SequenceEqual(bestPlay)).Average();
        var bestPlayText = $"First trick: {bestFirstTrickAsString}\nAverage tricks:{average:0.##}";
        return bestPlayText;
    }
    
    private static List<Face> FindBestPlay(IReadOnlyCollection<IGrouping<IList<Face>, int>> tricks)
    {
        var play = new List<Face>();
        while (play.Count < 3)
        {
            var trickWithNextCard = tricks.Where(x => x.Key.Count == play.Count + 1 && x.Key.StartsWith(play)).ToList();
            if (trickWithNextCard.Count == 0)
                break;
            play.Add(play.Count % 2 == 0 ? GetOurCard() : GetTheirCard());
            continue;

            Face GetOurCard() => trickWithNextCard.OrderByDescending(x => x.Average()).First().Key.Last();
            Face GetTheirCard() => trickWithNextCard.Where(x => x.Key.Last() != Face.Dummy).OrderBy(y => y.Key.Last()).First().Key.Last();
        }

        return play;
    }    

    private Task<IEnumerable<IGrouping<IList<Face>, int>>> GetAverageTrickCount(string northHand, string southHand)
    {
        return Task.Run(() => Calculate.GetAverageTrickCount(northHand, southHand,
            new CalculateOptions { UsePruning = UsePruningCheckBox.IsChecked }));
    }
}