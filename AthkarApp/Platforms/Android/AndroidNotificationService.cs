#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AthkarApp.Services;

namespace AthkarApp.Platforms.Android;

public class AndroidNotificationService : IAppNotificationService
{
    private readonly Context _context;

    public AndroidNotificationService()
    {
        _context = global::Android.App.Application.Context;
    }

    public void ScheduleAthkarAlarm(int id, string text, string soundName, DateTime notifyTime)
    {
        ScheduleAlarm(id, "athkar", text, "", soundName, notifyTime);
    }

    public void ScheduleAdhanAlarm(int id, string prayerName, string soundName, DateTime notifyTime)
    {
        ScheduleAlarm(id, "adhan", "", prayerName, soundName, notifyTime);
    }

    private void ScheduleAlarm(int id, string type, string text, string prayerName, string soundName, DateTime notifyTime)
    {
        try
        {
            var intent = new Intent(_context, typeof(AlarmReceiver));
            intent.PutExtra("id", id);
            intent.PutExtra("type", type);
            intent.PutExtra("text", text);
            intent.PutExtra("prayerName", prayerName);
            intent.PutExtra("soundName", soundName);

            var pendingIntent = PendingIntent.GetBroadcast(_context, id, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            var alarmManager = (AlarmManager)_context.GetSystemService(Context.AlarmService)!;

            long triggerAtMillis = new DateTimeOffset(notifyTime).ToUnixTimeMilliseconds();

            // للأرقام الخاصة بالتجربة (Preview)، نستخدم منبه غير دقيق لتجنب طلب صلاحيات إضافية ولضمان عدم خروج التطبيق
            bool isPreview = (id == 9999 || id == 7777);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S && !isPreview)
            {
                if (alarmManager.CanScheduleExactAlarms())
                {
                    alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
                }
                else
                {
                    // Fallback to inexact if permission missing
                    alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
                }
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                // إذا كان تجربة (Preview)، نستخدم Set بدلاً من SetExact لتجنب الـ SecurityException
                if (isPreview)
                    alarmManager.Set(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
                else
                    alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }
            else
            {
                alarmManager.SetExact(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
            }
        }
        catch (Exception ex)
        {
            // منع انهيار التطبيق في حال فشل جدولة المنبه (خاصة في أندرويد 14/15)
            System.Diagnostics.Debug.WriteLine($"Alarm scheduling failed: {ex.Message}");
        }
    }

    public void CancelNotification(int id)
    {
        var intent = new Intent(_context, typeof(AlarmReceiver));
        var pendingIntent = PendingIntent.GetBroadcast(_context, id, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        var alarmManager = (AlarmManager)_context.GetSystemService(Context.AlarmService)!;
        alarmManager.Cancel(pendingIntent);

        var notificationManager = (NotificationManager)_context.GetSystemService(Context.NotificationService)!;
        notificationManager.Cancel(id);
    }

    public void CancelAll()
    {
        // Cancel all potential IDs
        for (int i = 1000; i <= 1024; i++) CancelNotification(i); // Athkar
        for (int i = 2000; i <= 2005; i++) CancelNotification(i); // Adhan
    }
}
#endif
