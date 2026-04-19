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

            string nextPrayerName = Preferences.Default.Get("Widget_NextPrayerName", "غير متاح");
            string nextPrayerTime = Preferences.Default.Get("Widget_NextPrayerTime", "--:--");
            string countdownText = Preferences.Default.Get("Widget_Countdown", "");

            foreach (int widgetId in appWidgetIds)
            {
                // تحميل التصميم الخاص بالودجت
                RemoteViews views = new RemoteViews(context.PackageName, Resource.Layout.prayer_widget);

                views.SetTextViewText(Resource.Id.widget_prayer_name, nextPrayerName);
                views.SetTextViewText(Resource.Id.widget_prayer_time, nextPrayerTime);
                views.SetTextViewText(Resource.Id.widget_countdown, countdownText);

                // يمكننا إضافة Intent لفتح التطبيق عند الضغط على الودجت
                Intent intent = new Intent(context, typeof(MainActivity));
                PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.widget_prayer_name, pendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.widget_prayer_time, pendingIntent);

                // تحديث الودجت
                appWidgetManager.UpdateAppWidget(widgetId, views);
            }
        }
    }
}
