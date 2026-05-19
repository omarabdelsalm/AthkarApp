using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using AthkarApp.Services;
using AthkarApp.Models;
using CommunityToolkit.Maui.Views;

namespace AthkarApp.Views;

public partial class KhatmahPage : ContentPage
{
    private readonly IQuranApiService _quranApiService;
    private readonly IAthkarNotificationService _notificationService;

    private const string PlanActiveKey = "Khatmah_PlanActive";
    private const string PlanNameKey = "Khatmah_PlanName";
    private const string PlanDurationKey = "Khatmah_PlanDuration";
    private const string PlanStartDateKey = "Khatmah_PlanStartDate";
    private const string PlanStartPageKey = "Khatmah_PlanStartPage";
    private const string PlanEndPageKey = "Khatmah_PlanEndPage";
    private const string PlanColorKey = "Khatmah_PlanColor";
    private const string PlanReminderEnabledKey = "Khatmah_PlanReminderEnabled";
    private const string PlanReminderTimeKey = "Khatmah_PlanReminderTime";

    public KhatmahPage(IQuranApiService quranApiService, IAthkarNotificationService notificationService)
    {
        InitializeComponent();
        _quranApiService = quranApiService;
        _notificationService = notificationService;
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
            
            string planName = Preferences.Default.Get(PlanNameKey, "ختمتي");
            int durationDays = Preferences.Default.Get(PlanDurationKey, 30);
            DateTime startDate = Preferences.Default.Get(PlanStartDateKey, DateTime.Now.Date);
            int startPage = Preferences.Default.Get(PlanStartPageKey, 1);
            int endPage = Preferences.Default.Get(PlanEndPageKey, 604);
            
            int currentReadPage = Preferences.Default.Get("Mushaf_LastReadPage", 1);
            
            int totalPages = endPage - startPage + 1;
            int pagesRead = currentReadPage - startPage; 
            if (currentReadPage > endPage) pagesRead = totalPages;
            if (pagesRead < 0) pagesRead = 0;

            double progress = (double)pagesRead / totalPages;
            if (progress > 1) progress = 1;
            if (progress < 0) progress = 0;

            ProgressPercentLabel.Text = $"{(progress * 100):0.1}%";
            
            PagesReadLabel.Text = pagesRead.ToString();
            PagesLeftLabel.Text = Math.Max(0, totalPages - pagesRead).ToString();
            PagesTotalLabel.Text = $"من {totalPages} صفحة";

            // Calculate elapsed days and today's goal
            int daysPassed = (DateTime.Now.Date - startDate.Date).Days;
            if (daysPassed < 0) daysPassed = 0;

            int totalPagesPerDay = (int)Math.Ceiling((double)totalPages / durationDays);
            int expectedPageToBeAt = Math.Min(endPage, startPage - 1 + (daysPassed + 1) * totalPagesPerDay);

            DaysPassedLabel.Text = $"اليوم {daysPassed + 1}";
            TotalDaysLabel.Text = $"من {durationDays}";

            CurrentPageLabel.Text = currentReadPage.ToString();
            GoalPageLabel.Text = expectedPageToBeAt.ToString();

            // Load surah details for current page and goal page
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
                StatusLabel.Text = $"{planName} - أنت تسير حسب الخطة بشكل ممتاز.";
                StatusLabel.TextColor = Color.FromArgb("#D4AF37");
            }
            else
            {
                int remainingToday = expectedPageToBeAt - currentReadPage;
                TodayPortionSummaryLabel.Text = $"متبقى لك {remainingToday} صفحة لتصل إلى هدف اليوم ({goalAyah?.Surah?.Name}، آية {goalAyah?.NumberInSurah})";
                StatusLabel.Text = $"{planName} - اليوم {daysPassed + 1} من {durationDays}";
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
        // Find tab for Mushaf and navigate to it
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
            Preferences.Default.Remove(PlanNameKey);
            Preferences.Default.Remove(PlanDurationKey);
            Preferences.Default.Remove(PlanStartDateKey);
            Preferences.Default.Remove(PlanStartPageKey);
            Preferences.Default.Remove(PlanEndPageKey);
            Preferences.Default.Remove(PlanColorKey);
            Preferences.Default.Remove(PlanReminderEnabledKey);
            Preferences.Default.Remove(PlanReminderTimeKey);

            // Re-schedule alarms to clear the Khatmah reminder
            await _notificationService.EnsureNotificationsScheduledAsync(true);

            RefreshUI();
        }
    }

    private async void OnOpenCreatePopupClicked(object sender, EventArgs e)
    {
        var popup = new CreateKhatmahPopup();
        var result = await this.ShowPopupAsync(popup);

        if (result is KhatmahPlanConfig config)
        {
            CreatePlan(config);
        }
    }

    private async void CreatePlan(KhatmahPlanConfig config)
    {
        Preferences.Default.Set(PlanActiveKey, true);
        Preferences.Default.Set(PlanNameKey, config.Name);
        Preferences.Default.Set(PlanDurationKey, config.DurationDays);
        Preferences.Default.Set(PlanStartDateKey, DateTime.Now.Date);
        Preferences.Default.Set(PlanStartPageKey, config.StartPage);
        Preferences.Default.Set(PlanEndPageKey, config.EndPage);
        Preferences.Default.Set(PlanColorKey, config.ColorHex);
        Preferences.Default.Set(PlanReminderEnabledKey, config.ReminderEnabled);
        Preferences.Default.Set(PlanReminderTimeKey, config.ReminderTime.Ticks);

        // Reset user's current read page to start page of the plan
        Preferences.Default.Set("Mushaf_LastReadPage", config.StartPage);

        // Schedule new daily reminder notification
        await _notificationService.EnsureNotificationsScheduledAsync(true);

        RefreshUI();
    }
}

