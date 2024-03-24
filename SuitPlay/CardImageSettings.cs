namespace SuitPlay
{
    public class CardImageSettings
    {
        public string CardImage { get; private init; }
        public int XOffSet { get; private init; }
        public int YOffSet { get; private init; }
        public int CardWidth { get; private init; }
        public int CardHeight { get; private init; }
        public int XCardPadding { get; private init; }
        public int YCardPadding { get; private init; }
        public int CardDistance { get; private init; }
        public string CardOrder { get; private init; }
        public string SuitOrder { get; private init; }

        private static readonly CardImageSettings DefaultCardImageSettings = new CardImageSettings
        {
            CardImage = "cardfaces.png",
            XOffSet = 0,
            YOffSet = 0,
            CardWidth = 73,
            CardHeight = 98,
            XCardPadding = 0,
            YCardPadding = 0,
            CardDistance = 20,
            CardOrder = "A23456789TJQK",
            SuitOrder = "CSHD"
        };

        private static readonly CardImageSettings BboCardImageSettings = new CardImageSettings
        {
            CardImage = "cardfaces2.jpg",
            XOffSet = 14,
            YOffSet = 12,
            CardWidth = 38,
            CardHeight = 62,
            XCardPadding = 4,
            YCardPadding = 14,
            CardDistance = 32,
            CardOrder = "23456789TJQKA",
            SuitOrder = "DHCS"
        };

        public static CardImageSettings GetCardImageSettings(string settings)
        {
            return settings switch
            {
                "default" => DefaultCardImageSettings,
                "bbo" => BboCardImageSettings,
                _ => throw new NotImplementedException(),
            };
        }

    }
}
