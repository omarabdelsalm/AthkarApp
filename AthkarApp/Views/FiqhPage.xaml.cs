using AthkarApp.Services;
using AthkarApp.Models;
using System.Collections.ObjectModel;

namespace AthkarApp.Views
{
    public partial class FiqhPage : ContentPage
    {
        private readonly IFiqhService _fiqhService;
        private List<FiqhChapter> _allChapters = new();

        public FiqhPage(IFiqhService fiqhService)
        {
            InitializeComponent();
            _fiqhService = fiqhService;
            LoadFiqh();
        }

        private void LoadFiqh()
        {
            _allChapters = _fiqhService.GetFiqhChapters();
            ChaptersList.ItemsSource = _allChapters;
            PopulateIndex();
        }

        private void PopulateIndex()
        {
            IndexLayout.Children.Clear();
            foreach (var chapter in _allChapters)
            {
                var frame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#00796B"),
                    CornerRadius = 15,
                    Padding = new Thickness(12, 6),
                    HasShadow = false,
                    Content = new Label 
                    { 
                        Text = chapter.Title, 
                        TextColor = Colors.White,
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold
                    }
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => {
                    ChaptersList.ScrollTo(chapter, position: ScrollToPosition.Start, animate: true);
                };
                frame.GestureRecognizers.Add(tapGesture);

                IndexLayout.Children.Add(frame);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (!string.IsNullOrWhiteSpace(FiqhSearch.Text))
            {
                FiqhSearch.Text = string.Empty;
                return true;
            }

            if (Navigation.NavigationStack.Count > 1)
            {
                return base.OnBackButtonPressed();
            }

            Shell.Current.GoToAsync("//AthkarPage");
            return true;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                ChaptersList.ItemsSource = _allChapters;
                return;
            }

            var filtered = new List<FiqhChapter>();

            foreach (var chapter in _allChapters)
            {
                var matchingTopics = chapter
                    .Where(t => t.Title.ToLower().Contains(searchText) || t.Content.ToLower().Contains(searchText))
                    .ToList();

                if (chapter.Title.ToLower().Contains(searchText) || matchingTopics.Any())
                {
                    var filteredChapter = new FiqhChapter
                    {
                        Title = chapter.Title,
                        Icon = chapter.Icon
                    };
                    
                    filteredChapter.AddRange(matchingTopics.Any() ? matchingTopics : chapter);
                    filtered.Add(filteredChapter);
                }
            }

            ChaptersList.ItemsSource = filtered;
        }
    }
}
