using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using AthkarApp.Services;
using CommunityToolkit.Maui.Views;

namespace AthkarApp.Views;

public partial class KhatmahPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;
    private const string PlanActiveKey = "Khatmah_PlanActive";
    private const string PlanDurationKey = "Khatmah_PlanDuration";
    private const string PlanStartDateKey = "Khatmah_PlanStartDate";

    public KhatmahPage(IQuranApiService quranApiService)
    {
        InitializeComponent();
        _quranApiService = quranApiService;
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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshUI();
    }

    private async void RefreshUI()
    {
        bool isActive = Preferences.Default.Get(PlanActiveKey, false);

        if (isActive)
        {
            NewPlanUI.IsVisible = false;
            ActivePlanUI.IsVisible = true;
            
            int durationDays = Preferences.Default.Get(PlanDurationKey, 30);
            DateTime startDate = Preferences.Default.Get(PlanStartDateKey, DateTime.Now);
            
            int currentReadPage = Preferences.Default.Get("Mushaf_LastReadPage", 1);
            int pagesRead = currentReadPage; 

            double progress = (double)pagesRead / 604.0;
            if (progress > 1) progress = 1;

            ProgressPercentLabel.Text = $"{(progress * 100):0.1}%";
            
            PagesReadLabel.Text = pagesRead.ToString();
            PagesLeftLabel.Text = (604 - pagesRead).ToString();

            // حساب المدة المنقضية وهدف اليوم
            int daysPassed = (DateTime.Now.Date - startDate.Date).Days;
            int totalPagesPerDay = (int)Math.Ceiling(604.0 / durationDays);
            int expectedPageToBeAt = Math.Min(604, (daysPassed + 1) * totalPagesPerDay);

            DaysPassedLabel.Text = $"اليوم {daysPassed + 1}";
            TotalDaysLabel.Text = $"من {durationDays}";

            CurrentPageLabel.Text = currentReadPage.ToString();
            
            GoalPageLabel.Text = expectedPageToBeAt.ToString();

            // جلب تفاصيل السورة والآية (الحالي والهدف)
            var currentTask = LoadPageDetails(currentReadPage);
            var goalTask = LoadPageDetails(expectedPageToBeAt);

            await Task.WhenAll(currentTask, goalTask);

            var currentAyah = await currentTask;
            var goalAyah = await goalTask;

            if (currentAyah != null)
            {
                CurrentSuraLabel.Text = currentAyah.Surah?.Name ?? "...";
            }

            if (goalAyah != null)
            {
                GoalSuraLabel.Text = goalAyah.Surah?.Name ?? "...";
            }

            if (currentReadPage >= expectedPageToBeAt)
            {
                TodayPortionSummaryLabel.Text = "رائع! لقد أتممت وردك اليومي بنجاح ✅";
                StatusLabel.Text = "أنت تسير حسب الخطة بشكل ممتاز.";
                StatusLabel.TextColor = Color.FromArgb("#D4AF37");
            }
            else
            {
                int remainingToday = expectedPageToBeAt - currentReadPage;
                TodayPortionSummaryLabel.Text = $"متبقى لك {remainingToday} صفحة لتصل إلى هدف اليوم ({goalAyah?.Surah?.Name}، آية {goalAyah?.NumberInSurah})";
                StatusLabel.Text = $"اليوم {daysPassed + 1} من {durationDays}";
                StatusLabel.TextColor = Colors.White;
            }
        }
        else
        {
            NewPlanUI.IsVisible = true;
            ActivePlanUI.IsVisible = false;
            StatusLabel.Text = "لا توجد خطة حالية";
            StatusLabel.TextColor = Colors.White;
        }
    }

    private async Task<AthkarApp.Models.Ayah?> LoadPageDetails(int pageNumber)
    {
        try 
        {
            var pageData = await _quranApiService.GetPageAsync(pageNumber);
            if (pageData?.Ayahs != null && pageData.Ayahs.Any())
            {
                return pageData.Ayahs.First();
            }
        }
        catch 
        {
            // Ignore error
        }
        return null;
    }

    private void OnContinueReadingClicked(object sender, EventArgs e)
    {
        // البحث عن التبويب (Tab) الخاص بالمصحف والانتقال إليه
        foreach (var item in Shell.Current.Items[0].Items)
        {
            if (item.Title == "المصحف")
            {
                Shell.Current.CurrentItem = item;
                break;
            }
        }
    }

    private async void OnCancelPlanClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("إلغاء الخطة", "هل أنت متأكد من رغبتك بإلغاء خطة الختمة الحالية؟", "نعم", "لا");
        if (confirm)
        {
            Preferences.Default.Remove(PlanActiveKey);
            RefreshUI();
        }
    }

    private async void OnOpenCreatePopupClicked(object sender, EventArgs e)
    {
        var popup = new CreateKhatmahPopup();
        var result = await this.ShowPopupAsync(popup);

        if (result is int days)
        {
            CreatePlan(days);
        }
    }

    private void CreatePlan(int days)
    {
        Preferences.Default.Set(PlanActiveKey, true);
        Preferences.Default.Set(PlanDurationKey, days);
        Preferences.Default.Set(PlanStartDateKey, DateTime.Now.Date);
        RefreshUI();
    }
}
