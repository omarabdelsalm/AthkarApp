#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AthkarApp.Services;

namespace AthkarApp.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true, Name = "com.Almanar.athkarapp.AlarmReceiver")]
public class AlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        // Acquire simple WakeLock to prevent CPU sleep during Android 8 Foreground Service startup
        PowerManager? pm = (PowerManager?)context.GetSystemService(Context.PowerService);
        PowerManager.WakeLock? wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "AthkarApp::AlarmWakeLock");
        wakeLock?.Acquire(5000); // Hold for 5 seconds max

        var id = intent.GetIntExtra("id", 0);
        var type = intent.GetStringExtra("type"); // "athkar" or "adhan"
        var soundName = intent.GetStringExtra("soundName") ?? "om";
        var text = intent.GetStringExtra("text") ?? "";
        var prayerName = intent.GetStringExtra("prayerName") ?? "";

        if (soundName == "silent")
        {
            NativeNotificationHelper.ShowPreAdhanNotification(context, id, prayerName);
            return;
        }

        if (type == "adhan")
        {
            // للأذان: نبدأ الخدمة لتشغيل الصوت بالكامل وعرض إشعار مستمر
            var serviceIntent = new Intent(context, typeof(AthkarForegroundService));
            serviceIntent.SetAction("PLAY_ADHAN");
            serviceIntent.PutExtra("id", id);
            serviceIntent.PutExtra("prayerName", prayerName);
            serviceIntent.PutExtra("soundName", soundName);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(serviceIntent);
            }
            else
            {
                context.StartService(serviceIntent);
            }

            // سنعتمد على الخدمة لعرض إشعار الأذان مع زر الإيقاف
        }
        else
        {
            // للأذكار: إشعار عادي مع صوت
            NativeNotificationHelper.ShowAthkarNotification(context, id, text, soundName);
        }

        // الحل الجذري: إعادة جدولة الإشعارات لليوم التالي لضمان الاستمرارية
        _ = Task.Run(async () =>
        {
            try
            {
                // نستخدم IPlatformApplication للحصول على الخدمة من DI
                var notificationService = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<IAthkarNotificationService>();
                
                if (notificationService == null)
                {
                    notificationService = IPlatformApplication.Current?.Services.GetService<IAthkarNotificationService>();
                }

                if (notificationService != null)
                {
                    // إعادة الجدولة للتأكد من وجود مواعيد لـ 24 ساعة القادمة
                    await notificationService.EnsureNotificationsScheduledAsync(true);
                }
            }
            catch { }
        });
    }
}
#endif
