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

    // ==================== جدولة مرة واحدة في اليوم ====================

    public async Task EnsureScheduledTodayAsync()
    {
        try
        {
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            string lastDate = Preferences.Default.Get(LastScheduledDateKey, string.Empty);

            if (lastDate == todayStr) return;

            await InternalScheduleBatchAsync();

            Preferences.Default.Set(LastScheduledDateKey, todayStr);
           
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ EnsureScheduledTodayAsync Error: {ex.Message}");
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

        // 3. جدولة التنبيهات للأربع وعشرين ساعة القادمة
        for (int i = 0; i < 24; i++)
        {
            DateTime notifyTime = DateTime.Now.AddHours(i + 1);
            int hour = notifyTime.Hour;

            // استثناء فترة الهدوء (11 م - 6 ص)
            if (hour >= 23 || hour < 6) continue;

            // اختيار الذكر التالي
            int textIndex = (lastIndex + scheduledCount) % AthkarTexts.Length;
            string text = AthkarTexts[textIndex];

            // اختيار الصوت (إذا كانت القائمة فارغة، نرسل إشعاراً صامتاً)
            string sound = null;
            if (enabledSounds.Any())
            {
                int soundIndex = (lastIndex + scheduledCount) % enabledSounds.Count;
                sound = enabledSounds[soundIndex];
            }
            
            await ScheduleNotificationAsync(sound, text, scheduledCount, notifyTime);
            scheduledCount++;
        }

        // 4. حفظ المؤشر القادم للتناسق
        Preferences.Default.Set(LastSoundIndexKey, (lastIndex + scheduledCount) % 1000);
    }

    private static async Task ScheduleNotificationAsync(string soundName, string description, int sequenceIndex, DateTime notifyTime)
    {
        string channelId = $"athkar_channel_{soundName}";

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
            NotificationId = NotificationBaseId + sequenceIndex,
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
            // عند الريستارت، نمسح تاريخ اليوم لنجبر النظام على إعادة الجدولة
            Preferences.Default.Remove(LastScheduledDateKey);
            await InternalScheduleBatchAsync();
          //  System.Diagnostics.Debug.WriteLine($"✅ BootReceiver: تم إعادة جدولة الأذكار بعد تشغيل الجهاز.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ RescheduleAfterBootAsync: {ex.Message}");
        }
    }
}
