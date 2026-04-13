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

        // نستخدم Task.Run لأن OnReceive يجب أن يكون متزامناً
        Task.Run(async () =>
        {
            await AthkarNotificationService.RescheduleAfterBootAsync();
        });
    }
}
