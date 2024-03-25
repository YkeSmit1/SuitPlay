namespace SuitPlay.Views
{
    public partial class HandView
    {
        public event EventHandler<TappedEventArgs> OnImageTapped;
        public HandView()
        {
            InitializeComponent();
        }

        private void TapGestureRecognizer_OnTapped(object sender, TappedEventArgs e)
        {
            OnImageTapped?.Invoke(sender, e);
        }
    }
}