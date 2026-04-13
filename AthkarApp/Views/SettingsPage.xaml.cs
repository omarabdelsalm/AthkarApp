using AthkarApp.Services;
using Plugin.LocalNotification;

namespace AthkarApp.Views;

public partial class SettingsPage : ContentPage
{
    private readonly IAthkarNotificationService _notificationService;

    public SettingsPage(IAthkarNotificationService notificationService)
    {
        InitializeComponent();
        _notificationService = notificationService;

        // تحميل الإعدادات الحالية
        LoadSettings();
    }

    private void LoadSettings()
    {
        SwitchOm.IsToggled = Preferences.Default.Get("Sound_om_Enabled", true);
        SwitchAh.IsToggled = Preferences.Default.Get("Sound_ah_Enabled", true);
        SwitchMa.IsToggled = Preferences.Default.Get("Sound_ma_Enabled", true);
    }

    private async void OnSoundToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("Sound_om_Enabled", SwitchOm.IsToggled);
        Preferences.Default.Set("Sound_ah_Enabled", SwitchAh.IsToggled);
        Preferences.Default.Set("Sound_ma_Enabled", SwitchMa.IsToggled);

        // إعادة الجدولة فوراً لتطبيق الإعدادات الجديدة لكل ساعات اليوم
        await _notificationService.EnsureScheduledTodayAsync();
    }

    private async void OnPreviewOm(object sender, EventArgs e) => await PreviewSound("om");
    private async void OnPreviewAh(object sender, EventArgs e) => await PreviewSound("ah");
    private async void OnPreviewMa(object sender, EventArgs e) => await PreviewSound("ma");

    private async Task PreviewSound(string soundName)
    {
        try
        {
            // معاينة الصوت عبر نظام الإشعارات ليرى المستخدم كيف سيبدو الإشعار الحقيقي
            var request = new NotificationRequest
            {
                NotificationId = 9999, // معرف مؤقت للمعاينة
                Title = "تجربة صوت الأذكار",
                Description = $"هذا هو صوت إشعار {soundName}",
                Sound = DeviceInfo.Platform == DevicePlatform.WinUI ? $"ms-appx:///Assets/{soundName}.wav" : soundName,
                Android = new Plugin.LocalNotification.AndroidOption.AndroidOptions 
                { 
                    Priority = Plugin.LocalNotification.AndroidOption.AndroidPriority.High 
                }
            };
            await LocalNotificationCenter.Current.Show(request);
        }
        catch (Exception ex)
        {
            await DisplayAlert("خطأ", "تعذر تشغيل المعاينة: " + ex.Message, "موافق");
        }
    }
}
