using AthkarApp.Services;

namespace AthkarApp.Views
{
    public partial class HadithDetailPage : ContentPage
    {
        private readonly HadithService _hadithService = new();
        private readonly string _hadithId;

        public HadithDetailPage(string hadithId)
        {
            InitializeComponent();
            _hadithId = hadithId;
            LoadHadithDetail();
        }

        private async void LoadHadithDetail()
        {
            DetailLoadingIndicator.IsRunning = true;
            DetailLoadingIndicator.IsVisible = true;

            var hadith = await _hadithService.GetHadithDetailAsync(_hadithId);
            if (hadith != null)
            {
                Title = hadith.Title;
                HadithTextLabel.Text = hadith.Text;
                ExplanationLabel.Text = hadith.Explanation;
                AttributionLabel.Text = hadith.Attribution;
                GradeLabel.Text = $"درجة الحديث: {hadith.Grade}";
                ReferenceLabel.Text = $"المصدر: {hadith.Reference}";
            }

            DetailLoadingIndicator.IsRunning = false;
            DetailLoadingIndicator.IsVisible = false;
        }
    }
}
