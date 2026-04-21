#if ANDROID
using Android.App;
using Android.Content;
using AthkarApp.Services;

namespace AthkarApp.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class NotificationActionReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        string? action = intent.Action;
        int id = intent.GetIntExtra("notification_id", 0);

        if (action == "DONE_ATHKAR")
        {
            // إغلاق الإشعار
            var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
            manager.Cancel(id);
            
            // يمكن هنا إضافة منطق لتحديث الـ Streak برمجياً إذا لزم الأمر
        }
        else if (action == "SNOOZE_ATHKAR")
        {
            var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
            manager.Cancel(id);

            string text = intent.GetStringExtra("text") ?? "";
            string soundName = intent.GetStringExtra("soundName") ?? "om";

            // إعادة جدولة التنبيه بعد 5 دقائق
            var alarmIntent = new Intent(context, typeof(AlarmReceiver));
            alarmIntent.PutExtra("id", id);
            alarmIntent.PutExtra("type", "athkar");
            alarmIntent.PutExtra("text", text);
            alarmIntent.PutExtra("soundName", soundName);

            var pendingIntent = PendingIntent.GetBroadcast(context, id, alarmIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService)!;

            long triggerAt = Java.Lang.JavaSystem.CurrentTimeMillis() + (5 * 60 * 1000);
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAt, pendingIntent);
        }
    }
}
#endif
