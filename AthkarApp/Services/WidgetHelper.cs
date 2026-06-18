using System;

namespace AthkarApp.Services
{
    public static class WidgetHelper
    {
        public static void RequestPinUnifiedWidget()
        {
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
                var appWidgetManager = Android.Appwidget.AppWidgetManager.GetInstance(context);
                var myProvider = new Android.Content.ComponentName(context, Java.Lang.Class.FromType(typeof(AthkarApp.Platforms.Android.UnifiedWidgetProvider)));

                if (appWidgetManager != null && appWidgetManager.IsRequestPinAppWidgetSupported)
                {
                    appWidgetManager.RequestPinAppWidget(myProvider, null, null);
                }
            }
#endif
        }

        public static void RequestPinAthkarWidget()
        {
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
                var appWidgetManager = Android.Appwidget.AppWidgetManager.GetInstance(context);
                var myProvider = new Android.Content.ComponentName(context, Java.Lang.Class.FromType(typeof(AthkarApp.Platforms.Android.AthkarWidgetProvider)));

                if (appWidgetManager != null && appWidgetManager.IsRequestPinAppWidgetSupported)
                {
                    appWidgetManager.RequestPinAppWidget(myProvider, null, null);
                }
            }
#endif
        }

        public static void RequestPinPrayerWidget()
        {
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
                var appWidgetManager = Android.Appwidget.AppWidgetManager.GetInstance(context);
                var myProvider = new Android.Content.ComponentName(context, Java.Lang.Class.FromType(typeof(AthkarApp.Platforms.Android.PrayerWidgetProvider)));

                if (appWidgetManager != null && appWidgetManager.IsRequestPinAppWidgetSupported)
                {
                    appWidgetManager.RequestPinAppWidget(myProvider, null, null);
                }
            }
#endif
        }

        public static bool IsPinSupported()
        {
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
                var appWidgetManager = Android.Appwidget.AppWidgetManager.GetInstance(context);
                return appWidgetManager != null && appWidgetManager.IsRequestPinAppWidgetSupported;
            }
#endif
            return false;
        }
    }
}
