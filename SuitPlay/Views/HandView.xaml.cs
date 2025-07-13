namespace SuitPlay.Views
{
    public partial class HandView
    {
        public event EventHandler<TappedEventArgs> OnImageTapped;
        public event EventHandler<TappedEventArgs> OnHandTapped;
        public HandView()
        {
            InitializeComponent();
        }

        private void TapGestureRecognizer_OnImageTapped(object sender, TappedEventArgs e)
        {
            OnImageTapped?.Invoke(sender, e);
        }

        private void TapGestureRecognizer_OnHandTapped(object sender, TappedEventArgs e)
        {
            OnHandTapped?.Invoke(sender, e);
        }
    }
}