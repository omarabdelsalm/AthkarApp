using AthkarApp.Services;
using AthkarApp.Models;

namespace AthkarApp.Views
{
    public partial class ProphetsPage : ContentPage
    {
        private readonly IProphetService _prophetService;

        public ProphetsPage(IProphetService prophetService)
        {
            InitializeComponent();
            _prophetService = prophetService;
            LoadProphets();
        }

        protected override bool OnBackButtonPressed()
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                return base.OnBackButtonPressed();
            }

            Shell.Current.GoToAsync("//AthkarPage");
            return true;
        }

        private void LoadProphets()
        {
            ProphetsList.ItemsSource = _prophetService.GetAllStories();
        }

        private async void OnProphetTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is ProphetStory story)
            {
                await Navigation.PushAsync(new ProphetDetailPage(story));
            }
        }
    }
}
