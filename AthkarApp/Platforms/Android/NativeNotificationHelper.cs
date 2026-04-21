#if ANDROID
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;

namespace AthkarApp.Platforms.Android;

public static class NativeNotificationHelper
{
    private const string AthkarChannelIdPrefix = "athkar_native_channel_";
    private const string AdhanChannelIdPrefix = "adhan_native_channel_";

    // الألوان المميزة للتطبيق
    private static readonly int ColorGold   = unchecked((int)0xFFD4AF37); // الذهبي
    private static readonly int ColorGreen  = unchecked((int)0xFF0B5E1B); // الأخضر الإسلامي
    private static readonly int ColorAdhan  = unchecked((int)0xFF1A3A6B); // الأزرق الداكن للأذان

    // نصوص الأذكار المتنوعة تُعرض كـ تذييل bigText
    private static string GetIslamicGreeting() => new[]
    {
        "سبحان الله وبحمده، سبحان الله العظيم",
        "الله أكبر كبيراً والحمد لله كثيراً",
        "لا إله إلا الله وحده لا شريك له",
        "اللهم صل وسلم على سيدنا محمد",
        "استغفر الله الذي لا إله إلا هو الحي القيوم"
    }[new Random().Next(0, 5)];

    public static void CreateAthkarChannel(Context context, string soundName)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        string channelId = $"{AthkarChannelIdPrefix}{soundName}_v5";

        if (manager.GetNotificationChannel(channelId) == null)
        {
            var channel = new NotificationChannel(channelId, "✨ الأذكار اليومية", NotificationImportance.Max)
            {
                Description = "تذكير روحاني بأذكار الصباح والمساء وما بينهما"
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
                channel.SetVibrationPattern(new long[] { 0, 150, 80, 150 });
            }
            else
            {
                channel.SetSound(null, null);
                channel.EnableVibration(false);
            }

            channel.LockscreenVisibility = NotificationVisibility.Public;
            channel.EnableLights(true);
            channel.LightColor = ColorGold;
            manager.CreateNotificationChannel(channel);
        }
    }

    public static void CreateAdhanChannel(Context context, string soundName)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        string channelId = $"{AdhanChannelIdPrefix}{soundName}_v8";

        if (manager.GetNotificationChannel(channelId) == null)
        {
            int resId = context.Resources!.GetIdentifier(soundName, "raw", context.PackageName);
            if (resId == 0) return;

            var soundUri = global::Android.Net.Uri.Parse($"android.resource://{context.PackageName}/{resId}");
            var audioAttributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Notification)
                .SetContentType(AudioContentType.Sonification)
                .Build();

            var channel = new NotificationChannel(channelId, "🕌 أوقات الصلاة", NotificationImportance.Max)
            {
                Description = "تنبيهات حضور الصلاة في وقتها"
            };
            channel.SetSound(soundUri, audioAttributes);
            channel.EnableVibration(true);
            channel.SetVibrationPattern(new long[] { 0, 300, 100, 300, 100, 300 });
            channel.SetBypassDnd(true);
            channel.LockscreenVisibility = NotificationVisibility.Public;
            channel.EnableLights(true);
            channel.LightColor = ColorGold;
            manager.CreateNotificationChannel(channel);
        }
    }

    public static void ShowAthkarNotification(Context context, int id, string text, string soundName)
    {
        CreateAthkarChannel(context, soundName);
        string channelId = $"{AthkarChannelIdPrefix}{soundName}_v5";

        // تحديد السمة بناءً على الوقت (صباحي / مسائي)
        var now = DateTime.Now;
        bool isMorning = now.Hour >= 5 && now.Hour < 12;
        bool isNight = now.Hour >= 18 || now.Hour < 5;
        
        string themeEmoji = isMorning ? "☀️" : (isNight ? "🌙" : "✨");
        string themeTitle = isMorning ? "أذكار الصباح" : (isNight ? "أذكار المساء" : "تذكير روحاني");
        int themeColor = isMorning ? ColorGold : (isNight ? unchecked((int)0xFF1B263B) : ColorGreen);

        string title = $"{themeEmoji}  {themeTitle} — نديّ بذكر الله";
        string subtext = GetIslamicGreeting();
        
        // جلب السلسلة الحالية (Streak) من الإعدادات
        int streakCount = Microsoft.Maui.Storage.Preferences.Default.Get("athkar_streak_count", 0);
        string streakInfo = streakCount > 0 ? $"🔥 سلسلة الأذكار: {streakCount} يوم" : "";

        var bigTextStyle = new NotificationCompat.BigTextStyle()
            .BigText($"《 {text} 》\n\n‏ {subtext}\n{streakInfo}")
            .SetBigContentTitle(title)
            .SetSummaryText("📿 أذكار المسلم");

        var builder = new NotificationCompat.Builder(context, channelId)
            .SetSmallIcon(GetSafeIcon(context))
            .SetContentTitle(title)
            .SetContentText(text)
            .SetStyle(bigTextStyle)
            .SetColor(themeColor)
            .SetColorized(true)
            .SetPriority(NotificationCompat.PriorityMax)
            .SetCategory(NotificationCompat.CategoryReminder)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetAutoCancel(true)
            .SetShowWhen(true)
            .SetWhen(Java.Lang.JavaSystem.CurrentTimeMillis());

        var largeIcon = GetLargeIcon(context);
        if (largeIcon != null)
            builder.SetLargeIcon(largeIcon);

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
        intent.SetFlags(ActivityFlags.SingleTop);
        var pendingIntent = PendingIntent.GetActivity(context, id, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        builder.SetContentIntent(pendingIntent);

        // أزرار بلمسة جمالية وأيقونات واضحة
        var doneIntent = new Intent(context, typeof(NotificationActionReceiver));
        doneIntent.SetAction("DONE_ATHKAR");
        doneIntent.PutExtra("notification_id", id);
        var donePendingIntent = PendingIntent.GetBroadcast(context, id, doneIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        builder.AddAction(global::Android.Resource.Drawable.IcMenuSend, "💚  تم الذكر", donePendingIntent);

        var snoozeIntent = new Intent(context, typeof(NotificationActionReceiver));
        snoozeIntent.SetAction("SNOOZE_ATHKAR");
        snoozeIntent.PutExtra("notification_id", id);
        snoozeIntent.PutExtra("text", text);
        snoozeIntent.PutExtra("soundName", soundName);
        var snoozePendingIntent = PendingIntent.GetBroadcast(context, id + 500, snoozeIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        builder.AddAction(global::Android.Resource.Drawable.IcMenuRecentHistory, "⏰  بعد 5 دقائق", snoozePendingIntent);

        var manager = NotificationManagerCompat.From(context);
        manager.Notify(id, builder.Build());
    }

    public static void ShowPreAdhanNotification(Context context, int id, string prayerName)
    {
        string soundName = "silent";
        CreateAthkarChannel(context, soundName);
        string channelId = $"{AthkarChannelIdPrefix}{soundName}_v5";

        var bigTextStyle = new NotificationCompat.BigTextStyle()
            .BigText($"استعد لصلاة {prayerName} في سكينة...\n\n«  قُومُوا إِلَى صَلاتِكُمْ يَرحَمكُمُ الله  »")
            .SetBigContentTitle($"⏳  حانت صلاة {prayerName}")
            .SetSummaryText("🕌 نداء الصلاة قريب");

        var builder = new NotificationCompat.Builder(context, channelId)
            .SetSmallIcon(GetSafeIcon(context))
            .SetContentTitle($"⏳  قرب أذان {prayerName}")
            .SetContentText($"تبقت 4 دقائق على صلاة {prayerName}")
            .SetStyle(bigTextStyle)
            .SetColor(ColorAdhan)
            .SetColorized(true)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetCategory(NotificationCompat.CategoryReminder)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetAutoCancel(true)
            .SetShowWhen(true);

        var largeIcon = GetLargeIcon(context);
        if (largeIcon != null)
            builder.SetLargeIcon(largeIcon);

        var intent = context.PackageManager!.GetLaunchIntentForPackage(context.PackageName!)!;
        intent.SetFlags(ActivityFlags.SingleTop);
        var pendingIntent = PendingIntent.GetActivity(context, id, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        builder.SetContentIntent(pendingIntent);

        var manager = NotificationManagerCompat.From(context);
        manager.Notify(id, builder.Build());
    }

    private static Bitmap? GetLargeIcon(Context context)
    {
        try
        {
            int resId = context.Resources!.GetIdentifier("appicon_round", "mipmap", context.PackageName);
            if (resId == 0) resId = context.Resources.GetIdentifier("appicon", "mipmap", context.PackageName);
            if (resId == 0) return null;

            return BitmapFactory.DecodeResource(context.Resources, resId);
        }
        catch { return null; }
    }

    private static int GetSafeIcon(Context context)
    {
        int resId = context.Resources!.GetIdentifier("appicon_round", "mipmap", context.PackageName);
        if (resId == 0) resId = context.Resources.GetIdentifier("appicon", "mipmap", context.PackageName);
        if (resId == 0 && context.ApplicationInfo != null) resId = context.ApplicationInfo.Icon;
        if (resId == 0) resId = global::Android.Resource.Drawable.IcDialogInfo;
        return resId;
    }
}
#endif
