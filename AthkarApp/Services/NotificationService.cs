using Plugin.LocalNotification;
using Microsoft.Maui.Devices;
using System.Linq;
#if ANDROID
using Plugin.LocalNotification.AndroidOption;
using AndroidImportance = Plugin.LocalNotification.AndroidOption.AndroidImportance;
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace AthkarApp.Services;

public interface IAthkarNotificationService
{
    Task<bool> RequestPermissionsAsync();
    Task EnsureNotificationsScheduledAsync(bool force = false);
    Task ShowNotificationPreviewAsync(string soundName);
    Task<bool> RequestBatteryOptimizationAsync();
}

public class AthkarNotificationService : IAthkarNotificationService
{
    // ============================================================
    // قائمة ملفات الصوت - أضف أي ملف wav إلى:
    //   - Platforms/Android/Resources/raw/     (بدون امتداد في القائمة)
    //   - Resources/Raw/                        (للـ Raw assets)
    // ثم أضف اسمه هنا. مثال: "ah", "subhan", "takbir"
    // ============================================================
    private static readonly string[] SoundFiles = { "om", "ah", "ma" };

    // مصفوفة الأذكار المتغيرة للإشعارات
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

    private const string LastScheduledDateKey        = "athkar_last_scheduled_date";
    private const string LastSoundIndexKey           = "athkar_last_sound_index";
    private const string BatteryOptimizationRequestedKey = "athkar_battery_requested";
    private const string WindowsSoundDir             = "ms-appx:///Assets/";
    private const int    NotificationBaseId          = 1000;

    // الحصول على قائمة الأصوات التي فعلها المستخدم في الإعدادات
    public static List<string> GetEnabledSounds()
    {
        var enabled = new List<string>();
        foreach (var sound in SoundFiles)
        {
            // القيمة الافتراضية هي true (مفعل)
            if (Preferences.Default.Get($"Sound_{sound}_Enabled", true))
            {
                enabled.Add(sound);
            }
        }
        return enabled;
    }

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

    public async Task<bool> RequestBatteryOptimizationAsync()
    {
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity == null) return false;

        var powerManager = activity.GetSystemService(Context.PowerService) as PowerManager;
        if (powerManager != null && !powerManager.IsIgnoringBatteryOptimizations(activity.PackageName))
        {
            // نطلبها مرة واحدة فقط تلقائياً لتجنب إزعاج المستخدم
            if (Preferences.Default.Get(BatteryOptimizationRequestedKey, false)) return false;

            try
            {
                var intent = new Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                intent.SetData(Android.Net.Uri.Parse("package:" + activity.PackageName));
                intent.AddFlags(ActivityFlags.NewTask);
                activity.StartActivity(intent);
                Preferences.Default.Set(BatteryOptimizationRequestedKey, true);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ RequestBatteryOptimizationAsync Error: {ex.Message}");
            }
        }
#endif
        return false;
    }

    // ==================== جدولة منتظمة وثابتة ====================

    public async Task EnsureNotificationsScheduledAsync(bool force = false)
    {
        try
        {
            // التحقق من التاريخ لضمان الجدولة عند التثبيت الأول أو تغيير التاريخ
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            string lastScheduled = Preferences.Default.Get(LastScheduledDateKey, "");

            if (!force && lastScheduled == todayStr)
            {
                // إذا تم الجدولة اليوم بالفعل، نكتفي بالتأكد من وجود الإشعارات
                var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
                if (pending.Count >= 16) return;
            }

            await InternalScheduleBatchAsync();
            Preferences.Default.Set(LastScheduledDateKey, todayStr);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ EnsureNotificationsScheduledAsync Error: {ex.Message}");
        }
    }

    private static async Task InternalScheduleBatchAsync()
    {
        // 1. تنظيف السجلات القديمة
        LocalNotificationCenter.Current.CancelAll();

        // 2. الحصول على الأصوات المفعلة وآخر مؤشر
        var enabledSounds = GetEnabledSounds();
        int lastIndex = Preferences.Default.Get(LastSoundIndexKey, 0);
        int scheduledCount = 0;

        // 3. جدولة التنبيهات لساعات اليوم الثابتة (من 6 صباحاً حتى 10 مساءً)
        for (int hour = 6; hour <= 22; hour++)
        {
            // تحديد وقت التنبيه القادم لهذا الوقت
            DateTime notifyTime = DateTime.Today.AddHours(hour);
            if (notifyTime < DateTime.Now)
            {
                // إذا مر الوقت اليوم، نجعل البداية من غدٍ وتتكرر يومياً بعدها
                notifyTime = notifyTime.AddDays(1);
            }

            // اختيار الذكر التالي
            int textIndex = (lastIndex + scheduledCount) % AthkarTexts.Length;
            string text = AthkarTexts[textIndex];

            // اختيار الصوت
            string sound = null;
            if (enabledSounds.Any())
            {
                int soundIndex = (lastIndex + scheduledCount) % enabledSounds.Count;
                sound = enabledSounds[soundIndex];
            }
            
            // نستخدم معرف مبني على الساعة ليكون فريداً وثابتاً (1000 + الساعة)
            int notificationId = NotificationBaseId + hour;
            await ScheduleNotificationAsync(sound, text, notificationId, notifyTime);
            scheduledCount++;
        }

        // 4. إشعار ترحيبي عند التثبيت الأول للتأكد من عمل النظام
        if (Preferences.Default.Get(LastScheduledDateKey, "") == "")
        {
            await ShowWelcomeNotificationAsync();
        }

        // 5. حفظ المؤشر القادم للتناسق في المرة القادمة التي نعيد فيها الجدولة (تغيير إعدادات مثلاً)
        Preferences.Default.Set(LastSoundIndexKey, (lastIndex + scheduledCount) % 1000);
    }

    private static async Task ShowWelcomeNotificationAsync()
    {
        var request = new NotificationRequest
        {
            NotificationId = 8888,
            Title = "✅ تم تفعيل الأذكار",
            Description = "ستصلك الأذكار آلياً كل ساعة من 6 صباحاً حتى 10 مساءً بإذن الله.",
            Sound = GetEnabledSounds().FirstOrDefault() ?? "om",
#if ANDROID
            Android = new AndroidOptions
            {
                ChannelId = $"athkar_channel_{GetEnabledSounds().FirstOrDefault() ?? "om"}_v2",
                Priority = AndroidPriority.High
            }
#endif
        };
        await LocalNotificationCenter.Current.Show(request);
    }

    public async Task ShowNotificationPreviewAsync(string soundName)
    {
        try
        {
            string channelId = $"athkar_channel_{soundName}_v2";

#if ANDROID
            var channelRequest = new NotificationChannelRequest
            {
                Id                  = channelId,
                Name                = $"أذكار — {soundName}",
                Importance          = AndroidImportance.Max,
                Sound               = soundName,
                EnableSound         = true,
                EnableVibration     = true
            };
            LocalNotificationCenter.CreateNotificationChannels(new List<NotificationChannelRequest> { channelRequest });
#endif

            var request = new NotificationRequest
            {
                NotificationId = 9999,
                Title          = "تجربة صوت الأذكار",
                Description    = $"هذا هو صوت إشعار {soundName}",
                Sound          = DeviceInfo.Platform == DevicePlatform.WinUI ? $"{WindowsSoundDir}{soundName}.wav" : soundName,
#if ANDROID
                Android = new AndroidOptions
                {
                    ChannelId = channelId,
                    Priority = AndroidPriority.High
                }
#endif
            };

            await LocalNotificationCenter.Current.Show(request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ShowNotificationPreviewAsync Error: {ex.Message}");
        }
    }

    private static async Task ScheduleNotificationAsync(string soundName, string description, int notificationId, DateTime notifyTime)
    {
        string channelId = $"athkar_channel_{soundName}_v2";

#if ANDROID
        var channelRequest = new NotificationChannelRequest
        {
            Id                  = channelId,
            Name                = $"أذكار — {soundName}",
            Importance          = AndroidImportance.Max,
            Sound               = soundName,
            EnableSound         = true,
            EnableVibration     = true
        };
        LocalNotificationCenter.CreateNotificationChannels(new List<NotificationChannelRequest> { channelRequest });
#endif

        var request = new NotificationRequest
        {
            NotificationId = notificationId,
            Title          = "⭐ ذكر الله",
            Description    = description,
            CategoryType   = NotificationCategoryType.Reminder,
            Sound          = DeviceInfo.Platform == DevicePlatform.WinUI ? $"{WindowsSoundDir}{soundName}.wav" : soundName,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyTime,
                RepeatType = NotificationRepeat.Daily,
#if ANDROID
                Android = new AndroidScheduleOptions 
                { 
                    AlarmType = AndroidAlarmType.RtcWakeup
                }
#endif
            },
#if ANDROID 
            Android = new AndroidOptions 
            { 
                ChannelId = channelId, 
                Priority = AndroidPriority.High
            }
#endif
        };

        await LocalNotificationCenter.Current.Show(request);
    }

    public static async Task RescheduleAfterBootAsync()
    {
        try
        {
            // عند الريستارت، نجبر النظام على إعادة الجدولة للتأكد من استعادة كل الأوقات
            await (new AthkarNotificationService()).EnsureNotificationsScheduledAsync(force: true);
          //  System.Diagnostics.Debug.WriteLine($"✅ BootReceiver: تم إعادة جدولة الأذكار بعد تشغيل الجهاز.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ RescheduleAfterBootAsync: {ex.Message}");
        }
    }
}
