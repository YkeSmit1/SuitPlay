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
    private List<IGrouping<IList<Face>, int>> tricks;

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
        Preferences.Set("North", Utils.CardListToString(((HandViewModel)North.BindingContext).Cards.Select(x => x.Face).ToList()));
        Preferences.Set("South", Utils.CardListToString(((HandViewModel)South.BindingContext).Cards.Select(x => x.Face).ToList()));
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
    }

    private async void CalculateButton_OnClicked(object sender, EventArgs e)
    {
        try
        {
            CalculateButton.IsEnabled = false;
            BestPlay.Text = "Calculating...\nAverage";
            var stopWatch = Stopwatch.StartNew();
            var northHand = ((HandViewModel)North.BindingContext).Cards.Select(x => x.Face).ToList();
            var southHand = ((HandViewModel)South.BindingContext).Cards.Select(x => x.Face).ToList();
            tricks = await Task.Run(() =>
            {
                var bestPlay = Calculate.CalculateBestPlay(northHand, southHand);
                var cardsNS = northHand.Concat(southHand).OrderByDescending(x => x).ToList();
                return Calculate.GetAverageTrickCount(bestPlay, cardsNS).ToList();
            });
            OverviewButton.IsEnabled = true;
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

    private async void ButtonOverview_OnClicked(object sender, EventArgs e)
    {
        try
        {
            var overviewList = tricks.Where(y => y.Key.Count > 2 && y.Key.All(z => z != Face.Dummy)).Select(x => new OverviewItem
                { FirstTrick = PlayToString(x.Key), Average = x.Average(), Count = x.Count() }).ToList();
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
            var northHand = ((HandViewModel)North.BindingContext).Cards.Select(y => y.Face).ToList();
            var southHand = ((HandViewModel)South.BindingContext).Cards.Select(x => x.Face).ToList();
            var result = Task.Run(() =>
            {
                var calculateBestPlay = Calculate.CalculateBestPlay(northHand, southHand);
                var cardsNS = northHand.Concat(southHand).OrderByDescending(z => z).ToList();
                var filteredTrees = calculateBestPlay.ToDictionary(x => x.Key, y => y.Value.Where(x => x.Item1.Count == 3 && x.Item1.All(z => z != Face.Dummy)));
                
                return Calculate.GetResult(filteredTrees, cardsNS);
            });
            await Shell.Current.GoToAsync(nameof(DistributionsPage), new Dictionary<string, object> { ["Result"] = await result });
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
    }
    
    private static string PlayToString(IEnumerable<Face> tuple)
    {
        return string.Join("", tuple.Select(Utils.CardToChar));
    }
}