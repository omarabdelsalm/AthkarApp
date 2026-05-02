using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views
{
    public partial class HadithPage : ContentPage
    {
        private readonly HadithService _hadithService = new();
        private int _currentPage = 1;
        private bool _isLoading = false;

        public HadithPage()
        {
            InitializeComponent();
            LoadHadiths();
        }

        private async void LoadHadiths()
        {
            if (_isLoading) return;
            _isLoading = true;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            var hadiths = await _hadithService.GetHadithsAsync(_currentPage);
            if (hadiths != null && hadiths.Count > 0)
            {
                HadithCollectionView.ItemsSource = hadiths;
            }

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            _isLoading = false;
        }

        private async void OnHadithTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is Hadith selectedHadith)
            {
                // Navigate to detail page
                await Navigation.PushAsync(new HadithDetailPage(selectedHadith.Id));
            }
        }
    }
}
