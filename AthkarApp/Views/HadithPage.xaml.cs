using System.Collections.ObjectModel;
using AthkarApp.Models;
using AthkarApp.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AthkarApp.Views
{
    public partial class HadithPage : ContentPage
    {
        private readonly HadithService _hadithService = new();
        private int _currentPage = 1;
        private bool _isLoading = false;
        private string _selectedCategoryId = "5"; // الافتراضي: الفضائل والآداب

        public ObservableCollection<Hadith> HadithsList { get; set; } = new();

        private readonly List<CategoryItem> _categories = new()
        {
            new() { Id = "5", Title = "الفضائل والآداب", Icon = "🌸" },
            new() { Id = "3", Title = "العقيدة", Icon = "🕋" },
            new() { Id = "4", Title = "الفقه وأصوله", Icon = "⚖️" },
            new() { Id = "7", Title = "السيرة والتاريخ", Icon = "📜" },
            new() { Id = "1", Title = "القرآن وعلومه", Icon = "📖" },
            new() { Id = "6", Title = "الدعوة والحسبة", Icon = "📢" },
            new() { Id = "2", Title = "الحديث وعلومه", Icon = "✍️" }
        };

        public HadithPage()
        {
            InitializeComponent();
            HadithCollectionView.ItemsSource = HadithsList;
            PopulateCategories();
            _ = LoadHadiths();
        }

        private void PopulateCategories()
        {
            CategoriesLayout.Children.Clear();
            foreach (var cat in _categories)
            {
                var isSelected = cat.Id == _selectedCategoryId;
                
                var border = new Border
                {
                    StrokeShape = new RoundRectangle { CornerRadius = 15 },
                    StrokeThickness = 0,
                    BackgroundColor = isSelected ? Color.FromArgb("#FFD700") : (Color)Application.Current.Resources["Primary"],
                    Padding = new Thickness(15, 8),
                    Margin = new Thickness(2),
                    Content = new HorizontalStackLayout
                    {
                        Spacing = 6,
                        Children =
                        {
                            new Label { Text = cat.Icon, FontSize = 14, VerticalOptions = LayoutOptions.Center },
                            new Label 
                            { 
                                Text = cat.Title, 
                                FontSize = 14, 
                                FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None,
                                TextColor = isSelected ? Color.FromArgb("#0B1A0B") : Color.FromArgb("#FFFFFF"),
                                VerticalOptions = LayoutOptions.Center 
                            }
                        }
                    }
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    if (_isLoading) return;
                    _selectedCategoryId = cat.Id;
                    _currentPage = 1;
                    HadithsList.Clear();
                    PopulateCategories();
                    await LoadHadiths();
                };
                border.GestureRecognizers.Add(tapGesture);

                CategoriesLayout.Children.Add(border);
            }
        }

        private async Task LoadHadiths()
        {
            if (_isLoading) return;
            _isLoading = true;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            LoadMoreButton.IsVisible = false;

            var hadiths = await _hadithService.GetHadithsAsync(_currentPage, perPage: 20, categoryId: _selectedCategoryId);
            
            if (_currentPage == 1)
            {
                HadithsList.Clear();
            }

            if (hadiths != null && hadiths.Count > 0)
            {
                foreach (var hadith in hadiths)
                {
                    HadithsList.Add(hadith);
                }

                // إظهار زر تحميل المزيد في حال توفر المزيد من الأحاديث
                LoadMoreButton.IsVisible = hadiths.Count >= 20;
            }
            else
            {
                if (_currentPage == 1)
                {
                    await DisplayAlert("تنبيه", "لا توجد أحاديث في هذا التصنيف حالياً.", "حسناً");
                }
                else
                {
                    await DisplayAlert("تنبيه", "تم الوصول لنهاية هذا التصنيف.", "حسناً");
                }
            }

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            _isLoading = false;
        }

        private async void OnLoadMoreClicked(object sender, EventArgs e)
        {
            _currentPage++;
            await LoadHadiths();
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

    public class CategoryItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
    }
}
