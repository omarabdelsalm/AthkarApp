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

            var nextPrayer = GetNextPrayer();
            long baseTime = CalculateChronometerBase(nextPrayer.Time);

            foreach (int widgetId in appWidgetIds)
            {
                RemoteViews views = new RemoteViews(context.PackageName, Resource.Layout.prayer_widget);

                views.SetTextViewText(Resource.Id.widget_prayer_name, nextPrayer.Name);
                views.SetTextViewText(Resource.Id.widget_prayer_time, nextPrayer.Time);
                
                // الاعتماد على Chronometer الخاص بالأندرويد للعد العكسي بدون استهلاك بطارية
                views.SetChronometer(Resource.Id.widget_countdown, baseTime, "متبقي: %s", true);

                Intent intent = new Intent(context, typeof(MainActivity));
                PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.widget_title_label, pendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.widget_prayer_name, pendingIntent);

                appWidgetManager.UpdateAppWidget(widgetId, views);
            }
        }

        private (string Name, string Time) GetNextPrayer()
        {
            var prayers = new (string Name, string Key)[]
            {
                ("الفجر", "PrayerTime_Fajr"),
                ("الظهر", "PrayerTime_Dhuhr"),
                ("العصر", "PrayerTime_Asr"),
                ("المغرب", "PrayerTime_Maghrib"),
                ("العشاء", "PrayerTime_Isha")
            };

            DateTime now = DateTime.Now;
            var parsedTimes = new System.Collections.Generic.List<(string Name, DateTime Time, string TimeStr)>();

            foreach (var p in prayers)
            {
                string timeStr = Preferences.Default.Get(p.Key, "");
                if (!string.IsNullOrEmpty(timeStr))
                {
                    string cleanTime = timeStr.Split(' ')[0].Trim();
                    if (DateTime.TryParseExact(cleanTime, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var time))
                    {
                        var target = DateTime.Today.AddHours(time.Hour).AddMinutes(time.Minute);
                        parsedTimes.Add((p.Name, target, cleanTime));
                    }
                }
            }

            if (parsedTimes.Count == 0)
            {
                return (Preferences.Default.Get("Widget_NextPrayerName", "الفجر"), Preferences.Default.Get("Widget_NextPrayerTime", "--:--"));
            }

            var next = System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OrderBy(parsedTimes, t => t.Time), t => t.Time > now);
            if (next.Name == null)
            {
                next = System.Linq.Enumerable.First(System.Linq.Enumerable.OrderBy(parsedTimes, t => t.Time));
            }

            return (next.Name, next.TimeStr);
        }

        private long CalculateChronometerBase(string nextTime)
        {
            try 
            {
                if (DateTime.TryParseExact(nextTime, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var time))
                {
                    DateTime target = DateTime.Today.AddHours(time.Hour).AddMinutes(time.Minute);
                    if (target < DateTime.Now) target = target.AddDays(1);
                    
                    TimeSpan diff = target - DateTime.Now;
                    
                    // Chronometer uses ElapsedRealtime base.
                    // Using global:: to avoid collision with AthkarApp.Platforms.Android namespace
                    long nowRealtime = global::Android.OS.SystemClock.ElapsedRealtime();
                    return nowRealtime + (long)diff.TotalMilliseconds;
                }
            } catch { }
            return global::Android.OS.SystemClock.ElapsedRealtime();
        }
    }
}
