using Plugin.LocalNotification;
using Microsoft.Maui.Devices;
#if ANDROID
using Plugin.LocalNotification.AndroidOption;
using AndroidImportance = Plugin.LocalNotification.AndroidOption.AndroidImportance;
using Android.App;
using Android.Content;
#endif

namespace AthkarApp.Services;

public interface IAthkarNotificationService
{
    Task<bool> RequestPermissionsAsync();
    Task EnsureScheduledTodayAsync();
}

public class AthkarNotificationService : IAthkarNotificationService
{
    // ============================================================
    // قائمة ملفات الصوت - أضف أي ملف wav إلى:
    //   - Platforms/Android/Resources/raw/     (بدون امتداد في القائمة)
    //   - Resources/Raw/                        (للـ Raw assets)
    // ثم أضف اسمه هنا. مثال: "ah", "subhan", "takbir"
    // ============================================================
    private static readonly string[] SoundFiles = { "om", "ah" };

    private const string LastScheduledDateKey  = "athkar_last_scheduled_date";
    private const string LastSoundIndexKey     = "athkar_last_sound_index";
    private const string WindowsSoundDir       = "ms-appx:///Assets/";
    private const int    NotificationBaseId    = 1000;

    // ==================== الصلاحيات ====================

    public async Task<bool> RequestPermissionsAsync()
    {
        bool allowed = true;
        if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
            allowed = await LocalNotificationCenter.Current.RequestNotificationPermission();

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var activity    = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            var alarmMgr    = activity?.GetSystemService(Context.AlarmService) as AlarmManager;
            if (alarmMgr != null && !alarmMgr.CanScheduleExactAlarms())
            {
                var intent = new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                intent.SetData(Android.Net.Uri.Parse("package:" + activity!.PackageName));
                intent.AddFlags(ActivityFlags.NewTask);
                activity.StartActivity(intent);
            }
        }
#endif
        return allowed;
    }

    // ==================== جدولة مرة واحدة في اليوم ====================

    public async Task EnsureScheduledTodayAsync()
    {
        try
        {
            string todayStr    = DateTime.Today.ToString("yyyy-MM-dd");
            string lastDate    = Preferences.Default.Get(LastScheduledDateKey, string.Empty);

            if (lastDate == todayStr)
            {
                System.Diagnostics.Debug.WriteLine("🔔 الإشعارات مجدولة بالفعل لهذا اليوم.");
                return;
            }

            // إلغاء الجدولات القديمة لكل الأصوات
            for (int i = 0; i < SoundFiles.Length; i++)
                LocalNotificationCenter.Current.Cancel(NotificationBaseId + i);

            // اختيار الصوت التالي بالتناوب
            int lastIndex  = Preferences.Default.Get(LastSoundIndexKey, -1);
            int nextIndex  = (lastIndex + 1) % SoundFiles.Length;
            string sound   = SoundFiles[nextIndex];

            await ScheduleWithSoundAsync(sound, nextIndex);

            // حفظ التاريخ والصوت الحالي
            Preferences.Default.Set(LastScheduledDateKey, todayStr);
            Preferences.Default.Set(LastSoundIndexKey, nextIndex);

            System.Diagnostics.Debug.WriteLine($"🔔 تم التحديث: صوت \"{sound}\" (رقم {nextIndex + 1}/{SoundFiles.Length})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ خطأ في EnsureScheduledTodayAsync: {ex.Message}");
        }
    }

    // ==================== الجدولة الفعلية ====================

    private static async Task ScheduleWithSoundAsync(string soundName, int soundIndex)
    {
        string channelId = $"athkar_channel_{soundName}";

#if ANDROID
        // كل صوت يحتاج قناة مستقلة على Android
        var channelRequest = new NotificationChannelRequest
        {
            Id                  = channelId,
            Name                = $"أذكار — {soundName}",
            Description         = "تنبيهات الأذكار كل ساعة",
            Importance          = AndroidImportance.Max,
            Sound               = soundName,     // اسم الملف في res/raw/ بدون امتداد
            EnableSound         = true,
            EnableVibration     = true
        };

        LocalNotificationCenter.CreateNotificationChannels(
            new List<NotificationChannelRequest> { channelRequest }
        );
#endif

        var request = new NotificationRequest
        {
            NotificationId = NotificationBaseId + soundIndex,
            Title          = "⭐ ذكر الله",
            Description    = "سبحان الله والحمد لله ولا إله إلا الله والله أكبر",
            CategoryType   = NotificationCategoryType.Reminder,

            Sound = DeviceInfo.Platform == DevicePlatform.WinUI
                ? $"{WindowsSoundDir}{soundName}.wav"
                : soundName,

            Schedule = new NotificationRequestSchedule
            {
                NotifyTime           = DateTime.Now.AddSeconds(10),
                NotifyRepeatInterval = TimeSpan.FromHours(1),
                RepeatType           = NotificationRepeat.TimeInterval,
            },

#if ANDROID
            Android = new AndroidOptions
            {
                ChannelId  = channelId,
                Priority   = AndroidPriority.Max,
                AutoCancel = true,
                LaunchApp  = new AndroidLaunch()
            }
#endif
        };

        await LocalNotificationCenter.Current.Show(request);
        System.Diagnostics.Debug.WriteLine($"✅ تم جدولة الإشعار مع صوت: {soundName}");
    }

    // ==================== دالة عامة للاستخدام من BootReceiver ====================

    public static async Task RescheduleAfterBootAsync()
    {
        try
        {
            // مسح تاريخ الجدولة لإجبار إعادة الجدولة
            Preferences.Default.Remove(LastScheduledDateKey);

            int lastIndex = Preferences.Default.Get(LastSoundIndexKey, 0);
            string sound  = SoundFiles[lastIndex % SoundFiles.Length];

            // إلغاء القديم وإعادة الجدولة بعد دقيقة من الريستارت
            for (int i = 0; i < SoundFiles.Length; i++)
                LocalNotificationCenter.Current.Cancel(NotificationBaseId + i);

            string channelId = $"athkar_channel_{sound}";

#if ANDROID
            var channelRequest = new NotificationChannelRequest
            {
                Id                   = channelId,
                Name                 = $"أذكار — {sound}",
                Description          = "تنبيهات الأذكار كل ساعة",
                Importance           = AndroidImportance.Max,
                Sound                = sound,
                EnableSound          = true,
                EnableVibration      = true
            };
            LocalNotificationCenter.CreateNotificationChannels(
                new List<NotificationChannelRequest> { channelRequest }
            );
#endif

            var request = new NotificationRequest
            {
                NotificationId = NotificationBaseId + lastIndex,
                Title          = "⭐ ذكر الله",
                Description    = "سبحان الله والحمد لله ولا إله إلا الله والله أكبر",
                CategoryType   = NotificationCategoryType.Reminder,
                Sound          = sound,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime           = DateTime.Now.AddMinutes(1),
                    NotifyRepeatInterval = TimeSpan.FromHours(1),
                    RepeatType           = NotificationRepeat.TimeInterval,
                },
#if ANDROID
                Android = new AndroidOptions
                {
                    ChannelId  = channelId,
                    Priority   = AndroidPriority.Max,
                    AutoCancel = true,
                    LaunchApp  = new AndroidLaunch()
                }
#endif
            };

            await LocalNotificationCenter.Current.Show(request);
            System.Diagnostics.Debug.WriteLine($"✅ BootReceiver: إعادة جدولة بصوت {sound}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ RescheduleAfterBootAsync: {ex.Message}");
        }
    }
}
