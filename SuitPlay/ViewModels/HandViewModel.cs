using System.Collections.ObjectModel;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels
{
    public class HandViewModel : ObservableObject
    {
        public ObservableCollection<Card> Cards { get; set; } = [];

        public void AddCard(Card card)
        {
            Cards.Add(card);
            ReorderHand();
        }
        
        public void RemoveCard(Card card)
        {
            Cards.Remove(card);
        }

        public void ShowHand(string hand, bool alternateSuits, string cardProfile, Dictionary<(string suit ,string card), string> dictionary)
        {
            var settings = CardImageSettings.GetCardImageSettings(cardProfile);
            Cards.Clear();

            var suitOrder = alternateSuits ?
                new List<Suit> { Suit.Spades, Suit.Hearts, Suit.Clubs, Suit.Diamonds } :
                new List<Suit> { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };
            var suits = hand.Split(',').Select((x, index) => (x, (Suit)(3 - index))).OrderBy(x => suitOrder.IndexOf(x.Item2));
            var index = 0;

            foreach (var suit in suits)
            {
                foreach (var card in suit.x)
                {
                    var valueTuple = (Util.GetSuitDescriptionASCII(suit.Item2), card.ToString());
                    Cards.Add(new Card
                        {
                            Rect = new Rect(index * settings.CardDistance, 0, settings.CardWidth, settings.CardHeight),
                            Source = dictionary[valueTuple],
                            Face = Util.GetFaceFromDescription(card),
                            Suit = suit.Item2
                        }
                    );
                    index++;
                }
            }
        }

        private void ReorderHand()
        {
            var cards = Cards.OrderByDescending(x => x.Face == Face.Ace ? (Face)int.MaxValue : x.Face).ToList();
            var settings = CardImageSettings.GetCardImageSettings("default");
            Cards.Clear();
            var index = 0;

            foreach (var card in cards)
            {
                card.Rect = new Rect(index * settings.CardDistance, 0, settings.CardWidth, settings.CardHeight);
                Cards.Add(card);
                index++;
            }
        }
    }
}
