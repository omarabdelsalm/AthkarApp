using AthkarApp.Models;
using Microsoft.Maui.Devices;
// أضف هذا السطر

namespace AthkarApp.Services;

public interface IAthkarNotificationService
{
    Task<bool> RequestPermissionsAsync();
    Task EnsureNotificationsScheduledAsync(bool force = false);
    Task ShowNotificationPreviewAsync(string soundName);
    Task ShowAdhanPreviewAsync(string soundName);
    void StartForegroundService();
}

public class AthkarNotificationService : IAthkarNotificationService
{
    private readonly IAppNotificationService _nativeService;

    // الأصوات والذكار
    private static readonly string[] SoundFiles = { "om", "ah", "ma" };
    private static readonly string[] AthkarTexts = 
    {
        "سبحان الله وبحمده، سبحان الله العظيم",
        "لا إله إلا الله وحده لا شريك له، له الملك وله الحمد",
        "أستغفر الله وأتوب إليه",
        "اللهم صلِّ وسلم على نبينا محمد",
        "لا حول ولا قوة إلا بالله العلي العظيم",
        "الحمد لله رب العالمين",
        "الله أكبر كبيراً، والحمد لله كثيراً",
        "لا إله إلا أنت سبحانك إني كنت من الظالمين",
        "حسبي الله ونعم الوكيل",
        "سبحان الله، والحمد لله، ولا إله إلا الله، والله أكبر",
        "لا إله إلا الله الملك الحق المبين",
        "يا حي يا قيوم برحمتك أستغيث"
    };

    private const string LastScheduledDateKey = "athkar_last_scheduled_date";
    private const string LastSoundIndexKey    = "athkar_last_sound_index";
    private const int    NotificationBaseId   = 1000;

    public AthkarNotificationService()
    {
#if ANDROID
        _nativeService = new AthkarApp.Platforms.Android.AndroidNotificationService();
#elif WINDOWS
        _nativeService = new AthkarApp.Platforms.Windows.WindowsNotificationService();
#else
        _nativeService = null!; // Not implemented for other platforms
#endif
    }

    public async Task<bool> RequestPermissionsAsync()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            return status == PermissionStatus.Granted;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
             var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
             var alarmMgr = activity?.GetSystemService(Android.Content.Context.AlarmService) as Android.App.AlarmManager;
             if (alarmMgr != null && !alarmMgr.CanScheduleExactAlarms())
             {
                 var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                 intent.SetData(Android.Net.Uri.Parse("package:" + activity!.PackageName));
                 intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                 activity.StartActivity(intent);
             }
        }
#endif
        return true;
    }


    public void StartForegroundService()
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var intent = new Android.Content.Intent(context, typeof(AthkarApp.Services.AthkarForegroundService));
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                context.StartForegroundService(intent);
            else
                context.StartService(intent);
        }
        catch { }
#endif
    }

    public async Task EnsureNotificationsScheduledAsync(bool force = false)
    {
        string currentHourStr = DateTime.Now.ToString("yyyy-MM-dd-HH");
        string lastScheduled = Preferences.Default.Get(LastScheduledDateKey, "");

        // إذا كان المنبه قد تم جدولته لهذه الساعة بالفعل ولا يوجد طلب إجباري، نتخطى
        if (!force && lastScheduled == currentHourStr) return;

#if ANDROID
        StartForegroundService();
#endif

        // --- 1. جدولة الأذكار (الوضع الحالي) ---
        var enabledSounds = GetEnabledSounds();
        int lastIndex = Preferences.Default.Get(LastSoundIndexKey, 0);
        int athkarCount = 0;

        DateTime now = DateTime.Now;
        for (int i = 1; i <= 24; i++)
        {
            DateTime notifyTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(i);
            int hour = notifyTime.Hour;

            if (hour >= 1 && hour <= 4) continue;

            string text = AthkarTexts[(lastIndex + athkarCount) % AthkarTexts.Length];
            string sound = enabledSounds.Any() ? enabledSounds[(lastIndex + athkarCount) % enabledSounds.Count] : "om";

            _nativeService.ScheduleAthkarAlarm(NotificationBaseId + hour, text, sound, notifyTime);
            athkarCount++;
        }

        // --- 2. جدولة مواقيت الصلاة (الإصلاح الجديد) ---
        try
        {
            var prayerService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<IPrayerService>();
            if (prayerService == null)
            {
                prayerService = IPlatformApplication.Current?.Services.GetService<IPrayerService>();
            }

            if (prayerService != null)
            {
                var timings = await prayerService.GetPrayerTimingsAsync();
                if (timings != null)
                {
                    await prayerService.ScheduleAdhanNotificationsAsync(timings);
                }
            }
        }
        catch { }

        Preferences.Default.Set(LastScheduledDateKey, currentHourStr);
        Preferences.Default.Set(LastSoundIndexKey, (lastIndex + athkarCount) % 1000);
        await Task.CompletedTask;
    }

    public async Task ShowNotificationPreviewAsync(string soundName)
    {
        _nativeService.ScheduleAthkarAlarm(9999, "تجربة صوت الأذكار", soundName, DateTime.Now.AddSeconds(2));
        await Task.CompletedTask;
    }

    public async Task ShowAdhanPreviewAsync(string soundName)
    {
        _nativeService.ScheduleAdhanAlarm(7777, "تجربة الأذان", soundName, DateTime.Now.AddSeconds(2));
        await Task.CompletedTask;
    }

    private List<string> GetEnabledSounds()
    {
        var enabled = new List<string>();
        foreach (var s in SoundFiles)
            if (Preferences.Default.Get($"Sound_{s}_Enabled", true)) enabled.Add(s);
        return enabled;
    }

    public static async Task RescheduleAfterBootAsync()
    {
        await new AthkarNotificationService().EnsureNotificationsScheduledAsync(true);
    }
}

