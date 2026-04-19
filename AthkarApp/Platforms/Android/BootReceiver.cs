using Android.App;
using Android.Content;
using AthkarApp.Services;

namespace AthkarApp.Platforms.Android;

/// <summary>
/// يُستدعى تلقائياً عند إعادة تشغيل الجهاز لإعادة جدولة إشعارات الأذكار
/// لأن AlarmManager يفقد كل الجداول عند الريستارت
/// </summary>
[BroadcastReceiver(
    Enabled = true,
    Exported = true,
    DirectBootAware = true,
    Name = "com.Almanar.athkarapp.BootReceiver")]
[IntentFilter(
    new[] { Intent.ActionBootCompleted, "android.intent.action.QUICKBOOT_POWERON" },
    Categories = new[] { Intent.CategoryDefault })]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != Intent.ActionBootCompleted &&
            intent?.Action != "android.intent.action.QUICKBOOT_POWERON")
            return;

       // System.Diagnostics.Debug.WriteLine("📱 BootReceiver: الجهاز أُعيد تشغيله - إعادة جدولة الإشعارات...");

        var pendingResult = GoAsync();
        
        // نستخدم Task.Run لأن OnReceive يجب أن يكون متزامناً
        Task.Run(async () =>
        {
            try 
            {
                var service = new AthkarNotificationService();
                service.StartForegroundService();
                await AthkarNotificationService.RescheduleAfterBootAsync();

                var fileStorage = new FileStorageService();
                using var httpClient = new HttpClient();
                var prayerService = new PrayerService(httpClient, fileStorage);
                var data = await prayerService.GetPrayerTimingsAsync(false);
                if (data != null)
                {
                    await prayerService.ScheduleAdhanNotificationsAsync(data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ BootReceiver Error: {ex.Message}");
            }
            finally
            {
                pendingResult.Finish();
            }
        });
    }
}
