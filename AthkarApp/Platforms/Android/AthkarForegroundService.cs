using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace AthkarApp.Platforms.Android
{
    [Service(Name = "com.Almanar.athkarapp.AthkarForegroundService", Enabled = true, Exported = false)]
    public class AthkarForegroundService : Service
    {
        private const string ChannelId = "athkar_foreground_channel";
        private const int NotificationId = 999;

        public override IBinder? OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();

            // 1. إنشاء القناة فوراً (إلزامي لأندرويد 8 فما فوق)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, "خدمة الأذكار", NotificationImportance.Low);
                var manager = (NotificationManager)GetSystemService(Context.NotificationService)!;
                manager.CreateNotificationChannel(channel);
            }

            // 2. إنشاء الإشعار بأبسط صورة ممكنة وباستخدام مكتبات النظام الأصلية
            Notification? notification = null;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                notification = new Notification.Builder(this, ChannelId)
                    .SetContentTitle("أذكار")
                    .SetContentText("خدمة الأذكار نشطة")
                    .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                    .SetOngoing(true)
                    .Build();
            }
            else
            {
                notification = new Notification.Builder(this)
                    .SetContentTitle("أذكار")
                    .SetContentText("خدمة الأذكار نشطة")
                    .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                    .SetOngoing(true)
                    .Build();
            }

            // 3. استدعاء الوظيفة فوراً
            if (notification != null)
            {
                StartForeground(NotificationId, notification);
            }
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
