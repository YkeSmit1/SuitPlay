using System.Collections.ObjectModel;
using Calculator;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SuitPlay.ViewModels
{
    public class HandViewModel : ObservableObject
    {
        public ObservableCollection<UiCard> Cards { get; set; } = [];

        public void AddCard(UiCard uiCard)
        {
            Cards.Add(uiCard);
            ReorderHand();
        }
        
        public void RemoveCard(UiCard uiCard)
        {
            Cards.Remove(uiCard);
            ReorderHand();
        }

        public void ShowHand(string hand, string cardProfile, Dictionary<(string suit ,string card), string> dictionary)
        {
            var settings = CardImageSettings.GetCardImageSettings(cardProfile);
            Cards.Clear();

            var index = 0;

            foreach (var card in hand)
            {
                var valueTuple = (Utils.GetSuitDescriptionASCII(Suit.Hearts), card.ToString());
                Cards.Add(new UiCard
                    {
                        Rect = new Rect(index * settings.CardDistance, 0, settings.CardWidth, settings.CardHeight),
                        Source = dictionary[valueTuple],
                        Face = Utils.GetFaceFromDescription(card),
                    }
                );
                index++;
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
