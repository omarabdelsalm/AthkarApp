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
    Task ScheduleHourlyNotificationAsync();
}

public class AthkarNotificationService : IAthkarNotificationService
{
    private const string SoundFileName = "om";
    private const string ChannelId = "athkar_hourly_channel_v4";
    private const string WindowsSoundPath = "ms-appx:///Assets/om.wav";

    public async Task<bool> RequestPermissionsAsync()
    {
        bool allowed = true;
        if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
        {
            allowed = await LocalNotificationCenter.Current.RequestNotificationPermission();
        }

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            var alarmManager = activity?.GetSystemService(Android.Content.Context.AlarmService) as Android.App.AlarmManager;
            
            if (alarmManager != null && !alarmManager.CanScheduleExactAlarms())
            {
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                intent.SetData(Android.Net.Uri.Parse("package:" + activity.PackageName));
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                activity.StartActivity(intent);
            }
        }
#endif
        return allowed;
    }

    public async Task ScheduleHourlyNotificationAsync()
    {
        try
        {
#if ANDROID
            // إعداد القناة للأندرويد لضمان عمل الصوت المخصص
            var channelRequest = new NotificationChannelRequest
            {
                Id = ChannelId,
                Name = "Athkar Hourly Notifications",
                Description = "تنبيهات الأذكار كل ساعة", 
                Importance = AndroidImportance.Max,
                Sound = SoundFileName,
                EnableSound = true
            };

            LocalNotificationCenter.CreateNotificationChannels(new List<NotificationChannelRequest> { channelRequest });
#endif

            var notificationRequest = new NotificationRequest
            {
                NotificationId = 1000,
                Title = "ذكرالله",
                Description = "سبحان الله والحمد لله ولا إله إلا الله والله أكبر",
                Sound = DeviceInfo.Platform == DevicePlatform.WinUI ? WindowsSoundPath : SoundFileName,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddSeconds(10),
                    NotifyRepeatInterval = TimeSpan.FromHours(1),
                    RepeatType = NotificationRepeat.TimeInterval
                },
#if ANDROID
                Android = new AndroidOptions
                {
                    ChannelId = ChannelId,
                    Priority = AndroidPriority.Max,
                    LaunchApp = new AndroidLaunch()
                }
#endif
            };

            await LocalNotificationCenter.Current.Show(notificationRequest);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scheduling notification: {ex.Message}");
        }
    }
}
