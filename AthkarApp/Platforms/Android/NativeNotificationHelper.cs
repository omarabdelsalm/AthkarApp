#if ANDROID
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;

namespace AthkarApp.Platforms.Android;

public static class NativeNotificationHelper
{
    private const string AthkarChannelIdPrefix = "athkar_native_channel_";
    private const string AdhanChannelIdPrefix = "adhan_native_channel_";

    public static void CreateAthkarChannel(Context context, string soundName)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        // نسخة جديدة للإشعار لضمان تحديثه بناء على الأندرويد 8
        string channelId = $"{AthkarChannelIdPrefix}{soundName}_v5";

        if (manager.GetNotificationChannel(channelId) == null)
        {
            var channel = new NotificationChannel(channelId, "الأذكار", NotificationImportance.Max)
            {
                Description = "تنبيهات الأذكار الدورية"
            };

            if (soundName != "silent")
            {
                int resId = context.Resources!.GetIdentifier(soundName, "raw", context.PackageName);
                if (resId != 0)
                {
                    var soundUri = global::Android.Net.Uri.Parse($"android.resource://{context.PackageName}/{resId}");
                    var audioAttributes = new AudioAttributes.Builder()
                        .SetUsage(AudioUsageKind.Notification)
                        .SetContentType(AudioContentType.Sonification)
                        .Build();
                    channel.SetSound(soundUri, audioAttributes);
                }
                channel.EnableVibration(true);
            }
            else
            {
                channel.SetSound(null, null);
                channel.EnableVibration(false);
            }

            channel.LockscreenVisibility = NotificationVisibility.Public;
            manager.CreateNotificationChannel(channel);
        }
    }

    public static void CreateAdhanChannel(Context context, string soundName)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        // نسخة v8 لضمان تحديث الإعدادات
        string channelId = $"{AdhanChannelIdPrefix}{soundName}_v8";

        if (manager.GetNotificationChannel(channelId) == null)
        {
            int resId = context.Resources!.GetIdentifier(soundName, "raw", context.PackageName);
            if (resId == 0) return;

            // استخدام ID المورد مباشرة هو الأكثر ضماناً في أندرويد
            var soundUri = global::Android.Net.Uri.Parse($"android.resource://{context.PackageName}/{resId}");
            
            var audioAttributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Notification)
                .SetContentType(AudioContentType.Sonification)
                .Build();

            var channel = new NotificationChannel(channelId, "الأذان", NotificationImportance.Max)
            {
                Description = "تنبيهات مواقيت الصلاة"
            };
            channel.SetSound(soundUri, audioAttributes);
            channel.EnableVibration(true);
            channel.SetBypassDnd(true);
            channel.LockscreenVisibility = NotificationVisibility.Public;
            manager.CreateNotificationChannel(channel);
        }
    }

    public static void ShowAthkarNotification(Context context, int id, string text, string soundName)
    {
        CreateAthkarChannel(context, soundName);
        string channelId = $"{AthkarChannelIdPrefix}{soundName}_v5";

        var builder = new NotificationCompat.Builder(context, channelId)
            .SetSmallIcon(context.ApplicationInfo!.Icon)
            .SetContentTitle("⭐ ذكر الله")
            .SetContentText(text)
            .SetPriority(NotificationCompat.PriorityMax)
            .SetAutoCancel(true);

        if (soundName != "silent")
        {
            int resId = context.Resources!.GetIdentifier(soundName, "raw", context.PackageName);
            if (resId != 0)
            {
                var soundUri = global::Android.Net.Uri.Parse($"android.resource://{context.PackageName}/{resId}");
                builder.SetSound(soundUri);
            }
            builder.SetDefaults((int)NotificationDefaults.Vibrate | (int)NotificationDefaults.Lights);
        }

        var intent = context.PackageManager!.GetLaunchIntentForPackage(context.PackageName!)!;
        var pendingIntent = PendingIntent.GetActivity(context, id, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        builder.SetContentIntent(pendingIntent);

        var manager = NotificationManagerCompat.From(context);
        manager.Notify(id, builder.Build());
    }

    public static void ShowPreAdhanNotification(Context context, int id, string prayerName)
    {
        string soundName = "silent";
        CreateAthkarChannel(context, soundName);
        string channelId = $"{AthkarChannelIdPrefix}{soundName}_v5";

        var builder = new NotificationCompat.Builder(context, channelId)
            .SetSmallIcon(context.ApplicationInfo!.Icon)
            .SetContentTitle("🕌 اقتربت الصلاة")
            .SetContentText($"بقي دقيقتان على أذان {prayerName}")
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetAutoCancel(true);

        var intent = context.PackageManager!.GetLaunchIntentForPackage(context.PackageName!)!;
        var pendingIntent = PendingIntent.GetActivity(context, id, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        builder.SetContentIntent(pendingIntent);

        var manager = NotificationManagerCompat.From(context);
        manager.Notify(id, builder.Build());
    }
}
#endif
