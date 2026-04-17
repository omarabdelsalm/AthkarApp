using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly IAthkarNotificationService _notificationService;
    private readonly IQuranDownloadService _quranDownloadService;

    private readonly IPrayerService _prayerService;

    public SettingsPage(IAthkarNotificationService notificationService, IQuranDownloadService quranDownloadService, IPrayerService prayerService)
    {
        InitializeComponent();
        _notificationService = notificationService;
        _quranDownloadService = quranDownloadService;
        _prayerService = prayerService;

        // تحميل الإعدادات الحالية
        LoadSettings();
        LoadAdhanOptions();
        LoadCalculationSettings();
    }

    private void LoadCalculationSettings()
    {
        var methods = CalculationMethodOption.GetMethods();
        CalculationMethodPicker.ItemsSource = methods;
        CalculationMethodPicker.ItemDisplayBinding = new Binding("Name");

        int selectedMethod = Preferences.Default.Get("SelectedCalculationMethod", 5);
        CalculationMethodPicker.SelectedItem = methods.FirstOrDefault(m => m.Id == selectedMethod);

        var madhabs = MadhabOption.GetMadhabs();
        MadhabPicker.ItemsSource = madhabs;
        MadhabPicker.ItemDisplayBinding = new Binding("Name");

        int selectedMadhab = Preferences.Default.Get("SelectedMadhab", 0);
        MadhabPicker.SelectedItem = madhabs.FirstOrDefault(m => m.Id == selectedMadhab);
    }

    private void LoadAdhanOptions()
    {
        var options = AdhanOption.GetAvailableAdhans();
        AdhanPicker.ItemsSource = options;
        AdhanPicker.ItemDisplayBinding = new Binding("Name");

        string selected = Preferences.Default.Get("SelectedAdhanSound", "adhan");
        var selectedItem = options.FirstOrDefault(o => o.FileName == selected);
        if (selectedItem != null)
        {
            AdhanPicker.SelectedItem = selectedItem;
        }
    }

    private void LoadSettings()
    {
        SwitchOm.IsToggled = Preferences.Default.Get("Sound_om_Enabled", true);
        SwitchAh.IsToggled = Preferences.Default.Get("Sound_ah_Enabled", true);
        SwitchMa.IsToggled = Preferences.Default.Get("Sound_ma_Enabled", true);

        // تحميل قائمة القراء
        var reciters = QuranReciter.GetPopularReciters();
        ReciterPicker.ItemsSource = reciters;

        var selectedId = Preferences.Default.Get("SelectedReciterId", "ar.alafasy");
        var selectedReciter = reciters.FirstOrDefault(r => r.Id == selectedId);
        if (selectedReciter != null)
        {
            ReciterPicker.SelectedItem = selectedReciter;
        }
    }

    private void OnReciterChanged(object sender, EventArgs e)
    {
        if (ReciterPicker.SelectedItem is QuranReciter reciter)
        {
            Preferences.Default.Set("SelectedReciterId", reciter.Id);
            Preferences.Default.Set("SelectedReciterFolder", reciter.AudioFolderName);
            Preferences.Default.Set("SelectedReciterName", reciter.Name);
        }
    }

    private async void OnClearDownloadsClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("تأكيد الممسح", "هل أنت متأكد من مسح كافة ملفات الصوت المحملة؟ ستحتاج لترتيب تحميلها مرة أخرى للاستماع بدون إنترنت.", "نعم، امسح الكل", "إلغاء");
        if (confirm)
        {
            try
            {
                await _quranDownloadService.ClearAllDownloadsAsync();
                await DisplayAlert("تم", "تم مسح كافة التنزيلات بنجاح.", "حسناً");
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", $"فشل مسح الملفات: {ex.Message}", "حسناً");
            }
        }
    }

    private async void OnCalculationSettingChanged(object sender, EventArgs e)
    {
        if (CalculationMethodPicker.SelectedItem is CalculationMethodOption method)
        {
            Preferences.Default.Set("SelectedCalculationMethod", method.Id);
        }

        if (MadhabPicker.SelectedItem is MadhabOption madhab)
        {
            Preferences.Default.Set("SelectedMadhab", madhab.Id);
        }

        // إعادة جدولة الإشعارات فوراً بناءً على الطريقة الجديدة
        var data = await _prayerService.GetPrayerTimingsAsync();
        if (data != null)
        {
            await _prayerService.ScheduleAdhanNotificationsAsync(data);
        }
    }
    private async void OnAdhanSelected(object sender, EventArgs e)
    {
        if (AdhanPicker.SelectedItem is AdhanOption selected)
        {
            Preferences.Default.Set("SelectedAdhanSound", selected.FileName);
            
            // إعادة جدولة الإشعارات بالصوت الجديد
            var timings = await _prayerService.GetPrayerTimingsAsync();
            if (timings != null)
            {
                await _prayerService.ScheduleAdhanNotificationsAsync(timings);
            }
        }
    }

    private async void OnPreviewAdhan(object sender, EventArgs e)
    {
        string sound = Preferences.Default.Get("SelectedAdhanSound", "adhan");
        await _notificationService.ShowAdhanPreviewAsync(sound);
    }

    private async void OnSoundToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("Sound_om_Enabled", SwitchOm.IsToggled);
        Preferences.Default.Set("Sound_ah_Enabled", SwitchAh.IsToggled);
        Preferences.Default.Set("Sound_ma_Enabled", SwitchMa.IsToggled);

        // إعادة الجدولة فوراً لتطبيق الإعدادات الجديدة لكل ساعات اليوم
        await _notificationService.EnsureNotificationsScheduledAsync(force: true);
    }

    private async void OnPreviewOm(object sender, EventArgs e) => await PreviewSound("om");
    private async void OnPreviewAh(object sender, EventArgs e) => await PreviewSound("ah");
    private async void OnPreviewMa(object sender, EventArgs e) => await PreviewSound("ma");

    private async Task PreviewSound(string soundName)
    {
        await _notificationService.ShowNotificationPreviewAsync(soundName);
    }
}
