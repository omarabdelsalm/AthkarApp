using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using System;
using Microsoft.Maui.Storage;
using AthkarApp;

namespace AthkarApp.Platforms.Android
{
    [BroadcastReceiver(Exported = true, Label = "أوقات الصلاة")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidget_info")]
    public class PrayerWidgetProvider : AppWidgetProvider
    {
        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context == null || appWidgetManager == null || appWidgetIds == null)
                return;

            // استرجاع البيانات المحسوبة مسبقاً من التفضيلات
            string nextPrayerName = Preferences.Default.Get("Widget_NextPrayerName", "الفجر");
            string nextPrayerTime = Preferences.Default.Get("Widget_NextPrayerTime", "--:--");
            
            // حساب العد التنازلي التقريبي برمجياً
            string countdownText = CalculateCountdown(nextPrayerTime);

            foreach (int widgetId in appWidgetIds)
            {
                RemoteViews views = new RemoteViews(context.PackageName, Resource.Layout.prayer_widget);

                views.SetTextViewText(Resource.Id.widget_prayer_name, nextPrayerName);
                views.SetTextViewText(Resource.Id.widget_prayer_time, nextPrayerTime);
                views.SetTextViewText(Resource.Id.widget_countdown, countdownText);

                Intent intent = new Intent(context, typeof(MainActivity));
                PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.widget_title_label, pendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.widget_prayer_name, pendingIntent);

                appWidgetManager.UpdateAppWidget(widgetId, views);
            }
        }

        private string CalculateCountdown(string nextTime)
        {
            try 
            {
                if (DateTime.TryParseExact(nextTime, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var time))
                {
                    DateTime target = DateTime.Today.AddHours(time.Hour).AddMinutes(time.Minute);
                    if (target < DateTime.Now) target = target.AddDays(1);
                    
                    TimeSpan diff = target - DateTime.Now;
                    return $"متبقي: {diff.Hours:D2}:{diff.Minutes:D2}:{diff.Seconds:D2}";
                }
            } catch { }
            return "";
        }
    }
}
