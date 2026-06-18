using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using System;
using Microsoft.Maui.Storage;

namespace AthkarApp.Platforms.Android
{
    [BroadcastReceiver(Exported = true, Label = "أذكار (الوديجت الشامل)")]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/unified_widget_info")]
    public class UnifiedWidgetProvider : AppWidgetProvider
    {
        private static readonly string[] _athkarList = new[]
        {
            "اللَّهُمَّ إِنِّي أَسْأَلُكَ عِلْمًا نَافِعًا، وَرِزْقًا طَيِّبًا، وَعَمَلًا مُتَقَبَّلًا",
            "سُبْحَانَ اللَّهِ وَبِحَمْدِهِ، سُبْحَانَ اللَّهِ العَظِيمِ",
            "لا حَوْلَ وَلا قُوَّةَ إِلَّا بِاللَّهِ",
            "اللَّهُمَّ صَلِّ وَسَلِّمْ عَلَى نَبِيِّنَا مُحَمَّدٍ",
            "أَسْتَغْفِرُ اللَّهَ العَظِيمَ الَّذِي لاَ إِلَهَ إِلاَّ هُوَ الحَيُّ القَيُّومُ وَأَتُوبُ إِلَيْهِ",
            "رَضِيتُ بِاللَّهِ رَبًّا، وَبِالإِسْلامِ دِينًا، وَبِمُحَمَّدٍ نَبِيًّا",
            "يَا حَيُّ يَا قَيُّومُ بِرَحْمَتِكَ أَسْتَغِيثُ، أَصْلِحْ لِي شَأْنِي كُلَّهُ"
        };

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context == null || appWidgetManager == null || appWidgetIds == null)
                return;

            string selectedThikr = _athkarList[new Random().Next(_athkarList.Length)];
            
            string nextPrayerName = Preferences.Default.Get("Widget_NextPrayerName", "الصلاة القادمة");
            string nextPrayerTime = Preferences.Default.Get("Widget_NextPrayerTime", "--:--");

            foreach (int widgetId in appWidgetIds)
            {
                RemoteViews views = new RemoteViews(context.PackageName, Resource.Layout.unified_widget);

                views.SetTextViewText(Resource.Id.widget_prayer_name, nextPrayerName);
                views.SetTextViewText(Resource.Id.widget_prayer_time, nextPrayerTime);
                views.SetTextViewText(Resource.Id.widget_athkar_content, selectedThikr);

                Intent intent = new Intent(context, typeof(MainActivity));
                PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.widget_athkar_content, pendingIntent);

                appWidgetManager.UpdateAppWidget(widgetId, views);
            }
        }
    }
}
