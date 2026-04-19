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
        private const int NotificationId = 999;

        // Guard so we call StartForeground exactly once quickly
        private volatile bool _foregroundStarted;

        // Timing / diagnostics
        private readonly Stopwatch _startupWatch = new Stopwatch();
        private const int ForegroundTimeoutMs = 5000;

        public override IBinder? OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();

            _startupWatch.Restart();
          //  Debug.WriteLine("AthkarForegroundService: OnCreate start");

            // Best-effort: create channels first so the notification builder does not fail on Android 8+
            try
            {
                CreateNotificationChannels();
                Debug.WriteLine("AthkarForegroundService: Channels created");
            }
            catch 
            {
             // Debug.WriteLine($"AthkarForegroundService: CreateNotificationChannels error (ignored): {ex}");
            }

            // Call StartForeground immediately — MUST happen quickly after startForegroundService
            var notif = GetSimpleNotification();
            StartForegroundSafe(NotificationId, notif);
            _foregroundStarted = true;

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

           // Debug.WriteLine("AthkarForegroundService: OnCreate end");
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
                        StartForeground(id, notification, global::Android.Content.PM.ForegroundService.TypeMediaPlayback);
                    }
                    catch
                    {
                        // Fallback for devices/OS versions where the type fails
                        StartForeground(id, notification);
                    }
                }
                else
                {
                    StartForeground(id, notification);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AthkarForegroundService: CRITICAL StartForeground Error: {ex}");
                // We must surface this instead of failing silently 5 seconds later
                throw; 
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
          //      Debug.WriteLine($"AthkarForegroundService: Initialize Error: {ex}");
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
            System.Diagnostics.Debug.WriteLine($"AthkarForegroundService: OnStartCommand action={intent?.Action} _foregroundStarted={_foregroundStarted} (elapsedMs={_startupWatch.ElapsedMilliseconds})");

            try
            {
                // If OnCreate didn't get to StartForeground, attempt quickly here
                if (!_foregroundStarted)
                {
                    try
                    {
                        CreateNotificationChannels();
                        StartForegroundSafe(NotificationId, GetSimpleNotification());
                        _foregroundStarted = true;
                        //Debug.WriteLine($"AthkarForegroundService: StartForeground called from OnStartCommand (elapsedMs={_startupWatch.ElapsedMilliseconds})");
                    }
                    catch 
                    {
                      //  Debug.WriteLine($"AthkarForegroundService: StartForeground in OnStartCommand failed: {ex}");
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
                    UpdateDefaultNotification();
                }
                else
                {
                    UpdateDefaultNotification();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AthkarForegroundService: OnStartCommand Error: {ex}");
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
                Debug.WriteLine($"AthkarForegroundService: WakeLock Acquire Error: {ex}");
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
                            Debug.WriteLine("AthkarForegroundService: Audio focus not granted");
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
                            Debug.WriteLine($"AthkarForegroundService: MediaPlayer started (elapsedMs={_startupWatch.ElapsedMilliseconds})");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"AthkarForegroundService: Error starting MediaPlayer: {ex}");
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
                    Debug.WriteLine($"AthkarForegroundService: PlayAdhan Error: {ex}");
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
                {
                    flags |= PendingIntentFlags.Immutable;
                }

                var stopIntent = new Intent(this, typeof(AthkarForegroundService));
                stopIntent.SetAction("STOP_ADHAN");
                var stopPendingIntent = PendingIntent.GetService(this, id, stopIntent, flags);

                var notification = new NotificationCompat.Builder(this, AdhanChannelId)
                    .SetContentTitle($"🕌 أذان {prayerName}")
                    .SetContentText("حي على الصلاة.. حي على الفلاح")
                    .SetSmallIcon(GetSafeIcon())
                    .SetPriority(NotificationCompat.PriorityMax)
                    .SetCategory(NotificationCompat.CategoryAlarm)
                    .SetOngoing(true)
                    .AddAction(global::Android.Resource.Drawable.IcMenuCloseClearCancel, "إيقاف", stopPendingIntent)
                    .Build();

                StartForegroundSafe(NotificationId, notification);
                _foregroundStarted = true;
                Debug.WriteLine($"AthkarForegroundService: UpdateForegroundNotificationForAdhan called (elapsedMs={_startupWatch.ElapsedMilliseconds})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AthkarForegroundService: UpdateForeground Error: {ex}");
            }
        }

        private void UpdateDefaultNotification()
        {
            try
            {
                var notification = new NotificationCompat.Builder(this, ServiceChannelId)
                    .SetContentTitle("🕌 تطبيق أذكار المسلم")
                    .SetContentText("الخدمة تعمل في الخلفية")
                    .SetSmallIcon(GetSafeIcon())
                    .SetPriority(NotificationCompat.PriorityLow)
                    .SetOngoing(true)
                    .Build();

                StartForegroundSafe(NotificationId, notification);
                _foregroundStarted = true;
                Debug.WriteLine($"AthkarForegroundService: UpdateDefaultNotification called (elapsedMs={_startupWatch.ElapsedMilliseconds})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AthkarForegroundService: UpdateDefault Error: {ex}");
            }
        }

        public override void OnDestroy()
        {
            System.Diagnostics.Debug.WriteLine("AthkarForegroundService: OnDestroy");
           // StopAdhanSound();
           // ReleaseWakeLock();
            base.OnDestroy();
        }
    }
}