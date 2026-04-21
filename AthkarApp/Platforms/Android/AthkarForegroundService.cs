using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static Android.OS.PowerManager;
using Debug = System.Diagnostics.Debug;

namespace AthkarApp.Services
{
    using AthkarApp.Platforms.Android;
    [Service(Name = "com.Almanar.athkarapp.AthkarForegroundService",
             Enabled = true,
             Exported = false,
             ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
    public class AthkarForegroundService : Service
    {
        private MediaPlayer? _mediaPlayer;
        private AudioManager? _audioManager;
        private PowerManager.WakeLock? _wakeLock;

        private const string ServiceChannelId = "athkar_foreground_channel";
        private const string AdhanChannelId = "athkar_adhan_channel";
        private const int ServiceNotificationId = 999;
        private const int AdhanNotificationId = 888;

        // Guard so we call StartForeground exactly once quickly
        private volatile bool _foregroundStarted;

        // Timing / diagnostics
        private readonly Stopwatch _startupWatch = new Stopwatch();
        private const int ForegroundTimeoutMs = 5000;

        public override IBinder? OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            // Call CreateNotificationChannels first thing!
            try
            {
                CreateNotificationChannels();
            }
            catch { }

            // MUST call StartForeground IMMEDIATELY after startForegroundService is called by the system.
            try
            {
                var notif = GetSimpleNotification();
                StartForegroundSafe(ServiceNotificationId, notif);
                _foregroundStarted = true;
            }
            catch (Exception ex)
            {
                // If it fails here, we are about to crash anyway, but let's try to continue
            }

            base.OnCreate();

            _startupWatch.Restart();

            // If StartForeground didn't happen extremely quickly, log a structured warning shortly after
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ForegroundTimeoutMs).ConfigureAwait(false);
                    
                }
                catch 
                {
                   
                }
            });

            // Move potentially heavier initialization off the main/UI thread so StartForeground is observed promptly
            Task.Run(() =>
            {
                try
                {
                    InitializeComponents();
                }
                catch 
                {
                    
                }
            });
        }

        // Simple notification used immediately
        private Notification GetSimpleNotification()
        {
            return new NotificationCompat.Builder(this, ServiceChannelId)
                .SetContentTitle("أذكار المسلم")
                .SetContentText("جاري التشغيل...")
                .SetSmallIcon(GetSafeIcon())
                .SetPriority(NotificationCompat.PriorityLow)
                .SetOngoing(true)
                .Build();
        }

        private int GetSafeIcon()
        {
            int resId = Resources!.GetIdentifier("appicon_round", "mipmap", PackageName);
            if (resId == 0) resId = Resources.GetIdentifier("appicon", "mipmap", PackageName);
            if (resId == 0 && ApplicationInfo != null) resId = ApplicationInfo.Icon;
            if (resId == 0) resId = global::Android.Resource.Drawable.IcDialogInfo;
            return resId;
        }

        private void StartForegroundSafe(int id, Notification notification)
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q) // Android 10+
                {
                    try
                    {
                        // On Android 14 (API 34), we MUST provide a valid type that is declared in the manifest.
                        StartForeground(id, notification, global::Android.Content.PM.ForegroundService.TypeMediaPlayback);
                    }
                    catch
                    {
                        // Fallback
                        StartForeground(id, notification);
                    }
                }
                else
                {
                    // Android 8.0 - 9.0
                    StartForeground(id, notification);
                }
            }
            catch (Exception ex)
            {
                // On some devices, calling StartForeground can throw if the notification is invalid or permissions are missing.
                System.Diagnostics.Debug.WriteLine($"Failed to start foreground: {ex.Message}");
            }
        }

        private void InitializeComponents()
        {
            try
            {
                // These calls are fast; moved to background to avoid blocking the main thread during startup.
                _audioManager = (AudioManager)GetSystemService(Context.AudioService);

                var powerManager = (PowerManager)GetSystemService(PowerService);
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "AthkarApp:AdhanWakeLock");

                // Heavy resource loads / UI/layout work must NOT run on the service's main thread.
                // If you have large bitmap decoding, database migrations, or other CPU work at app startup,
                // move them to background tasks from your Activity/App initialization code.
            }
            catch 
            {
            }
        }

        private void CreateNotificationChannels()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

            var manager = (NotificationManager)GetSystemService(Context.NotificationService)!;

            var serviceChannel = new NotificationChannel(ServiceChannelId,
                "خدمة الأذكار",
                NotificationImportance.Low);
            serviceChannel.SetSound(null, null);
            manager.CreateNotificationChannel(serviceChannel);

            var adhanChannel = new NotificationChannel(AdhanChannelId,
                "تنبيهات الأذان",
                NotificationImportance.High);
            manager.CreateNotificationChannel(adhanChannel);
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            try
            {
                // If OnCreate didn't get to StartForeground, attempt quickly here
                if (!_foregroundStarted)
                {
                    try
                    {
                        CreateNotificationChannels();
                        StartForegroundSafe(ServiceNotificationId, GetSimpleNotification());
                        _foregroundStarted = true;
                    }
                    catch 
                    {
                    }
                }

                string? action = intent?.Action;

                if (action == "PLAY_ADHAN")
                {
                    int id = intent!.GetIntExtra("id", 0);
                    string soundName = intent.GetStringExtra("soundName") ?? "adhan";
                    string prayerName = intent.GetStringExtra("prayerName") ?? "الصلاة";

                    AcquireWakeLock();
                    UpdateForegroundNotificationForAdhan(prayerName, id);

                    // Prepare and start playback without blocking the main thread.
                    // Use PrepareAsync and the Prepared event to avoid synchronous Prepare().
                    PlayAdhanSoundAsync(soundName);
                }
                else if (action == "STOP_ADHAN")
                {
                    StopAdhanSound();
                    ReleaseWakeLock();
                    
                    // إزالة إشعار الأذان المنفصل والعودة لإشعار الخدمة العادي
                    var manager = (NotificationManager)GetSystemService(Context.NotificationService)!;
                    manager.Cancel(AdhanNotificationId);
                    UpdateDefaultNotification();
                }
                else
                {
                    UpdateDefaultNotification();
                }
            }
            catch (Exception ex)
            {
            }

            return StartCommandResult.Sticky;
        }

        private void AcquireWakeLock()
        {
            try
            {
                if (_wakeLock != null && !_wakeLock.IsHeld)
                {
                    _wakeLock.Acquire(10000);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void ReleaseWakeLock()
        {
            try
            {
                if (_wakeLock != null && _wakeLock.IsHeld)
                {
                    _wakeLock.Release();
                }
            }
            catch { }
        }

        // Non-blocking media start: uses PrepareAsync to avoid blocking the main thread.
        private void PlayAdhanSoundAsync(string soundName)
        {
            Task.Run(() =>
            {
                try
                {
                    if (_audioManager != null)
                    {
                        var result = _audioManager.RequestAudioFocus(null, Android.Media.Stream.Alarm, AudioFocus.Gain);
                        if (result != AudioFocusRequest.Granted)
                        {
                            ReleaseWakeLock();
                            UpdateDefaultNotification();
                            return;
                        }
                    }

                    StopAdhanSound();

                    int resId = Resources!.GetIdentifier(soundName, "raw", PackageName);

                    if (resId == 0)
                    {
                        resId = Resources.GetIdentifier("default_adhan", "raw", PackageName);
                        if (resId == 0)
                        {
                            ReleaseWakeLock();
                            UpdateDefaultNotification();
                            return;
                        }
                    }

                    // Create MediaPlayer and set up events. Prepare asynchronously.
                    _mediaPlayer = new MediaPlayer();
                    _mediaPlayer.SetAudioAttributes(new AudioAttributes.Builder()
                        .SetUsage(AudioUsageKind.Alarm)
                        .SetContentType(AudioContentType.Music)
                        .Build());

                    var soundUri = Android.Net.Uri.Parse($"android.resource://{PackageName}/{resId}");
                    _mediaPlayer.SetDataSource(this, soundUri);

                    // PrepareAsync and start when prepared:
                    _mediaPlayer.Prepared += (s, e) =>
                    {
                        try
                        {
                            _mediaPlayer?.Start();
                        }
                        catch (Exception ex)
                        {
                            ReleaseWakeLock();
                            UpdateDefaultNotification();
                        }
                    };

                    _mediaPlayer.Completion += (s, e) =>
                    {
                        StopAdhanSound();
                        ReleaseWakeLock();
                        UpdateDefaultNotification();
                    };

                    _mediaPlayer.PrepareAsync();
                }
                catch (Exception ex)
                {
                    ReleaseWakeLock();
                    UpdateDefaultNotification();
                }
            });
        }

        private void StopAdhanSound()
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    try
                    {
                        if (_mediaPlayer.IsPlaying)
                            _mediaPlayer.Stop();
                    }
                    catch { }

                    try
                    {
                        _mediaPlayer.Release();
                    }
                    catch { }

                    _mediaPlayer = null;
                }

                if (_audioManager != null)
                {
                    _audioManager.AbandonAudioFocus(null);
                }
            }
            catch { }
        }

        private void UpdateForegroundNotificationForAdhan(string prayerName, int id)
        {
            try
            {
                var flags = PendingIntentFlags.UpdateCurrent;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                    flags |= PendingIntentFlags.Immutable;

                // --- زر «إيقاف» ---
                var stopIntent = new Intent(this, typeof(AthkarForegroundService));
                stopIntent.SetAction("STOP_ADHAN");
                var stopPendingIntent = PendingIntent.GetService(this, id, stopIntent, flags);

                // --- زر «تأجيل» ---
                var snoozeIntent = new Intent(this, typeof(AthkarApp.Platforms.Android.NotificationActionReceiver));
                snoozeIntent.SetAction("SNOOZE_ATHKAR");
                snoozeIntent.PutExtra("notification_id", id);
                snoozeIntent.PutExtra("type", "adhan");
                var snoozePendingIntent = PendingIntent.GetBroadcast(this, id + 600, snoozeIntent, flags);

                // --- Big Text Style مع الآية الكريمة ---
                var bigTextStyle = new NotificationCompat.BigTextStyle()
                    .BigText($"حَيَّ عَلَى الصَّلاةِ ✦ حَيَّ عَلَى الفَلاحِ\n\n« إِنَّ الصَّلاةَ كَانَتْ عَلَى المُؤْمِنِينَ كِتَابًا مَّوقُوتًا »")
                    .SetBigContentTitle($"🕌  نداء الصلاة — {prayerName}")
                    .SetSummaryText("⏰ حان الآن موقت الأذان");

                var builder = new NotificationCompat.Builder(this, AdhanChannelId)
                    .SetContentTitle($"🕌  أذان {prayerName}")
                    .SetContentText("حي على الصلاة ✦ حي على الفلاح")
                    .SetStyle(bigTextStyle)
                    .SetSmallIcon(GetSafeIcon())
                    .SetColor(unchecked((int)0xFF1A3A6B))   // أزرق داكن فخم
                    .SetColorized(true)
                    .SetPriority(NotificationCompat.PriorityMax)
                    .SetCategory(NotificationCompat.CategoryAlarm)
                    .SetVisibility(NotificationCompat.VisibilityPublic)
                    .SetOngoing(true)
                    .SetShowWhen(true)
                    .SetWhen(Java.Lang.JavaSystem.CurrentTimeMillis())
                    .AddAction(global::Android.Resource.Drawable.IcMenuCloseClearCancel, "🔇  إيقاف الصوت", stopPendingIntent)
                    .AddAction(global::Android.Resource.Drawable.IcMenuRecentHistory, "⏰  تأجيل 5 دقائق", snoozePendingIntent);

                var manager = (NotificationManager)GetSystemService(Context.NotificationService)!;
                manager.Notify(AdhanNotificationId, builder.Build());
                
                // نبقي الخدمة في المقدمة بالإشعار البسيط لضمان الاستمرارية
                UpdateDefaultNotification();
            }
            catch (Exception ex)
            {
            }
        }
// Brennan

        private void UpdateDefaultNotification()
        {
            try
            {
                // اختر تسبيحة عشوائية كـ subtext للإشعار الدائم
                var tasbeehList = new[]
                {
                    "سبحان الله وبحمده",
                    "الحمد لله على كل حال",
                    "لا إله إلا الله الحليم الكريم",
                    "سبحان الله العظيم وبحمده",
                    "استغفر الله وأتوب إليه"
                };
                string tasbeeh = tasbeehList[new Random().Next(tasbeehList.Length)];

                var notification = new NotificationCompat.Builder(this, ServiceChannelId)
                    .SetContentTitle("📿  أذكار المسلم")
                    .SetContentText(tasbeeh)
                    .SetSubText("جاري المتابعة في الخلفية")
                    .SetSmallIcon(GetSafeIcon())
                    .SetColor(unchecked((int)0xFF0B5E1B))  // أخضر إسلامي
                    .SetPriority(NotificationCompat.PriorityLow)
                    .SetOngoing(true)
                    .Build();

                StartForegroundSafe(ServiceNotificationId, notification);
                _foregroundStarted = true;
            }
            catch (Exception ex)
            {
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}