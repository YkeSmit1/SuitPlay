namespace SuitPlay.Views
{
    public partial class HandView
    {
        public event EventHandler<HandView> OnImageTapped;
        public event EventHandler<HandView> OnHandTapped;
        public HandView()
        {
            InitializeComponent();
        }

        private void TapGestureRecognizer_OnImageTapped(object sender, TappedEventArgs e)
        {
            OnImageTapped?.Invoke(sender, this);
        }

        private void TapGestureRecognizer_OnHandTapped(object sender, TappedEventArgs e)
        {
            OnHandTapped?.Invoke(sender, this);
        }
    }
}