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
    Name = "com.almanar.athkarapp.BootReceiver")]
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


        var pendingResult = GoAsync();
        
        // نستخدم Task.Run لأن OnReceive يجب أن يكون متزامناً
        Task.Run(async () =>
        {
            try 
            {
                var service = new AthkarNotificationService();
                service.StartForegroundService();
                await AthkarNotificationService.RescheduleAfterBootAsync();

                // استخدم Dependency Injection للحصول على الخدمة بدلاً من إنشائها يدوياً
                var prayerService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<IPrayerService>();
                
                // بديل آخر في حال لم تكن الواجهة الرسومية قد بدأت بعد (وهو الأفضل للـ BootReceiver)
                if (prayerService == null)
                {
                    prayerService = IPlatformApplication.Current?.Services.GetService<IPrayerService>();
                }

                if (prayerService != null)
                {
                    var data = await prayerService.GetPrayerTimingsAsync(false);
                    if (data != null)
                    {
                        await prayerService.ScheduleAdhanNotificationsAsync(data);
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                pendingResult.Finish();
            }
        });
    }
}
