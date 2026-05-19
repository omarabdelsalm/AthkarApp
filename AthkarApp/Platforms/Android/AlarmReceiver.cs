#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AthkarApp.Services;

namespace AthkarApp.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true, Name = "com.almanar.athkarapp.AlarmReceiver")]
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

            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    context.StartForegroundService(serviceIntent);
                }
                else
                {
                    context.StartService(serviceIntent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start service from receiver: {ex.Message}");
            }
        }
        else
        {
            // للأذكار: تشغيل الصوت القصير مباشرة عبر MediaPlayer في خلفية الـ Receiver
            // هذا يلغي تماماً الحاجة لبدء Foreground Service ويمنع انهيار النظام عند تشغيل الأذكار التذكيرية في الخلفية.
            Task.Run(() =>
            {
                try
                {
                    int resId = context.Resources!.GetIdentifier(soundName, "raw", context.PackageName);
                    if (resId != 0)
                    {
                        var player = new global::Android.Media.MediaPlayer();
                        player.SetAudioAttributes(new global::Android.Media.AudioAttributes.Builder()
                            .SetUsage(global::Android.Media.AudioUsageKind.Notification)
                            .SetContentType(global::Android.Media.AudioContentType.Sonification)
                            .Build());

                        var soundUri = global::Android.Net.Uri.Parse($"android.resource://{context.PackageName}/{resId}");
                        player.SetDataSource(context, soundUri);
                        player.Prepared += (s, e) => player.Start();
                        player.Completion += (s, e) => player.Release();
                        player.Prepare();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to play short sound directly: {ex.Message}");
                }
            });

            // عرض الإشعار مباشرة بشكل صامت لأن الصوت تم تشغيله بالفعل
            NativeNotificationHelper.ShowAthkarNotification(context, id, text, "silent");
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
