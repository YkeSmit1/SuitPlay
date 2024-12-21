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
        try
        {
            CalculateButton.IsEnabled = false;
            BestPlay.Text = "Calculating...\nAverage";
            var stopWatch = Stopwatch.StartNew();
            var southHand = GetHand(((HandViewModel)South.BindingContext).Cards);
            var northHand = GetHand(((HandViewModel)North.BindingContext).Cards);
            tricks = await GetAverageTrickCount(northHand, southHand);
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

    private string GetHand(IEnumerable<UiCard> observableCollection)
    {
        return observableCollection.Aggregate("",
            (current, card) => current + dictionary.Single(x => x.Value == card.Source).Key.card);
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

    private Task<List<IGrouping<IList<Face>, int>>> GetAverageTrickCount(string northHand, string southHand)
    {
        return Task.Run(() => Calculate.GetAverageTrickCount(northHand, southHand).ToList());
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
            var northHandCards = ((HandViewModel)North.BindingContext).Cards.Select(y => y.Face).ToList();
            var southHandCards = ((HandViewModel)South.BindingContext).Cards.Select(x => x.Face).ToList();
            var cardsNS = northHandCards.Concat(southHandCards).ToList();
            var northHand = string.Join("", northHandCards.Select(Utils.CardToChar));
            var southHand = string.Join("", southHandCards.Select(Utils.CardToChar));
            var bestPlay = await GetCalculateBestPlay(northHand, southHand);
            var segmentsNS = cardsNS.OrderByDescending(x => x).Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
            var filteredTrees = bestPlay.Trees.OrderBy(x => string.Join("", x.Key.Select(Utils.CardToChar)))
                .ToDictionary(x => x.Key, y => y.Value.Where(x => x.Item1.Count == 3 && x.Item1.All(z => z != Face.Dummy)).ToList());
            var allPlays = filteredTrees.SelectMany(x => x.Value).Select(y => y.Item1).Select(z => z.ConvertToSmallCards(segmentsNS).ToList()).Distinct(new ListComparer<Face>() ).ToList();
            
            var combinations = Combinations.AllCombinations(Utils.GetAllCards().Except(cardsNS)).Select(x => x.OrderByDescending(y => y));

            var distributionList = filteredTrees.Select(x =>
            {
                var westHand = Utils.GetAllCards().Except(x.Key).Except(cardsNS).Reverse().ToList();
                var nrOfTricks = new int[allPlays.Count]; 
                Array.Fill(nrOfTricks, -1);
                foreach (var play in x.Value)
                {
                    nrOfTricks[allPlays.FindIndex(y => y.SequenceEqual(play.Item1.ConvertToSmallCards(segmentsNS)))] = play.Item2;
                }

                var similarCombinations = Calculate.SimilarCombinations(combinations, westHand, cardsNS.OrderByDescending(y => y)).ToList();
                var probability = Utils.GetDistributionProbabilitySpecific(x.Key.Count, westHand.Count) * similarCombinations.Count * 100;
                return new DistributionItem
                {
                    East = x.Key.ConvertToSmallCards(segmentsNS).ToList(), 
                    West = westHand.ConvertToSmallCards(segmentsNS).ToList(),
                    Occurrences = similarCombinations.Count,
                    Probability = probability,
                    NrOfTricks = nrOfTricks.ToList(),
                };
            });
            await Shell.Current.GoToAsync(nameof(DistributionsPage), new Dictionary<string, object> { ["DistributionList"] = distributionList.ToList(), ["AllPlays"] = allPlays });
        }
        catch (Exception exception)
        {
            await DisplayAlert("Error", exception.Message, "OK");
        }
    }

    private Task<Calculate.Result> GetCalculateBestPlay(string northHand, string southHand)
    {
        return Task.Run(() => Calculate.CalculateBestPlay(northHand, southHand));
    }
    
    private static string PlayToString(IList<Face> tuple)
    {
        return string.Join("", tuple.Select(Utils.CardToChar));
    }
}