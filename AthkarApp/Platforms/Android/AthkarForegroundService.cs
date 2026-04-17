using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Android.Content.PM;
using Android.Media;

namespace AthkarApp.Platforms.Android
{
    [Service(Name = "com.Almanar.athkarapp.AthkarForegroundService", Enabled = true, Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
    public class AthkarForegroundService : Service
    {
        private MediaPlayer? _mediaPlayer;
        private const string ChannelId = "athkar_foreground_channel";
        private const int NotificationId = 999;

        public override IBinder? OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();

            // إنشاء القناة المنخفضة الأولوية للخدمة الخلفية
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var manager = (NotificationManager)GetSystemService(Context.NotificationService)!;
                if (manager.GetNotificationChannel(ChannelId) == null)
                {
                    var channel = new NotificationChannel(ChannelId, "خدمة الأذكار (الخلفية)", NotificationImportance.Low);
                    channel.SetShowBadge(false);
                    manager.CreateNotificationChannel(channel);
                }
            }

            // استدعاء StartForeground فوراً لتجنب الـ Crash
            var initialNotification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("أذكار")
                .SetContentText("خدمة الأذكار نشطة")
                .SetSmallIcon(ApplicationInfo!.Icon)
                .SetPriority(NotificationCompat.PriorityMin)
                .SetOngoing(true)
                .Build();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                StartForeground(NotificationId, initialNotification, ForegroundService.TypeMediaPlayback);
            else
                StartForeground(NotificationId, initialNotification);
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            string? action = intent?.Action;

            if (action == "PLAY_ADHAN")
            {
                int id = intent!.GetIntExtra("id", 0);
                string soundName = intent.GetStringExtra("soundName") ?? "adhan";
                string prayerName = intent.GetStringExtra("prayerName") ?? "الصلاة";

                // أولاً: تحديث الإشعار ليظهر اسم الصلاة وزر الإيقاف
                UpdateForegroundNotificationForAdhan(prayerName, id);

                // ثانياً: تشغيل الصوت بشكل مستقل عبر MediaPlayer مع AudioFocus
                PlayAdhanWithMediaPlayer(soundName);
            }
            else if (action == "STOP_ADHAN")
            {
                StopAdhanSound();
                StopSelf();
            }
            else
            {
                ShowDefaultNotification();
            }

            return StartCommandResult.Sticky;
        }

        private void PlayAdhanWithMediaPlayer(string soundName)
        {
            try
            {
                StopAdhanSound();

                int resId = Resources!.GetIdentifier(soundName, "raw", PackageName);
                if (resId == 0) return;

                _mediaPlayer = MediaPlayer.Create(this, resId);
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.SetAudioAttributes(new AudioAttributes.Builder()
                        .SetUsage(AudioUsageKind.Alarm)
                        .SetContentType(AudioContentType.Music)
                        .Build());
                    _mediaPlayer.Looping = false;
                    _mediaPlayer.Start();
                }
            }
            catch { }
        }

        private void StopAdhanSound()
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    if (_mediaPlayer.IsPlaying)
                        _mediaPlayer.Stop();
                    _mediaPlayer.Release();
                    _mediaPlayer = null;
                }
            }
            catch { }
        }

        private void UpdateForegroundNotificationForAdhan(string prayerName, int id)
        {
            var stopServiceIntent = new Intent(this, typeof(AthkarForegroundService));
            stopServiceIntent.SetAction("STOP_ADHAN");
            var stopPendingIntent = PendingIntent.GetService(
                this, id + 500, stopServiceIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var notification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("🕌 حان الآن وقت أذان " + prayerName)
                .SetContentText("حي على الصلاة.. حي على الفلاح")
                .SetSmallIcon(ApplicationInfo!.Icon)
                .SetPriority(NotificationCompat.PriorityMax)
                .SetCategory(NotificationCompat.CategoryAlarm)
                .SetOngoing(true)
                .SetAutoCancel(false)
                .AddAction(global::Android.Resource.Drawable.IcMenuCloseClearCancel, "إيقاف الأذان", stopPendingIntent)
                .Build();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                StartForeground(NotificationId, notification, ForegroundService.TypeMediaPlayback);
            else
                StartForeground(NotificationId, notification);
        }

        private void ShowDefaultNotification()
        {
            var notification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("تطبيق الأذكار")
                .SetContentText("الخدمة تعمل في الخلفية لضمان التنبيهات")
                .SetSmallIcon(ApplicationInfo!.Icon)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetOngoing(true)
                .Build();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                StartForeground(NotificationId, notification, ForegroundService.TypeMediaPlayback);
            else
                StartForeground(NotificationId, notification);
        }

        public override void OnDestroy()
        {
            StopAdhanSound();
            base.OnDestroy();
        }
    }
}
